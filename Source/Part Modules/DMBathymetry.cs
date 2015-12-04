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
		[KSPField(isPersistant = true)]
		public bool lightsOn = false;

		private Light redLight;
		private Light blueLight;

		public override void OnStart(PartModule.StartState state)
		{
			redLight = part.FindModelComponent<Light>(redLightName);
			blueLight = part.FindModelComponent<Light>(blueLightName);

			base.OnStart(state);

			if (lightsOn)
				turnLightsOn();

			Events["turnLightsOn"].unfocusedRange = interactionRange;
			Events["turnLightsOff"].unfocusedRange = interactionRange;
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
		public void toggleLights()
		{
			if (lightsOn)
				turnLightsOff();
			else
				turnLightsOn();
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

			if (redLight != null)
				redLight.enabled = true;
			if (blueLight != null)
				blueLight.enabled = true;
		}

		[KSPEvent(guiActive = true, guiName = "Turn Lights Off", guiActiveEditor = true, guiActiveUnfocused = true, externalToEVAOnly = true, active = false)]
		public void turnLightsOff()
		{
			lightsOn = false;

			Events["turnLightsOn"].active = true;
			Events["turnLightsOff"].active = false;

			if (redLight != null)
				redLight.enabled = false;
			if (blueLight != null)
				blueLight.enabled = false;
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
