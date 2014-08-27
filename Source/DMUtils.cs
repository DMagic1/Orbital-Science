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
	internal static class DMUtils
	{
		internal static System.Random rand;
		internal static Dictionary<string, Dictionary<string, DMScienceContainer>> availableScience;
		internal static Dictionary<string, List<string>> backStory;
		internal static float science, reward, forward, penalty, deadline;
		internal static int maxSurvey, maxAsteroid, maxAnomaly, maxMagnetic;
		internal static EventData<CelestialBody, String, String> OnAnomalyScience;
		internal static EventData<String, String> OnAsteroidScience;

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
					return 4f;
				case 2:
					return 6f;
				case 3:
					return 8f;
				case 4:
					return 10f;
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
				D /= (3600 * 6);
			else
				D /= (3600 * 24);
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
			DMUtils.DebugLog("Searching For Acceptable Body");
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
			//Select unreached body for; remove Kerbin system, return already reached bodies if all have been visited
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
			DMUtils.DebugLog("Found {0} Valid Experimental Situations", expSitList.Count);
			return expSitList;
		}

		internal static List<ExperimentSituations> availableSituationsLimited(ScienceExperiment exp, int i, CelestialBody b)
		{
			DMUtils.DebugLog("Finding Situations");
			List<ExperimentSituations> expSitList = new List<ExperimentSituations>();
			if (((ExperimentSituations)i & ExperimentSituations.FlyingHigh) == ExperimentSituations.FlyingHigh && b.atmosphere)
				expSitList.Add(ExperimentSituations.FlyingHigh);
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
			DMUtils.DebugLog("Searching For Biomes: Value Sensitive");
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
						if (subB.scientificValue > 0.5f)
							s.Add(bName);
					}
				}
			}
			DMUtils.DebugLog("Found Acceptable Biomes");
			return s;
		}

		internal static List<string> fetchBiome(CelestialBody b)
		{
			DMUtils.DebugLog("Searching For Biomes: Value InSensitive");
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
					s.Add(bName);
				}
			}
			return s;
		}

