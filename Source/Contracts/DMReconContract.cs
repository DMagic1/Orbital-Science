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
	public class DMReconContract : Contract
	{
		private CelestialBody body;
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
					customReachedBodies.AddRange(GetBodies_Reached(true, false));
					customReachedBodies.AddRange(GetBodies_NextUnreached(2, null));
					customReachedBodies.RemoveAll(b => b.pqsController == null);

					body = customReachedBodies[rand.Next(0, customReachedBodies.Count)];
					break;
				case ContractPrestige.Significant:
					customReachedBodies = ContractSystem.Instance.GetCompletedContracts<DMReconContract>().Where(r => r.prestige == ContractPrestige.Trivial).Select(r => r.body).ToList();

					body = customReachedBodies[rand.Next(0, customReachedBodies.Count)];
					break;
				case ContractPrestige.Exceptional:
					customReachedBodies = ContractSystem.Instance.GetCompletedContracts<DMReconContract>().Where(r => r.prestige == ContractPrestige.Significant).Select(r => r.body).ToList();

					body = customReachedBodies[rand.Next(0, customReachedBodies.Count)];
					break;
			}

			if (body == null)
				return false;

			double time = 0;

			OrbitType orbitType = OrbitType.POLAR;
			double deviation = 10;

			Dictionary<int, List<string>> parts = new Dictionary<int, List<string>>();
			Orbit o = new Orbit();

			switch(prestige)
			{
				case ContractPrestige.Trivial:
					parts.Add(0, DMContractDefs.DMRecon.reconTrivialParts);
					if (!DMUtils.partAvailable(DMContractDefs.DMRecon.reconTrivialParts))
						return false;
					o = CelestialUtilities.GenerateOrbit(orbitType, this.MissionSeed, body, 0.5, ContractDefs.Satellite.TrivialInclinationDifficulty);
					time = 1080000d * (double)(prestige + 1) * ((double)rand.Next(6, 17) / 10d);
					break;
				case ContractPrestige.Significant:
					parts.Add(0, DMContractDefs.DMRecon.reconSignificantParts);
					if (!DMUtils.partAvailable(DMContractDefs.DMRecon.reconSignificantParts))
						return false;
					if (SystemUtilities.CoinFlip(rand))
						orbitType = OrbitType.KOLNIYA;
					else
						orbitType = OrbitType.TUNDRA;
					o = CelestialUtilities.GenerateOrbit(orbitType, this.MissionSeed, body, 0.5, ContractDefs.Satellite.TrivialInclinationDifficulty);time = 3240000d * (double)(prestige + 1) * ((double)rand.Next(6, 17) / 10d);
					break;
				case ContractPrestige.Exceptional:
					parts.Add(0, DMContractDefs.DMRecon.reconExceptionalParts);
					if (!DMUtils.partAvailable(DMContractDefs.DMRecon.reconExceptionalParts))
						return false;
					o = CelestialUtilities.GenerateOrbit(orbitType, this.MissionSeed, body, 0.5, ContractDefs.Satellite.TrivialInclinationDifficulty);
					time = 2160000d * (double)(prestige + 1) * ((double)rand.Next(6, 17) / 10d);
					break;
			}

			DMLongOrbitParameter longOrbit = new DMLongOrbitParameter(time);
			DMPartRequestParameter partRequest = new DMPartRequestParameter(parts, body);
			DMReconOrbitParameter reconParam = new DMReconOrbitParameter(orbitType, o.inclination, o.eccentricity, o.semiMajorAxis, o.LAN, o.argumentOfPeriapsis, o.meanAnomalyAtEpoch, o.epoch, body, ContractDefs.Satellite.SignificantDeviation, longOrbit);

			longOrbit.AddParameter(reconParam);
			longOrbit.AddParameter(partRequest);

			reconParam.AddParameter(new DMSpecificOrbitParameterExtended(orbitType, o.inclination, o.eccentricity, o.semiMajorAxis, o.LAN, o.argumentOfPeriapsis, o.meanAnomalyAtEpoch, o.epoch, body, deviation));

			if (this.ParameterCount == 0)
				return false;

			float primaryModifier = ((float)rand.Next(80, 121) / 100f);
			float diffModifier = 1 + ((float)this.Prestige * 0.5f);

			float Mod = primaryModifier * diffModifier;

			this.agent = AgentList.Instance.GetAgent("DMagic");

			if (this.agent == null)
				this.agent = AgentList.Instance.GetAgentRandom();

			base.SetExpiry(DMContractDefs.DMRecon.Expire.MinimumExpireDays, DMContractDefs.DMRecon.Expire.MaximumExpireDays);
			base.SetDeadlineDays((float)(time / KSPUtil.KerbinDay) * DMContractDefs.DMRecon.Expire.DeadlineModifier * (this.GetDestinationWeight(body) / 1.8f) * primaryModifier, null);
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

		protected override string GetNotes()
		{
			return base.GetNotes();
		}

		protected override string GetSynopsys()
		{
			switch(prestige)
			{
				case ContractPrestige.Trivial:
					return "";
				case ContractPrestige.Significant:
					return "";
				default:
					return "";
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
					return string.Format("");
				case ContractPrestige.Significant:
					return string.Format("");
				default:
					return string.Format("");
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

	}
}
