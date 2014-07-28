#region license
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

namespace DMagic
{
	static class DMUtils
	{
		internal static System.Random rand;
		internal static Dictionary<string, DMScienceContainer> availableScience;
		internal static Dictionary<string, DMScienceContainer> surfaceScience;
		internal static Dictionary<string, DMScienceContainer> atmosphericScience;
		internal static Dictionary<string, DMScienceContainer> orbitalScience;
		internal static float science, reward, forward, penalty;
		internal static List<string> storyList;

		internal static void Logging(string s, params object[] stringObjects)
		{
			s = string.Format(s, stringObjects);
			string finalLog = string.Format("[DM] {0}", s);
			Debug.Log(finalLog);
		}

		#region Debug Logging

		[System.Diagnostics.Conditional("DEBUG")]
		internal static void DebugLog(string s, params object[] stringObjects)
		{
			Logging(s, stringObjects);
		}

		#endregion

		#region Random parameters

		internal static CelestialBody nextTargetBody(Contract.ContractPrestige c, List<CelestialBody> cR, List<CelestialBody> cUR)
		{
			DMUtils.DebugLog("Searching For Acceptable Body");
			if (c == Contract.ContractPrestige.Trivial)
				return FlightGlobals.Bodies[rand.Next(1, 4)];
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
			else if (c == Contract.ContractPrestige.Exceptional)
			{
				if (cUR.Contains(FlightGlobals.Bodies[1]))
					cUR.Remove(FlightGlobals.Bodies[1]);
				if (cUR.Contains(FlightGlobals.Bodies[2]))
					cUR.Remove(FlightGlobals.Bodies[2]);
				if (cUR.Contains(FlightGlobals.Bodies[3]))
					cUR.Remove(FlightGlobals.Bodies[3]);
				if (cUR.Count == 0)
					cUR = cR;
				return cUR[rand.Next(0, cUR.Count)];
			}
			return null;
		}

		internal static List<ExperimentSituations> availableSituations(ScienceExperiment exp, int i, CelestialBody b)
		{
			DMUtils.DebugLog("Finding Situations");
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
				if (!exp.requireAtmosphere)
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
			DMUtils.DebugLog("Found {0} Valid Experimental Situations", expSitList.Count);
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
			DMUtils.DebugLog("Searching For Biomes");
			List<string> s = new List<string>();
			if (b.BiomeMap == null || b.BiomeMap.Map == null)
			{
				DMUtils.DebugLog("No Biomes Present For Target Planet");
				s.Add("");
				return s;
			}
			else
			{
				for (int j = 0; j < b.BiomeMap.Attributes.Length; j++)
				{
					string bName = b.BiomeMap.Attributes[j].name;
					ScienceSubject subB = ResearchAndDevelopment.GetSubjectByID(string.Format("{0}@{1}{2}{3}", exp.id, b.name, sit, bName.Replace(" ", "")));
					if (subB == null)
					{
						s.Add(bName);
						continue;
					}
					else
					{
						if (subB.scientificValue > 0.4f)
							s.Add(bName);
					}
				}
			}
			DMUtils.DebugLog("Found Acceptable Biomes");
			return s;
		}

#endregion

	}

	#region Paramater Generator

	static class DMCollectContractGenerator
	{
		private static System.Random rand = DMUtils.rand;

