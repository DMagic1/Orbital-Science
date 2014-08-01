#region license
/* DMagic Orbital Science - DMMagneticSurveyContract
 * Class for generating contracts to study planetary magnetic environment
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

namespace DMagic.Contracts
{
	class DMMagneticSurveyContract: Contract
	{
		private CelestialBody body;
		private DMCollectScience[] magParams = new DMCollectScience[4];
		private DMLongOrbitParameter longOrbit;
		private System.Random rand = DMUtils.rand;

		protected override bool Generate()
		{
			if (!GetBodies_Reached(true, true).Contains(FlightGlobals.Bodies[1]))
				return false;
			int total = ContractSystem.Instance.GetCurrentContracts<DMMagneticSurveyContract>().Count();
			int finished = ContractSystem.Instance.GetCompletedContracts<DMMagneticSurveyContract>().Count();
			if ((total - finished) > 1)
				return false;

			body = DMUtils.nextTargetBody(this.Prestige, GetBodies_Reached(false, true), GetBodies_NextUnreached(4, null));
			if (body == null)
				return false;

			magParams[0] = DMCollectContractGenerator.fetchScienceContract(body, ExperimentSituations.InSpaceLow, ResearchAndDevelopment.GetExperiment("magScan"));
			magParams[1] = DMCollectContractGenerator.fetchScienceContract(body, ExperimentSituations.InSpaceHigh, ResearchAndDevelopment.GetExperiment("magScan"));
			magParams[2] = DMCollectContractGenerator.fetchScienceContract(body, ExperimentSituations.InSpaceLow, ResearchAndDevelopment.GetExperiment("rpwsScan"));
			magParams[3] = DMCollectContractGenerator.fetchScienceContract(body, ExperimentSituations.InSpaceHigh, ResearchAndDevelopment.GetExperiment("rpwsScan"));

			longOrbit = DMLongOrbitGenerator.fetchLongOrbit(body, this.Prestige);
			this.AddParameter(longOrbit);
			DMUtils.DebugLog("Added Long Orbit Param");

			foreach (DMCollectScience DMCS in magParams)
			{
				if (DMCS == null)
					return false;
				else
				{
					this.AddParameter(DMCS, null);
					DMUtils.DebugLog("Added Mag Survey Param");
					DMCS.SetFunds(1f, body);
					DMCS.SetReputation(1f, body);
					DMCS.SetScience(1f, body);
				}
			}

			if (this.ParameterCount == 0)
				return false;

			int a = rand.Next(0, 2);
			if (a == 0)
				this.agent = AgentList.Instance.GetAgent("DMagic");
			else
				this.agent = AgentList.Instance.GetAgentRandom();

			base.SetExpiry(10, Math.Max(15, 15) * (float)(this.prestige + 1));
			base.SetDeadlineDays(300f * (float)(this.prestige + 1), body);
			base.SetReputation(0.5f * DMUtils.reward, body);
			base.SetFunds(3000 * DMUtils.forward, 3000 * DMUtils.reward, 1000 * DMUtils.penalty, body);
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
			return string.Format("Conduct mag survey of {0}", body.theName);
		}

		protected override string GetDescription()
		{
			string story = DMUtils.backStory["magnetic"][rand.Next(0, DMUtils.backStory["magnetic"].Count)];
			return string.Format(story, this.agent.Name, body.theName);
		}

		protected override string GetSynopsys()
		{
			DMUtils.DebugLog("Generating Mag Synopsis From Target Body: [{0}]", body.theName);
			return string.Format("Study {0} for signs of on-going or past biological activity by conducting several science experiments.", body.theName);
		}

		protected override string MessageCompleted()
		{
			return string.Format("You completed a survey of {0}, well done.", body.theName);
		}

		protected override void OnLoad(ConfigNode node)
		{
			DMUtils.DebugLog("Loading Mag Contract");
			int target;
			if (int.TryParse(node.GetValue("Mag_Survey_Target"), out target))
				body = FlightGlobals.Bodies[target];
			else
			{
				DMUtils.Logging("Failed To Load Mag Contract");
				this.Cancel();
			}
			if (this.ParameterCount == 0)
				this.Cancel();
		}

		protected override void OnSave(ConfigNode node)
		{
			DMUtils.DebugLog("Saving Mag Contract");
			node.AddValue("Mag_Survey_Target", body.flightGlobalsIndex);
		}

		public override bool MeetRequirements()
		{
			return true;
		}

	}
}
