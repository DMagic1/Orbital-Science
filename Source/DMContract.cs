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

		#region overrides

		protected override bool Generate()
		{
			System.Random rand = new System.Random();
			DMscience = DMConfigLoader.availableScience.ElementAt(rand.Next(0, DMConfigLoader.availableScience.Count - 1)).Value;
			if (DMscience.sciPart != "None")
			{
				pTechNode = ResearchAndDevelopment.Instance.GetTechState(DMscience.sciNode);
				if (pTechNode == null)
					return false;
				else
				{
					if (pTechNode.state != RDTech.State.Available)
						return false;
					else
					{
						aPart = pTechNode.partsPurchased.FirstOrDefault(p => p.name == DMscience.sciPart);
						if (aPart == null)
							return false;
					}
				}
			}

			if (body == null)
				body = nextBody();
			if (exp == null)
				exp = DMscience.exp;

			situations = availableSituations(DMscience.sitMask);

			if (situations.Count == 0)
				return false;
			else
			{
				System.Random randSit = new System.Random();
				targetSituation = situations[randSit.Next(0, situations.Count - 1)];
			}

			if (sub == null)
				return false;
			else
			{
				sub = ResearchAndDevelopment.GetExperimentSubject(exp, targetSituation, body, biome);
				sub.subjectValue = DMModuleScienceAnimate.fixSubjectValue(targetSituation, sub.subjectValue, 1f, body);
			}

			if (sub.scientificValue < 0.4f)
				return false;

			if (DMscience.agent != "Any")
				this.agent = Contracts.Agents.AgentList.Instance.GetAgent(DMscience.agent);

			this.AddParameter(new DMCollectScience(body, scienceLocation, sub, exp), null);

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
			int targetBodyID = int.Parse(node.GetValue("ScienceTarget"));
			foreach (CelestialBody cBody in FlightGlobals.Bodies)
			{
				if (cBody.flightGlobalsIndex == targetBodyID)
					body = cBody;
			}
			ScienceExperiment sciExp = ResearchAndDevelopment.GetExperiment(node.GetValue("ScienceExperiment"));
			if (sciExp != null)
				exp = sciExp;
			string location = node.GetValue("TargetLocation");
			if (location != null)
				if (location == "Space")
					scienceLocation = BodyLocation.Space;
				else
					scienceLocation = BodyLocation.Surface;
		}

		protected override void OnSave(ConfigNode node)
		{
			Debug.Log("[DM] Saving Contract");
			node.AddValue("ScienceTarget", body.flightGlobalsIndex);
			node.AddValue("ScienceExperiment", exp.id);
			node.AddValue("TargetLocation", scienceLocation);
		}

		public override bool MeetRequirements()
		{
			return true;
		}

		#endregion

		private CelestialBody nextBody()
		{
			GetBodies(true, true);
			return null;
		}

		private List<ExperimentSituations> availableSituations(int i)
		{
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
			node.AddValue("ScienceExperiment", scienceTargetSubject.id);
			node.AddValue("TargetLocation", scienceLocation);
		}

		protected override void OnLoad(ConfigNode node)
		{
			Debug.Log("[DM] Loading Contract Parameter");
			int targetBodyID = int.Parse(node.GetValue("ScienceTarget"));
			foreach (CelestialBody body in FlightGlobals.Bodies)
			{
				if (body.flightGlobalsIndex == targetBodyID)
					scienceTargetBody = body;
			}
			ScienceExperiment exp = ResearchAndDevelopment.GetExperiment(node.GetValue("ScienceExperiment"));
			if (exp != null)
				scienceTargetExperiment = exp;
			string location = node.GetValue("TargetLocation");
			if (location != null)
				if (location == "Space")
					scienceLocation = BodyLocation.Space;
				else
					scienceLocation = BodyLocation.Surface;
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
