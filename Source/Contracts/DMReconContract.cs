#region license
/* DMagic Orbital Science - DMReconContract
 * Class for generating recon survey contracts
 *
 * Copyright (c) 2016, David Grandy <david.grandy@gmail.com>
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
using FinePrint;
using FinePrint.Utilities;
using FinePrint.Contracts.Parameters;

namespace DMagic.Contracts
{
	public class DMReconContract : Contract, IUpdateWaypoints
	{
		private CelestialBody body;
		private DMCollectScience[] sciParams = new DMCollectScience[2];
		private System.Random rand = DMUtils.rand;

		protected override bool Generate()
		{
			DMReconContract[] reconContracts = ContractSystem.Instance.GetCurrentContracts<DMReconContract>();
			int offers = 0;
			int active = 0;
			int maxOffers = DMContractDefs.DMRecon.maxOffers;
			int maxActive = DMContractDefs.DMRecon.maxActive;

			for (int i = 0; i < reconContracts.Length; i++)
			{
				DMReconContract r = reconContracts[i];
				if (r.ContractState == State.Offered)
					offers++;
				else if (r.ContractState == State.Active)
					active++;
			}

			if (offers >= maxOffers)
				return false;
			if (active >= maxActive)
				return false;

			List<CelestialBody> customReachedBodies = new List<CelestialBody>();

			switch(prestige)
			{
				case ContractPrestige.Trivial:
					Func<CelestialBody, bool> cb = delegate(CelestialBody b)
					{
						if (b == Planetarium.fetch.Sun)
							return false;

						if (b.pqsController == null)
							return false;

						return true;
					};
					customReachedBodies.AddRange(ProgressUtilities.GetBodiesProgress(ProgressType.ORBIT, true, cb));
					customReachedBodies.AddRange(ProgressUtilities.GetNextUnreached(2, cb));
					var activeBodies = ContractSystem.Instance.GetCurrentActiveContracts<DMReconContract>().Select(r => r.body).ToList();
					customReachedBodies.RemoveAll(a => activeBodies.Contains(a));
					var completedBodies = ContractSystem.Instance.GetCompletedContracts<DMReconContract>().Select(r => r.body).ToList();
					customReachedBodies.RemoveAll(a => completedBodies.Contains(a));
					break;
				case ContractPrestige.Significant:
					customReachedBodies = ContractSystem.Instance.GetCompletedContracts<DMReconContract>().Where(r => r.prestige == ContractPrestige.Trivial).Select(r => r.body).ToList();
					break;
				case ContractPrestige.Exceptional:
					customReachedBodies = ContractSystem.Instance.GetCompletedContracts<DMReconContract>().Where(r => r.prestige == ContractPrestige.Significant).Select(r => r.body).ToList();
					break;
			}

			if (customReachedBodies.Count <= 0)
				return false;

			body = customReachedBodies[rand.Next(0, customReachedBodies.Count)];

			if (body == null)
				return false;

			double time = 0;

			OrbitType orbitType = OrbitType.POLAR;

			Dictionary<int, List<string>> parts = new Dictionary<int, List<string>>();
			Orbit o = new Orbit();

			double incMod = (rand.NextDouble() * 10) - 5;
			double timeMod = 1080000;

			if (!DMUtils.availableScience.ContainsKey("All"))
				return false;

			DMScienceContainer container = null;

			switch(prestige)
			{
				case ContractPrestige.Trivial:
					if (!DMUtils.partAvailable(DMContractDefs.DMRecon.reconTrivialParts))
						return false;

					parts.Add(0, DMContractDefs.DMRecon.reconTrivialParts);

					if (!DMUtils.availableScience["All"].ContainsKey(DMContractDefs.DMRecon.trivialExperimentTitle))
						return false;

					container = DMUtils.availableScience["All"][DMContractDefs.DMRecon.trivialExperimentTitle];

					o = CelestialUtilities.GenerateOrbit(orbitType, this.MissionSeed, body, 0.15, ContractDefs.Satellite.TrivialInclinationDifficulty);

					double st = o.semiMajorAxis - o.referenceBody.Radius;

					double mt = o.referenceBody.scienceValues.spaceAltitudeThreshold * 5 * .95;

					if (st > mt)
						o.semiMajorAxis = (mt * Math.Max(0.4, rand.NextDouble())) + o.referenceBody.Radius;

					timeMod = DMContractDefs.DMRecon.trivialTimeModifier * 6 * 3600;
					break;
				case ContractPrestige.Significant:
					if (!DMUtils.partAvailable(DMContractDefs.DMRecon.reconSignificantParts))
						return false;

					parts.Add(0, DMContractDefs.DMRecon.reconSignificantParts);

					if (!DMUtils.availableScience["All"].ContainsKey(DMContractDefs.DMRecon.significantExperimentTitle))
						return false;

					container = DMUtils.availableScience["All"][DMContractDefs.DMRecon.significantExperimentTitle];

					if (SystemUtilities.CoinFlip(rand))
					{
						if (CelestialUtilities.CanBodyBeKolniya(body))
							orbitType = OrbitType.KOLNIYA;
						else if (CelestialUtilities.CanBodyBeTundra(body))
							orbitType = OrbitType.TUNDRA;
						else
							orbitType = OrbitType.POLAR;
					}
					else
					{
						if (CelestialUtilities.CanBodyBeTundra(body))
							orbitType = OrbitType.TUNDRA;
						else if (CelestialUtilities.CanBodyBeKolniya(body))
							orbitType = OrbitType.KOLNIYA;
						else
							orbitType = OrbitType.POLAR;
					}
					o = CelestialUtilities.GenerateOrbit(orbitType, this.MissionSeed, body, 0.5, ContractDefs.Satellite.TrivialInclinationDifficulty);
					timeMod = DMContractDefs.DMRecon.significantTimeModifier * 6 * 3600;
					incMod = 0;
					break;
				case ContractPrestige.Exceptional:
					if (!DMUtils.partAvailable(DMContractDefs.DMRecon.reconExceptionalParts))
						return false;

					parts.Add(0, DMContractDefs.DMRecon.reconExceptionalParts);

					if (!DMUtils.availableScience["All"].ContainsKey(DMContractDefs.DMRecon.exceptionalExperimentTitle))
						return false;

					container = DMUtils.availableScience["All"][DMContractDefs.DMRecon.exceptionalExperimentTitle];

					o = CelestialUtilities.GenerateOrbit(orbitType, this.MissionSeed, body, 0.15, ContractDefs.Satellite.TrivialInclinationDifficulty);

					double se = o.semiMajorAxis - o.referenceBody.Radius;

					double me = o.referenceBody.scienceValues.spaceAltitudeThreshold * 5 * .95;

					if (se > me)
						o.semiMajorAxis = (me * Math.Max(0.4, rand.NextDouble())) + o.referenceBody.Radius;

					timeMod = DMContractDefs.DMRecon.exceptionalTimeModifier * 6 * 3600;
					break;
			}

			if (container == null)
				return false;

			time = timeMod * ((double)rand.Next(6, 17) / 10d);
			o.inclination += incMod;

			DMLongOrbitParameter longOrbit = new DMLongOrbitParameter(time);
			DMPartRequestParameter partRequest = new DMPartRequestParameter(parts, DMContractDefs.DMRecon.useVesselWaypoints, body);
			DMSpecificOrbitParameter reconParam = new DMSpecificOrbitParameter(orbitType, o.inclination, o.eccentricity, o.semiMajorAxis, o.LAN, o.argumentOfPeriapsis, o.meanAnomalyAtEpoch, o.epoch, body, ContractDefs.Satellite.SignificantDeviation, longOrbit);

			this.AddParameter(longOrbit);
			longOrbit.AddParameter(reconParam);
			longOrbit.AddParameter(partRequest);
			longOrbit.setPartRequest(partRequest);

			sciParams[0] = DMCollectContractGenerator.fetchScienceContract(body, ExperimentSituations.InSpaceLow, "SouthernHemisphere", container);
			sciParams[1] = DMCollectContractGenerator.fetchScienceContract(body, ExperimentSituations.InSpaceLow, "NorthernHemisphere", container);

			//Add the science collection parent parameter
			DMCompleteParameter DMcp = new DMCompleteParameter(1, 0);
			this.AddParameter(DMcp);

			foreach (DMCollectScience DMCS in sciParams)
			{
				if (DMCS == null)
					return false;
				else
				{
					float modifier = ((float)rand.Next(85, 116) / 100f);
					DMcp.addToSubParams(DMCS);
					DMCS.SetFunds(DMContractDefs.DMRecon.Funds.ParamReward * modifier, DMContractDefs.DMRecon.Funds.ParamFailure * modifier, body);
					DMCS.SetScience(DMContractDefs.DMRecon.Science.ParamReward * DMUtils.fixSubjectVal(DMCS.Situation, 1f, body), null);
					DMCS.SetReputation(DMContractDefs.DMRecon.Reputation.ParamReward * modifier, DMContractDefs.DMRecon.Reputation.ParamFailure * modifier, null);
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

			base.SetExpiry(DMContractDefs.DMRecon.Expire.MinimumExpireDays, DMContractDefs.DMRecon.Expire.MaximumExpireDays);
			base.SetDeadlineDays((float)(time / KSPUtil.KerbinDay) * DMContractDefs.DMRecon.Expire.DeadlineModifier * (this.GetDestinationWeight(body) / 1.4f) * primaryModifier, null);
			base.SetReputation(DMContractDefs.DMRecon.Reputation.BaseReward * Mod, DMContractDefs.DMRecon.Reputation.BaseFailure * Mod, null);
			base.SetFunds(DMContractDefs.DMRecon.Funds.BaseAdvance * Mod, DMContractDefs.DMRecon.Funds.BaseReward * Mod, DMContractDefs.DMRecon.Funds.BaseFailure * Mod, body);
			base.SetScience(DMContractDefs.DMRecon.Science.BaseReward * Mod, body);
			return true;
		}

		public override bool MeetRequirements()
		{
			return ProgressTracking.Instance.NodeComplete(new string[] { Planetarium.fetch.Home.name, "Orbit" });
		}

		public override bool CanBeCancelled()
		{
			return true;
		}

		public override bool CanBeDeclined()
		{
			return true;
		}

		protected override string GetDescription()
		{
			string story = DMContractDefs.DMRecon.backStory[rand.Next(0, DMContractDefs.DMRecon.backStory.Count)];
			return string.Format(story, this.agent.Name, body.theName);
		}

		protected override string GetHashString()
		{
			return string.Format("{0}{1}", body.name, prestige);
		}

		protected override string GetSynopsys()
		{
			switch(prestige)
			{
				case ContractPrestige.Trivial:
					return string.Format("We want you to conduct a detailed orbital reconnaissance survey of {0} using a long-term research vessel.", body.theName);
				case ContractPrestige.Significant:
					return string.Format("The intitial orbital survey of {0} has revealed several interesting findings; we now want you to conduct a long-term radio reconnaissance survey.", body.theName);
				default:
					return string.Format("Very high resolution, stereographic images of {0}'s surface are required to complete our reconnaissance survey; place a long-term research vessel into orbit.", body.theName);
			}
		}

		protected override string GetTitle()
		{
			string t = "";

			switch (prestige)
			{
				case ContractPrestige.Trivial:
					t = "orbital";
					break;
				case ContractPrestige.Significant:
					t = "radio signals";
					break;
				case ContractPrestige.Exceptional:
					t = "stereographic orbital";
					break;
			}

			return string.Format("Conduct a long term {0} reconnaissance survey of {1}", t, body.theName);
		}

		protected override string MessageCompleted()
		{
			switch (prestige)
			{
				case ContractPrestige.Trivial:
					return string.Format("Well done. You've completed an initial orbital survey of {0}; we'll begin analyzing the data immediately.", body.theName);
				case ContractPrestige.Significant:
					return string.Format("Well done. This radio signal survey of {0} has provided us with much valuable data; we'll begin analyzing it for follow up studies immediately.", body.theName);
				default:
					return string.Format("Well done. This completes our orbital reconnaissance surveys of {0}.", body.theName);
			}
		}

		protected override void OnLoad(ConfigNode node)
		{
			body = node.parse("Recon_Target", (CelestialBody)null);

			if (body == null)
			{
				DMUtils.Logging("Error while loading Recon Contract target body; removing contract now...");
				this.Unregister();
				ContractSystem.Instance.Contracts.Remove(this);
				return;
			}

			if (this.ParameterCount == 0)
			{
				DMUtils.Logging("No Parameters Loaded For This Recon Contract; Removing Now...");
				this.Unregister();
				ContractSystem.Instance.Contracts.Remove(this);
				return;
			}
		}

		protected override void OnSave(ConfigNode node)
		{
			node.AddValue("Recon_Target", body.flightGlobalsIndex);
		}

		/// <summary>
		/// Used externally to return the target Celestial Body
		/// </summary>
		/// <param name="cP">Instance of the requested Contract</param>
		/// <returns>Celestial Body object</returns>
		public static CelestialBody TargetBody(Contract c)
		{
			if (c == null || c.GetType() != typeof(DMReconContract))
				return null;

			try
			{
				DMReconContract Instance = (DMReconContract)c;
				return Instance.body;
			}
			catch (Exception e)
			{
				Debug.LogError("Error while accessing DMagic Recon Contract Target Body\n" + e);
				return null;
			}
		}

		public CelestialBody Body
		{
			get { return body; }
		}

	}
}
