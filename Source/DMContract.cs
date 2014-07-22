using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Contracts;
using Contracts.Parameters;
using KSPAchievements;

namespace DMagic
{
	public class DMContract: Contract
	{

		private CelestialBody body = null;
		private BodyLocation scienceLocation;
		private ScienceExperiment exp = null;
		private ScienceSubject sub = null;
		private ExperimentSituations targetSituation;
		private DMcontractScience DMscience;
		private AvailablePart aPart = null;
		private ProtoTechNode pTechNode = null;
		private List<ExperimentSituations> situations;
		private string biome = "";
		private System.Random rand = new System.Random();
		private bool biomeIsRelevant;

		#region overrides

		protected override bool Generate()
		{
			DMscience = DMConfigLoader.availableScience.ElementAt(rand.Next(0, DMConfigLoader.availableScience.Count - 1)).Value;
			Debug.Log("[DM] Generating Contract Now");
			if (DMscience.sciPart != "None")
			{
				Debug.Log("[DM] Standard Experiment Generating");
				pTechNode = ResearchAndDevelopment.Instance.GetTechState(DMscience.sciNode);
				if (pTechNode == null)
					return false;
				else
				{
					Debug.Log("[DM] Tech Node Found");
					if (pTechNode.state != RDTech.State.Available)
						return false;
					else
					{
						Debug.Log("[DM] Tech Node Researched");
						aPart = pTechNode.partsPurchased.FirstOrDefault(p => p.name == DMscience.sciPart);
						if (aPart == null)
							return false;
						Debug.Log("[DM] Part Purchased");
					}
				}
			}

			if (body == null)
			{
				body = nextTargetBody();
				if (body == null)
					return false;
			}
			if (exp == null)
			{
				exp = DMscience.exp;
				if (exp == null)
					return false;
			}

			situations = availableSituations(DMscience.sitMask);

			if (situations.Count == 0)
				return false;
			else
			{
				Debug.Log("[DM] Acceptable Situations Found");
				targetSituation = situations[rand.Next(0, situations.Count - 1)];
				scienceLocation = setBodyLocation(targetSituation);
			}

			biomeIsRelevant = biomeRelevant(targetSituation, DMscience.bioMask);

			if (biomeIsRelevant)
			{
				int i = rand.Next(0, 3);
				if (i == 0)
					biome = fetchBiome(body);
			}

			sub = ResearchAndDevelopment.GetExperimentSubject(exp, targetSituation, body, biome);

			if (sub == null)
			{
				Debug.Log("[DM] No Acceptable Science Subject Found");
				return false;
			}
			else
			{
				Debug.Log("[DM] Acceptable Science Subject Found");
				sub.subjectValue = DMModuleScienceAnimate.fixSubjectValue(targetSituation, sub.subjectValue, 1f, body);
				if (sub.scientificValue < 0.4f)
					return false;
			}

			if (DMscience.agent != "Any")
				this.agent = Contracts.Agents.AgentList.Instance.GetAgent(DMscience.agent);

			this.AddParameter(new DMCollectScience(body, scienceLocation, sub, exp), null);
			Debug.Log("[DM] Parameter Added");
			base.SetExpiry();
			base.SetScience(Math.Max(exp.baseValue, (exp.baseValue * sub.subjectValue) / 2), body);
			base.SetDeadlineDays(20f * sub.subjectValue, body);
			base.SetReputation(5f, 10f, body);
			base.SetFunds(100f * sub.subjectValue, 1000f * sub.subjectValue, 500f * sub.subjectValue, body);
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
			return sub.id;
		}

		protected override string GetTitle()
		{
			return "Collect science data";
		}

		protected override string GetDescription()
		{
			return "Do something!";
		}

		protected override string GetSynopsys()
		{
			return "You did something!";
		}

		protected override string MessageCompleted()
		{
			return "Success";
		}

		protected override void OnLoad(ConfigNode node)
		{
			Debug.Log("[DM] Loading Contract");
			int targetBodyID;
			if (int.TryParse(node.GetValue("ScienceTarget"), out targetBodyID))
				body = FlightGlobals.Bodies[targetBodyID];
			ScienceSubject trySub = ResearchAndDevelopment.GetSubjectByID(node.GetValue("ScienceSubject"));
			if (trySub != null)
				sub = trySub;
		}

		protected override void OnSave(ConfigNode node)
		{
			Debug.Log("[DM] Saving Contract");
			node.AddValue("ScienceTarget", body.flightGlobalsIndex);
			node.AddValue("ScienceSubject", sub.id);
		}

		public override bool MeetRequirements()
		{
			return true;
		}

		#endregion

		private CelestialBody nextTargetBody()
		{
			Debug.Log("[DM] Searching For Acceptable Body");
			List<CelestialBody> bList;
			if (this.prestige == ContractPrestige.Trivial)
				return FlightGlobals.Bodies[rand.Next(1, 3)];
			else if (this.prestige == ContractPrestige.Significant)
			{
				bList = GetBodies_Reached(false, true);
				return bList[rand.Next(0, bList.Count - 1)];
			}
			else if (this.prestige == ContractPrestige.Exceptional)
			{
				bList = GetBodies_NextUnreached(3, null);
				bList.Remove(FlightGlobals.Bodies[1]);
				bList.Remove(FlightGlobals.Bodies[2]);
				bList.Remove(FlightGlobals.Bodies[3]);
				return bList[rand.Next(0, bList.Count - 1)];
			}
			return null;
		}

