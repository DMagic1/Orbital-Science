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
using DMagic.Parameters;

namespace DMagic.Contracts
{
	public class DMAsteroidSurveyContract: Contract
	{
		private DMAsteroidParameter[] newParams = new DMAsteroidParameter[6];
		private DMScienceContainer DMScience;
		private List<DMScienceContainer> sciList = new List<DMScienceContainer>();
		private string hash;
		private int size, i = 0;
		private System.Random rand = DMUtils.rand;

		protected override bool Generate()
		{
			DMAsteroidSurveyContract[] astContracts = ContractSystem.Instance.GetCurrentContracts<DMAsteroidSurveyContract>();
			int offers = 0;
			int active = 0;
			int maxOffers = DMContractDefs.DMAsteroid.maxOffers;
			int maxActive = DMContractDefs.DMAsteroid.maxActive;

			for (int i = 0; i < astContracts.Length; i++)
			{
				DMAsteroidSurveyContract a = astContracts[i];
				if (a.ContractState == State.Offered)
					offers++;
				else if (a.ContractState == State.Active)
					active++;
			}

			if (offers >= maxOffers)
				return false;
			if (active >= maxActive)
				return false;

			switch (prestige)
			{
				case ContractPrestige.Trivial:
					return false;
				case ContractPrestige.Significant:
					size = rand.Next(0, 3);
					break;
				case ContractPrestige.Exceptional:
					size = rand.Next(2, 4);
					break;
			}

			hash = DMUtils.sizeHash(size);

			//Make sure that the grappling device is available
			if (!DMUtils.partAvailable(new List<string>(1) { "GrapplingDevice" }))
				return false;

			sciList.AddRange(DMUtils.availableScience[DMScienceType.Asteroid.ToString()].Values);

			//Generates new asteroid science experiments
			for (i = 0; i < 6; i++)
			{
				if (sciList.Count > 0)
				{
					DMScience = sciList[rand.Next(0, sciList.Count)];
					newParams[i] = DMAsteroidGenerator.fetchAsteroidParameter(DMScience);
					sciList.Remove(DMScience);
				}
				else
					newParams[i] = null;
			}

			//Add the science collection parent parameter
			DMCompleteParameter DMcp = new DMCompleteParameter(2, 1);
			this.AddParameter(DMcp);

			int limit = 0;
			int maxRequests = 1;

			switch (prestige)
			{
				case ContractPrestige.Trivial:
					maxRequests = DMContractDefs.DMAsteroid.trivialScienceRequests;
					break;
				case ContractPrestige.Significant:
					maxRequests = DMContractDefs.DMAsteroid.significantScienceRequests;
					break;
				case ContractPrestige.Exceptional:
					maxRequests = DMContractDefs.DMAsteroid.exceptionalScienceRequests;
					break;
			}

			//Add in all acceptable paramaters to the contract
			foreach (DMAsteroidParameter DMAP in newParams)
			{
				if (limit > maxRequests)
					break;
				if (DMAP != null)
				{
					if (DMAP.Container == null)
						continue;

					DMcp.addToSubParams(DMAP);
					float modifier = ((float)rand.Next(85, 116) / 100f);
					DMAP.SetScience(DMAP.Container.Exp.baseValue * DMContractDefs.DMAsteroid.Science.ParamReward * (DMUtils.asteroidSubjectVal(size) / 2), null);
					DMAP.SetFunds(DMContractDefs.DMAsteroid.Funds.ParamReward * modifier, DMContractDefs.DMAsteroid.Funds.ParamFailure * DMUtils.asteroidSubjectVal(size) * modifier, null);
					DMAP.SetReputation(DMContractDefs.DMAsteroid.Reputation.ParamReward * modifier, DMContractDefs.DMAsteroid.Reputation.ParamFailure * modifier, null);
					limit++;
				}
			}

			if (DMcp.ParameterCount < 3)
				return false;

			float primaryModifier = ((float)rand.Next(85, 116) / 100f);

			float Mod = primaryModifier * DMcp.ParameterCount;

			this.agent = AgentList.Instance.GetAgent("DMagic");

			if (this.agent == null)
				this.agent = AgentList.Instance.GetAgentRandom();

			base.SetExpiry(DMContractDefs.DMAsteroid.Expire.MinimumExpireDays, DMContractDefs.DMAsteroid.Expire.MaximumExpireDays);
			base.SetDeadlineYears(DMContractDefs.DMAsteroid.Expire.DeadlineYears * primaryModifier, null);
			base.SetReputation(DMContractDefs.DMAsteroid.Reputation.BaseReward * primaryModifier, DMContractDefs.DMAsteroid.Reputation.BaseFailure * primaryModifier, null);
			base.SetFunds(DMContractDefs.DMAsteroid.Funds.BaseAdvance * Mod, DMContractDefs.DMAsteroid.Funds.BaseReward * Mod, DMContractDefs.DMAsteroid.Funds.BaseFailure * Mod, null);
			base.SetScience(DMContractDefs.DMAsteroid.Science.BaseReward * primaryModifier, null);
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
			return string.Format("Conduct a scientific survey of a {0} asteroid", hash);
		}

		protected override string GetNotes()
		{
			return "Only DMagic brand science parts can be used for this contract. Data must be collected while near to, or grappled to an asteroid of the specified class.";
		}

		protected override string GetDescription()
		{
			string story = DMContractDefs.DMAsteroid.backStory[rand.Next(0, DMContractDefs.DMAsteroid.backStory.Count)];
			return string.Format(story, this.agent.Name, hash);
		}

		protected override string GetSynopsys()
		{
			return string.Format("We want you to find a {0} asteroid and study it by collecting and returning or transmitting multiple scientific observations.", hash);
		}

		protected override string MessageCompleted()
		{
			return string.Format("You completed a survey of a {0} asteroid, well done.", hash);
		}

		protected override void OnLoad(ConfigNode node)
		{
			hash = node.parse("Asteroid_Size_Class", "Class B");

			if (this.ParameterCount == 0)
			{
				DMUtils.Logging("No Parameters Loaded For This Asteroid Contract; Removing Now...");
				this.Unregister();
				ContractSystem.Instance.Contracts.Remove(this);
				return;
			}
		}

		protected override void OnSave(ConfigNode node)
		{
			node.AddValue("Asteroid_Size_Class", hash);
		}

		public override bool MeetRequirements()
		{
			if (Planetarium.fetch.Home.orbitingBodies.Count < 2)
				return GameVariables.Instance.UnlockedSpaceObjectDiscovery(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.TrackingStation));
			else
				return ProgressTracking.Instance.NodeComplete(new string[] { Planetarium.fetch.Home.orbitingBodies[1].name, "Orbit" }) && GameVariables.Instance.UnlockedSpaceObjectDiscovery(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.TrackingStation));
		}

		public string AsteroidSize
		{
			get { return hash; }
		}
	}
}
