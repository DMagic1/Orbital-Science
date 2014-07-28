#region license
/* DMagic Orbital Science - DMCollectContract
 * Class for generating generic science experiment contracts
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

	public class DMCollectContract : Contract
	{

		internal DMCollectScience newParam;
		private CelestialBody body;
		private ExperimentSituations targetSituation;
		private DMScienceContainer DMscience;
		private AvailablePart aPart = null;
		private string biome = "";
		private string name;
		private string subject;
		private float subjectValue;
		private System.Random rand = DMUtils.rand;
		
		protected override bool Generate()
		{
			if (!GetBodies_Reached(true, true).Contains(FlightGlobals.Bodies[1]))
				return false;
			if (ContractSystem.Instance.GetCurrentContracts<DMCollectContract>().Count() > 2)
				return false;

			//Generates the science experiment, returns null if experiment fails any check
			if ((newParam = DMCollectContractGenerator.fetchScienceContract(this.prestige, GetBodies_Reached(false, true), GetBodies_NextUnreached(4, null))) == null)
				return false;

			//Set various parameters to be used for the title, rewards, descriptions, etc...
			body = newParam.Body;
			targetSituation = newParam.Sit;
			DMscience = newParam.Container;
			biome = newParam.Biome;
			subject = newParam.Subject;
			name = newParam.Name;
			subjectValue = DMModuleScienceAnimate.fixSubjectValue(targetSituation, 1, 1, body);
			try
			{
				aPart = PartLoader.getPartInfoByName(DMscience.sciPart);
				DMUtils.DebugLog("Part: [{0}] Assigned", aPart.name);
			}
			catch
			{
				DMUtils.DebugLog("No Valid Part Associated With This Experiment");
				aPart = null;
			}

			if (DMscience.agent != "Any")
				this.agent = Contracts.Agents.AgentList.Instance.GetAgent(DMscience.agent);

			this.AddParameter(newParam, null);
			DMUtils.DebugLog("Parameter Added");
			base.SetExpiry(10 * subjectValue, Math.Max(15, 15 * subjectValue) * (float)(this.prestige + 1));
			base.SetScience(Math.Max(DMscience.exp.baseValue, (DMscience.exp.baseValue * subjectValue) / 2) * DMUtils.science, body);
			base.SetDeadlineDays(20f * subjectValue * (float)(this.prestige + 1), body);
			base.SetReputation(5f * (float)(this.prestige + 1), 10f * (float)(this.prestige + 1), body);
			base.SetFunds(100f * subjectValue * DMUtils.forward, 1000f * subjectValue * DMUtils.reward, 500f * subjectValue * DMUtils.penalty, body);
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
			return subject;
		}

		protected override string GetTitle()
		{
			if (targetSituation == ExperimentSituations.InSpaceLow || targetSituation == ExperimentSituations.InSpaceHigh)
				return string.Format("Collect {0} data from orbit around {1}", DMscience.exp.experimentTitle, body.theName);
			else if (targetSituation == ExperimentSituations.SrfLanded)
				return string.Format("Collect {0} data from the surface of {1}", DMscience.exp.experimentTitle, body.theName);
			else if (targetSituation == ExperimentSituations.SrfSplashed)
				return string.Format("Collect {0} data from the oceans of {1}", DMscience.exp.experimentTitle, body.theName);
			else if (targetSituation == ExperimentSituations.FlyingLow || targetSituation == ExperimentSituations.FlyingHigh)
				return string.Format("Collect {0} data during atmospheric flight over {1}", DMscience.exp.experimentTitle, body.theName);
			return "Stupid Code Is Stupid";
		}

		protected override string GetDescription()
		{
			string story = DMUtils.storyList[rand.Next(0, DMUtils.storyList.Count)];
			if (aPart != null)
				return string.Format(story, this.agent.Name, name, body.theName, aPart.title, targetSituation);
			else
				return string.Format(story, this.agent.Name, name, body.theName, "Kerbal", targetSituation);
		}

		protected override string GetSynopsys()
		{
			DMUtils.DebugLog("Generating Synopsis From {0} Experimental Situation", targetSituation);
			if (!string.IsNullOrEmpty(biome))
			{
				if (targetSituation == ExperimentSituations.InSpaceHigh)
					return string.Format("We need you to record some {0} observations from high orbit above the {2} around {1}", DMscience.exp.experimentTitle, body.theName, biome);
				else if (targetSituation == ExperimentSituations.InSpaceLow)
					return string.Format("We need you to record some {0} observations from low orbit above the {2} around {1}", DMscience.exp.experimentTitle, body.theName, biome);
				else if (targetSituation == ExperimentSituations.SrfLanded)
					return string.Format("We need you to record some {0} observations from the {2} while on the surface of {1}", DMscience.exp.experimentTitle, body.theName, biome);
				else if (targetSituation == ExperimentSituations.SrfSplashed)
					return string.Format("We need you to record some {0} observations from the {2} while on the oceans of {1}", DMscience.exp.experimentTitle, body.theName, biome);
				else if (targetSituation == ExperimentSituations.FlyingLow)
					return string.Format("We need you to record some {0} observations during low altitude atmospheric flight over the {2} at {1}", DMscience.exp.experimentTitle, body.theName, biome);
				else if (targetSituation == ExperimentSituations.FlyingHigh)
					return string.Format("We need you to record some {0} observations during high altitude atmospheric flight over the {2} at {1}", DMscience.exp.experimentTitle, body.theName, biome);
			}
			else
			{
				if (targetSituation == ExperimentSituations.InSpaceHigh)
					return string.Format("We need you to record some {0} observations from high orbit around {1}", DMscience.exp.experimentTitle, body.theName);
				else if (targetSituation == ExperimentSituations.InSpaceLow)
					return string.Format("We need you to record some {0} observations from low orbit around {1}", DMscience.exp.experimentTitle, body.theName);
				else if (targetSituation == ExperimentSituations.SrfLanded)
					return string.Format("We need you to record some {0} observations from the surface of {1}", DMscience.exp.experimentTitle, body.theName);
				else if (targetSituation == ExperimentSituations.SrfSplashed)
					return string.Format("We need you to record some {0} observations from the oceans of {1}", DMscience.exp.experimentTitle, body.theName);
				else if (targetSituation == ExperimentSituations.FlyingLow)
					return string.Format("We need you to record some {0} observations during low altitude atmospheric flight at {1}", DMscience.exp.experimentTitle, body.theName);
				else if (targetSituation == ExperimentSituations.FlyingHigh)
					return string.Format("We need you to record some {0} observations during high altitude atmospheric flight at {1}", DMscience.exp.experimentTitle, body.theName);
			}
			return "Fix Your Stupid Code Idiot...";
		}

		protected override string MessageCompleted()
		{
			return string.Format("You recovered {0} from {1}, well done.", DMscience.exp.experimentTitle, body.theName);
		}

		protected override void OnLoad(ConfigNode node)
		{
			DMUtils.DebugLog("Loading Contract");
			int targetBodyID, targetLocation;
			string[] scienceString = node.GetValue("Science_Subject").Split('|');
			name = scienceString[0];
			if (DMUtils.availableScience.TryGetValue(name, out DMscience))
			{
				try
				{
					aPart = PartLoader.getPartInfoByName(DMscience.sciPart);
				}
				catch
				{
					DMUtils.DebugLog("No Valid Part Associated With This Experiment");
					aPart = null;
				}
			}
			if (int.TryParse(scienceString[1], out targetBodyID))
				body = FlightGlobals.Bodies[targetBodyID];
			if (int.TryParse(scienceString[2], out targetLocation))
				targetSituation = (ExperimentSituations)targetLocation;
			biome = scienceString[3];
			subject = string.Format("{0}@{1}{2}{3}", DMscience.exp.id, body.name, targetSituation, biome.Replace(" ", ""));
		}

		protected override void OnSave(ConfigNode node)
		{
			DMUtils.DebugLog("Saving Contract");
			node.AddValue("Science_Subject", string.Format("{0}|{1}|{2}|{3}", name, body.flightGlobalsIndex, (int)targetSituation, biome));
		}

		public override bool MeetRequirements()
		{
			return true;
		}

	}

	#endregion


}