		//Generate fully random science experiment contract parameter
		internal static DMCollectScience fetchScienceContract(Contract.ContractPrestige p, List<CelestialBody> cR, List<CelestialBody> cUR)
		{
			DMScienceContainer scienceContainer;
			CelestialBody body;
			ExperimentSituations targetSituation;
			List<ExperimentSituations> situations;
			ScienceExperiment exp;
			ScienceSubject sub;
			AvailablePart aPart;
			string name;
			string biome = "";

			//Choose a random science experiment from our list generated at startup
			scienceContainer = DMUtils.availableScience.ElementAt(rand.Next(0, DMUtils.availableScience.Count)).Value;
			name = DMUtils.availableScience.FirstOrDefault(n => n.Value == scienceContainer).Key;
			DMUtils.DebugLog("Checking Contract Requirements");

			//Determine if the science part is available if applicable
			if (scienceContainer.sciPart != "None")
			{
				DMUtils.DebugLog("Checking For Part {0} Now", scienceContainer.sciPart);
				aPart = PartLoader.getPartInfoByName(scienceContainer.sciPart);
				if (aPart == null)
					return null;
				if (!ResearchAndDevelopment.PartModelPurchased(aPart))
					return null;
				DMUtils.DebugLog("Part: [{0}] Purchased; Contract Meets Requirements", aPart.name);
			}

			//Select a random Celestial Body based on contract prestige levels
			body = DMUtils.nextTargetBody(p, cR, cUR);
			if (body == null)
				return null;

			//Make sure our experiment is OK
			exp = scienceContainer.exp;
			if (exp == null)
				return null;

			//Choose an acceptable experimental situation for a given science experiment
			if ((situations = DMUtils.availableSituations(exp, scienceContainer.sitMask, body)).Count == 0)
				return null;
			else
			{
				DMUtils.DebugLog("Acceptable Situations Found");
				targetSituation = situations[rand.Next(0, situations.Count)];
				DMUtils.DebugLog("Experimental Situation: {0}", targetSituation);
			}

			//Build a list of acceptable biomes if applicable, choose one with remaining science
			if (DMUtils.biomeRelevant(targetSituation, scienceContainer.bioMask))
			{
				DMUtils.DebugLog("Checking For Biome Usage");
				List<string> bList = DMUtils.fetchBiome(body, exp, targetSituation);
				if (bList.Count == 0)
				{
					DMUtils.DebugLog("Planet All Tapped Out; No Remaining Science Here");
					return null;
				}
				int i = rand.Next(0, 2);
				if (i == 0)
				{
					biome = bList[rand.Next(0, bList.Count)];
					DMUtils.DebugLog("Acceptable Biome Found: {0}", biome);
				}
			}

			//Make sure that our chosen science subject has science remaining to be gathered
			if ((sub = ResearchAndDevelopment.GetSubjectByID(string.Format("{0}@{1}{2}{3}", exp.id, body.name, targetSituation, biome.Replace(" ", "")))) != null)
			{
				if (sub.scientificValue < 0.4f)
					return null;
			}

			return new DMCollectScience(body, targetSituation, biome, name, 0);
		}

		//Generate random experiment for a given celestial body
		internal static DMCollectScience fetchScienceContract(CelestialBody body)
		{
			DMScienceContainer scienceContainer;
			ExperimentSituations targetSituation;
			List<ExperimentSituations> situations;
			ScienceExperiment exp;
			ScienceSubject sub;
			AvailablePart aPart;
			string name;
			string biome = "";

			//Choose a random science experiment from our list generated at startup
			scienceContainer = DMUtils.availableScience.ElementAt(rand.Next(0, DMUtils.availableScience.Count)).Value;
			name = DMUtils.availableScience.FirstOrDefault(n => n.Value == scienceContainer).Key;
			DMUtils.DebugLog("Checking Contract Requirements");

			//Determine if the science part is available if applicable
			if (scienceContainer.sciPart != "None")
			{
				DMUtils.DebugLog("Checking For Part {0} Now", scienceContainer.sciPart);
				aPart = PartLoader.getPartInfoByName(scienceContainer.sciPart);
				if (aPart == null)
					return null;
				if (!ResearchAndDevelopment.PartModelPurchased(aPart))
					return null;
				DMUtils.DebugLog("Part: [{0}] Purchased; Contract Meets Requirements", aPart.name);
			}

			//Make sure our experiment is OK
			exp = scienceContainer.exp;
			if (exp == null)
				return null;

			//Choose an acceptable experimental situation for a given science experiment
			if ((situations = DMUtils.availableSituations(exp, scienceContainer.sitMask, body)).Count == 0)
				return null;
			else
			{
				DMUtils.DebugLog("Acceptable Situations Found");
				targetSituation = situations[rand.Next(0, situations.Count)];
				DMUtils.DebugLog("Experimental Situation: {0}", targetSituation);
			}

			//Build a list of acceptable biomes if applicable, choose one with remaining science
			if (DMUtils.biomeRelevant(targetSituation, scienceContainer.bioMask))
			{
				DMUtils.DebugLog("Checking For Biome Usage");
				List<string> bList = DMUtils.fetchBiome(body, exp, targetSituation);
				if (bList.Count == 0)
				{
					DMUtils.DebugLog("Planet All Tapped Out; No Remaining Science Here");
					return null;
				}
				int i = rand.Next(0, 2);
				if (i == 0)
				{
					biome = bList[rand.Next(0, bList.Count)];
					DMUtils.DebugLog("Acceptable Biome Found: {0}", biome);
				}
			}

			//Make sure that our chosen science subject has science remaining to be gathered
			if ((sub = ResearchAndDevelopment.GetSubjectByID(string.Format("{0}@{1}{2}{3}", exp.id, body.name, targetSituation, biome.Replace(" ", "")))) != null)
			{
				if (sub.scientificValue < 0.4f)
					return null;
			}

			return new DMCollectScience(body, targetSituation, biome, name, 0);
		}

