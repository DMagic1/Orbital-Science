﻿
#region license
/* DMagic Orbital Science - Seismic Sensor
 * Science Module For Seismic Sensor Pod
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
 */
#endregion

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

			if (state == StartState.Editor)
				return;

			if (IsDeployed)
				Fields["scoreString"].guiActive = true;
			else
				Fields["scoreString"].guiActive = false;

			Fields["scoreString"].guiName = "Experiment Value";

			GameEvents.onVesselWasModified.Add(onVesselModified);
		}

		public override string GetInfo()
		{
			string info = base.GetInfo();

			string ranges = string.Format("\nIdeal Seismic Pod Ranges:\nNear: {0:N0}m - {1:N0}m\nFar: {2:N0}m - {3:N0}m", DMSeismicHandler.nearPodThreshold, DMSeismicHandler.nearPodMaxDistance, DMSeismicHandler.farPodThreshold, DMSeismicHandler.farPodMaxDistance);

			string angles = string.Format("\nIdeal Seismic Pod Angle Difference: {0:N0}° - 180°", DMSeismicHandler.podAngleThreshold);

			return info + ranges + angles; ;
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);
		}

		public override void OnSave(ConfigNode node)
		{
			base.OnSave(node);
		}

		private void OnDestroy()
		{
			GameEvents.onVesselWasModified.Remove(onVesselModified);
		}

		private void onVesselModified(Vessel v)
		{
			if (v == null)
				return;

			if (vessel != v)
				return;

			values.OnAsteroid = DMAsteroidScience.AsteroidGrappled;
		}

		private void Update()
		{
			base.EventsCheck();

			if (!HighLogic.LoadedSceneIsFlight)
				return;

			if (values != null)
			{
				if (vessel.Landed || values.OnAsteroid)
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

			getScienceData(values.NearbySensorCount <= 0, values.OnAsteroid);
		}

		private void getScienceData(bool sensorOnly, bool asteroid)
		{
			ScienceData data = DMSeismicHandler.makeData(values.getBestHammer(), exp, experimentID, sensorOnly, asteroid);

			if (data == null)
				return;

			GameEvents.OnExperimentDeployed.Fire(data);

			scienceReports.Add(data);
			Deployed = true;
			ReviewData();
		}

		protected override bool canConduct()
		{
			failMessage = "";
			if (Inoperable)
			{
				failMessage = "Experiment is no longer functional; must be reset at a science lab or returned to Kerbin";
				return false;
			}
			else if (Deployed)
			{
				failMessage = experimentFullMessage;
				return false;
			}
			else if (scienceReports.Count > 0)
			{
				failMessage = experimentFullMessage;
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
