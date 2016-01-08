#region license
/* DMagic Orbital Science - DMContractGenerators
 * Static utilities class for generating contract parameters
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

using System.Collections.Generic;
using System.Linq;
using Contracts;
using DMagic.Contracts;
using DMagic.Parameters;

namespace DMagic
{
	static class DMCollectContractGenerator
	{
		private static System.Random rand = DMUtils.rand;

		//Use for magnetic field survey
		internal static DMCollectScience fetchScienceContract(CelestialBody Body, ExperimentSituations Situation, DMScienceContainer DMScience)
		{
			string name;

			//Choose science container based on a given science experiment
			name = DMUtils.availableScience["All"].FirstOrDefault(n => n.Value == DMScience).Key;

			if (DMScience.Exp == null)
				return null;

			//Determine if the science part is available if applicable
			if (DMScience.SciPart != "None")
			{
				if (!DMUtils.partAvailable(new List<string>(1) { DMScience.SciPart }))
					return null;
			}

			return new DMCollectScience(Body, Situation, "", name, 1);
		}
	}

	static class DMSurveyGenerator
	{
		private static System.Random rand = DMUtils.rand;

		//Used for initial orbital and surface survey parameter
		internal static DMCollectScience fetchSurveyScience(Contract.ContractPrestige c, List<CelestialBody> cR, List<CelestialBody> cUR, DMScienceContainer DMScience)
		{
			CelestialBody body;
			ExperimentSituations targetSituation;
			ScienceSubject sub;
			string name;
			string biome = "";

			name = DMUtils.availableScience["All"].FirstOrDefault(n => n.Value == DMScience).Key;

			//Determine if the science part is available if applicable
			if (DMScience.SciPart != "None")
			{
				if (!DMUtils.partAvailable(new List<string>(1) { DMScience.SciPart }))
					return null;
			}

			body = DMUtils.nextTargetBody(c, cR, cUR);
			if (body == null)
				return null;

			//Make sure our experiment is OK
			if (DMScience.Exp == null)
				return null;

			if (!body.atmosphere && DMScience.Exp.requireAtmosphere)
				return null;
			if (((ExperimentSituations)DMScience.SitMask & ExperimentSituations.InSpaceHigh) == ExperimentSituations.InSpaceHigh && ((ExperimentSituations)DMScience.SitMask & ExperimentSituations.InSpaceLow) == ExperimentSituations.InSpaceLow)
			{
				if (rand.Next(0, 2) == 0)
					targetSituation = ExperimentSituations.InSpaceHigh;
				else
					targetSituation = ExperimentSituations.InSpaceLow;
			}
			else if (((ExperimentSituations)DMScience.SitMask & ExperimentSituations.InSpaceHigh) == ExperimentSituations.InSpaceHigh)
				targetSituation = ExperimentSituations.InSpaceHigh;
			else
				targetSituation = ExperimentSituations.InSpaceLow;

			if (DMUtils.biomeRelevant(targetSituation, DMScience.BioMask) && targetSituation != ExperimentSituations.SrfSplashed)
			{
				List<string> bList = DMUtils.fetchBiome(body, DMScience.Exp, targetSituation);
				if (bList.Count == 0)
				{
					return null;
				}
				else
				{
					biome = bList[rand.Next(0, bList.Count)];
				}
			}

			//Make sure that our chosen science subject has science remaining to be gathered
			string subId = string.Format("{0}@{1}{2}{3}", DMScience.Exp.id, body.name, targetSituation, biome.Replace(" ", ""));

			if (ResearchAndDevelopment.GetSubjects().Any(s => s.id == subId))
			{
				sub = ResearchAndDevelopment.GetSubjectByID(subId);
				if (sub.scientificValue < 0.5f)
					return null;
			}

			return new DMCollectScience(body, targetSituation, "", name, 0);
		}

		//Used for orbital survey
		internal static DMCollectScience fetchSurveyScience(CelestialBody Body, DMScienceContainer DMScience)
		{
			ExperimentSituations targetSituation;
			ScienceSubject sub;
			string name;

			name = DMUtils.availableScience["All"].FirstOrDefault(n => n.Value == DMScience).Key;

			//Determine if the science part is available if applicable
			if (DMScience.SciPart != "None")
			{
				if (!DMUtils.partAvailable(new List<string>(1) { DMScience.SciPart }))
					return null;
			}

			//Make sure our experiment is OK
			if (DMScience.Exp == null)
				return null;

			if (!Body.atmosphere && DMScience.Exp.requireAtmosphere)
				return null;
			if (((ExperimentSituations)DMScience.SitMask & ExperimentSituations.InSpaceHigh) == ExperimentSituations.InSpaceHigh && ((ExperimentSituations)DMScience.SitMask & ExperimentSituations.InSpaceLow) == ExperimentSituations.InSpaceLow)
			{
				if (rand.Next(0, 2) == 0)
					targetSituation = ExperimentSituations.InSpaceHigh;
				else
					targetSituation = ExperimentSituations.InSpaceLow;
			}
			else if (((ExperimentSituations)DMScience.SitMask & ExperimentSituations.InSpaceHigh) == ExperimentSituations.InSpaceHigh)
				targetSituation = ExperimentSituations.InSpaceHigh;
			else
				targetSituation = ExperimentSituations.InSpaceLow;

			if (DMUtils.biomeRelevant(targetSituation, DMScience.BioMask))
			{
				List<string> bList = DMUtils.fetchBiome(Body, DMScience.Exp, targetSituation);
				if (bList.Count == 0)
				{
					return null;
				}
			}
			else
			{
				string subId = string.Format("{0}@{1}{2}", DMScience.Exp.id, Body.name, targetSituation);

				if (ResearchAndDevelopment.GetSubjects().Any(s => s.id == subId))
				{
					sub = ResearchAndDevelopment.GetSubjectByID(subId);
					if (sub.scientificValue < 0.5f)
						return null;
				}
			}

			return new DMCollectScience(Body, targetSituation, "", name, 0);
		}
	}

	static class DMAsteroidGenerator
	{
		private static System.Random rand = DMUtils.rand;

		internal static DMAsteroidParameter fetchAsteroidParameter(DMScienceContainer DMScience)
		{
			ExperimentSituations targetSituation;
			string name;

			name = DMUtils.availableScience["All"].FirstOrDefault(n => n.Value == DMScience).Key;

			//Determine if the science part is available if applicable
			if (DMScience.SciPart != "None")
			{
				if (!DMUtils.partAvailable(new List<string>(1) { DMScience.SciPart }))
					return null;
			}

			if (DMScience.Exp == null)
				return null;

			if (((ExperimentSituations)DMScience.SitMask & ExperimentSituations.InSpaceLow) == ExperimentSituations.InSpaceLow)
				if (((ExperimentSituations)DMScience.SitMask & ExperimentSituations.SrfLanded) == ExperimentSituations.SrfLanded)
					if (rand.Next(0, 2) == 0)
						targetSituation = ExperimentSituations.SrfLanded;
					else
						targetSituation = ExperimentSituations.InSpaceLow;
				else
					targetSituation = ExperimentSituations.InSpaceLow;
			else if (((ExperimentSituations)DMScience.SitMask & ExperimentSituations.SrfLanded) == ExperimentSituations.SrfLanded)
				targetSituation = ExperimentSituations.SrfLanded;
			else
				return null;

			return new DMAsteroidParameter(targetSituation, name);
		}

	}

	static class DMAnomalyGenerator
	{
		private static System.Random rand = DMUtils.rand;

		internal static DMCollectScience fetchAnomalyParameter(CelestialBody Body, DMAnomalyObject City)
		{
			ExperimentSituations targetSituation;
			ScienceSubject sub;
			string anomName;

			if (Body == null)
				return null;

			if (City == null)
				return null;

			if (ResearchAndDevelopment.GetExperiment("AnomalyScan") == null)
				return null;

			if (rand.Next(0, 2) == 0)
				targetSituation = ExperimentSituations.SrfLanded;
			else
				targetSituation = ExperimentSituations.FlyingLow;

			anomName = DMagic.Part_Modules.DMAnomalyScanner.anomalyCleanup(City.Name);

			string subId = string.Format("AnomalyScan@{0}{1}{2}", Body.name, targetSituation, anomName);

			if (ResearchAndDevelopment.GetSubjects().Any(s => s.id == subId))
			{
				sub = ResearchAndDevelopment.GetSubjectByID(subId);
				if (sub.scientificValue < 0.4f)
					return null;
			}

			return new DMCollectScience(Body, targetSituation, anomName, "Anomaly Scan", 2);
		}

		internal static DMAnomalyParameter fetchAnomalyParameter(CelestialBody Body, DMScienceContainer DMScience)
		{
			ExperimentSituations targetSituation;
			List<ExperimentSituations> situations;
			string name;

			name = DMUtils.availableScience["All"].FirstOrDefault(n => n.Value == DMScience).Key;

			if (DMScience.Exp == null)
				return null;

			//Determine if the science part is available if applicable
			if (DMScience.SciPart != "None")
			{
				if (!DMUtils.partAvailable(new List<string>(1) { DMScience.SciPart }))
					return null;
			}

			if ((situations = DMUtils.availableSituationsLimited(DMScience.Exp, DMScience.SitMask, Body)).Count == 0)
				return null;
			else
			{
				targetSituation = situations[rand.Next(0, situations.Count)];
			}

			return new DMAnomalyParameter(targetSituation, name);
		}
	}
}