		//Generate experiment for a given Celestial Body and experimental situation
		internal static DMCollectScience fetchScienceContract(CelestialBody body, ExperimentSituations targetSituation)
		{
			DMScienceContainer scienceContainer;
			ScienceExperiment exp;
			ScienceSubject sub;
			AvailablePart aPart;
			string name;
			string biome = "";

			//Choose a random science experiment from our list generated at startup
			scienceContainer = DMUtils.availableScience.ElementAt(rand.Next(0, DMUtils.availableScience.Count)).Value;
			name = DMUtils.availableScience.FirstOrDefault(n => n.Value == scienceContainer).Key;
			DMUtils.DebugLog("Checking Contract Requirements");

			//Determine if the science part is available if applicable
			if (scienceContainer.sciPart != "None")
			{
				DMUtils.DebugLog("Checking For Part {0} Now", scienceContainer.sciPart);
				aPart = PartLoader.getPartInfoByName(scienceContainer.sciPart);
				if (aPart == null)
					return null;
				if (!ResearchAndDevelopment.PartModelPurchased(aPart))
					return null;
				DMUtils.DebugLog("Part: [{0}] Purchased; Contract Meets Requirements", aPart.name);
			}

			//Make sure our experiment is OK
			exp = scienceContainer.exp;
			if (exp == null)
				return null;

			//Make sure that the experiment can be conducted in this situation
			if (((ExperimentSituations)exp.situationMask & targetSituation) != targetSituation)
				return null;

			//Build a list of acceptable biomes if applicable, choose one with remaining science
			if (DMUtils.biomeRelevant(targetSituation, scienceContainer.bioMask))
			{
				DMUtils.DebugLog("Checking For Biome Usage");
				List<string> bList = DMUtils.fetchBiome(body, exp, targetSituation);
				if (bList.Count == 0)
				{
					DMUtils.DebugLog("Planet All Tapped Out; No Remaining Science Here");
					return null;
				}
				int i = rand.Next(0, 2);
				if (i == 0)
				{
					biome = bList[rand.Next(0, bList.Count)];
					DMUtils.DebugLog("Acceptable Biome Found: {0}", biome);
				}
			}

			//Make sure that our chosen science subject has science remaining to be gathered
			if ((sub = ResearchAndDevelopment.GetSubjectByID(string.Format("{0}@{1}{2}{3}", exp.id, body.name, targetSituation, biome.Replace(" ", "")))) != null)
			{
				if (sub.scientificValue < 0.4f)
					return null;
			}

			return new DMCollectScience(body, targetSituation, biome, name, 0);
		}

		//Generate random experiment for a given celestial body
		internal static DMCollectScience fetchScienceContract(CelestialBody body, ScienceExperiment exp)
		{
			DMScienceContainer scienceContainer;
			ExperimentSituations targetSituation;
			List<ExperimentSituations> situations;
			ScienceSubject sub;
			AvailablePart aPart;
			string name;
			string biome = "";

			//Choose science container based on a given science experiment
			scienceContainer = DMUtils.availableScience.FirstOrDefault(e => e.Value.exp == exp).Value;
			name = DMUtils.availableScience.FirstOrDefault(n => n.Value == scienceContainer).Key;
			DMUtils.DebugLog("Checking Contract Requirements");

			//Determine if the science part is available if applicable
			if (scienceContainer.sciPart != "None")
			{
				DMUtils.DebugLog("Checking For Part {0} Now", scienceContainer.sciPart);
				aPart = PartLoader.getPartInfoByName(scienceContainer.sciPart);
				if (aPart == null)
					return null;
				if (!ResearchAndDevelopment.PartModelPurchased(aPart))
					return null;
				DMUtils.DebugLog("Part: [{0}] Purchased; Contract Meets Requirements", aPart.name);
			}

			//Make sure our experiment is OK
			exp = scienceContainer.exp;
			if (exp == null)
				return null;

			//Choose an acceptable experimental situation for a given science experiment
			if ((situations = DMUtils.availableSituations(exp, scienceContainer.sitMask, body)).Count == 0)
				return null;
			else
			{
				DMUtils.DebugLog("Acceptable Situations Found");
				targetSituation = situations[rand.Next(0, situations.Count)];
				DMUtils.DebugLog("Experimental Situation: {0}", targetSituation);
			}

			//Build a list of acceptable biomes if applicable, choose one with remaining science
			if (DMUtils.biomeRelevant(targetSituation, scienceContainer.bioMask))
			{
				DMUtils.DebugLog("Checking For Biome Usage");
				List<string> bList = DMUtils.fetchBiome(body, exp, targetSituation);
				if (bList.Count == 0)
				{
					DMUtils.DebugLog("Planet All Tapped Out; No Remaining Science Here");
					return null;
				}
				int i = rand.Next(0, 2);
				if (i == 0)
				{
					biome = bList[rand.Next(0, bList.Count)];
					DMUtils.DebugLog("Acceptable Biome Found: {0}", biome);
				}
			}

			//Make sure that our chosen science subject has science remaining to be gathered
			if ((sub = ResearchAndDevelopment.GetSubjectByID(string.Format("{0}@{1}{2}{3}", exp.id, body.name, targetSituation, biome.Replace(" ", "")))) != null)
			{
				if (sub.scientificValue < 0.4f)
					return null;
			}

			return new DMCollectScience(body, targetSituation, biome, name, 0);
		}


	}

