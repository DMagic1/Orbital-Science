#region license
/* DMagic Orbital Science - Bathymetry
 * Science Module Underwater Experiment
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
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DMagic.Part_Modules
{
	public class DMBathymetry : DMModuleScienceAnimate
	{
		[KSPField]
		public float depthThreshold = 100;
		[KSPField]
		public string redLightName = "redLight";
		[KSPField]
		public string blueLightName = "blueLight";
		[KSPField]
		public string redLightMaterial = "redLightMaterial";
		[KSPField]
		public string blueLightMaterial = "blueLightMaterial";
		[KSPField(isPersistant = true)]
		public bool lightsOn = false;

		private Light redLight;
		private Light blueLight;
		private Material redLightMat;
		private Material blueLightMat;
		private Color redLightColor = new Color(0.8f, 0.082f, 0.082f, 1);
		private Color blueLightColor = new Color(0.596f, 0.890f, 0.933f, 1);
		private Color offColor = new Color();

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);

			Events["turnLightsOn"].unfocusedRange = interactionRange;
			Events["turnLightsOff"].unfocusedRange = interactionRange;

			redLight = part.FindModelComponent<Light>(redLightName);
			blueLight = part.FindModelComponent<Light>(blueLightName);

			Transform redLightT = part.FindModelTransform(redLightMaterial);
			Transform blueLightT = part.FindModelTransform(blueLightMaterial);

			if (redLightT != null && redLightT.renderer != null)
				redLightMat = redLightT.renderer.material;
			if (blueLightT != null && blueLightT.renderer != null)
				blueLightMat = blueLightT.renderer.material;

			if (redLight != null)
				redLight.enabled = false;
			if (blueLight != null)
				blueLight.enabled = false;

			if (lightsOn)
				turnLightsOn();
			else
				turnLightsOff();
		}

		public override string GetInfo()
		{
			string s = base.GetInfo();

			s += "\nDepth Threshold: " + depthThreshold.ToString("N0") + "m";

			return s;
		}

		public override void deployEvent()
		{
			base.deployEvent();

			turnLightsOn();
		}

		public override void retractEvent()
		{
			base.retractEvent();

			turnLightsOff();
		}

		[KSPAction("Toggle Lights", KSPActionGroup.Light)]
		public void toggleLights(KSPActionParam param)
		{
			if (param.type == KSPActionType.Activate)
				turnLightsOn();
			else
				turnLightsOff();
		}

		[KSPAction("Turn Lights On")]
		public void LightOnAction(KSPActionParam param)
		{
			turnLightsOn();
		}

		[KSPAction("Turn Lights Off")]
		public void LightOffAction(KSPActionParam param)
		{
			turnLightsOff();
		}

		[KSPEvent(guiActive = true, guiName = "Turn Lights On", guiActiveEditor = true, guiActiveUnfocused = true, externalToEVAOnly = true, active = true)]
		public void turnLightsOn()
		{
			lightsOn = true;

			Events["turnLightsOn"].active = false;
			Events["turnLightsOff"].active = true;

			StopCoroutine("dimLights");
			StartCoroutine("brightenLights");
		}

		[KSPEvent(guiActive = true, guiName = "Turn Lights Off", guiActiveEditor = true, guiActiveUnfocused = true, externalToEVAOnly = true, active = false)]
		public void turnLightsOff()
		{
			lightsOn = false;

			Events["turnLightsOn"].active = true;
			Events["turnLightsOff"].active = false;

			StopCoroutine("brightenLights");
			StartCoroutine("dimLights");
		}

		private IEnumerator brightenLights()
		{
			int timer = 0;

			if (redLight != null)
				redLight.enabled = true;
			if (blueLight != null)
				blueLight.enabled = true;

			while (timer < 30)
			{
				timer++;

				if (blueLight != null && blueLight.intensity <= 1)
					blueLight.intensity += 0.0333f;
				if (redLight != null && redLight.intensity <= 1.5)
					redLight.intensity += 0.05f;

				setEmissive(redLightMat, redLightColor);
				setEmissive(blueLightMat, blueLightColor);
				yield return null;
			}
		}

		private IEnumerator dimLights()
		{
			int timer = 0;

			while (timer < 30)
			{
				timer++;

				if (blueLight != null && blueLight.intensity > 0)
					blueLight.intensity -= 0.0333f;
				if (redLight != null && redLight.intensity > 0)
					redLight.intensity -= 0.05f;

				setEmissive(redLightMat, offColor);
				setEmissive(blueLightMat, offColor);
				yield return null;
			}

			if (redLight != null)
				redLight.enabled = false;
			if (blueLight != null)
				blueLight.enabled = false;
		}

		private void setEmissive(Material m, Color c)
		{
			if (m == null)
				return;

			Color old = m.GetColor("_EmissiveColor");

			Color target = Color.Lerp(old, c, TimeWarp.deltaTime);

			m.SetColor("_EmissiveColor", target);
		}

		protected override ExperimentSituations getSituation()
		{
			switch (vessel.situation)
			{
				case Vessel.Situations.LANDED:
				case Vessel.Situations.SPLASHED:
				case Vessel.Situations.PRELAUNCH:
				case Vessel.Situations.SUB_ORBITAL:
				case Vessel.Situations.FLYING:
					if (vessel.mainBody.ocean && part.WaterContact && part.submergedPortion >= 0.95)
						return ExperimentSituations.SrfSplashed;
					else
						return ExperimentSituations.InSpaceHigh;
				default:
					return ExperimentSituations.InSpaceHigh;
			}
		}

		protected override string getBiome(ExperimentSituations s)
		{
			if ((bioMask & (int)s) == 0)
				return "";
			else
			{
				string depth = "";

				if (part.depth < depthThreshold)
					depth = "Shallow";
				else
					depth = "Deep";

				if (vessel.mainBody.BiomeMap != null)
					return vessel.mainBody.BiomeMap.GetAtt(vessel.latitude * Mathf.Deg2Rad, vessel.longitude * Mathf.Deg2Rad).name + depth;
				else
					return depth;
			}
		}

		protected override float fixSubjectValue(ExperimentSituations s, float f, float boost, CelestialBody body)
		{
			if (part.depth >= depthThreshold)
				boost = 2f;

			return base.fixSubjectValue(s, f, boost, body);
		}

		protected override string situationCleanup(ExperimentSituations expSit, string b)
		{
			if (b.EndsWith("Shallow"))
			{
				b = b.Remove(b.LastIndexOf("Shallow"));
				return string.Format(" from the shallows of {0}'s {1}", vessel.mainBody.theName, b);
			}
			else if (b.EndsWith("Deep"))
			{
				b = b.Remove(b.LastIndexOf("Deep"));
				return string.Format(" from deep in {0}'s {1}", vessel.mainBody.theName, b);
			}
			else
				return string.Format("In {0}'s {1}", vessel.mainBody.theName, b);
		}
	}
}
