﻿#region license
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
using DMagic.Parameters;

namespace DMagic.Contracts
{
	public class DMSurveyContract : Contract
	{
		private DMCollectScience[] newParams = new DMCollectScience[8];
		private CelestialBody body;
		private DMScienceContainer DMScience;
		private List<DMScienceContainer> sciList = new List<DMScienceContainer>();
		private System.Random rand = DMUtils.rand;

		protected override bool Generate()
		{
			DMSurveyContract[] surveyContracts = ContractSystem.Instance.GetCurrentContracts<DMSurveyContract>();
			int offers = 0;
			int active = 0;
			int maxOffers = DMUtils.maxSurveyOffered;
			int maxActive = DMUtils.maxSurveyActive;

			for (int i = 0; i < surveyContracts.Length; i++)
			{
				DMSurveyContract s = surveyContracts[i];
				if (s.ContractState == State.Offered)
					offers++;
				else if (s.ContractState == State.Active)
					active++;
			}

			if (offers >= maxOffers)
				return false;
			if (active >= maxActive)
				return false;

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
			if ((newParams[0] = DMSurveyGenerator.fetchSurveyScience(this.Prestige, GetBodies_Reached(false, false), GetBodies_NextUnreached(4, null), DMScience)) == null)
				return false;

			body = newParams[0].Body;

			//Add an orbital parameter to difficult contracts
			if (this.Prestige == ContractPrestige.Exceptional)
				this.AddParameter(new EnterOrbit(body));

			for (int j = 1; j < 8; j++)
			{
				if (sciList.Count > 0)
				{
					DMScience = sciList[rand.Next(0, sciList.Count)];
					newParams[j] = DMSurveyGenerator.fetchSurveyScience(body, DMScience);
					sciList.Remove(DMScience);
				}
				else
					newParams[j] = null;
			}

			//Add the science collection parent parameter
			DMCompleteParameter DMcp = new DMCompleteParameter(0, 1);
			this.AddParameter(DMcp);

			int limit = 1;

			//Add in all acceptable paramaters to the contract
			foreach (DMCollectScience DMC in newParams)
			{
				if (limit > (3 + (int)this.Prestige))
					break;
				if (DMC != null)
				{
					DMcp.addToSubParams(DMC, "CollectScience");
					float locationMod = GameVariables.Instance.ScoreSituation(DMUtils.convertSit(DMC.Situation), DMC.Body) * ((float)rand.Next(85, 116) / 100f);
					DMC.SetScience(DMC.Container.Exp.baseValue * 0.2f * DMUtils.science * DMUtils.fixSubjectVal(DMC.Situation, 1f, body), null);
					DMC.SetFunds(3500f * DMUtils.reward * locationMod, body);
					limit++;
				}
			}

			if (DMcp.ParameterCount < 3)
				return false;

			int a = rand.Next(0, 4);
			if (a == 0)
				this.agent = AgentList.Instance.GetAgent("DMagic");
			else if (a == 1)
				this.agent = AgentList.Instance.GetAgent(newParams[0].Container.Agent);
			else
				this.agent = AgentList.Instance.GetAgentRandom();

			float primaryLocationMod = GameVariables.Instance.ScoreSituation(DMUtils.convertSit(newParams[0].Situation), newParams[0].Body) * ((float)rand.Next(85, 116) / 100f);
			base.SetExpiry(10f * DMUtils.deadline, 20f * DMUtils.deadline);
			base.SetDeadlineYears(1.7f * ((float)rand.Next(80, 121)) / 100f * DMUtils.deadline, body);
			base.SetReputation(1.9f * DMcp.ParameterCount * DMUtils.reward * primaryLocationMod, 1.5f * DMcp.ParameterCount * DMUtils.penalty * primaryLocationMod, null);
			base.SetFunds(8500 * DMcp.ParameterCount * DMUtils.forward * primaryLocationMod, 10500 * DMcp.ParameterCount * DMUtils.reward * primaryLocationMod, 7500 * DMcp.ParameterCount * DMUtils.penalty * primaryLocationMod, body);
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
			return string.Format("{0}{1}", body.name, this.ParameterCount);
		}

		protected override string GetTitle()
		{
			return string.Format("Conduct an orbital survey of {0}", body.theName);
		}

		protected override string GetDescription()
		{
			string story = DMUtils.backStory["survey"][rand.Next(0, DMUtils.backStory["survey"].Count)];
			return string.Format(story, this.agent.Name, "orbital", body.theName);
		}

		protected override string GetSynopsys()
		{
			return string.Format("We would like you to conduct a detailed orbital survey of {0}. Collect and return or transmit multiple scientific observations.", body.theName);
		}

		protected override string MessageCompleted()
		{
			return string.Format("You completed a survey of {0}, well done.", body.theName);
		}

		protected override void OnLoad(ConfigNode node)
		{
			int target;
			if (int.TryParse(node.GetValue("Survey_Target"), out target))
				body = FlightGlobals.Bodies[target];
			else
			{
				this.Unregister();
				ContractSystem.Instance.Contracts.Remove(this);
				return;
			}
			if (this.ParameterCount == 0)
			{
				DMUtils.Logging("No Parameters Loaded For This Survey Contract; Removing Now...");
				this.Unregister();
				ContractSystem.Instance.Contracts.Remove(this);
				return;
			}
		}

		protected override void OnSave(ConfigNode node)
		{
			node.AddValue("Survey_Target", body.flightGlobalsIndex);
		}

		public override bool MeetRequirements()
		{
			return ProgressTracking.Instance.NodeComplete(new string[] { "Kerbin", "Orbit" });
		}

		/// <summary>
		/// Used externally to return the target Celestial Body
		/// </summary>
		/// <param name="cP">Instance of the requested Contract</param>
		/// <returns>Celestial Body object</returns>
		public static CelestialBody TargetBody(Contract c)
		{
			if (c == null || c.GetType() != typeof(DMSurveyContract))
				return null;

			DMSurveyContract Instance = (DMSurveyContract)c;
			return Instance.body;
		}

	}
}
