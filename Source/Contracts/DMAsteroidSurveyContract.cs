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
		private Vessel v;
		private string hash;
		private int size = 2;
		private int i, j = 0;
		private System.Random rand = DMUtils.rand;

		protected override bool Generate()
		{
			if (!GetBodies_Reached(true, true).Contains(FlightGlobals.Bodies[1]))
				return false;
			if (ContractSystem.Instance.GetCurrentContracts<DMAsteroidSurveyContract>().Count() > 0)
				return false;
			if (this.Prestige == ContractPrestige.Trivial)
				return false;

			//Make sure that the grappling device is available
			AvailablePart aPart = PartLoader.getPartInfoByName("GrapplingDevice");
			if (aPart == null)
				return false;
			if (!ResearchAndDevelopment.PartModelPurchased(aPart))
				return false;

			if ((newParams[0] = DMAsteroidGenerator.fetchAsteroidParameter(ResearchAndDevelopment.GetExperiment("dmImagingPlatform"), 2)) == null)
				return false;

			v = newParams[0].Vessel;
			size = (int)v.DiscoveryInfo.objectSize;
			hash = v.vesselName;

			//Generates new asteroid science experiments
			for (i = 1; i < 4; i++)
			{
				DMScienceContainer DMScience = DMUtils.availableScience[DMScienceType.Asteroid.ToString()].ElementAt(rand.Next(0, DMUtils.availableScience[DMScienceType.Asteroid.ToString()].Count)).Value;
				newParams[i] = DMAsteroidGenerator.fetchAsteroidParameter(v, DMScience.exp, 2);
			}

			//Add in all acceptable paramaters to the contract
			foreach (DMCollectScience DMC in newParams)
			{
				if (DMC != null)
				{
					this.AddParameter(newParams[j], null);
					DMC.SetScience(DMC.Container.exp.baseValue * 2f * DMUtils.science * (size + 1), null);
					DMC.SetFunds(2000f * DMUtils.reward * (size + 1), null);
					DMC.SetReputation(5f * DMUtils.reward * (size + 1), null);
					DMUtils.DebugLog("Asteroid Survey Parameter Added");
				}
				j++;
			}

			int a = rand.Next(0, 5);
			if (a == 0)
				this.agent = Contracts.Agents.AgentList.Instance.GetAgent("DMagic");
			else if (a == 1)
				this.agent = Contracts.Agents.AgentList.Instance.GetAgent(newParams[0].Container.agent);
			else
				this.agent = Contracts.Agents.AgentList.Instance.GetAgentRandom();

			base.SetExpiry(10, 20 * (float)(this.prestige + 1));
			base.SetDeadlineDays(40f * (float)(this.prestige + 1), null);
			base.SetReputation(newParams.Length * 5f * DMUtils.reward * (size + 1), newParams.Length * 3f * DMUtils.penalty, null);
			base.SetFunds(3000 * newParams.Length * DMUtils.forward * (size + 1), 3000 * newParams.Length * DMUtils.reward * (size + 1), 1000 * newParams.Length * DMUtils.penalty * (size + 1), null);
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
			return string.Format("Conduct a survey of the asteroid {0} by collecting multiple scienctific observations", hash);
		}

		protected override string GetDescription()
		{
			//Return a random asteroid survey backstory; use the same format as generic backstory
			string story = DMUtils.backStory["survey"][rand.Next(0, DMUtils.backStory["survey"].Count)];
			return string.Format(story, this.agent.Name, hash, "");
		}

		protected override string GetSynopsys()
		{
			DMUtils.DebugLog("Generating Asteroid Synopsis From Target Body: [{0}]", hash);
			return string.Format("Study the asteroid {0} by collecting multiple scientific observations.", hash);
		}

		protected override string MessageCompleted()
		{
			return string.Format("You completed a survey of {0}, well done.", hash);
		}

		protected override void OnLoad(ConfigNode node)
		{
			DMUtils.DebugLog("Loading Asteroid Survey Contract");
			int aSize;
			hash = node.GetValue("Asteroid_Survey_Target");
			if (HighLogic.LoadedScene != GameScenes.EDITOR)
				v = FlightGlobals.Vessels.FirstOrDefault(V => V.vesselName == hash);
			if (int.TryParse(node.GetValue("Asteroid_Size_Class"), out aSize))
				size = aSize;
		}

		protected override void OnSave(ConfigNode node)
		{
			DMUtils.DebugLog("Saving Asteroid Survey Contract");
			node.AddValue("Asteroid_Survey_Target", hash);
			node.AddValue("Asteroid_Size_Class", size);
		}

		public override bool MeetRequirements()
		{
			return true;
		}

	}
}
