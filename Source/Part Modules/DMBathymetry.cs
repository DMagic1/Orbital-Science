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

		private Light redLight;
		private Light blueLight;

		public override void OnStart(PartModule.StartState state)
		{
			redLight = part.FindModelComponent<Light>(redLightName);
			blueLight = part.FindModelComponent<Light>(blueLightName);

			base.OnStart(state);
		}

		public override void deployEvent()
		{
			base.deployEvent();

			if (redLight != null)
				redLight.enabled = true;
			if (blueLight != null)
				blueLight.enabled = true;
		}

		public override void retractEvent()
		{
			base.retractEvent();

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
					if (part.WaterContact && part.submergedPortion >= 0.95)
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
