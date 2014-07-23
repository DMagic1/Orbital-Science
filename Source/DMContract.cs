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
						Debug.Log(string.Format("[DM] Part: [{0}] Purchased", aPart.name));
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
				Debug.Log(string.Format("[DM] Experimental Situation: {0}", targetSituation.ToString()));
				scienceLocation = setBodyLocation(targetSituation);
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

			biomeIsRelevant = biomeRelevant(targetSituation, DMscience.bioMask);

			if (biomeIsRelevant)
			{
				Debug.Log("[DM] Checking For Biome Usage");
				int i = rand.Next(0, 1);
				if (i == 0)
					biome = fetchBiome(body);
			}

			if (DMscience.agent != "Any")
				this.agent = Contracts.Agents.AgentList.Instance.GetAgent(DMscience.agent);

			this.AddParameter(new DMCollectScience(body, scienceLocation, sub, exp, biomeIsRelevant), null);
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
			return string.Format("Collect {0} data at {1}", exp.experimentTitle, body.theName);
		}

		protected override string GetDescription()
		{
			return "Do something!";
		}

		protected override string GetSynopsys()
		{
			if (biomeIsRelevant)
			{
				if (targetSituation == ExperimentSituations.InSpaceLow || targetSituation == ExperimentSituations.InSpaceHigh)
					return string.Format("We need you to record some {0} observations from in orbit above the {2} around {1}", exp.experimentTitle, body.theName, biome);
				else if (targetSituation == ExperimentSituations.SrfLanded)
					return string.Format("We need you to record some {0} observations from the {2} while on the surface of {1}", exp.experimentTitle, body.theName, biome);
				else if (targetSituation == ExperimentSituations.SrfSplashed)
					return string.Format("We need you to record some {0} observations from the {2} while on the oceans of {1}", exp.experimentTitle, body.theName, biome);
				else if (targetSituation == ExperimentSituations.FlyingLow || targetSituation == ExperimentSituations.FlyingHigh)
					return string.Format("We need you to record some {0} observations during atmospheric flight over the {2} at {1}", exp.experimentTitle, body.theName, biome);
			}
			else
			{
				if (targetSituation == ExperimentSituations.InSpaceLow || targetSituation == ExperimentSituations.InSpaceHigh)
					return string.Format("We need you to record some {0} observations from orbit around {1}", exp.experimentTitle, body.theName);
				else if (targetSituation == ExperimentSituations.SrfLanded)
					return string.Format("We need you to record some {0} observations from the surface of {1}", exp.experimentTitle, body.theName);
				else if (targetSituation == ExperimentSituations.SrfSplashed)
					return string.Format("We need you to record some {0} observations from the oceans of {1}", exp.experimentTitle, body.theName);
				else if (targetSituation == ExperimentSituations.FlyingLow || targetSituation == ExperimentSituations.FlyingHigh)
					return string.Format("We need you to record some {0} observations during atmospheric flight at {1}", exp.experimentTitle, body.theName);
			}
			return string.Format("We need you to record some {0} observations from {1}", exp.experimentTitle, body.theName);
		}

		protected override string MessageCompleted()
		{
			return string.Format("You recovered {0} from {1}, well done.", exp.experimentTitle, body.theName);
		}

		protected override void OnLoad(ConfigNode node)
		{
			Debug.Log("[DM] Loading Contract");
			int targetBodyID, targetLocation;
			if (int.TryParse(node.GetValue("ScienceTarget"), out targetBodyID))
				body = FlightGlobals.Bodies[targetBodyID];
			ScienceSubject trySub = ResearchAndDevelopment.GetSubjectByID(node.GetValue("ScienceSubject"));
			if (trySub != null)
				sub = trySub;
			ScienceExperiment tryExp = ResearchAndDevelopment.GetExperiment(node.GetValue("ScienceExperiment"));
			if (tryExp != null)
				exp = tryExp;
			if (int.TryParse(node.GetValue("ScienceLocation"), out targetLocation))
				scienceLocation = (BodyLocation)targetLocation;
		}

		protected override void OnSave(ConfigNode node)
		{
			Debug.Log("[DM] Saving Contract");
			node.AddValue("ScienceTarget", body.flightGlobalsIndex);
			node.AddValue("ScienceSubject", sub.id);
			node.AddValue("ScienceExperiment", exp.id);
			node.AddValue("ScienceLocation", (int)scienceLocation);
		}

		public override bool MeetRequirements()
		{
			return true;
		}

		#endregion

		#region Utilities

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
				bList = GetBodies_NextUnreached(4, null);
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
			//ExperimentSituations expMask = (ExperimentSituations)i;
			if (((ExperimentSituations)i & ExperimentSituations.FlyingHigh) == ExperimentSituations.FlyingHigh)
				expSitList.Add(ExperimentSituations.FlyingHigh);
			if (((ExperimentSituations)i & ExperimentSituations.FlyingLow) == ExperimentSituations.FlyingLow)
				expSitList.Add(ExperimentSituations.FlyingLow);
			if (((ExperimentSituations)i & ExperimentSituations.InSpaceHigh) == ExperimentSituations.InSpaceHigh)
				expSitList.Add(ExperimentSituations.InSpaceHigh);
			if (((ExperimentSituations)i & ExperimentSituations.InSpaceLow) == ExperimentSituations.InSpaceLow)
				expSitList.Add(ExperimentSituations.InSpaceLow);
			if (((ExperimentSituations)i & ExperimentSituations.SrfLanded) == ExperimentSituations.SrfLanded)
				expSitList.Add(ExperimentSituations.SrfLanded);
			if (((ExperimentSituations)i & ExperimentSituations.SrfSplashed) == ExperimentSituations.SrfSplashed)
				expSitList.Add(ExperimentSituations.SrfSplashed);
			Debug.Log(string.Format("[DM] Found {0} Valid Experimental Situations", expSitList.Count));
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
			Debug.Log("[DM] Searching For Biomes");
			string s = "";
			if (b.BiomeMap == null || b.BiomeMap.Map == null)
				return s;
			else
				s = b.BiomeMap.Attributes[rand.Next(0, b.BiomeMap.Attributes.Length - 1)].name;
			Debug.Log(string.Format("[DM] Found Biome: {0}", s));
			return s;
		}

		#endregion

	}

	#region Contract Parameter

	public class DMCollectScience: CollectScience
	{
		public CelestialBody scienceTargetBody;
		public BodyLocation scienceLocation;
		public ScienceSubject scienceTargetSubject;
		public ScienceExperiment scienceTargetExperiment;
		public bool biome;

		public DMCollectScience()
		{
		}

		public DMCollectScience(CelestialBody target, BodyLocation location, ScienceSubject subject, ScienceExperiment exp, bool Biome)
		{
			scienceTargetBody = target;
			scienceLocation = location;
			scienceTargetSubject = subject;
			scienceTargetExperiment = exp;
			biome = Biome;
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
			node.AddValue("ScienceExperiment", scienceTargetExperiment.id);
			node.AddValue("ScienceLocation", (int)scienceLocation);
			node.AddValue("BiomeRelevant", biome);
		}

		protected override void OnLoad(ConfigNode node)
		{
			Debug.Log("[DM] Loading Contract Parameter");
			int targetBodyID, targetLocation;
			if (int.TryParse(node.GetValue("ScienceTarget"), out targetBodyID))
				scienceTargetBody = FlightGlobals.Bodies[targetBodyID];
			ScienceSubject trySub = ResearchAndDevelopment.GetSubjectByID(node.GetValue("ScienceSubject"));
			if (trySub != null)
				scienceTargetSubject = trySub;
			ScienceExperiment tryExp = ResearchAndDevelopment.GetExperiment(node.GetValue("ScienceExperiment"));
			if (tryExp != null)
				scienceTargetExperiment = tryExp;
			if (int.TryParse(node.GetValue("ScienceLocation"), out targetLocation))
				scienceLocation = (BodyLocation)targetLocation;
			biome = bool.Parse(node.GetValue("BiomeRelevant"));
		}

		private void scienceRecieve(float sci, ScienceSubject sub)
		{
			if (biome)
			{
				if (sub.id == scienceTargetSubject.id)
				{
					Debug.Log("[DM] Contract Complete");
					base.SetComplete();
				}
			}
			else
			{
				Debug.Log("[DM] Figure Something Out Dummy!!!");
				if (sub.id.StartsWith(scienceTargetSubject.id))
				{
					Debug.Log("[DM] Contract Complete");
					base.SetComplete();
				}
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
