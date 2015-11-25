using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DMagic.Part_Modules
{
	public class DMSeismicSensor : DMBasicScienceModule, IDMSeismometer
	{
		[KSPField]
		public float baseExperimentValue = 0.2f;
		[KSPField(guiActive = false)]
		public string scoreString = "0%";

		private Dictionary<uint, DMSeismicHammer> nearbyHammers = new Dictionary<uint, DMSeismicHammer>();

		private string failMessage;

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);

			if (IsDeployed)
				experimentArm = true;
			else
				experimentArm = false;
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);
		}

		public override void OnSave(ConfigNode node)
		{
			base.OnSave(node);
		}

		private void Update()
		{
			base.EventsCheck();

			scoreString = experimentScore.ToString("P0");
		}

		public override void deployEvent()
		{
			base.deployEvent();
			experimentArm = true;
		}

		public override void retractEvent()
		{
			base.retractEvent();
			experimentArm = false;
		}

		#region Science Setup

		new public void DeployExperiment()
		{
			if (!canConduct())
			{
				ScreenMessages.PostScreenMessage(failMessage, 5f, ScreenMessageStyle.UPPER_CENTER);
				return;
			}

			getScienceData(nearbyHammers.Count <= 0, DMAsteroidScience.AsteroidGrappled);
		}

		private void getScienceData(bool sensorOnly, bool asteroid)
		{
			ScienceData data = DMSeismicHandler.makeData(this, part.flightID, exp, experimentID, vessel.mainBody, vessel, sensorOnly, asteroid);

			if (data == null)
				return;

			scienceReports.Add(data);
			Deployed = true;
			ReviewData();
		}

		private bool canConduct()
		{
			failMessage = "";
			if (Inoperable)
			{
				failMessage = "Experiment is no longer functional; must be reset at a science lab or returned to Kerbin";
				return false;
			}
			else if (Deployed)
			{
				failMessage = customFailMessage;
				return false;
			}
			else if (scienceReports.Count > 0)
			{
				failMessage = customFailMessage;
				return false;
			}
			else if (vessel.situation != Vessel.Situations.LANDED && vessel.situation != Vessel.Situations.PRELAUNCH && !DMAsteroidScience.AsteroidGrappled)
			{
				failMessage = customFailMessage;
				return false;
			}
			else if (FlightGlobals.ActiveVessel.isEVA)
			{
				if (!ScienceUtil.RequiredUsageExternalAvailable(part.vessel, FlightGlobals.ActiveVessel, (ExperimentUsageReqs)usageReqMaskExternal, exp, ref usageReqMessage))
				{
					failMessage = usageReqMessage;
					return false;
				}
				else
					return true;
			}
			else
				return true;
		}

		#endregion

		#region IDMSeismometer

		public void addSeismometer(IDMSeismometer s, Vector2 v = new Vector2())
		{
			if (nearbyHammers.ContainsKey(((DMSeismicHammer)s).part.flightID))
				return;

			nearbyHammers.Add(((DMSeismicHammer)s).part.flightID, (DMSeismicHammer)s);
		}

		public void removeSeismometer(IDMSeismometer s)
		{
			if (nearbyHammers.ContainsKey(((DMSeismicHammer)s).part.flightID))
				nearbyHammers.Remove(((DMSeismicHammer)s).part.flightID);
		}

		public void updateScore()
		{
			if (nearbyHammers.Count <= 0)
				experimentScore = baseExperimentValue;
			else
				experimentScore = nearbyHammers.Values.OrderBy(h => h.experimentScore).Last().experimentScore;
		}

		public float experimentScore { get; set; }

		public bool experimentArm { get; set; }

		#endregion

		public bool SensorsInRange
		{
			get { return nearbyHammers.Count > 0; }
		}
	}
}