	static class DMOrbitalSurveyGenerator
	{
		private static System.Random rand = DMUtils.rand;

		internal static DMCollectScience fetchOrbitalScience(Contract.ContractPrestige c, List<CelestialBody> cR, List<CelestialBody> cUR)
		{
			DMScienceContainer scienceContainer;
			CelestialBody body;
			ExperimentSituations targetSituation;
			ScienceExperiment exp;
			AvailablePart aPart;
			string name;
			string biome = "";

			scienceContainer = DMUtils.orbitalScience.ElementAt(rand.Next(0, DMUtils.availableScience.Count)).Value;
			name = DMUtils.availableScience.FirstOrDefault(n => n.Value == scienceContainer).Key;

			//Determine if the science part is available if applicable
			if (scienceContainer.sciPart != "None")
			{
				DMUtils.DebugLog("Checking For Part {0} Now", scienceContainer.sciPart);
				aPart = PartLoader.getPartInfoByName(scienceContainer.sciPart);
				if (aPart == null)
					return null;
				if (!ResearchAndDevelopment.PartModelPurchased(aPart))
					return null;
				DMUtils.DebugLog("Part: [{0}] Purchased; Contract Meets Requirements", aPart.name);
			}

			body = DMUtils.nextTargetBody(c, cR, cUR);
			if (body == null)
				return null;

			//Make sure our experiment is OK
			exp = scienceContainer.exp;
			if (exp == null)
				return null;

			if (((ExperimentSituations)scienceContainer.sitMask & ExperimentSituations.InSpaceHigh) == ExperimentSituations.InSpaceHigh)
			{
				if (rand.Next(0, 2) == 0)
					targetSituation = ExperimentSituations.InSpaceHigh;
				else
					targetSituation = ExperimentSituations.InSpaceLow;
			}
			else
				targetSituation = ExperimentSituations.InSpaceLow;

			if (DMUtils.biomeRelevant(targetSituation, scienceContainer.bioMask))
			{
				DMUtils.DebugLog("Checking For Biome Usage");
				if (body.BiomeMap == null || body.BiomeMap.Map == null)
					biome = "";
				else if (rand.Next(0, 2) == 0)
				{
					biome = body.BiomeMap.Attributes[rand.Next(0, body.BiomeMap.Attributes.Length)].name;
				}
				else
					biome = "";
			}

			return new DMCollectScience(body, targetSituation, biome, name, 1);
		}

		internal static DMCollectScience fetchOrbitalScience(CelestialBody b)
		{
			DMScienceContainer scienceContainer;
			CelestialBody body = b;
			ExperimentSituations targetSituation;
			ScienceExperiment exp;
			AvailablePart aPart;
			string name;
			string biome = "";

			scienceContainer = DMUtils.orbitalScience.ElementAt(rand.Next(0, DMUtils.availableScience.Count)).Value;
			name = DMUtils.availableScience.FirstOrDefault(n => n.Value == scienceContainer).Key;

			//Determine if the science part is available if applicable
			if (scienceContainer.sciPart != "None")
			{
				DMUtils.DebugLog("Checking For Part {0} Now", scienceContainer.sciPart);
				aPart = PartLoader.getPartInfoByName(scienceContainer.sciPart);
				if (aPart == null)
					return null;
				if (!ResearchAndDevelopment.PartModelPurchased(aPart))
					return null;
				DMUtils.DebugLog("Part: [{0}] Purchased; Contract Meets Requirements", aPart.name);
			}

			//Make sure our experiment is OK
			exp = scienceContainer.exp;
			if (exp == null)
				return null;

			if (((ExperimentSituations)scienceContainer.sitMask & ExperimentSituations.InSpaceHigh) == ExperimentSituations.InSpaceHigh)
			{
				if (rand.Next(0, 2) == 0)
					targetSituation = ExperimentSituations.InSpaceHigh;
				else
					targetSituation = ExperimentSituations.InSpaceLow;
			}
			else
				targetSituation = ExperimentSituations.InSpaceLow;

			if (DMUtils.biomeRelevant(targetSituation, scienceContainer.bioMask))
			{
				DMUtils.DebugLog("Checking For Biome Usage");
				if (body.BiomeMap == null || body.BiomeMap.Map == null)
					biome = "";
				else if (rand.Next(0, 2) == 0)
				{
					biome = body.BiomeMap.Attributes[rand.Next(0, body.BiomeMap.Attributes.Length)].name;
				}
				else
					biome = "";
			}

			return new DMCollectScience(body, targetSituation, biome, name, 1);
		}


	}

	#endregion

}
