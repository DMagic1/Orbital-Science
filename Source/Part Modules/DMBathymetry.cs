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

		protected override ExperimentSituations getSituation()
		{
			switch (vessel.situation)
			{
				case Vessel.Situations.LANDED:
				case Vessel.Situations.SPLASHED:
				case Vessel.Situations.PRELAUNCH:
				case Vessel.Situations.SUB_ORBITAL:
				case Vessel.Situations.FLYING:
					if (part.WaterContact && part.partBuoyancy.submergedPortion >= 0.95)
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

		protected override string situationCleanup(ExperimentSituations expSit, string b)
		{
			if (b.EndsWith("Shallow"))
			{
				b = b.Remove(b.LastIndexOf("Shallow"));
				return string.Format("In the shallows of {0}'s {1}", vessel.mainBody.theName, b);
			}
			else if (b.EndsWith("Deep"))
			{
				b = b.Remove(b.LastIndexOf("Deep"));
				return string.Format("Deep in {0}'s {1}", vessel.mainBody.theName, b);
			}
			else
				return string.Format("In {0}'s {1}", vessel.mainBody.theName, b);
		}
	}
}
