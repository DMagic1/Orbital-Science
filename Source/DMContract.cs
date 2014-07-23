#region license
/* DMagic Orbital Science - DMContract
 * Classes for generating generic science experiment contracts
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
using System.Text;
using UnityEngine;
using Contracts;
using Contracts.Parameters;
using KSPAchievements;

namespace DMagic
{

	#region Contract Generator

	public class DMContract : Contract
	{

		private CelestialBody body = null;
		private ScienceExperiment exp = null;
		private ScienceSubject sub = null;
		private ExperimentSituations targetSituation;
		private DMcontractScience DMscience;
		private AvailablePart aPart = null;
		private ProtoTechNode pTechNode = null;
		private List<ExperimentSituations> situations;
		private string biome = "";
		private string name;
		private System.Random rand = DMUtils.rand;

		#region overrides

		protected override bool Generate()
		{
			DMscience = DMUtils.availableScience.ElementAt(rand.Next(0, DMUtils.availableScience.Count)).Value;
			name = DMUtils.availableScience.FirstOrDefault(n => n.Value == DMscience).Key;
			DMUtils.DebugLog("Generating Contract Now");
			if (DMscience.sciPart != "None")
			{
				DMUtils.DebugLog("Standard Experiment Generating");
				pTechNode = ResearchAndDevelopment.Instance.GetTechState(DMscience.sciNode);
				if (pTechNode == null)
					return false;
				else
				{
					DMUtils.DebugLog("Tech Node Found");
					if (pTechNode.state != RDTech.State.Available)
						return false;
					else
					{
						DMUtils.DebugLog("Tech Node Researched");
						aPart = pTechNode.partsPurchased.FirstOrDefault(p => p.name == DMscience.sciPart);
						if (aPart == null)
							return false;
						DMUtils.DebugLog("Part: [{0}] Purchased", aPart.name);
					}
				}
			}

			if (body == null)
			{
				body = nextTargetBody();
				if (body == null)
					return false;
			}
			if (exp == null)
			{
				exp = DMscience.exp;
				if (exp == null)
					return false;
			}

			situations = availableSituations(DMscience.sitMask, body);

			if (situations.Count == 0)
				return false;
			else
			{
				DMUtils.DebugLog("Acceptable Situations Found");
				targetSituation = situations[rand.Next(0, situations.Count)];
				DMUtils.DebugLog("Experimental Situation: {0}", targetSituation);
			}

			if (biomeRelevant(targetSituation, DMscience.bioMask))
			{
				DMUtils.DebugLog("Checking For Biome Usage");
				int i = rand.Next(0, 2);
				if (i == 0)
					biome = fetchBiome(body);
			}

			sub = ResearchAndDevelopment.GetExperimentSubject(exp, targetSituation, body, biome);

			if (sub == null)
			{
				DMUtils.DebugLog("No Acceptable Science Subject Found");
				return false;
			}
			else
			{
				DMUtils.DebugLog("Acceptable Science Subject Found");
				sub.subjectValue = DMModuleScienceAnimate.fixSubjectValue(targetSituation, sub.subjectValue, 1f, body);
				if (sub.scientificValue < 0.4f)
					return false;
			}

			if (DMscience.agent != "Any")
				this.agent = Contracts.Agents.AgentList.Instance.GetAgent(DMscience.agent);

			this.AddParameter(new DMCollectScience(body, targetSituation, sub, exp, biome), null);
			DMUtils.DebugLog("Parameter Added");
			base.SetExpiry(10 * sub.subjectValue, Math.Max(15, 15 * sub.subjectValue) * (float)(this.prestige + 1));
			base.SetScience(Math.Max(exp.baseValue, (exp.baseValue * sub.subjectValue) / 2) * DMUtils.science, body);
			base.SetDeadlineDays(20f * sub.subjectValue * (float)(this.prestige + 1), body);
			base.SetReputation(5f * (float)(this.prestige + 1), 10f * (float)(this.prestige + 1), body);
			base.SetFunds(100f * sub.subjectValue * DMUtils.forward, 1000f * sub.subjectValue * DMUtils.reward, 500f * sub.subjectValue * DMUtils.penalty, body);
			return true;
		}

		public override bool CanBeCancelled()
		{
			return true;
		}

		public override bool CanBeDeclined()
		{
			return true;
		}

		protected override string GetHashString()
		{
			return sub.id;
		}

		protected override string GetTitle()
		{
			if (targetSituation == ExperimentSituations.InSpaceLow || targetSituation == ExperimentSituations.InSpaceHigh)
				return string.Format("Collect {0} data from orbit around {1}", exp.experimentTitle, body.theName);
			else if (targetSituation == ExperimentSituations.SrfLanded)
				return string.Format("Collect {0} data from the surface of {1}", exp.experimentTitle, body.theName);
			else if (targetSituation == ExperimentSituations.SrfSplashed)
				return string.Format("Collect {0} data from the oceans of {1}", exp.experimentTitle, body.theName);
			else if (targetSituation == ExperimentSituations.FlyingLow || targetSituation == ExperimentSituations.FlyingHigh)
				return string.Format("Collect {0} data during atmospheric flight over {1}", exp.experimentTitle, body.theName);
			return "Stupid Code Is Stupid";
		}

		protected override string GetDescription()
		{
			string story = DMUtils.storyList[rand.Next(0, DMUtils.storyList.Count)];
			return string.Format(story, this.agent.Name, DMUtils.availableScience.FirstOrDefault(v => v.Value == DMscience).Key, body.theName, aPart.title, targetSituation); ;
		}

		protected override string GetSynopsys()
		{
			DMUtils.DebugLog("Generating Synopsis From {0} Experimental Situation", targetSituation);
			if (!string.IsNullOrEmpty(biome))
			{
				if (targetSituation == ExperimentSituations.InSpaceHigh)
					return string.Format("We need you to record some {0} observations from high orbit above the {2} around {1}", exp.experimentTitle, body.theName, biome);
				else if (targetSituation == ExperimentSituations.InSpaceLow)
					return string.Format("We need you to record some {0} observations from low orbit above the {2} around {1}", exp.experimentTitle, body.theName, biome);
				else if (targetSituation == ExperimentSituations.SrfLanded)
					return string.Format("We need you to record some {0} observations from the {2} while on the surface of {1}", exp.experimentTitle, body.theName, biome);
				else if (targetSituation == ExperimentSituations.SrfSplashed)
					return string.Format("We need you to record some {0} observations from the {2} while on the oceans of {1}", exp.experimentTitle, body.theName, biome);
				else if (targetSituation == ExperimentSituations.FlyingLow)
					return string.Format("We need you to record some {0} observations during low altitude atmospheric flight over the {2} at {1}", exp.experimentTitle, body.theName, biome);
				else if (targetSituation == ExperimentSituations.FlyingHigh)
					return string.Format("We need you to record some {0} observations during high altitude atmospheric flight over the {2} at {1}", exp.experimentTitle, body.theName, biome);
			}
			else
			{
				if (targetSituation == ExperimentSituations.InSpaceHigh)
					return string.Format("We need you to record some {0} observations from high orbit around {1}", exp.experimentTitle, body.theName);
				else if (targetSituation == ExperimentSituations.InSpaceLow)
					return string.Format("We need you to record some {0} observations from low orbit around {1}", exp.experimentTitle, body.theName);
				else if (targetSituation == ExperimentSituations.SrfLanded)
					return string.Format("We need you to record some {0} observations from the surface of {1}", exp.experimentTitle, body.theName);
				else if (targetSituation == ExperimentSituations.SrfSplashed)
					return string.Format("We need you to record some {0} observations from the oceans of {1}", exp.experimentTitle, body.theName);
				else if (targetSituation == ExperimentSituations.FlyingLow)
					return string.Format("We need you to record some {0} observations during low altitude atmospheric flight at {1}", exp.experimentTitle, body.theName);
				else if (targetSituation == ExperimentSituations.FlyingHigh)
					return string.Format("We need you to record some {0} observations during high altitude atmospheric flight at {1}", exp.experimentTitle, body.theName);
			}
			return "Fix Your Stupid Code Idiot...";
		}

		protected override string MessageCompleted()
		{
			return string.Format("You recovered {0} from {1}, well done.", exp.experimentTitle, body.theName);
		}

		protected override void OnLoad(ConfigNode node)
		{
			DMUtils.DebugLog("Loading Contract");
			int targetBodyID, targetLocation;
			if (int.TryParse(node.GetValue("ScienceTarget"), out targetBodyID))
				body = FlightGlobals.Bodies[targetBodyID];
			ScienceSubject trySub = ResearchAndDevelopment.GetSubjectByID(node.GetValue("ScienceSubject"));
			if (trySub != null)
				sub = trySub;
			name = node.GetValue("ScienceExperiment");
			if (DMUtils.availableScience.TryGetValue(name, out DMscience))
			{
				aPart = ResearchAndDevelopment.Instance.GetTechState(DMscience.sciNode).partsPurchased.FirstOrDefault(p => p.name == DMscience.sciPart);
				ScienceExperiment tryExp = DMscience.exp;
				if (tryExp != null)
					exp = tryExp;
			}
			if (int.TryParse(node.GetValue("ScienceLocation"), out targetLocation))
				targetSituation = (ExperimentSituations)targetLocation;
			biome = node.GetValue("Biome");
		}

		protected override void OnSave(ConfigNode node)
		{
			DMUtils.DebugLog("Saving Contract");
			node.AddValue("ScienceTarget", body.flightGlobalsIndex);
			node.AddValue("ScienceSubject", sub.id);
			node.AddValue("ScienceExperiment", name);
			node.AddValue("ScienceLocation", (int)targetSituation);
			node.AddValue("Biome", biome);
		}

		public override bool MeetRequirements()
		{
			return true;
		}

		#endregion

		#region Utilities

		private CelestialBody nextTargetBody()
		{
			DMUtils.DebugLog("Searching For Acceptable Body");
			List<CelestialBody> bList;
			if (this.prestige == ContractPrestige.Trivial)
				return FlightGlobals.Bodies[rand.Next(1, 4)];
			else if (this.prestige == ContractPrestige.Significant)
			{
				bList = GetBodies_Reached(false, true);
				if (bList.Count == 0)
					return null;
				return bList[rand.Next(0, bList.Count)];
			}
			else if (this.prestige == ContractPrestige.Exceptional)
			{
				bList = GetBodies_NextUnreached(4, null);
				if (bList.Contains(FlightGlobals.Bodies[1]))
					bList.Remove(FlightGlobals.Bodies[1]);
				if (bList.Contains(FlightGlobals.Bodies[2]))
					bList.Remove(FlightGlobals.Bodies[2]);
				if (bList.Contains(FlightGlobals.Bodies[3]))
					bList.Remove(FlightGlobals.Bodies[3]);
				return bList[rand.Next(0, bList.Count)];
			}
			return null;
		}

		private List<ExperimentSituations> availableSituations(int i, CelestialBody b)
		{
			DMUtils.DebugLog("Finding Situations");
			List<ExperimentSituations> expSitList = new List<ExperimentSituations>();
			if (((ExperimentSituations)i & ExperimentSituations.FlyingHigh) == ExperimentSituations.FlyingHigh && b.atmosphere)
				expSitList.Add(ExperimentSituations.FlyingHigh);
			if (((ExperimentSituations)i & ExperimentSituations.FlyingLow) == ExperimentSituations.FlyingLow && b.atmosphere)
				expSitList.Add(ExperimentSituations.FlyingLow);
			if (((ExperimentSituations)i & ExperimentSituations.InSpaceHigh) == ExperimentSituations.InSpaceHigh && !exp.requireAtmosphere)
				expSitList.Add(ExperimentSituations.InSpaceHigh);
			if (((ExperimentSituations)i & ExperimentSituations.InSpaceLow) == ExperimentSituations.InSpaceLow && !exp.requireAtmosphere)
				expSitList.Add(ExperimentSituations.InSpaceLow);
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

		private bool biomeRelevant(ExperimentSituations s, int i)
		{
			if ((i & (int)s) == 0)
				return false;
			else
				return true;
		}

		private string fetchBiome(CelestialBody b)
		{
			DMUtils.DebugLog("Searching For Biomes");
			string s = "";
			if (b.BiomeMap == null || b.BiomeMap.Map == null)
				return s;
			else
				s = b.BiomeMap.Attributes[rand.Next(0, b.BiomeMap.Attributes.Length)].name;
			DMUtils.DebugLog("Found Biome: {0}", s);
			return s;
		}

		#endregion

	}

	#endregion

	#region Contract Parameter

	public class DMCollectScience : CollectScience
	{
		public CelestialBody body;
		public ExperimentSituations scienceLocation;
		public ScienceSubject subject;
		public ScienceExperiment exp;
		public string biomeName;

		public DMCollectScience()
		{
		}

		public DMCollectScience(CelestialBody target, ExperimentSituations location, ScienceSubject Subject, ScienceExperiment Exp, string BiomeName)
		{
			body = target;
			scienceLocation = location;
			subject = Subject;
			exp = Exp;
			biomeName = BiomeName;
		}

		protected override string GetHashString()
		{
			return subject.id;
		}

		protected override string GetTitle()
		{
			if (!string.IsNullOrEmpty(biomeName))
			{
				if (scienceLocation == ExperimentSituations.InSpaceHigh)
					return string.Format("Collect {0} data from high orbit around {1}'s {2}", exp.experimentTitle, body.theName, biomeName);
				else if (scienceLocation == ExperimentSituations.InSpaceLow)
					return string.Format("Collect {0} data from low orbit around {1}'s {2}", exp.experimentTitle, body.theName, biomeName);
				else if (scienceLocation == ExperimentSituations.SrfLanded)
					return string.Format("Collect {0} data from the surface at {1}'s {2}", exp.experimentTitle, body.theName, biomeName);
				else if (scienceLocation == ExperimentSituations.SrfSplashed)
					return string.Format("Collect {0} data from the oceans at {1}'s {2}", exp.experimentTitle, body.theName, biomeName);
				else if (scienceLocation == ExperimentSituations.FlyingHigh)
					return string.Format("Collect {0} data during high altitude flight over {1}'s {2}", exp.experimentTitle, body.theName, biomeName);
				else if (scienceLocation == ExperimentSituations.FlyingLow)
					return string.Format("Collect {0} data during low altitude flight over {1}'s {2}", exp.experimentTitle, body.theName, biomeName);
			}
			else
			{
				if (scienceLocation == ExperimentSituations.InSpaceHigh)
					return string.Format("Collect {0} data from high orbit around {1}", exp.experimentTitle, body.theName);
				else if (scienceLocation == ExperimentSituations.InSpaceLow)
					return string.Format("Collect {0} data from low orbit around {1}", exp.experimentTitle, body.theName);
				else if (scienceLocation == ExperimentSituations.SrfLanded)
					return string.Format("Collect {0} data from the surface of {1}", exp.experimentTitle, body.theName);
				else if (scienceLocation == ExperimentSituations.SrfSplashed)
					return string.Format("Collect {0} data from the oceans of {1}", exp.experimentTitle, body.theName);
				else if (scienceLocation == ExperimentSituations.FlyingHigh)
					return string.Format("Collect {0} data during high altitude flight at {1}", exp.experimentTitle, body.theName);
				else if (scienceLocation == ExperimentSituations.FlyingLow)
					return string.Format("Collect {0} data during low altitude flight at {1}", exp.experimentTitle, body.theName);
			}
			return "Stupid Code Is Stupid";
		}

		protected override void OnRegister()
		{
			GameEvents.OnScienceRecieved.Add(scienceRecieve);
		}

		protected override void OnUnregister()
		{
			GameEvents.OnScienceRecieved.Remove(scienceRecieve);
		}

		protected override void OnSave(ConfigNode node)
		{
			DMUtils.DebugLog("Saving Contract Parameter");
			node.AddValue("ScienceTarget", body.flightGlobalsIndex);
			node.AddValue("ScienceSubject", subject.id);
			node.AddValue("ScienceExperiment", exp.id);
			node.AddValue("ScienceLocation", (int)scienceLocation);
			node.AddValue("Biome", biomeName);
		}

		protected override void OnLoad(ConfigNode node)
		{
			DMUtils.DebugLog("Loading Contract Parameter");
			int targetBodyID, targetLocation;
			if (int.TryParse(node.GetValue("ScienceTarget"), out targetBodyID))
				body = FlightGlobals.Bodies[targetBodyID];
			ScienceSubject trySub = ResearchAndDevelopment.GetSubjectByID(node.GetValue("ScienceSubject"));
			if (trySub != null)
				subject = trySub;
			ScienceExperiment tryExp = ResearchAndDevelopment.GetExperiment(node.GetValue("ScienceExperiment"));
			if (tryExp != null)
				exp = tryExp;
			if (int.TryParse(node.GetValue("ScienceLocation"), out targetLocation))
				scienceLocation = (ExperimentSituations)targetLocation;
			biomeName = node.GetValue("Biome");
		}

		private void scienceRecieve(float sci, ScienceSubject sub)
		{
			DMUtils.DebugLog("New Science Results Collected With ID: {0}", sub.id);
			DMUtils.DebugLog("Comparing To Target Science With ID: {0}", subject.id);
			if (!string.IsNullOrEmpty(biomeName))
			{
				if (sub.id == subject.id)
				{
					DMUtils.DebugLog("Contract Complete");
					base.SetComplete();
				}
			}
			else
			{
				DMUtils.DebugLog("Figure Something Out Dummy!!!");
				string clippedSub = sub.id.Replace("@", "");
				string clippedTargetSub = subject.id.Replace("@", "");
				DMUtils.DebugLog("Comparing New Strings [{0}] And [{1}]", clippedSub, clippedTargetSub);
				if (clippedSub.StartsWith(clippedTargetSub))
				{
					DMUtils.DebugLog("Contract Complete");
					base.SetComplete();
				}
			}
		}

	}

	#endregion

}