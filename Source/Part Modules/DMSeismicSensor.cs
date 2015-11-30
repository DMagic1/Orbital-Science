using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DMagic.Part_Modules
{
	public class DMSeismicSensor : DMBasicScienceModule, IDMSeismometer
	{
		[KSPField(isPersistant = true)]
		public float baseExperimentValue = 0.2f;
		[KSPField(guiActive = false)]
		public string scoreString = "0%";

		private string failMessage;
		private DMSeismometerValues values;

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);

			if (IsDeployed)
				Fields["scoreString"].guiActive = true;
			else
				Fields["scoreString"].guiActive = false;

			Fields["scoreString"].guiName = "Experiment Value";
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

			if (!HighLogic.LoadedSceneIsFlight)
				return;

			if (values != null)
			{
				if (vessel.Landed)
					scoreString = values.Score.ToString("P0");
				else
					scoreString = "Not Valid";
			}
			else
				values = DMSeismicHandler.Instance.getSeismicSensor(part.flightID);
		}

		protected override void EventsCheck()
		{
			base.EventsCheck();

			Events["DeployExperiment"].active = IsDeployed;
		}

		public override void deployEvent()
		{
			base.deployEvent();
			Fields["scoreString"].guiActive = true;
			if (values != null)
				values.Armed = true;
		}

		public override void retractEvent()
		{
			base.retractEvent();
			Fields["scoreString"].guiActive = false;
			if (values != null)
				values.Armed = false;
		}

		#region Science Setup

		new public void DeployExperiment()
		{
			if (!canConduct())
			{
				ScreenMessages.PostScreenMessage(failMessage, 5f, ScreenMessageStyle.UPPER_CENTER);
				return;
			}

			if (!IsDeployed)
				deployEvent();

			getScienceData(values.NearbySensorCount <= 0, DMAsteroidScience.AsteroidGrappled);
		}

		private void getScienceData(bool sensorOnly, bool asteroid)
		{
			ScienceData data = DMSeismicHandler.makeData(values.getBestHammer(), exp, experimentID, sensorOnly, asteroid);

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
	}
}
