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
using FinePrint.Utilities;
using DMagic.Parameters;

namespace DMagic.Contracts
{
	public class DMMagneticSurveyContract: Contract, IUpdateWaypoints
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

			List<CelestialBody> bodies = new List<CelestialBody>();
			Func<CelestialBody, bool> cb = null;

			switch (prestige)
			{
				case ContractPrestige.Trivial:
					cb = delegate(CelestialBody b)
					{
						if (b == Planetarium.fetch.Sun)
							return false;

						if (b.scienceValues.RecoveryValue > 4)
							return false;

						return true;
					};
					bodies.AddRange(ProgressUtilities.GetBodiesProgress(ProgressType.ORBIT, true, cb));
					break;
				case ContractPrestige.Significant:
					cb = delegate(CelestialBody b)
					{
						if (b == Planetarium.fetch.Sun)
							return false;

						if (b == Planetarium.fetch.Home)
							return false;

						if (b.scienceValues.RecoveryValue > 8)
							return false;

						return true;
					};
					bodies.AddRange(ProgressUtilities.GetBodiesProgress(ProgressType.FLYBY, true, cb));
					bodies.AddRange(ProgressUtilities.GetNextUnreached(2, cb));
					break;
				case ContractPrestige.Exceptional:
					cb = delegate(CelestialBody b)
					{
						if (b == Planetarium.fetch.Home)
							return false;

						if (Planetarium.fetch.Home.orbitingBodies.Count > 0)
						{
							foreach (CelestialBody B in Planetarium.fetch.Home.orbitingBodies)
							{
								if (b == B)
									return false;
							}
						}

						if (b.scienceValues.RecoveryValue < 4)
							return false;

						return true;
					};
					bodies.AddRange(ProgressUtilities.GetBodiesProgress(ProgressType.FLYBY, true, cb));
					bodies.AddRange(ProgressUtilities.GetNextUnreached(4, cb));
					break;
			}

			if (bodies.Count <= 0)
				return false;

			body = bodies[rand.Next(0, bodies.Count)];

			if (body == null)
				return false;

			DMScienceContainer magContainer = null;
			DMScienceContainer rpwsContainer = null;

			if (!DMUtils.availableScience.ContainsKey("All"))
				return false;

			if (!DMUtils.availableScience["All"].ContainsKey(DMContractDefs.DMMagnetic.magnetometerExperimentTitle))
				return false;
			
			magContainer = DMUtils.availableScience["All"][DMContractDefs.DMMagnetic.magnetometerExperimentTitle];

			if (!DMUtils.availableScience["All"].ContainsKey(DMContractDefs.DMMagnetic.rpwsExperimentTitle))
				return false;

			rpwsContainer = DMUtils.availableScience["All"][DMContractDefs.DMMagnetic.rpwsExperimentTitle];

			magParams[0] = DMCollectContractGenerator.fetchScienceContract(body, ExperimentSituations.InSpaceLow, magContainer);
			magParams[1] = DMCollectContractGenerator.fetchScienceContract(body, ExperimentSituations.InSpaceHigh, magContainer);
			magParams[2] = DMCollectContractGenerator.fetchScienceContract(body, ExperimentSituations.InSpaceLow, rpwsContainer);
			magParams[3] = DMCollectContractGenerator.fetchScienceContract(body, ExperimentSituations.InSpaceHigh, rpwsContainer);

			double eccMod = 0.2;
			double incMod = 20;
			double timeMod = 2160000;

			switch(prestige)
			{
				case ContractPrestige.Trivial:
					eccMod = DMContractDefs.DMMagnetic.trivialEccentricityMultiplier;
					incMod = DMContractDefs.DMMagnetic.trivialInclinationMultiplier;
					timeMod = DMContractDefs.DMMagnetic.trivialTimeModifier * 6 * 3600;
					break;
				case ContractPrestige.Significant:
					eccMod = DMContractDefs.DMMagnetic.significantEccentricityMultiplier;
					incMod = DMContractDefs.DMMagnetic.significantInclinationMultiplier;
					timeMod = DMContractDefs.DMMagnetic.significantTimeModifier * 6 * 3600;
					break;
				case ContractPrestige.Exceptional:
					eccMod = DMContractDefs.DMMagnetic.exceptionalEccentricityMultiplier;
					incMod = DMContractDefs.DMMagnetic.exceptionalInclinationMultiplier;
					timeMod = DMContractDefs.DMMagnetic.exceptionalTimeModifier * 6 * 3600;
					break;
			}

