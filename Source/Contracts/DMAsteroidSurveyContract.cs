#region license
/* DMagic Orbital Science - DMAsteroidSurveyContract
 * Class for generating asteroid science experiment contracts
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
	class DMAsteroidSurveyContract: Contract
	{
		internal DMCollectScience[] newParams = new DMCollectScience[4];
		private DMScienceContainer DMScience;
		private List<DMScienceContainer> sciList = new List<DMScienceContainer>();
		private string hash;
		private int size, i = 0;
		private System.Random rand = DMUtils.rand;

		protected override bool Generate()
		{
			if (!GetBodies_Reached(true, true).Contains(FlightGlobals.Bodies[1]))
				return false;
			int total = ContractSystem.Instance.GetCurrentContracts<DMAsteroidSurveyContract>().Count();
			if (total >= DMUtils.maxAsteroid)
				return false;
			if (this.Prestige == ContractPrestige.Trivial)
				return false;
			else if (this.Prestige == ContractPrestige.Significant)
				size = rand.Next(0, 3);
			else if (this.Prestige == ContractPrestige.Exceptional)
				size = rand.Next(2, 4);
			else
				return false;
			hash = DMUtils.sizeHash(size);

			//Make sure that the grappling device is available
			AvailablePart aPart = PartLoader.getPartInfoByName("GrapplingDevice");
			if (aPart == null)
				return false;
			if (!ResearchAndDevelopment.PartModelPurchased(aPart))
				return false;

			sciList.AddRange(DMUtils.availableScience[DMScienceType.Asteroid.ToString()].Values);

			//Generates new asteroid science experiments
			for (i = 0; i < 4; i++)
			{
				if (sciList.Count > 0)
				{
					DMScience = sciList[rand.Next(0, sciList.Count)];
					newParams[i] = DMAsteroidGenerator.fetchAsteroidParameter(size, DMScience, 2);
					sciList.Remove(DMScience);
				}
				else
					newParams[i] = null;
			}

			//Add in all acceptable paramaters to the contract
			foreach (DMCollectScience DMC in newParams)
			{
				if (DMC != null)
				{
					this.AddParameter(DMC, "collectDMScience");
					DMC.SetScience(DMC.Container.exp.baseValue * 2f * DMUtils.science * DMUtils.asteroidSubjectVal(1f, size), null);
					DMC.SetFunds(15000f * DMUtils.reward * DMUtils.asteroidSubjectVal(1f, size), 10000f * DMUtils.penalty * (size + 1), null);
					DMC.SetReputation(15f * DMUtils.reward * (size + 1), 10f * DMUtils.penalty * (size + 1), null);
					DMUtils.DebugLog("Asteroid Survey Parameter Added");
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

			base.SetExpiry(10, 20 * (float)(this.prestige + 1));
			base.SetDeadlineYears(3f * (float)(this.prestige + 1), null);
			base.SetReputation(newParams.Length * 5f * DMUtils.reward * (size + 1), newParams.Length * 3f * DMUtils.penalty, null);
			base.SetFunds(20000 * newParams.Length * DMUtils.forward * (size + 1), 15000 * newParams.Length * DMUtils.reward * (size + 1), 15000 * newParams.Length * DMUtils.penalty * (size + 1), null);
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
			return hash;
		}

		protected override string GetTitle()
		{
			return string.Format("Conduct a survey of a {0} asteroid by collecting multiple scienctific observations", hash);
		}

		protected override string GetDescription()
		{
			//Return a random asteroid survey backstory; use the same format as generic backstory
			string story = DMUtils.backStory["asteroid"][rand.Next(0, DMUtils.backStory["asteroid"].Count)];
			return string.Format(story, this.agent.Name, hash);
		}

		protected override string GetSynopsys()
		{
			DMUtils.DebugLog("Generating Asteroid Synopsis From Target Body: [{0}]", hash);
			return string.Format("Study the a {0} asteroid by collecting multiple scientific observations.", hash);
		}

		protected override string MessageCompleted()
		{
			return string.Format("You completed a survey of a {0} asteroid, well done.", hash);
		}

		protected override void OnLoad(ConfigNode node)
		{
			DMUtils.DebugLog("Loading Asteroid Survey Contract");
			hash = node.GetValue("Asteroid_Size_Class");
			if (this.ParameterCount == 0)
				this.Cancel();
		}

		protected override void OnSave(ConfigNode node)
		{
			DMUtils.DebugLog("Saving Asteroid Survey Contract");
			node.AddValue("Asteroid_Size_Class", hash);
		}

		public override bool MeetRequirements()
		{
			return true;
		}

	}
}
