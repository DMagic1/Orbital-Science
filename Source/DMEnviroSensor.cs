#region license
/* DMagic Orbital Science - DMEnviroSensor
 * Module to control spiffy animations for replacement stock science parts
 *
 * Copyright (c) 2014, David Grandy <david.grandy@gmail.com>
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without modification, 
 * are permitted provided that the following conditions are met:
 * 
 * 1. Redistributions of source code must retain the above copyright notice, 
 * this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright notice, 
 * this list of conditions and the following disclaimer in the documentation and/or other materials 
 * provided with the distribution.
 * 
 * 3. Neither the name of the copyright holder nor the names of its contributors may be used 
 * to endorse or promote products derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE 
 * GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF 
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT 
 * OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *  
 */
#endregion

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DMagic
{
	internal class DMEnviroSensor : ModuleEnviroSensor
	{
		[KSPField]
		public float min = 0f;
		[KSPField]
		public float max = 1f;
		[KSPField]
		public bool primary = false;

		private Transform indicator = null;
		private Transform rotor1 = null;
		private Transform rotor2 = null;
		private Transform rotor3 = null;
		private Transform tilt1 = null;
		private Vector3 indicatorPosition = new Vector3(0, 0, 0);
		private int sensorInt = 0;
		private List<DMEnviroSensor> modList = new List<DMEnviroSensor>();
		private float timeDelay = 0f;
		private float lastValue = 0f;

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);
			if (HighLogic.LoadedSceneIsFlight) {
				sensorInt = sensorTypeInt(sensorType);
				Fields["readoutInfo"].guiName = guiReadout(sensorInt);
				//Assign transforms for all of the indicator needles, etc.. for each part type
				if (sensorInt == 1 || sensorInt == 2 || sensorInt == 3)
					indicator = part.FindModelTransform(sensorType);
				if (sensorInt == 3)
					indicatorPosition = indicator.localPosition;
				if (sensorInt == 4) {
					rotor1 = part.FindModelTransform(sensorType + "_000");
					rotor2 = part.FindModelTransform(sensorType + "_001");
					rotor3 = part.FindModelTransform(sensorType + "_002");
					tilt1 = part.FindModelTransform(sensorType + "_003");
				}
				//Prevent multiple modules from interfering with each other
				if (primary) {
					modList = this.part.FindModulesImplementing<DMEnviroSensor>();
					if (modList.Count > 1) {
						modList[0].Events["toggleSensor"].active = true;
						modList[1].Events["toggleSensor"].active = false;
					}
				}
			}
		}

		public override string GetInfo()
		{
			string info = base.GetInfo();
			if (primary)	
				info += "Requires:\n- " + resourceName + ": " + powerConsumption.ToString() + "/s\n";
			return info;
		}

		[KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = true, guiActive = false, guiName = "Activate", active = false)]
		public void toggleSensor()
		{
			foreach (DMEnviroSensor DMES in modList) {
				DMES.sensorActive= !DMES.sensorActive;
				if (DMES.primary) {
					if (DMES.sensorActive)
						Events["toggleSensor"].guiName = "Deactivate";
					else
						Events["toggleSensor"].guiName = "Activate";
				}
			}
		}

		 private int sensorTypeInt(string type)
		{
			switch (type) {
				case "PRES":
					return 1;
				case "GRAV":
					return 2;
				case "TEMP":
					return 3;
				case "ACC":
					return 4;
				default:
					return 0;
			}
		}

		public void Update()
		{
			animateIndicator();
		}

		private void animateIndicator()
		{
			if (indicator != null || rotor1 != null) {
				float maxSensorValue = 0f;
				float currentSensorValue = 0f;
				float normSensorValue = 0f;
				if (sensorActive) {
					if (timeDelay <= 1f)
						timeDelay += TimeWarp.deltaTime;
					maxSensorValue = sensorValue(sensorInt);
					currentSensorValue = parseSensor();
					if (sensorInt == 3)
						currentSensorValue += 429; //For negative temp values
					normSensorValue = Mathf.Clamp(currentSensorValue / maxSensorValue, 0f, 1f);
					normSensorValue *= timeDelay;
					lastValue = normSensorValue;
				}
				else {
					if (timeDelay >= 0f)
						timeDelay -= TimeWarp.deltaTime;
					normSensorValue = lastValue * timeDelay;
				}
				if (sensorInt == 1 || sensorInt == 2)
					indicator.localRotation = Quaternion.Euler(Mathf.Lerp(min, max, normSensorValue), 0f, 0f);
				if (sensorInt == 3)
					indicator.localPosition = Vector3.MoveTowards(indicator.localPosition, indicatorPosition + new Vector3(0f, 0f, 0.12f * normSensorValue), Time.deltaTime);
				if (sensorInt == 4) {
					rotor1.Rotate(1000 * TimeWarp.deltaTime, 0f, 0f);
					rotor2.Rotate(0f, 0f, 3000 * normSensorValue * TimeWarp.deltaTime);
					rotor3.Rotate(0f, 3000 * normSensorValue * TimeWarp.deltaTime, 0f);
					tilt1.localRotation = Quaternion.Euler(0f, Mathf.Lerp(min, max, normSensorValue), 0f);
				}
			}
		}

		//Pull the readings out of the string from the base module
		private float parseSensor()
		{
			float parseValue = 0f;
			if (float.TryParse(readoutInfo, out parseValue))
				return parseValue;
			else {
				string a = "";
				for (int i = 0; i < readoutInfo.Length; i++) {
					if (Char.IsDigit(readoutInfo[i]) || readoutInfo[i] == '.' || readoutInfo[i] == '-')
						a += readoutInfo[i];
				}
				if (float.TryParse(a, out parseValue))
					return parseValue;
			}
			return parseValue;
		}

		//Some imperically determined max values for each sensor
		private float sensorValue(int type)
		{
			switch (type) {
				case 1:
					return 3;
				case 2:
					return 30;
				case 3:
					return 1500;
				case 4:
					return 10;
				default:
					return 1;
			}
		}

		private string guiReadout(int type)
		{
			switch (type) {
				case 1:
					return "PRES";
				case 2:
					return "GRAV";
				case 3:
					return "TEMP";
				case 4:
					return "ACC";
				default:
					return "Display";
			}
		}

	}
}