			double time = timeMod * ((double)rand.Next(6, 17) / 10d);
			double eccen = eccMod * ((double)rand.Next(8, 13) / 10d);
			if (eccen > 0.7) eccen = 0.7;
			double inclination = incMod * ((double)rand.Next(7, 14) / 10d);
			if (inclination > 75) inclination = 75;

			DictionaryValueList<int, List<string>> parts = new DictionaryValueList<int, List<string>>();
			parts.Add(0, DMContractDefs.DMMagnetic.magParts);
			parts.Add(1, DMContractDefs.DMMagnetic.rpwsParts);

			DMLongOrbitParameter longParam = new DMLongOrbitParameter(time);
			DMOrbitalParameters eccentricParam = new DMOrbitalParameters(eccen, 0, longParam);
			DMOrbitalParameters inclinedParam = new DMOrbitalParameters(inclination, 1, longParam);
			DMPartRequestParameter partRequest = new DMPartRequestParameter(parts, DMContractDefs.DMMagnetic.useVesselWaypoints, body);

			this.AddParameter(longParam);
			longParam.AddParameter(eccentricParam);
			longParam.AddParameter(inclinedParam);
			longParam.AddParameter(partRequest);

			longParam.setPartRequest(partRequest);

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
					if (DMCS.Container == null)
						continue;

					float modifier = ((float)rand.Next(85, 116) / 100f);
					DMcp.addToSubParams(DMCS);
					DMCS.SetFunds(DMContractDefs.DMMagnetic.Funds.ParamReward * modifier, DMContractDefs.DMMagnetic.Funds.ParamFailure * modifier, body);
					DMCS.SetScience(DMContractDefs.DMMagnetic.Science.ParamReward * DMUtils.fixSubjectVal(DMCS.Situation, 1f, body), null);
					DMCS.SetReputation(DMContractDefs.DMMagnetic.Reputation.ParamReward * modifier, DMContractDefs.DMMagnetic.Reputation.ParamFailure * modifier, null);
				}
			}

			if (this.ParameterCount == 0)
				return false;

			float primaryModifier = ((float)rand.Next(80, 121) / 100f);
			float diffModifier = 1 + ((float)this.Prestige * 0.5f);

			float Mod = primaryModifier * diffModifier;

			this.agent = AgentList.Instance.GetAgent("DMagic");

			if (this.agent == null)
				this.agent = AgentList.Instance.GetAgentRandom();

			base.SetExpiry(DMContractDefs.DMMagnetic.Expire.MinimumExpireDays, DMContractDefs.DMMagnetic.Expire.MaximumExpireDays);
			base.SetDeadlineDays((float)(time / 3600) * DMContractDefs.DMMagnetic.Expire.DeadlineModifier * (this.GetDestinationWeight(body) / 1.8f) * primaryModifier, null);
			base.SetReputation(DMContractDefs.DMMagnetic.Reputation.BaseReward * Mod, DMContractDefs.DMMagnetic.Reputation.BaseFailure * Mod, null);
			base.SetFunds(DMContractDefs.DMMagnetic.Funds.BaseAdvance * Mod, DMContractDefs.DMMagnetic.Funds.BaseReward * Mod, DMContractDefs.DMMagnetic.Funds.BaseFailure * Mod, body);
			base.SetScience(DMContractDefs.DMMagnetic.Science.BaseReward * Mod, body);
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
			return string.Format("{0}{1}", body.bodyName, (int)this.prestige);
		}

		protected override string GetTitle()
		{
			if (body == null)
				return "Whoops. Something bad happened here...";

			return string.Format("Conduct a survey of the magnetic field environment around {0}", body.displayName.LocalizeBodyName());
		}

		protected override string GetDescription()
		{
			if (body == null)
				return "Whoops. Something bad happened here...";

			string story = DMContractDefs.DMMagnetic.backStory[rand.Next(0, DMContractDefs.DMMagnetic.backStory.Count)];
			return string.Format(story, this.agent.Name, body.displayName.LocalizeBodyName());
		}

		protected override string GetSynopsys()
		{
			if (body == null)
				return "Whoops. Something bad happened here...";

			return string.Format("Study the magnetic field environment around {0} by inserting a long-term research vessel into orbit.", body.displayName.LocalizeBodyName());
		}

		protected override string MessageCompleted()
		{
			if (body == null)
				return "Whoops. Something bad happened here...";

			return string.Format("You completed a survey of {0}, well done.", body.displayName.LocalizeBodyName());
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
			if (body == null)
				return;

			node.AddValue("Mag_Survey_Target", body.flightGlobalsIndex);
		}

		public override bool MeetRequirements()
		{
			return ProgressTracking.Instance.NodeComplete(new string[] { Planetarium.fetch.Home.bodyName, "Escape" });
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
