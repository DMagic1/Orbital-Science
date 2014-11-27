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
	public class DMMagneticSurveyContract: Contract, IDMagicContract
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

			DMScienceContainer magContainer = DMUtils.availableScience["All"].FirstOrDefault(m => m.Key == "Magnetometer Scan").Value;
			DMScienceContainer rpwsContainer = DMUtils.availableScience["All"].FirstOrDefault(r => r.Key == "Radio Plasma Wave Scan").Value;

			magParams[0] = DMCollectContractGenerator.fetchScienceContract(body, ExperimentSituations.InSpaceLow, magContainer);
			magParams[1] = DMCollectContractGenerator.fetchScienceContract(body, ExperimentSituations.InSpaceHigh, magContainer);
			magParams[2] = DMCollectContractGenerator.fetchScienceContract(body, ExperimentSituations.InSpaceLow, rpwsContainer);
			magParams[3] = DMCollectContractGenerator.fetchScienceContract(body, ExperimentSituations.InSpaceHigh, rpwsContainer);

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

			longParam.SetFunds(50000f * DMUtils.reward  * ((float)rand.Next(85, 116) / 100f), body);
			longParam.SetReputation(50f * DMUtils.reward  * ((float)rand.Next(85, 116) / 100f), body);
			longParam.SetScience(60f * DMUtils.science  * ((float)rand.Next(85, 116) / 100f), body);

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
					DMCS.SetFunds(8000f * DMUtils.reward  * ((float)rand.Next(85, 116) / 100f), body);
					DMCS.SetReputation(25f * DMUtils.reward  * ((float)rand.Next(85, 116) / 100f), body);
					DMCS.SetScience(25f * DMUtils.science * DMUtils.fixSubjectVal(DMCS.Situation, 1f, body), null);
				}
			}

			if (this.ParameterCount == 0)
				return false;

			float primaryModifier = ((float)rand.Next(80, 121) / 100f);

			this.agent = AgentList.Instance.GetAgent("DMagic");
			base.SetExpiry(10 * DMUtils.deadline, 20f * DMUtils.deadline);
			base.SetDeadlineDays((float)(time  / KSPUtil.KerbinDay) * 3.7f * (this.GetDestinationWeight(body) / 1.8f) * DMUtils.deadline * primaryModifier, null);
			base.SetReputation(50f * DMUtils.reward * primaryModifier, 10f * DMUtils.penalty * primaryModifier, body);
			base.SetFunds(50000 * DMUtils.forward * primaryModifier, 55000 * DMUtils.reward * primaryModifier, 20000 * DMUtils.penalty * primaryModifier, body);
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
			//if (DMScienceScenario.SciScenario != null)
			//	if (DMScienceScenario.SciScenario.contractsReload)
			//		DMUtils.resetContracts();
			int target;
			if (int.TryParse(node.GetValue("Mag_Survey_Target"), out target))
				body = FlightGlobals.Bodies[target];
			else
			{
				DMUtils.Logging("Failed To Load Mag Contract");
				this.Unregister();
				ContractSystem.Instance.Contracts.Remove(this);
				return;
			}
			if (this.GetParameter<DMLongOrbitParameter>() == null)
			{
				DMUtils.Logging("Magnetic Field Long Orbit Parameter Not Found; Removing This Contract");
				this.Unregister();
				ContractSystem.Instance.Contracts.Remove(this);
				return;
			}
			if (this.ParameterCount == 0)
			{
				DMUtils.Logging("No Parameters Loaded For Mag Contract; Removing Now...");
				this.Unregister();
				ContractSystem.Instance.Contracts.Remove(this);
				return;
			}
		}

		protected override void OnSave(ConfigNode node)
		{
			node.AddValue("Mag_Survey_Target", body.flightGlobalsIndex);
		}

		public override bool MeetRequirements()
		{
			return ProgressTracking.Instance.NodeComplete(new string[] { "Kerbin", "Escape" });
		}
	}
}
