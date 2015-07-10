﻿#region license
/* DMagic Orbital Science - DMUtils
 * Static utilities class for various methods
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
using System.Linq;
using UnityEngine;
using Contracts;
using DMagic.Contracts;
using DMagic.Parameters;

namespace DMagic
{
	internal static class DMUtils
	{
		internal static System.Random rand;
		internal static Dictionary<string, Dictionary<string, DMScienceContainer>> availableScience;
		internal static Dictionary<string, List<string>> backStory;
		internal static float science = 1f;
		internal static float reward = 1f;
		internal static float forward = 1f;
		internal static float penalty = 1f;
		internal static float deadline = 1f;
		internal static int maxSurveyOffered = 2;
		internal static int maxSurveyActive = 4;
		internal static int maxAsteroidOffered = 1;
		internal static int maxAsteroidActive = 3;
		internal static int maxAnomalyOffered = 1;
		internal static int maxAnomalyActive = 3;
		internal static int maxMagneticOffered = 2;
		internal static int maxMagneticActive = 4;
		internal static string version = "v0.9.2";
		internal static EventData<CelestialBody, String, String> OnAnomalyScience;
		internal static EventData<String, String> OnAsteroidScience;
		internal static bool whiteListed = false;

		internal static void Logging(string s, params object[] stringObjects)
		{
			s = string.Format(s, stringObjects);
			string finalLog = string.Format("[DM] {0}", s);
			Debug.Log(finalLog);
		}

		internal static float fixSubjectVal(ExperimentSituations s, float f, CelestialBody body)
		{
			float subV = f;
			if (s == ExperimentSituations.SrfLanded) subV = body.scienceValues.LandedDataValue;
			else if (s == ExperimentSituations.SrfSplashed) subV = body.scienceValues.SplashedDataValue;
			else if (s == ExperimentSituations.FlyingLow) subV = body.scienceValues.FlyingLowDataValue;
			else if (s == ExperimentSituations.FlyingHigh) subV = body.scienceValues.FlyingHighDataValue;
			else if (s == ExperimentSituations.InSpaceLow) subV = body.scienceValues.InSpaceLowDataValue;
			else if (s == ExperimentSituations.InSpaceHigh) subV = body.scienceValues.InSpaceHighDataValue;
			return subV;
		}

		internal static Vessel.Situations convertSit(ExperimentSituations s)
		{
			switch (s)
			{
				case ExperimentSituations.SrfLanded:
					return Vessel.Situations.LANDED;
				case ExperimentSituations.SrfSplashed:
					return Vessel.Situations.SPLASHED;
				case ExperimentSituations.FlyingLow:
					return Vessel.Situations.FLYING;
				case ExperimentSituations.FlyingHigh:
					return Vessel.Situations.SUB_ORBITAL;
				case ExperimentSituations.InSpaceLow:
					return Vessel.Situations.ORBITING;
				default:
					return Vessel.Situations.ESCAPING;
			}
		}

		internal static float asteroidSubjectVal(float f, int i)
		{
			switch (i)
			{
				case 0:
					return 2f;
				case 1:
					return 3f;
				case 2:
					return 5f;
				case 3:
					return 7f;
				case 4:
					return 9f;
				default:
					return 1f;
			}
		}

		internal static string sizeHash(int i)
		{
			switch (i)
			{
				case 0:
					return "Class A";
				case 1:
					return "Class B";
				case 2:
					return "Class C";
				case 3:
					return "Class D";
				case 4:
					return "Class E";
				default:
					return "Class Unholy";
			}
		}

		internal static double timeInDays(double D)
		{
			if (GameSettings.KERBIN_TIME)
				D /= KSPUtil.KerbinDay;
			else
				D /= KSPUtil.EarthDay;
			return D;
		}

		#region Debug Logging

		[System.Diagnostics.Conditional("DEBUG")]
		internal static void DebugLog(string s, params object[] stringObjects)
		{
			Logging(s, stringObjects);
		}

		#endregion

		#region Random parameters

		//Select target celestial body based on contract prestige
		internal static CelestialBody nextTargetBody(Contract.ContractPrestige c, List<CelestialBody> cR, List<CelestialBody> cUR)
		{
			//Select Kerbin system for trivial
			if (c == Contract.ContractPrestige.Trivial)
				return FlightGlobals.Bodies[rand.Next(1, 4)];
			//Select already visited planets for significant, add Mun and Minmus if they aren't already present
			else if (c == Contract.ContractPrestige.Significant)
			{
				if (!cR.Contains(FlightGlobals.Bodies[2]))
					cR.Add(FlightGlobals.Bodies[2]);
				if (!cR.Contains(FlightGlobals.Bodies[3]))
					cR.Add(FlightGlobals.Bodies[3]);
				if (cR.Count == 0)
					return null;
				return cR[rand.Next(0, cR.Count)];
			}
			//Select unreached body; remove Kerbin system, return already reached bodies if all have been visited
			else if (c == Contract.ContractPrestige.Exceptional)
			{
				if (cUR.Count == 0)
					cUR = cR;
				if (cUR.Contains(FlightGlobals.Bodies[1]))
					cUR.Remove(FlightGlobals.Bodies[1]);
				if (cUR.Contains(FlightGlobals.Bodies[2]))
					cUR.Remove(FlightGlobals.Bodies[2]);
				if (cUR.Contains(FlightGlobals.Bodies[3]))
					cUR.Remove(FlightGlobals.Bodies[3]);
				return cUR[rand.Next(0, cUR.Count)];
			}
			return null;
		}

		//Return a list of valid experiment situations based on the experiment parameters
		internal static List<ExperimentSituations> availableSituations(ScienceExperiment exp, int i, CelestialBody b)
		{
			List<ExperimentSituations> expSitList = new List<ExperimentSituations>();
			if (((ExperimentSituations)i & ExperimentSituations.FlyingHigh) == ExperimentSituations.FlyingHigh && b.atmosphere)
				expSitList.Add(ExperimentSituations.FlyingHigh);
			if (((ExperimentSituations)i & ExperimentSituations.FlyingLow) == ExperimentSituations.FlyingLow && b.atmosphere)
				expSitList.Add(ExperimentSituations.FlyingLow);
			if (((ExperimentSituations)i & ExperimentSituations.InSpaceHigh) == ExperimentSituations.InSpaceHigh)
			{
				if (!exp.requireAtmosphere)
					expSitList.Add(ExperimentSituations.InSpaceHigh);
				else if (b.atmosphere)
					expSitList.Add(ExperimentSituations.InSpaceHigh);
			}
			if (((ExperimentSituations)i & ExperimentSituations.InSpaceLow) == ExperimentSituations.InSpaceLow)
			{
				if (!exp.requireAtmosphere)
					expSitList.Add(ExperimentSituations.InSpaceLow);
				else if (b.atmosphere)
					expSitList.Add(ExperimentSituations.InSpaceLow);
			}
			if (((ExperimentSituations)i & ExperimentSituations.SrfLanded) == ExperimentSituations.SrfLanded && b.pqsController != null)
			{
				if (!exp.requireAtmosphere && exp.id != "dmbiodrillscan")
					expSitList.Add(ExperimentSituations.SrfLanded);
				else if (b.atmosphere)
					expSitList.Add(ExperimentSituations.SrfLanded);
			}
			if (((ExperimentSituations)i & ExperimentSituations.SrfSplashed) == ExperimentSituations.SrfSplashed && b.ocean && b.pqsController != null)
			{
				if (!exp.requireAtmosphere)
					expSitList.Add(ExperimentSituations.SrfSplashed);
				else if (b.atmosphere)
					expSitList.Add(ExperimentSituations.SrfSplashed);
			}
			return expSitList;
		}

		internal static List<ExperimentSituations> availableSituationsLimited(ScienceExperiment exp, int i, CelestialBody b)
		{
			List<ExperimentSituations> expSitList = new List<ExperimentSituations>();
			if (((ExperimentSituations)i & ExperimentSituations.FlyingLow) == ExperimentSituations.FlyingLow && b.atmosphere)
				expSitList.Add(ExperimentSituations.FlyingLow);
			if (((ExperimentSituations)i & ExperimentSituations.InSpaceLow) == ExperimentSituations.InSpaceLow)
			{
				if (!exp.requireAtmosphere)
					expSitList.Add(ExperimentSituations.InSpaceLow);
				else if (b.atmosphere)
					expSitList.Add(ExperimentSituations.InSpaceLow);
			}
			if (((ExperimentSituations)i & ExperimentSituations.SrfLanded) == ExperimentSituations.SrfLanded && b.pqsController != null)
			{
				if (!exp.requireAtmosphere && exp.id != "dmbiodrillscan")
					expSitList.Add(ExperimentSituations.SrfLanded);
				else if (b.atmosphere)
					expSitList.Add(ExperimentSituations.SrfLanded);
			}
			return expSitList;
		}

		internal static bool biomeRelevant(ExperimentSituations s, int i)
		{
			if ((i & (int)s) == 0)
				return false;
			else
				return true;
		}

		internal static List<string> fetchBiome(CelestialBody b, ScienceExperiment exp, ExperimentSituations sit)
		{
			List<string> s = new List<string>();
			if (b.BiomeMap == null)
			{
				s.Add("");
				return s;
			}
			else
			{
				for (int j = 0; j < b.BiomeMap.Attributes.Length; j++)
				{
					string bName = b.BiomeMap.Attributes[j].name;
					string subId = string.Format("{0}@{1}{2}{3}", exp.id, b.name, sit, bName.Replace(" ", ""));

					if (ResearchAndDevelopment.GetSubjects().Any(a => a.id == subId))
					{
						ScienceSubject subB = ResearchAndDevelopment.GetSubjectByID(subId);
						if (subB.scientificValue > 0.5f)
							s.Add(bName);
					}
					else
						s.Add(bName);
				}
			}
			return s;
		}

		internal static List<string> fetchBiome(CelestialBody b)
		{
			List<string> s = new List<string>();
			if (b.BiomeMap == null)
			{
				s.Add("");
				return s;
			}
			else
			{
				for (int j = 0; j < b.BiomeMap.Attributes.Length; j++)
				{
					string bName = b.BiomeMap.Attributes[j].name;
					s.Add(bName);
				}
			}
			return s;
		}

#endregion

	}

}
