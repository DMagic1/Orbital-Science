using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Contracts;
using Contracts.Parameters;

namespace DMagic
{
	public class DMContract: Contract
	{

		private CelestialBody body = null;
		private BodyLocation scienceLocation;
		private ScienceExperiment exp = null;

		#region overrides

		protected override bool Generate()
		{
			if (body == null)
				body = FlightGlobals.Bodies[1];
			scienceLocation = BodyLocation.Space;
			if (exp == null)
				exp = ResearchAndDevelopment.GetExperiment("rpwsScan");
			this.agent = Contracts.Agents.AgentList.Instance.GetAgent("DMagic");
			this.AddParameter(new DMCollectScience(body, scienceLocation, exp), null);
			base.SetExpiry();
			base.SetScience(5f, body);
			base.SetDeadlineYears(100f, body);
			base.SetReputation(5f, 10f, body);
			base.SetFunds(100f, 10f, 50f, body);
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
			return body.name + scienceLocation.ToString() + exp.id;
		}

		protected override string GetTitle()
		{
			return "Contract to do something";
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
			Debug.Log("[DM] Body Set");
			ScienceExperiment sciExp = ResearchAndDevelopment.GetExperiment(node.GetValue("ScienceExperiment"));
			if (sciExp != null)
				exp = sciExp;
			Debug.Log("[DM] Experiment Set");
			string location = node.GetValue("TargetLocation");
			if (location != null)
				if (location == "Space")
					scienceLocation = BodyLocation.Space;
				else
					scienceLocation = BodyLocation.Surface;
			Debug.Log("[DM] Location Set");
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

	}

	#region Contract Parameter

	public class DMCollectScience: CollectScience
	{
		public CelestialBody scienceTargetBody;
		public BodyLocation scienceLocation;
		public ScienceExperiment targetScience;

		public DMCollectScience()
		{
		}

		public DMCollectScience(CelestialBody target, BodyLocation location, ScienceExperiment experiment)
		{
			scienceTargetBody = target;
			scienceLocation = location;
			targetScience = experiment;
		}

		protected override string GetHashString()
		{
			return scienceTargetBody.name + scienceLocation.ToString() + targetScience.id;
		}

		protected override string GetTitle()
		{
			if (scienceLocation == BodyLocation.Space)
				return string.Format("Collect {0} data from in orbit around {1}.", targetScience.experimentTitle, scienceTargetBody.theName);
			else
				return string.Format("Collect {0} data from the surface of {1}.", targetScience.experimentTitle, scienceTargetBody.theName);
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
			node.AddValue("ScienceExperiment", targetScience.id);
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
			Debug.Log("[DM] Param Body Set");
			ScienceExperiment exp = ResearchAndDevelopment.GetExperiment(node.GetValue("ScienceExperiment"));
			if (exp != null)
				targetScience = exp;
			Debug.Log("[DM] Param Experiment Set");
			string location = node.GetValue("TargetLocation");
			if (location != null)
				if (location == "Space")
					scienceLocation = BodyLocation.Space;
				else
					scienceLocation = BodyLocation.Surface;
			Debug.Log("[DM] Param Location Set");
		}

		private void scienceRecieve(float sci, ScienceSubject sub)
		{
			if (sub.id == targetScience.id + "@" + scienceTargetBody.name + ExperimentSituations.InSpaceLow.ToString())
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