		private List<ExperimentSituations> availableSituations(int i)
		{
			Debug.Log("[DM] Finding Situations");
			List<ExperimentSituations> expSitList = new List<ExperimentSituations>();
			ExperimentSituations expMask = (ExperimentSituations)i;
			if ((expMask & ExperimentSituations.FlyingHigh) == ExperimentSituations.FlyingHigh)
				expSitList.Add(ExperimentSituations.FlyingHigh);
			if ((expMask & ExperimentSituations.FlyingLow) == ExperimentSituations.FlyingLow)
				expSitList.Add(ExperimentSituations.FlyingLow);
			if ((expMask & ExperimentSituations.InSpaceHigh) == ExperimentSituations.InSpaceHigh)
				expSitList.Add(ExperimentSituations.InSpaceHigh);
			if ((expMask & ExperimentSituations.InSpaceLow) == ExperimentSituations.InSpaceLow)
				expSitList.Add(ExperimentSituations.InSpaceLow);
			if ((expMask & ExperimentSituations.SrfLanded) == ExperimentSituations.SrfLanded)
				expSitList.Add(ExperimentSituations.SrfLanded);
			if ((expMask & ExperimentSituations.SrfSplashed) == ExperimentSituations.SrfSplashed)
				expSitList.Add(ExperimentSituations.SrfSplashed);
			return expSitList;
		}

		private bool biomeRelevant(ExperimentSituations s, int i)
		{
			if ((i & (int)s) == 0)
				return false;
			else
				return true;
		}

		private BodyLocation setBodyLocation(ExperimentSituations sit)
		{
			if (sit == ExperimentSituations.InSpaceHigh || sit == ExperimentSituations.InSpaceLow)
				return BodyLocation.Space;
			else
				return BodyLocation.Surface;
		}

		private string fetchBiome(CelestialBody b)
		{
			string s = "";
			if (b.BiomeMap == null || b.BiomeMap.Map == null)
				return s;
			else
				s = b.BiomeMap.Attributes[rand.Next(0, b.BiomeMap.Attributes.Length - 1)].name;
			return s;
		}

	}

	#region Contract Parameter

	public class DMCollectScience: CollectScience
	{
		public CelestialBody scienceTargetBody;
		public BodyLocation scienceLocation;
		public ScienceSubject scienceTargetSubject;
		public ScienceExperiment scienceTargetExperiment;

		public DMCollectScience()
		{
		}

		public DMCollectScience(CelestialBody target, BodyLocation location, ScienceSubject subject, ScienceExperiment exp)
		{
			scienceTargetBody = target;
			scienceLocation = location;
			scienceTargetSubject = subject;
			scienceTargetExperiment = exp;
		}

		protected override string GetHashString()
		{
			return scienceTargetSubject.id;
		}

		protected override string GetTitle()
		{
			if (scienceLocation == BodyLocation.Space)
				return string.Format("Collect {0} data from in orbit around {1}.", scienceTargetExperiment.experimentTitle, scienceTargetBody.theName);
			else
				return string.Format("Collect {0} data from the surface of {1}.", scienceTargetExperiment.experimentTitle, scienceTargetBody.theName);
		}

		protected override void OnRegister()
		{
			GameEvents.OnScienceRecieved.Add(scienceRecieve);
		}

		protected override void OnUnregister()
		{
			GameEvents.OnScienceRecieved.Remove(scienceRecieve);
		}

		protected override void OnSave(ConfigNode node)
		{
			Debug.Log("[DM] Saving Contract Parameter");
			node.AddValue("ScienceTarget", scienceTargetBody.flightGlobalsIndex);
			node.AddValue("ScienceSubject", scienceTargetSubject.id);
		}

		protected override void OnLoad(ConfigNode node)
		{
			Debug.Log("[DM] Loading Contract Parameter");
			int targetBodyID;
			if (int.TryParse(node.GetValue("ScienceTarget"), out targetBodyID))
				scienceTargetBody = FlightGlobals.Bodies[targetBodyID];
			ScienceSubject trySub = ResearchAndDevelopment.GetSubjectByID(node.GetValue("ScienceSubject"));
			if (trySub != null)
				scienceTargetSubject = trySub;
		}

		private void scienceRecieve(float sci, ScienceSubject sub)
		{
			if (sub.id == scienceTargetSubject.id)
			{
				Debug.Log("[DM] Contract Complete");
				base.SetComplete();
			}
		}


	}

	#endregion

	[KSPAddon(KSPAddon.Startup.Flight, false)]
	class DMEvents: MonoBehaviour
	{
		public void Start()
		{
			Debug.Log("Adding Science Events");
			GameEvents.OnScienceChanged.Add(scienceChange);
			GameEvents.OnScienceRecieved.Add(scienceReceive);
		}

		public void OnDestroy()
		{
			Debug.Log("Removing Science Events");
			GameEvents.OnScienceChanged.Remove(scienceChange);
			GameEvents.OnScienceRecieved.Remove(scienceReceive);
		}

		public void scienceChange(float sci)
		{
			Debug.Log(string.Format("[Science Events] Science Changed by this much: {0}", sci.ToString()));
		}

		public void scienceReceive(float sci, ScienceSubject sub)
		{
			Debug.Log(string.Format("[Science Events] Subject {0} recieved for {1} science", sub.title, sci.ToString()));
		}

	}
}
