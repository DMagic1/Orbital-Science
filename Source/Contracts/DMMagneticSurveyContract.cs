﻿#region license
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
using DMagic.Parameters;

namespace DMagic.Contracts
{
	public class DMMagneticSurveyContract: Contract
	{
		private CelestialBody body;
		private DMCollectScience[] magParams = new DMCollectScience[4];
		private System.Random rand = DMUtils.rand;

		protected override bool Generate()
		{
			DMMagneticSurveyContract[] magContracts = ContractSystem.Instance.GetCurrentContracts<DMMagneticSurveyContract>();
			int offers = 0;
			int active = 0;
			int maxOffers = DMContractDefs.DMMagnetic.maxOffers;
			int maxActive = DMContractDefs.DMMagnetic.maxActive;

			for (int i = 0; i < magContracts.Length; i++)
			{
				DMMagneticSurveyContract m = magContracts[i];
				if (m.ContractState == State.Offered)
					offers++;
				else if (m.ContractState == State.Active)
					active++;
			}

			if (offers >= maxOffers)
				return false;
			if (active >= maxActive)
				return false;

			//Make sure that the parts are available
			if (!DMUtils.partAvailable(DMContractDefs.DMMagnetic.magParts) || !DMUtils.partAvailable(DMContractDefs.DMMagnetic.rpwsParts))
				return false;

			body = DMUtils.nextTargetBody(this.Prestige, GetBodies_Reached(false, false), GetBodies_NextUnreached(4, null));
			if (body == null)
				return false;

			DMScienceContainer magContainer = DMUtils.availableScience["All"].FirstOrDefault(m => m.Key == "Magnetometer Scan").Value;
			DMScienceContainer rpwsContainer = DMUtils.availableScience["All"].FirstOrDefault(r => r.Key == "Radio Plasma Wave Scan").Value;

			magParams[0] = DMCollectContractGenerator.fetchScienceContract(body, ExperimentSituations.InSpaceLow, magContainer);
			magParams[1] = DMCollectContractGenerator.fetchScienceContract(body, ExperimentSituations.InSpaceHigh, magContainer);
			magParams[2] = DMCollectContractGenerator.fetchScienceContract(body, ExperimentSituations.InSpaceLow, rpwsContainer);
			magParams[3] = DMCollectContractGenerator.fetchScienceContract(body, ExperimentSituations.InSpaceHigh, rpwsContainer);

			double time = 2160000d *(double)(this.Prestige + 1) * ((double)rand.Next(6, 17) / 10d);
			double eccen = 0.15d * (double)(this.Prestige + 1) * ((double)rand.Next(10, 21) / 10d);
			if (eccen > 0.7) eccen = 0.7;
			double inclination = 20d * (double)(this.Prestige + 1) * ((double)rand.Next(8, 15) / 10d);
			if (inclination > 75) inclination = 75;

			Dictionary<int, List<string>> parts = new Dictionary<int, List<string>>();
			parts.Add(0, DMContractDefs.DMMagnetic.magParts);
			parts.Add(1, DMContractDefs.DMMagnetic.rpwsParts);

			DMLongOrbitParameter longParam = new DMLongOrbitParameter(time);
			DMOrbitalParameters eccentricParam = new DMOrbitalParameters(eccen, 0, longParam);
			DMOrbitalParameters inclinedParam = new DMOrbitalParameters(inclination, 1, longParam);
			DMPartRequestParameter partRequest = new DMPartRequestParameter(parts, body);

			this.AddParameter(longParam);
			longParam.AddParameter(eccentricParam);
			longParam.AddParameter(inclinedParam);
			longParam.AddParameter(partRequest);

			if (eccentricParam == null || inclinedParam == null)
				return false;

			//Add the science collection parent parameter
			DMCompleteParameter DMcp = new DMCompleteParameter(1, 0);
			this.AddParameter(DMcp);

			foreach (DMCollectScience DMCS in magParams)
			{
				if (DMCS == null)
					return false;
				else
				{
					DMcp.addToSubParams(DMCS, "MagFieldScience");
					DMCS.SetFunds(5000f * DMUtils.reward  * ((float)rand.Next(85, 116) / 100f), body);
					DMCS.SetScience(2f * DMUtils.science * DMUtils.fixSubjectVal(DMCS.Situation, 1f, body), null);
				}
			}

			if (this.ParameterCount == 0)
				return false;

			float primaryModifier = ((float)rand.Next(80, 121) / 100f);
			float diffModifier = 1 + ((float)this.Prestige * 0.5f);

			this.agent = AgentList.Instance.GetAgent("DMagic");
			base.SetExpiry(10 * DMUtils.deadline, 20f * DMUtils.deadline);
			base.SetDeadlineDays((float)(time  / KSPUtil.KerbinDay) * 3.7f * (this.GetDestinationWeight(body) / 1.8f) * DMUtils.deadline * primaryModifier, null);
			base.SetReputation(8f * diffModifier * DMUtils.reward * primaryModifier, 7f * diffModifier * DMUtils.penalty * primaryModifier, null);
			base.SetFunds(35000 * diffModifier * DMUtils.forward * primaryModifier, 40000 * diffModifier * DMUtils.reward * primaryModifier, 28000 * diffModifier * DMUtils.penalty * primaryModifier, body);
			base.SetScience(10f * diffModifier * DMUtils.science * primaryModifier, body);
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
			string story = DMContractDefs.DMMagnetic.backStory[rand.Next(0, DMContractDefs.DMMagnetic.backStory.Count)];
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
			body = node.parse("Mag_Survey_Target", (CelestialBody)null);

			if (body == null)
			{
				DMUtils.Logging("Error while loading Magnetic Field Survey Contract target body; removing contract now...");
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
			return ProgressTracking.Instance.NodeComplete(new string[] { Planetarium.fetch.Home.name, "Escape" });
		}

		/// <summary>
		/// Used externally to return the target Celestial Body
		/// </summary>
		/// <param name="cP">Instance of the requested Contract</param>
		/// <returns>Celestial Body object</returns>
		public static CelestialBody TargetBody(Contract c)
		{
			if (c == null || c.GetType() != typeof(DMMagneticSurveyContract))
				return null;

			try
			{
				DMMagneticSurveyContract Instance = (DMMagneticSurveyContract)c;
				return Instance.body;
			}
			catch (Exception e)
			{
				Debug.LogError("Error while accessing DMagic Magnetic Survey Contract Target Body\n" + e);
				return null;
			}
		}

		public CelestialBody Body
		{
			get { return body; }
		}
	}
}
