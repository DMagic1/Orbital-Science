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

namespace DMagic
{
	class DMMagneticSurveyContract: Contract
	{
		private CelestialBody body;
		private DMCollectScience[] magParams = new DMCollectScience[4];
		private System.Random rand = DMUtils.rand;

		protected override bool Generate()
		{
			int total = ContractSystem.Instance.GetCurrentContracts<DMMagneticSurveyContract>().Count();
			if (total >= DMUtils.maxMagnetic)
				return false;

			//Make sure that the RPWS is available
			AvailablePart aPart = PartLoader.getPartInfoByName("rpwsAnt");
			if (aPart == null)
				return false;
			if (!ResearchAndDevelopment.PartModelPurchased(aPart))
				return false;

			body = DMUtils.nextTargetBody(this.Prestige, GetBodies_Reached(false, true), GetBodies_NextUnreached(4, null));
			if (body == null)
				return false;

			magParams[0] = DMCollectContractGenerator.fetchScienceContract(body, ExperimentSituations.InSpaceLow, ResearchAndDevelopment.GetExperiment("magScan"));
			magParams[1] = DMCollectContractGenerator.fetchScienceContract(body, ExperimentSituations.InSpaceHigh, ResearchAndDevelopment.GetExperiment("magScan"));
			magParams[2] = DMCollectContractGenerator.fetchScienceContract(body, ExperimentSituations.InSpaceLow, ResearchAndDevelopment.GetExperiment("rpwsScan"));
			magParams[3] = DMCollectContractGenerator.fetchScienceContract(body, ExperimentSituations.InSpaceHigh, ResearchAndDevelopment.GetExperiment("rpwsScan"));

			double time = 2160000d *(double)(this.Prestige + 1) * ((double)rand.Next(6, 21) / 10d);
			double eccen = 0.15d * (double)(this.Prestige + 1) * ((double)rand.Next(10, 21) / 10d);
			if (eccen > 0.7) eccen = 0.7;
			double inclination = 20d * (double)(this.Prestige + 1) * ((double)rand.Next(8, 15) / 10d);
			if (inclination > 75) inclination = 75;

			DMLongOrbitParameter longParam = new DMLongOrbitParameter(body, time);
			DMOrbitalParameters eccentricParam = new DMOrbitalParameters(body, eccen, 0);
			DMOrbitalParameters inclinedParam = new DMOrbitalParameters(body, inclination, 1);

			this.AddParameter(longParam);
			longParam.AddParameter(eccentricParam);
			longParam.AddParameter(inclinedParam);

			longParam.SetFunds(50000f * DMUtils.reward, body);
			longParam.SetReputation(50f * DMUtils.reward, body);
			longParam.SetScience(50f * DMUtils.science, body);

			if (eccentricParam == null || inclinedParam == null)
				return false;

			foreach (DMCollectScience DMCS in magParams)
			{
				if (DMCS == null)
					return false;
				else
				{
					this.AddParameter(DMCS, "collectDMScience");
					DMUtils.DebugLog("Added Mag Survey Param");
					DMCS.SetFunds(5000f * DMUtils.reward, body);
					DMCS.SetReputation(25f * DMUtils.reward, body);
					DMCS.SetScience(20f * DMUtils.science * DMUtils.fixSubjectVal(DMCS.Situation, 1f, body), null);
				}
			}

			if (this.ParameterCount == 0)
				return false;

			this.agent = AgentList.Instance.GetAgent("DMagic");
			base.SetExpiry(10, 15f * (float)(this.prestige + 1));
			base.SetDeadlineDays((float)DMUtils.timeInDays(time) * 13f, null);
			base.SetReputation(50f * DMUtils.reward, 10f * DMUtils.penalty, body);
			base.SetFunds(50000 * DMUtils.forward, 55000 * DMUtils.reward, 20000 * DMUtils.penalty, body);
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
			return string.Format("{0}{1}", body.name, (int)this.prestige);
		}

		protected override string GetTitle()
		{
			return string.Format("Conduct a survey of the magnetic field environment around {0}", body.theName);
		}

		protected override string GetDescription()
		{
			string story = DMUtils.backStory["magnetic"][rand.Next(0, DMUtils.backStory["magnetic"].Count)];
			return string.Format(story, this.agent.Name, body.theName);
		}

		protected override string GetSynopsys()
		{
			return string.Format("Study the magnetic field environment around {0} by inserting a long-term research vessel into orbit.", body.theName);
		}

		protected override string MessageCompleted()
		{
			return string.Format("You completed a survey of {0}, well done.", body.theName);
		}

		protected override void OnLoad(ConfigNode node)
		{
			//DMUtils.DebugLog("Loading Mag Contract");
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
			//DMUtils.DebugLog("Saving Mag Contract");
			node.AddValue("Mag_Survey_Target", body.flightGlobalsIndex);
		}

		public override bool MeetRequirements()
		{
			return ProgressTracking.Instance.NodeComplete(new string[] { "Kerbin", "Escape" });
		}
	}
}