#endregion

	}

	#region Parameter Generator

	static class DMCollectContractGenerator
	{
		private static System.Random rand = DMUtils.rand;

		////Generate fully random science experiment contract parameter
		//internal static DMCollectScience fetchScienceContract(Contract.ContractPrestige p, List<CelestialBody> cR, List<CelestialBody> cUR)
		//{
		//    DMScienceContainer scienceContainer;
		//    CelestialBody body;
		//    ExperimentSituations targetSituation;
		//    List<ExperimentSituations> situations;
		//    ScienceExperiment exp;
		//    ScienceSubject sub;
		//    AvailablePart aPart;
		//    string name;
		//    string biome = "";

		//    //Choose a random science experiment from our list generated at startup
		//    scienceContainer = DMUtils.availableScience["All"].ElementAt(rand.Next(0, DMUtils.availableScience["All"].Count)).Value;
		//    name = DMUtils.availableScience["All"].FirstOrDefault(n => n.Value == scienceContainer).Key;
		//    DMUtils.DebugLog("Checking Contract Requirements");

		//    //Determine if the science part is available if applicable
		//    if (scienceContainer.sciPart != "None")
		//    {
		//        DMUtils.DebugLog("Checking For Part {0} Now", scienceContainer.sciPart);
		//        aPart = PartLoader.getPartInfoByName(scienceContainer.sciPart);
		//        if (aPart == null)
		//            return null;
		//        if (!ResearchAndDevelopment.PartModelPurchased(aPart))
		//            return null;
		//        DMUtils.DebugLog("Part: [{0}] Purchased; Contract Meets Requirements", aPart.name);
		//    }

		//    //Select a random Celestial Body based on contract prestige levels
		//    body = DMUtils.nextTargetBody(p, cR, cUR);
		//    if (body == null)
		//        return null;

		//    //Make sure our experiment is OK
		//    exp = scienceContainer.exp;
		//    if (exp == null)
		//        return null;

		//    //Choose an acceptable experimental situation for a given science experiment
		//    if ((situations = DMUtils.availableSituations(exp, scienceContainer.sitMask, body)).Count == 0)
		//        return null;
		//    else
		//    {
		//        DMUtils.DebugLog("Acceptable Situations Found");
		//        targetSituation = situations[rand.Next(0, situations.Count)];
		//        DMUtils.DebugLog("Experimental Situation: {0}", targetSituation);
		//    }

		//    //Build a list of acceptable biomes if applicable, choose one with remaining science
		//    if (DMUtils.biomeRelevant(targetSituation, scienceContainer.bioMask) && targetSituation != ExperimentSituations.SrfSplashed)
		//    {
		//        DMUtils.DebugLog("Checking For Biome Usage");
		//        List<string> bList = DMUtils.fetchBiome(body, exp, targetSituation);
		//        if (bList.Count == 0)
		//        {
		//            DMUtils.DebugLog("Planet All Tapped Out; No Remaining Science Here");
		//            return null;
		//        }
		//        int i = rand.Next(0, 2);
		//        if (i == 0)
		//        {
		//            biome = bList[rand.Next(0, bList.Count)];
		//            DMUtils.DebugLog("Acceptable Biome Found: {0}", biome);
		//        }
		//    }

		//    //Make sure that our chosen science subject has science remaining to be gathered
		//    if ((sub = ResearchAndDevelopment.GetSubjectByID(string.Format("{0}@{1}{2}{3}", exp.id, body.name, targetSituation, biome.Replace(" ", "")))) != null)
		//    {
		//        if (sub.scientificValue < 0.4f)
		//            return null;
		//    }

		//    return new DMCollectScience(body, targetSituation, biome, name, 0);
		//}

		////Generate random experiment for a given celestial body
		//internal static DMCollectScience fetchScienceContract(CelestialBody body)
		//{
		//    DMScienceContainer scienceContainer;
		//    ExperimentSituations targetSituation;
		//    List<ExperimentSituations> situations;
		//    ScienceExperiment exp;
		//    ScienceSubject sub;
		//    AvailablePart aPart;
		//    string name;
		//    string biome = "";

		//    //Choose a random science experiment from our list generated at startup
		//    scienceContainer = DMUtils.availableScience["All"].ElementAt(rand.Next(0, DMUtils.availableScience.Count)).Value;
		//    name = DMUtils.availableScience["All"].FirstOrDefault(n => n.Value == scienceContainer).Key;
		//    DMUtils.DebugLog("Checking Contract Requirements");

		//    //Determine if the science part is available if applicable
		//    if (scienceContainer.sciPart != "None")
		//    {
		//        DMUtils.DebugLog("Checking For Part {0} Now", scienceContainer.sciPart);
		//        aPart = PartLoader.getPartInfoByName(scienceContainer.sciPart);
		//        if (aPart == null)
		//            return null;
		//        if (!ResearchAndDevelopment.PartModelPurchased(aPart))
		//            return null;
		//        DMUtils.DebugLog("Part: [{0}] Purchased; Contract Meets Requirements", aPart.name);
		//    }

		//    //Make sure our experiment is OK
		//    exp = scienceContainer.exp;
		//    if (exp == null)
		//        return null;

		//    //Choose an acceptable experimental situation for a given science experiment
		//    if ((situations = DMUtils.availableSituations(exp, scienceContainer.sitMask, body)).Count == 0)
		//        return null;
		//    else
		//    {
		//        DMUtils.DebugLog("Acceptable Situations Found");
		//        targetSituation = situations[rand.Next(0, situations.Count)];
		//        DMUtils.DebugLog("Experimental Situation: {0}", targetSituation);
		//    }

		//    //Build a list of acceptable biomes if applicable, choose one with remaining science
		//    if (DMUtils.biomeRelevant(targetSituation, scienceContainer.bioMask) && targetSituation != ExperimentSituations.SrfSplashed)
		//    {
		//        DMUtils.DebugLog("Checking For Biome Usage");
		//        List<string> bList = DMUtils.fetchBiome(body, exp, targetSituation);
		//        if (bList.Count == 0)
		//        {
		//            DMUtils.DebugLog("Planet All Tapped Out; No Remaining Science Here");
		//            return null;
		//        }
		//        int i = rand.Next(0, 2);
		//        if (i == 0)
		//        {
		//            biome = bList[rand.Next(0, bList.Count)];
		//            DMUtils.DebugLog("Acceptable Biome Found: {0}", biome);
		//        }
		//    }

		//    //Make sure that our chosen science subject has science remaining to be gathered
		//    if ((sub = ResearchAndDevelopment.GetSubjectByID(string.Format("{0}@{1}{2}{3}", exp.id, body.name, targetSituation, biome.Replace(" ", "")))) != null)
		//    {
		//        if (sub.scientificValue < 0.4f)
		//            return null;
		//    }

		//    return new DMCollectScience(body, targetSituation, biome, name, 0);
		//}

		////Generate experiment for a given Celestial Body and experimental situation
		//internal static DMCollectScience fetchScienceContract(CelestialBody body, ExperimentSituations targetSituation)
		//{
		//    DMScienceContainer scienceContainer;
		//    ScienceExperiment exp;
		//    ScienceSubject sub;
		//    AvailablePart aPart;
		//    string name;
		//    string biome = "";

		//    //Choose a random science experiment from our list generated at startup
		//    scienceContainer = DMUtils.availableScience["All"].ElementAt(rand.Next(0, DMUtils.availableScience.Count)).Value;
		//    name = DMUtils.availableScience["All"].FirstOrDefault(n => n.Value == scienceContainer).Key;
		//    DMUtils.DebugLog("Checking Contract Requirements");

		//    //Determine if the science part is available if applicable
		//    if (scienceContainer.sciPart != "None")
		//    {
		//        DMUtils.DebugLog("Checking For Part {0} Now", scienceContainer.sciPart);
		//        aPart = PartLoader.getPartInfoByName(scienceContainer.sciPart);
		//        if (aPart == null)
		//            return null;
		//        if (!ResearchAndDevelopment.PartModelPurchased(aPart))
		//            return null;
		//        DMUtils.DebugLog("Part: [{0}] Purchased; Contract Meets Requirements", aPart.name);
		//    }

		//    //Make sure our experiment is OK
		//    exp = scienceContainer.exp;
		//    if (exp == null)
		//        return null;

		//    //Make sure that the experiment can be conducted in this situation
		//    if (((ExperimentSituations)exp.situationMask & targetSituation) != targetSituation)
		//        return null;

		//    //Build a list of acceptable biomes if applicable, choose one with remaining science
		//    if (DMUtils.biomeRelevant(targetSituation, scienceContainer.bioMask) && targetSituation != ExperimentSituations.SrfSplashed)
		//    {
		//        DMUtils.DebugLog("Checking For Biome Usage");
		//        List<string> bList = DMUtils.fetchBiome(body, exp, targetSituation);
		//        if (bList.Count == 0)
		//        {
		//            DMUtils.DebugLog("Planet All Tapped Out; No Remaining Science Here");
		//            return null;
		//        }
		//        int i = rand.Next(0, 2);
		//        if (i == 0)
		//        {
		//            biome = bList[rand.Next(0, bList.Count)];
		//            DMUtils.DebugLog("Acceptable Biome Found: {0}", biome);
		//        }
		//    }

		//    //Make sure that our chosen science subject has science remaining to be gathered
		//    if ((sub = ResearchAndDevelopment.GetSubjectByID(string.Format("{0}@{1}{2}{3}", exp.id, body.name, targetSituation, biome.Replace(" ", "")))) != null)
		//    {
		//        if (sub.scientificValue < 0.4f)
		//            return null;
		//    }

		//    return new DMCollectScience(body, targetSituation, biome, name, 0);
		//}

		////Generate random experiment for a given celestial body
		//internal static DMCollectScience fetchScienceContract(CelestialBody body, ScienceExperiment exp)
		//{
		//    DMScienceContainer scienceContainer;
		//    ExperimentSituations targetSituation;
		//    List<ExperimentSituations> situations;
		//    ScienceSubject sub;
		//    AvailablePart aPart;
		//    string name;
		//    string biome = "";

		//    //Choose science container based on a given science experiment
		//    scienceContainer = DMUtils.availableScience["All"].FirstOrDefault(e => e.Value.exp == exp).Value;
		//    name = DMUtils.availableScience["All"].FirstOrDefault(n => n.Value == scienceContainer).Key;
		//    DMUtils.DebugLog("Checking Contract Requirements");

		//    //Determine if the science part is available if applicable
		//    if (scienceContainer.sciPart != "None")
		//    {
		//        DMUtils.DebugLog("Checking For Part {0} Now", scienceContainer.sciPart);
		//        aPart = PartLoader.getPartInfoByName(scienceContainer.sciPart);
		//        if (aPart == null)
		//            return null;
		//        if (!ResearchAndDevelopment.PartModelPurchased(aPart))
		//            return null;
		//        DMUtils.DebugLog("Part: [{0}] Purchased; Contract Meets Requirements", aPart.name);
		//    }

		//    //Make sure our experiment is OK
		//    exp = scienceContainer.exp;
		//    if (exp == null)
		//        return null;

		//    //Choose an acceptable experimental situation for a given science experiment
		//    if ((situations = DMUtils.availableSituations(exp, scienceContainer.sitMask, body)).Count == 0)
		//        return null;
		//    else
		//    {
		//        DMUtils.DebugLog("Acceptable Situations Found");
		//        targetSituation = situations[rand.Next(0, situations.Count)];
		//        DMUtils.DebugLog("Experimental Situation: {0}", targetSituation);
		//    }

		//    //Build a list of acceptable biomes if applicable, choose one with remaining science
		//    if (DMUtils.biomeRelevant(targetSituation, scienceContainer.bioMask) && targetSituation != ExperimentSituations.SrfSplashed)
		//    {
		//        DMUtils.DebugLog("Checking For Biome Usage");
		//        List<string> bList = DMUtils.fetchBiome(body, exp, targetSituation);
		//        if (bList.Count == 0)
		//        {
		//            DMUtils.DebugLog("Planet All Tapped Out; No Remaining Science Here");
		//            return null;
		//        }
		//        int i = rand.Next(0, 2);
		//        if (i == 0)
		//        {
		//            biome = bList[rand.Next(0, bList.Count)];
		//            DMUtils.DebugLog("Acceptable Biome Found: {0}", biome);
		//        }
		//    }

		//    //Make sure that our chosen science subject has science remaining to be gathered
		//    if ((sub = ResearchAndDevelopment.GetSubjectByID(string.Format("{0}@{1}{2}{3}", exp.id, body.name, targetSituation, biome.Replace(" ", "")))) != null)
		//    {
		//        if (sub.scientificValue < 0.4f)
		//            return null;
		//    }

		//    return new DMCollectScience(body, targetSituation, biome, name, 0);
		//}

		internal static DMCollectScience fetchScienceContract(CelestialBody Body, ExperimentSituations Situation, ScienceExperiment Exp)
		{
			DMScienceContainer scienceContainer;
			AvailablePart aPart;
			string name;

			//Choose science container based on a given science experiment
			scienceContainer = DMUtils.availableScience["All"].FirstOrDefault(e => e.Value.exp == Exp).Value;
			name = DMUtils.availableScience["All"].FirstOrDefault(n => n.Value == scienceContainer).Key;
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

			return new DMCollectScience(Body, Situation, "", name, 1);
		}
	}

	static class DMSurveyGenerator
	{
		private static System.Random rand = DMUtils.rand;

		//Used for initial orbital and surface survey parameter
		internal static DMCollectScience fetchSurveyScience(Contract.ContractPrestige c, List<CelestialBody> cR, List<CelestialBody> cUR, DMScienceContainer DMScience, int sT)
		{
			CelestialBody body;
			ExperimentSituations targetSituation;
			ScienceSubject sub;
			AvailablePart aPart;
			string name;
			string biome = "";
			int surveyType = sT;

			name = DMUtils.availableScience["All"].FirstOrDefault(n => n.Value == DMScience).Key;

			//Determine if the science part is available if applicable
			if (DMScience.sciPart != "None")
			{
				DMUtils.DebugLog("Checking For Part {0} Now", DMScience.sciPart);
				aPart = PartLoader.getPartInfoByName(DMScience.sciPart);
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
			if (DMScience.exp == null)
				return null;

			if (surveyType == 0)
			{
				if (!body.atmosphere && DMScience.exp.requireAtmosphere)
					return null;
				if (((ExperimentSituations)DMScience.sitMask & ExperimentSituations.InSpaceHigh) == ExperimentSituations.InSpaceHigh)
				{
					if (rand.Next(0, 2) == 0)
						targetSituation = ExperimentSituations.InSpaceHigh;
					else
						targetSituation = ExperimentSituations.InSpaceLow;
				}
				else
					targetSituation = ExperimentSituations.InSpaceLow;
			}
			else if (surveyType == 1)
			{
				if (body.pqsController == null)
					return null;
				if (!body.atmosphere && DMScience.exp.requireAtmosphere)
					return null;
				if (((ExperimentSituations)DMScience.sitMask & ExperimentSituations.SrfLanded) == ExperimentSituations.SrfLanded)
				{
					if (DMScience.exp.id != "dmbiodrillscan")
						targetSituation = ExperimentSituations.SrfLanded;
					else if (body.atmosphere)
						targetSituation = ExperimentSituations.SrfLanded;
					else
						return null;
				}
				else
					return null;
			}
			else if (surveyType == 2)
			{
				if (!body.atmosphere)
					return null;
				if (rand.Next(0, 2) == 0)
					targetSituation = ExperimentSituations.FlyingHigh;
				else
					targetSituation = ExperimentSituations.FlyingLow;
			}
			else
				return null;

			if (DMUtils.biomeRelevant(targetSituation, DMScience.bioMask) && targetSituation != ExperimentSituations.SrfSplashed)
			{
				DMUtils.DebugLog("Checking For Biome Usage");
				List<string> bList = DMUtils.fetchBiome(body, DMScience.exp, targetSituation);
				if (bList.Count == 0)
				{
					DMUtils.DebugLog("Planet All Tapped Out; No Remaining Science Here");
					return null;
				}
				else
				{
					biome = bList[rand.Next(0, bList.Count)];
					DMUtils.DebugLog("Acceptable Biome Found: {0}", biome);
				}
			}

			//Make sure that our chosen science subject has science remaining to be gathered
			if ((sub = ResearchAndDevelopment.GetSubjectByID(string.Format("{0}@{1}{2}{3}", DMScience.exp.id, body.name, targetSituation, biome.Replace(" ", "")))) != null)
			{
				if (sub.scientificValue < 0.5f)
					return null;
			}

			if (surveyType == 0)
				return new DMCollectScience(body, targetSituation, "", name, 0);
			else if (surveyType == 1)
				return new DMCollectScience(body, targetSituation, biome, name, 0);
			else if (surveyType == 2)
				return new DMCollectScience(body, targetSituation, biome, name, 1);
			else
				return null;
		}

		//Used for orbital survey
		internal static DMCollectScience fetchSurveyScience(CelestialBody Body, DMScienceContainer DMScience)
		{
			ExperimentSituations targetSituation;
			ScienceSubject sub;
			AvailablePart aPart;
			string name;

			name = DMUtils.availableScience["All"].FirstOrDefault(n => n.Value == DMScience).Key;

			//Determine if the science part is available if applicable
			if (DMScience.sciPart != "None")
			{
				DMUtils.DebugLog("Checking For Part {0} Now", DMScience.sciPart);
				aPart = PartLoader.getPartInfoByName(DMScience.sciPart);
				if (aPart == null)
					return null;
				if (!ResearchAndDevelopment.PartModelPurchased(aPart))
					return null;
				DMUtils.DebugLog("Part: [{0}] Purchased; Contract Meets Requirements", aPart.name);
			}

			//Make sure our experiment is OK
			if (DMScience.exp == null)
				return null;

			if (!Body.atmosphere && DMScience.exp.requireAtmosphere)
				return null;
			if (((ExperimentSituations)DMScience.sitMask & ExperimentSituations.InSpaceHigh) == ExperimentSituations.InSpaceHigh)
			{
				if (rand.Next(0, 2) == 0)
					targetSituation = ExperimentSituations.InSpaceHigh;
				else
					targetSituation = ExperimentSituations.InSpaceLow;
			}
			else
				targetSituation = ExperimentSituations.InSpaceLow;

			if (DMUtils.biomeRelevant(targetSituation, DMScience.bioMask))
			{
				DMUtils.DebugLog("Checking For Biome Usage");
				List<string> bList = DMUtils.fetchBiome(Body, DMScience.exp, targetSituation);
				if (bList.Count == 0)
				{
					DMUtils.DebugLog("Planet All Tapped Out; No Remaining Science Here");
					return null;
				}
			}

			if ((sub = ResearchAndDevelopment.GetSubjectByID(string.Format("{0}@{1}{2}", DMScience.exp.id, Body.name, targetSituation))) != null)
				if (sub.scientificValue < 0.5f)
					return null;

			return new DMCollectScience(Body, targetSituation, "", name, 0);
		}


		//Used for surface surveys
		internal static DMCollectScience fetchSurveyScience(CelestialBody Body, DMScienceContainer DMScience, string Biome)
		{
			AvailablePart aPart;
			ScienceSubject sub;
			string name;

			name = DMUtils.availableScience["All"].FirstOrDefault(n => n.Value == DMScience).Key;

			//Determine if the science part is available if applicable
			if (DMScience.sciPart != "None")
			{
				DMUtils.DebugLog("Checking For Part {0} Now", DMScience.sciPart);
				aPart = PartLoader.getPartInfoByName(DMScience.sciPart);
				if (aPart == null)
					return null;
				if (!ResearchAndDevelopment.PartModelPurchased(aPart))
					return null;
				DMUtils.DebugLog("Part: [{0}] Purchased; Contract Meets Requirements", aPart.name);
			}

			//Make sure our experiment is OK
			if (DMScience.exp == null)
				return null;

			if (!DMUtils.biomeRelevant(ExperimentSituations.SrfLanded, DMScience.bioMask))
				Biome = "";

			if (Body.pqsController == null)
				return null;
			if (!Body.atmosphere && DMScience.exp.requireAtmosphere)
				return null;
			if (((ExperimentSituations)DMScience.sitMask & ExperimentSituations.SrfLanded) != ExperimentSituations.SrfLanded)
				return null;
			if (DMScience.exp.id == "dmbiodrillscan" && !Body.atmosphere)
				return null;
			if ((sub = ResearchAndDevelopment.GetSubjectByID(string.Format("{0}@{1}{2}{3}", DMScience.exp.id, Body.name, ExperimentSituations.SrfLanded, Biome.Replace(" ", "")))) != null)
				if (sub.scientificValue < 0.5f)
					return null;

			return new DMCollectScience(Body, ExperimentSituations.SrfLanded, Biome, name, 0);
		}

		//Used for biological survey
		internal static DMCollectScience fetchBioSurveyScience(CelestialBody body)
		{
			string biome = "";

			DMUtils.DebugLog("Checking Contract Requirements");

			//Build a list of acceptable biomes if applicable, choose one with remaining science
			DMUtils.DebugLog("Checking For Biome Usage");
			List<string> bList = DMUtils.fetchBiome(body);
			if (bList.Count == 0)
				return null;
			int i = rand.Next(0, 2);
			if (i == 0)
			{
				biome = bList[rand.Next(0, bList.Count)];
				DMUtils.DebugLog("Acceptable Biome Found: {0}", biome);
			}

			return new DMCollectScience(body, ExperimentSituations.SrfLanded, biome, "Bio Drill Scan", 1);
		}

		internal static DMCollectScience fetchBioSurveyScience(CelestialBody body, DMScienceContainer DMScience, string Biome)
		{
			ExperimentSituations targetSituation;
			List<ExperimentSituations> situations;
			AvailablePart aPart;
			string name;

			name = DMUtils.availableScience["All"].FirstOrDefault(n => n.Value == DMScience).Key;
			DMUtils.DebugLog("Checking Contract Requirements");

			//Determine if the science part is available if applicable
			if (DMScience.sciPart != "None")
			{
				DMUtils.DebugLog("Checking For Part {0} Now", DMScience.sciPart);
				aPart = PartLoader.getPartInfoByName(DMScience.sciPart);
				if (aPart == null)
					return null;
				if (!ResearchAndDevelopment.PartModelPurchased(aPart))
					return null;
				DMUtils.DebugLog("Part: [{0}] Purchased; Contract Meets Requirements", aPart.name);
			}

			//Make sure our experiment is OK
			if (DMScience.exp == null)
				return null;

			//Choose an acceptable experimental situation for a given science experiment
			if ((situations = DMUtils.availableSituations(DMScience.exp, DMScience.sitMask, body)).Count == 0)
				return null;
			else
			{
				DMUtils.DebugLog("Acceptable Situations Found");
				targetSituation = situations[rand.Next(0, situations.Count)];
				DMUtils.DebugLog("Experimental Situation: {0}", targetSituation);
			}

			if (targetSituation != ExperimentSituations.SrfLanded)
				Biome = "";

			return new DMCollectScience(body, targetSituation, Biome, name, 1);
		}
	}

	static class DMAsteroidGenerator
	{
		private static System.Random rand = DMUtils.rand;

		internal static DMAsteroidParameter fetchAsteroidParameter(int Size, DMScienceContainer DMScience)
		{
			ExperimentSituations targetSituation;
			AvailablePart aPart;
			string name;

			name = DMUtils.availableScience["All"].FirstOrDefault(n => n.Value == DMScience).Key;

			//Determine if the science part is available if applicable
			if (DMScience.sciPart != "None")
			{
				DMUtils.DebugLog("Checking For Part {0} Now", DMScience.sciPart);
				aPart = PartLoader.getPartInfoByName(DMScience.sciPart);
				if (aPart == null)
					return null;
				if (!ResearchAndDevelopment.PartModelPurchased(aPart))
					return null;
				DMUtils.DebugLog("Part: [{0}] Purchased; Contract Meets Requirements", aPart.name);
			}

			if (DMScience.exp == null)
				return null;

			if (((ExperimentSituations)DMScience.sitMask & ExperimentSituations.InSpaceLow) == ExperimentSituations.InSpaceLow)
				if (((ExperimentSituations)DMScience.sitMask & ExperimentSituations.SrfLanded) == ExperimentSituations.SrfLanded)
					if (rand.Next(0, 2) == 0)
						targetSituation = ExperimentSituations.SrfLanded;
					else
						targetSituation = ExperimentSituations.InSpaceLow;
				else
					targetSituation = ExperimentSituations.InSpaceLow;
			else if (((ExperimentSituations)DMScience.sitMask & ExperimentSituations.SrfLanded) == ExperimentSituations.SrfLanded)
				targetSituation = ExperimentSituations.SrfLanded;
			else
				return null;

			DMUtils.DebugLog("Successfully Generated Asteroid Survey Parameter");
			return new DMAsteroidParameter(Size, targetSituation, name);
		}

	}

	static class DMAnomalyGenerator
	{
		private static System.Random rand = DMUtils.rand;

		internal static DMCollectScience fetchAnomalyParameter(CelestialBody Body, DMAnomalyObject City)
		{
			CelestialBody body;
			ExperimentSituations targetSituation;
			DMAnomalyObject city;
			ScienceSubject sub;
			string subject, anomName;

			body = Body;
			if (body == null)
				return null;

			city = City;
			if (city == null)
				return null;

			if (rand.Next(0, 2) == 0)
				targetSituation = ExperimentSituations.SrfLanded;
			else
				targetSituation = ExperimentSituations.FlyingLow;

			anomName = DMAnomalyScanner.anomalyCleanup(city.name);

			subject = string.Format("AnomalyScan@{0}{1}{2}", body.name, targetSituation, anomName);

			//Make sure that our chosen science subject has science remaining to be gathered
			if ((sub = ResearchAndDevelopment.GetSubjectByID(subject)) != null)
			{
				if (sub.scientificValue < 0.4f)
					return null;
			}

			DMUtils.DebugLog("Primary Anomaly Parameter Assigned");
			return new DMCollectScience(body, targetSituation, anomName, "Anomaly Scan", 2);
		}

		internal static DMAnomalyParameter fetchAnomalyParameter(CelestialBody Body, DMAnomalyObject City, DMScienceContainer DMScience)
		{
			AvailablePart aPart;
			ExperimentSituations targetSituation;
			List<ExperimentSituations> situations;
			string name;

			name = DMUtils.availableScience["All"].FirstOrDefault(n => n.Value == DMScience).Key;

			//Determine if the science part is available if applicable
			if (DMScience.sciPart != "None")
			{
				DMUtils.DebugLog("Checking For Part {0} Now", DMScience.sciPart);
				aPart = PartLoader.getPartInfoByName(DMScience.sciPart);
				if (aPart == null)
					return null;
				if (!ResearchAndDevelopment.PartModelPurchased(aPart))
					return null;
				DMUtils.DebugLog("Part: [{0}] Purchased; Contract Meets Requirements", aPart.name);
			}

			if ((situations = DMUtils.availableSituationsLimited(DMScience.exp, DMScience.sitMask, Body)).Count == 0)
				return null;
			else
			{
				DMUtils.DebugLog("Acceptable Situations Found");
				targetSituation = situations[rand.Next(0, situations.Count)];
				DMUtils.DebugLog("Experimental Situation: {0}", targetSituation);
			}

			return new DMAnomalyParameter(Body, City, targetSituation, name);
		}
	}

	#endregion

}
