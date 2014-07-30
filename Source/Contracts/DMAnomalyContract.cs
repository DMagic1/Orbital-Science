#region license
/* DMagic Orbital Science - DMAnomalyContract
 * Class for generating anomaly science experiment contracts
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
using Contracts.Parameters;
using Contracts.Agents;

namespace DMagic
{
	class DMAnomalyContract: Contract
	{
		private DMCollectScience newParam;
		private CelestialBody body;
		private ExperimentSituations targetSituation;
		private DMScienceContainer DMscience;
		private AvailablePart aPart = null;
		private PQSCity targetAnomaly;
		private string biome = "";
		private string name;
		private string subject;
		private List<PQSCity> aList = new List<PQSCity>();
		private System.Random rand = DMUtils.rand;

		protected override bool Generate()
		{
			//return false;
			if (!GetBodies_Reached(true, true).Contains(FlightGlobals.Bodies[1]))
				return false;
			if (ContractSystem.Instance.GetCurrentContracts<DMCollectContract>().Count() > 1)
				return false;

			//Make sure that the anomaly scanner is available
			AvailablePart aPart = PartLoader.getPartInfoByName("dmAnomScanner");
			if (aPart == null)
				return false;
			if (!ResearchAndDevelopment.PartModelPurchased(aPart))
				return false;

			//Kerbin or Mun Anomalies for trivial contracts
			if (this.Prestige == ContractPrestige.Trivial)
			{
				if (rand.Next(0, 3) == 0)
					body = FlightGlobals.Bodies[1];
				else
					body = FlightGlobals.Bodies[2];
			}
			//Minmus and Duna are next
			else if (this.Prestige == ContractPrestige.Significant)
			{
				if (rand.Next(0, 2) == 0)
					body = FlightGlobals.Bodies[3];
				else
					body = FlightGlobals.Bodies[6];
			}
			//Vall, Tylo, and Bop are last
			else if (this.Prestige == ContractPrestige.Exceptional)
			{
				int i = rand.Next(0, 3);
				if (i == 0)
					body = FlightGlobals.Bodies[10];
				else if (i == 1)
					body = FlightGlobals.Bodies[11];
				else if (i == 2)
					body = FlightGlobals.Bodies[12];
				else
					return false;
			}
			else
				return false;

			//Build a list of anomalies for the target planet
			PQSCity[] Cities = UnityEngine.Object.FindObjectsOfType(typeof(PQSCity)) as PQSCity[];
			foreach (PQSCity city in Cities)
			{
				if (city.transform.parent.name == body.name)
					aList.Add(city);
			}

			//Select random anomaly
			targetAnomaly = aList[rand.Next(0, aList.Count)];
			DMUtils.DebugLog("Anomaly [{0}] Selected", targetAnomaly.name);

			//Assign primary anomaly contract parameter
			if ((newParam = DMAnomalyGenerator.fetchAnomalyParameter(body, targetAnomaly)) == null)
				return false;

			if (DMscience.agent != "Any")
				this.agent = Contracts.Agents.AgentList.Instance.GetAgent(DMscience.agent);

			this.AddParameter(newParam, null);

			base.SetExpiry(10, 20 * (float)(this.prestige + 1));
			base.SetScience(DMscience.exp.baseValue * 0.8f * DMUtils.science, body);
			base.SetDeadlineDays(20f * (float)(this.prestige + 1), body);
			base.SetReputation(5f * (float)(this.prestige + 1) * DMUtils.reward, 10f * (float)(this.prestige + 1) * DMUtils.penalty, body);
			base.SetFunds(600f * DMUtils.forward, 500f * DMUtils.reward, 500f * DMUtils.penalty, body);
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
			
			return "Stupid Code Is Stupid";
		}

		protected override string GetDescription()
		{
			string story = DMUtils.backStory["generic"][rand.Next(0, DMUtils.backStory["generic"].Count)];
			if (aPart != null)
				return string.Format(story, this.agent.Name, name, body.theName, aPart.title, targetSituation);
			else
				return string.Format(story, this.agent.Name, name, body.theName, "Kerbal", targetSituation);
		}

		protected override string GetSynopsys()
		{
			DMUtils.DebugLog("Generating Synopsis From {0} Experimental Situation", targetSituation);
			
			return "Fix Your Stupid Code Idiot...";
		}

		protected override string MessageCompleted()
		{
			return string.Format("You recovered {0} from {1}, well done.", DMscience.exp.experimentTitle, body.theName);
		}

		protected override void OnLoad(ConfigNode node)
		{
			DMUtils.DebugLog("Loading Anomaly Contract");
			int targetBodyID, targetLocation;
			string[] scienceString = node.GetValue("Science_Subject").Split('|');
			name = scienceString[0];
			if (DMUtils.availableScience["All"].TryGetValue(name, out DMscience))
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
			DMUtils.DebugLog("Saving Anomaly Contract");
			node.AddValue("Science_Subject", string.Format("{0}|{1}|{2}|{3}", name, body.flightGlobalsIndex, (int)targetSituation, biome));
		}

		public override bool MeetRequirements()
		{
			return true;
		}

	}
}
