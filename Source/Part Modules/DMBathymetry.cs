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
