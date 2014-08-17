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

namespace DMagic
{
	class DMOrbitalSurveyContract: Contract
	{
		internal DMCollectScience[] newParams = new DMCollectScience[4];
		private CelestialBody body;
		private DMScienceContainer DMScience;
		private List<DMScienceContainer> sciList = new List<DMScienceContainer>();
		private int j = 0;
		private System.Random rand = DMUtils.rand;
		
		protected override bool Generate()
		{
			if (!GetBodies_Reached(true, true).Contains(FlightGlobals.Bodies[1]))
				return false;
			int total = ContractSystem.Instance.GetCurrentContracts<DMOrbitalSurveyContract>().Count();
			if (total >= DMUtils.maxOrbital)
				return false;

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
			
			for (j = 1; j < 3; j++)
			{
				if (sciList.Count > 0)
				{
					DMScience = sciList[rand.Next(0, sciList.Count)];
					newParams[j] = DMSurveyGenerator.fetchSurveyScience(body, DMScience, 0);
					sciList.Remove(DMScience);
				}
				else
					newParams[j] = null;
			}

			//Add an orbital parameter
			EnterOrbit orbitParam = new EnterOrbit(body);
			this.AddParameter(orbitParam, null);

			//Add in all acceptable paramaters to the contract
			foreach(DMCollectScience DMC in newParams)
			{
				if (DMC != null)
				{
					this.AddParameter(DMC, "collectDMScience");
					DMC.SetScience(DMC.Container.exp.baseValue * 0.6f * DMUtils.science * DMUtils.fixSubjectVal(DMC.Situation, 1f, body), null);
					DMC.SetFunds(4000f * DMUtils.reward, 2000f * DMUtils.penalty, body);
					DMC.SetReputation(15f * DMUtils.reward, 10f * DMUtils.penalty, body);
					DMUtils.DebugLog("Orbital Survey Parameter Added");
				}
			}

			if (this.ParameterCount == 0)
				return false;

			int a = rand.Next(0, 5);
			if (a == 0)
				this.agent = AgentList.Instance.GetAgent("DMagic");
			else if (a == 1)
				this.agent = AgentList.Instance.GetAgent(newParams[0].Container.agent);
			else
				this.agent = AgentList.Instance.GetAgentRandom();

			base.expiryType = DeadlineType.None;
			base.SetDeadlineYears(3f, body);
			base.SetReputation(newParams.Length * 8f * DMUtils.reward, newParams.Length * 5f * DMUtils.penalty, body);
			base.SetFunds(5000 * newParams.Length * DMUtils.forward, 3000 * newParams.Length * DMUtils.reward, 2000 * newParams.Length * DMUtils.penalty, body);
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
			return body.name;
		}

		protected override string GetTitle()
		{
			return string.Format("Conduct an orbital survey of {0} by collecting multiple scienctific observations", body.theName);
		}

		protected override string GetDescription()
		{
			//Return a random survey backstory; use the same format as generic backstory
			string story = DMUtils.backStory["survey"][rand.Next(0, DMUtils.backStory["survey"].Count)];
			return string.Format(story, this.agent.Name, "orbital", body.theName);
		}

		protected override string GetSynopsys()
		{
			DMUtils.DebugLog("Generating Orbital Synopsis From Target Body: [{0}]", body.theName);
			return string.Format("Conduct an orbital survey of {0} by collecting multiple science observations.", body.theName);
		}

		protected override string MessageCompleted()
		{
			return string.Format("You completed a survey of {0}, well done.", body.theName);
		}

		protected override void OnLoad(ConfigNode node)
		{
			DMUtils.DebugLog("Loading Orbital Contract");
			int target;
			target = int.Parse(node.GetValue("Orbital_Survey_Target"));
			body = FlightGlobals.Bodies[target];
			if (this.ParameterCount == 0)
				this.Cancel();
		}

		protected override void OnSave(ConfigNode node)
		{
			DMUtils.DebugLog("Saving Orbital Contract");
			node.AddValue("Orbital_Survey_Target", body.flightGlobalsIndex);
		}

		public override bool MeetRequirements()
		{
			return true;
		}


	}
}