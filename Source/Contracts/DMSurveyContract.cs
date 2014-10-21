#region license
/* DMagic Orbital Science - DMOrbitalSurveyContract
 * Class for generating orbital science experiment contracts
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
	public class DMSurveyContract: Contract, IDMagicContract
	{
		internal DMCollectScience[] newParams = new DMCollectScience[8];
		private CelestialBody body;
		private DMScienceContainer DMScience;
		private List<DMScienceContainer> sciList = new List<DMScienceContainer>();
		private string biome = "";
		private int i, j = 0;
		private int surveyType;
		private System.Random rand = DMUtils.rand;

		protected override bool Generate()
		{
			int total = ContractSystem.Instance.GetCurrentContracts<DMSurveyContract>().Count();
			if (total >= DMUtils.maxSurvey)
				return false;

			surveyType = rand.Next(0, 3);
			if (surveyType == 0)
			{
				//Make sure that the magnetometer is at least available
				AvailablePart aPart = PartLoader.getPartInfoByName("dmmagBoom");
				if (aPart == null)
					return false;
				if (!ResearchAndDevelopment.PartModelPurchased(aPart))
					return false;

				sciList.AddRange(DMUtils.availableScience[DMScienceType.Space.ToString()].Values);

				if (sciList.Count > 0)
				{
					DMScience = sciList[rand.Next(0, sciList.Count)];
					sciList.Remove(DMScience);
				}
				else
					return false;

				//Generates the science experiment, returns null if experiment fails any check
				if ((newParams[0] = DMSurveyGenerator.fetchSurveyScience(this.Prestige, GetBodies_Reached(false, true), GetBodies_NextUnreached(4, null), DMScience, 0)) == null)
					return false;

				body = newParams[0].Body;
				//Add an orbital parameter
				this.AddParameter(new EnterOrbit(body), null);
			}
			else if (surveyType == 1)
			{
				//Make sure that the laser is at least available
				AvailablePart aPart = PartLoader.getPartInfoByName("dmsurfacelaser");
				if (aPart == null)
					return false;
				if (!ResearchAndDevelopment.PartModelPurchased(aPart))
					return false;

				sciList.AddRange(DMUtils.availableScience[DMScienceType.Surface.ToString()].Values);

				if (sciList.Count > 0)
				{
					DMScience = sciList[rand.Next(0, sciList.Count)];
					sciList.Remove(DMScience);
				}
				else
					return false;

				if ((newParams[0] = DMSurveyGenerator.fetchSurveyScience(this.Prestige, GetBodies_Reached(false, true), GetBodies_NextUnreached(4, null), DMScience, 1)) == null)
					return false;

				body = newParams[0].Body;
				biome = newParams[0].Biome;

				if (biome == "")
				{
					List<string> biomes = DMUtils.fetchBiome(body);
					biome = biomes[rand.Next(0, biomes.Count)];
				}
				this.AddParameter(new LandOnBody(body), null);
			}
			else if (surveyType == 2)
			{
				if (this.Prestige == ContractPrestige.Trivial)
					return false;
				//Make sure that drill is at least available
				AvailablePart aPart = PartLoader.getPartInfoByName("dmbioDrill");
				if (aPart == null)
					return false;
				if (!ResearchAndDevelopment.PartModelPurchased(aPart))
					return false;

				//Duna and Eve are the easy targets
				if (this.Prestige == ContractPrestige.Significant)
				{
					if (!ProgressTracking.Instance.NodeComplete(new string[] { "Kerbin", "Escape" }))
						return false;
					if (rand.Next(0, 2) == 0)
						body = FlightGlobals.Bodies[5];
					else
						body = FlightGlobals.Bodies[6];
				}
				else if (this.Prestige == ContractPrestige.Exceptional)
				{
					//Account for mod planets and Laythe
					if (!ProgressTracking.Instance.NodeComplete(new string[] { "Jool", "Flyby" }))
						return false;
					List<CelestialBody> bList = new List<CelestialBody>();
					foreach (CelestialBody b in FlightGlobals.Bodies)
					{
						if (b.flightGlobalsIndex != 1 && b.flightGlobalsIndex != 5 && b.flightGlobalsIndex != 6 && b.flightGlobalsIndex != 8)
							if (b.atmosphere && b.pqsController != null)
								bList.Add(b);
					}
					body = bList[rand.Next(0, bList.Count)];
				}
				else
					return false;

				sciList.AddRange(DMUtils.availableScience[DMScienceType.Biological.ToString()].Values);

				if ((newParams[0] = DMSurveyGenerator.fetchBioSurveyScience(body)) == null)
					return false;

				biome = newParams[0].Biome;

				if (biome == "")
				{
					List<string> biomes = DMUtils.fetchBiome(body);
					biome = biomes[rand.Next(0, biomes.Count)];
				}

				this.AddParameter(new LandOnBody(body), null);
				this.AddParameter(new EnterOrbit(body), null);
			}
			else
				return false;

			for (j = 1; j < 7; j++)
			{
				if (sciList.Count > 0)
				{
					DMScience = sciList[rand.Next(0, sciList.Count)];
					if (surveyType == 0)
						newParams[j] = DMSurveyGenerator.fetchSurveyScience(body, DMScience);
					else if (surveyType == 1)
						newParams[j] = DMSurveyGenerator.fetchSurveyScience(body, DMScience, biome);
					else if (surveyType == 2)
						newParams[j] = DMSurveyGenerator.fetchBioSurveyScience(body, DMScience, biome);
					sciList.Remove(DMScience);
				}
				else
					newParams[j] = null;
			}

			//Add in all acceptable paramaters to the contract
			foreach (DMCollectScience DMC in newParams)
			{
				if (i > (3 + (int)this.prestige)) break;
				if (DMC != null)
				{
					this.AddParameter(DMC, "collectDMScience");
					float locationMod = GameVariables.Instance.ScoreSituation(DMUtils.convertSit(DMC.Situation), DMC.Body) * ((float)rand.Next(85, 116) / 100f);
					DMC.SetScience(DMC.Container.exp.baseValue * 0.7f * DMUtils.science * DMUtils.fixSubjectVal(DMC.Situation, 1f, body), null);
					DMC.SetFunds(4000f * DMUtils.reward * locationMod, 1500f * DMUtils.penalty * locationMod, body);
					DMC.SetReputation(15f * DMUtils.reward * locationMod, 10f * DMUtils.penalty * locationMod, body);
					i++;
					DMUtils.DebugLog("Survey Parameter Added");
				}
			}

			if (surveyType == 0 || surveyType == 1)
			{
				if (this.ParameterCount < 4)
					return false;
			}
			else if (surveyType == 2)
				if (this.ParameterCount < 5)
					return false;

			int a = rand.Next(0, 4);
			if (a == 0)
				this.agent = AgentList.Instance.GetAgent("DMagic");
			else if (a == 1)
				this.agent = AgentList.Instance.GetAgent(newParams[0].Container.agent);
			else
				this.agent = AgentList.Instance.GetAgentRandom();

			float primaryLocationMod = GameVariables.Instance.ScoreSituation(DMUtils.convertSit(newParams[0].Situation), newParams[0].Body) * ((float)rand.Next(85, 116) / 100f);
			base.SetExpiry(10f * DMUtils.deadline, 20f * DMUtils.deadline);
			base.SetDeadlineYears(1.9f * ((float)rand.Next(80, 121)) / 100f * DMUtils.deadline, body);
			base.SetReputation(newParams.Length * 8f * DMUtils.reward * primaryLocationMod, newParams.Length * 5f * DMUtils.penalty * primaryLocationMod, body);
			base.SetFunds(3000 * newParams.Length * DMUtils.forward * primaryLocationMod, 2500 * newParams.Length * DMUtils.reward * primaryLocationMod, 2000 * newParams.Length * DMUtils.penalty * primaryLocationMod, body);
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
			return string.Format("{0}{1}{2}", body.name, surveyType, this.ParameterCount);
		}

		protected override string GetTitle()
		{
			if (surveyType == 0)
				return string.Format("Conduct orbital survey of {0}; return or transmit multiple scienctific observations", body.theName);
			else if (surveyType == 1)
				return string.Format("Conduct surface survey of {0}; return or transmit multiple scienctific observations", body.theName);
			else if (surveyType == 2)
				return string.Format("Conduct a search for on-going or past biological activity on {0}; return or transmit multiple scienctific observations", body.theName);
			else
				return "Durrr";
		}

		protected override string GetNotes()
		{
			if (surveyType == 0 || surveyType == 1)
				return "Science experiments with little to no transmission value remaining may need to be returned to Kerbin to complete each parameter.";
			else
				return "Science experiments for biological activity surveys are not required to yield any science value.";
		}

		protected override string GetDescription()
		{
			string story = DMUtils.backStory["survey"][rand.Next(0, DMUtils.backStory["survey"].Count)];
			if (surveyType == 0)
				return string.Format(story, this.agent.Name, "orbital", body.theName);
			else if (surveyType == 1)
				return string.Format(story, this.agent.Name, "surface", body.theName);
			else if (surveyType == 2)
			{
				story = DMUtils.backStory["biological"][rand.Next(0, DMUtils.backStory["biological"].Count)];
				return string.Format(story, this.agent.Name, body.theName);
			}
			else
				return "Dumb Code";
		}

		protected override string GetSynopsys()
		{
			if (surveyType == 0)
				return string.Format("We would like you to conduct a detailed orbital survey of {0}. Collect and return or transmit multiple science observations.", body.theName);
			else if (surveyType == 1)
				return string.Format("We would like you to study a region on the surface of {0}. Collect and return or transmit multiple science observations.", body.theName);
			else if (surveyType == 2)
				return string.Format("We would like you to study {0} for signs of on-going or past biological activity. Collect and return or transmit multiple science observations.", body.theName);
			else
				return "Fix me :(";
		}

		protected override string MessageCompleted()
		{
			return string.Format("You completed a survey of {0}, well done.", body.theName);
		}

		protected override void OnLoad(ConfigNode node)
		{
			//if (DMScienceScenario.SciScenario != null)
			//	if (DMScienceScenario.SciScenario.contractsReload)
			//		DMUtils.resetContracts();
			int target;
			if (int.TryParse(node.GetValue("Survey_Target"), out target))
				body = FlightGlobals.Bodies[target];
			else
			{
				this.Unregister();
				ContractSystem.Instance.Contracts.Remove(this);
			}
			if (!int.TryParse(node.GetValue("Survey_Type"), out surveyType))
			{
				this.Unregister();
				ContractSystem.Instance.Contracts.Remove(this);
			}
			if (this.ParameterCount == 0)
			{
				this.Unregister();
				ContractSystem.Instance.Contracts.Remove(this);
			}
		}

		protected override void OnSave(ConfigNode node)
		{
			node.AddValue("Survey_Target", body.flightGlobalsIndex);
			node.AddValue("Survey_Type", surveyType);
		}

		public override bool MeetRequirements()
		{
			return ProgressTracking.Instance.NodeComplete(new string[] { "Kerbin", "Orbit" });
		}

	}
}
