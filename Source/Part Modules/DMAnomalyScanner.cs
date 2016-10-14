#region license
/* DMagic Orbital Science - Anomaly Scanner
 * Anomaly detection and science data setup.
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

using DMagic.Scenario;
using System.Collections;
using UnityEngine;

namespace DMagic.Part_Modules
{
	class DMAnomalyScanner : DMModuleScienceAnimate
	{
		[KSPField]
		public string camAnimate = "";
		[KSPField]
		public string foundAnimate = "";
		[KSPField]
		public float resourceCost = 0f;

		private string closestAnom = "";
		private bool anomCloseRange, anomInRange, anomScienceInRange, camDeployed, rotating, closeRange, fullyDeployed = false;
		private Animation animSecondary;
		private Transform cam, dish;
		private DMAnomalyStorage currentAnomalies;
		private DMAnomalyObject currentAnomaly;
		private const string camTransform = "camBase";
		private const string dishTransform = "radarBaseArmNode0";

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);
			animSecondary = part.FindModelAnimators(camAnimate)[0];
			animSecondary = part.FindModelAnimators(foundAnimate)[0];
			if (IsDeployed)
				fullyDeployed = true;
			base.Events["CollectDataExternalEvent"].active = false;
			if (!HighLogic.LoadedSceneIsEditor)
			{
				cam = part.FindModelTransform(camTransform);
				dish = part.FindModelTransform(dishTransform);
				GameEvents.OnPQSCityLoaded.Add(PQSloaded);
				GameEvents.OnPQSCityUnloaded.Add(PQSunloaded);
			}
		}

		protected override void Update()
		{
			base.Update();

			if (HighLogic.LoadedSceneIsFlight)
			{
				if (IsDeployed)
				{
					if (PartResourceLibrary.Instance.GetDefinition(resourceExperiment) != null)
					{
						float cost = resourceCost * Time.deltaTime;
						part.RequestResource(resourceExperiment, cost);
					}
					if (fullyDeployed)
					{
						inRange();
						rotating = true;
						dishRotate();
					}
				}

				if (IsDeployed)
					DMAnomalyList.ScannerUpdating = true;
				else if (DMAnomalyList.ScannerUpdating)
					DMAnomalyList.ScannerUpdating = false;

				if (!fullyDeployed && rotating)
					spinDishDown();
			}
		}
		
		protected override void OnDestroy()
		{
			base.OnDestroy();

			DMAnomalyList.ScannerUpdating = false;
			if (!HighLogic.LoadedSceneIsEditor)
			{
				GameEvents.OnPQSCityLoaded.Remove(PQSloaded);
				GameEvents.OnPQSCityUnloaded.Remove(PQSunloaded);
			}
			
			DMAnomalyList.ScannerUpdating = false;
		}

		#region animators

		public void newSecondaryAnimator(string animName, float speed, float dishTime, WrapMode wrap)
		{
			if (animSecondary != null)
			{
				animSecondary[animName].speed = speed;
				animSecondary[animName].normalizedTime = dishTime;
				animSecondary[animName].wrapMode = wrap;
				anim.Blend(animName, 1f);
			}
		}

		public override void deployEvent()
		{
			if (!IsDeployed && fullyDeployed)
				StopCoroutine("retractEnumerator");
			StartCoroutine("deployEnumerator");
		}

		private IEnumerator deployEnumerator()
		{
			primaryAnimator(1f, 0, WrapMode.Default, animationName, anim);
			IsDeployed = true;

			yield return new WaitForSeconds(anim[animationName].length);

			fullyDeployed = true;
		}

		public override void retractEvent()
		{
			if (IsDeployed && !fullyDeployed)
				StopCoroutine("deployEnumerator");
			StartCoroutine("retractEnumerator");
		}

		private IEnumerator retractEnumerator()
		{
			if (fullyDeployed)
			{
				if (camDeployed)
				{
					animSecondary[foundAnimate].wrapMode = WrapMode.Default;
					cam.localRotation = Quaternion.Slerp(cam.localRotation, new Quaternion(0, 0, 0, 1), 1f);
					newSecondaryAnimator(camAnimate, -1f, 1f, WrapMode.Default);
					camDeployed = false;
				}
			}

			fullyDeployed = false;

			if (dish != null)
			{
				while (dish.localEulerAngles.y > 1)
					yield return null;
			}

			primaryAnimator(-1f, 1f, WrapMode.Default, animationName, anim);
			IsDeployed = false;
		}

		new public void editorDeployEvent()
		{
			deployEvent();
			newSecondaryAnimator(camAnimate, 1f, 1f, WrapMode.Default);
			newSecondaryAnimator(foundAnimate, 1f, 0f, WrapMode.PingPong);
			IsDeployed = false;
			Events["editorDeployEvent"].active = false;
			Events["editorRetractEvent"].active = true;
		}

		new public void editorRetractEvent()
		{
			retractEvent();
			fullyDeployed = false;
			IsDeployed = false;
			newSecondaryAnimator(camAnimate, -1f, 0f, WrapMode.Default);
			animSecondary[foundAnimate].wrapMode = WrapMode.Default;
			Events["editorDeployEvent"].active = true;
			Events["editorRetractEvent"].active = false;
		}

		//Rotate camera on its y-axis to look at the anomaly.
		private void camRotate(Vector3 anom)
		{
			DMUtils.DebugLog("Rotating Camera To [{0}]", anom);
			Vector3 localAnom = transform.InverseTransformPoint(anom);
			Vector3 toTarget = localAnom - part.transform.position;
			toTarget.y = 0;
			Quaternion lookToTarget = Quaternion.LookRotation(localAnom);
			lookToTarget.x = 0;
			lookToTarget.z = 0;
			cam.localRotation = Quaternion.Slerp(cam.localRotation, lookToTarget, Time.deltaTime * 2f);
		}

		//Slowly rotate dish
		private void dishRotate()
		{
			if (dish != null)
				dish.Rotate(Vector3.up * Time.deltaTime * 40f);
		}

		private void spinDishDown()
		{
			if (dish != null)
			{
				if (dish.localEulerAngles.y > 1)
					dish.Rotate(Vector3.up * Time.deltaTime * 60f);
				else
					rotating = false;
			}
		}

		#endregion

		#region anomaly detection
		
		private void PQSloaded(CelestialBody body, string name)
		{
			if (body == null)
				return;

			if (string.IsNullOrEmpty(name))
				return;

			if (body.pqsController == null)
				return;

			PQSSurfaceObject[] Cities = body.pqsSurfaceObjects;

			for (int i = 0; i < Cities.Length; i++)
			{
				PQSSurfaceObject city = Cities[i];

				if (city == null)
					continue;

				if (city.transform.parent.name != body.name)
					continue;

				if (city.SurfaceObjectName != name)
					continue;

				currentAnomaly = new DMAnomalyObject(city);
			}
		}

		private void PQSunloaded(CelestialBody body, string name)
		{
			currentAnomaly = null;
		}

		private void inRange()
		{
			anomInRange = false;

			currentAnomalies = DMAnomalyList.getAnomalyStorage(vessel.mainBody.name);

			if (currentAnomalies == null)
			{
				if (currentAnomaly == null)
					return;

				checkAnomalyDistance(currentAnomaly);
			}
			else
			{
				for (int i = 0; i < currentAnomalies.AnomalyCount; i++)
				{
					DMAnomalyObject anom = currentAnomalies.getAnomaly(i);

					if (checkAnomalyDistance(anom))
						break;
				}
			}

			if (!anomInRange && camDeployed)
			{
				animSecondary[foundAnimate].wrapMode = WrapMode.Default;
				cam.localRotation = Quaternion.Slerp(cam.localRotation, new Quaternion(0, 0, 0, 1), 1f);
				newSecondaryAnimator(camAnimate, -1f, 1f, WrapMode.Default);
				camDeployed = false;
			}
		}

		private bool checkAnomalyDistance(DMAnomalyObject a)
		{
			if (a == null)
				return false;

			DMAnomalyList.updateAnomaly(vessel, a);

			if (a.VDistance >= 50000)
				return false;

			if (a.VHorizontal >= (11000 * (1 - a.VHeight / 6000)))
				return false;

			anomInRange = true;

			if (a.VHorizontal >= (10000 * (1 - a.VHeight / 5000)))
				return false;

			if (!camDeployed)
			{
				newSecondaryAnimator(camAnimate, 1f, 0f, WrapMode.Default);
				camDeployed = true;
			}

			if (camDeployed)
			{
				camRotate(a.WorldLocation);
				if (a.VDistance < 250 && closeRange == false)
				{
					newSecondaryAnimator(foundAnimate, 1f, 0f, WrapMode.PingPong);
					closeRange = true;
					return true;
				}
				if (a.VDistance >= 275 && closeRange == true)
				{
					animSecondary[foundAnimate].wrapMode = WrapMode.Default;
					closeRange = false;
				}
			}

			return false;
		}

		internal void getAnomValues()
		{
			anomCloseRange = false;
			anomScienceInRange = false;
			closestAnom = "";

			currentAnomalies = DMAnomalyList.getAnomalyStorage(vessel.mainBody.name);

			if (currentAnomalies == null)
			{
				if (currentAnomaly == null)
					return;

				checkAnomalyForScience(currentAnomaly);
			}
			else
			{
				for (int i = 0; i < currentAnomalies.AnomalyCount; i++)
				{
					DMAnomalyObject anom = currentAnomalies.getAnomaly(i);

					if (checkAnomalyForScience(anom))
						break;					
				}
			}
		}

		private bool checkAnomalyForScience(DMAnomalyObject a)
		{
			if (a == null)
				return false;

			DMAnomalyList.updateAnomaly(vessel, a);

			if (a.VDistance >= 50000)
				return false;

			//Determine cutoff distance on sliding scale based on altitude above the anomaly.
			if (a.VHorizontal >= (30000 * (1 - a.VHeight / 15000)))
				return false;

			//Calculate the bearing to the anomaly from the current vessel position.
			DMAnomalyList.bearing(vessel, a);

			//Get cardinal directions based on the bearing.
			string anomDirection = direction(a.Bearing);

			anomScienceInRange = true;

			DMUtils.Logging("Anomaly: {0} is at bearing: {1:N1} deg at a distance of {2:N1}m.", a.Name, a.Bearing, a.VDistance);

			//Scanning range distance for science experiment.
			if (a.VDistance < 250)
			{
				closestAnom = a.Name;
				anomCloseRange = true;
				return true;
			}
			//Use alternate message when more than 10km above the anomaly.
			else if (a.VHeight > 10000)
			{
				ScreenMessages.PostScreenMessage(string.Format("Anomalous signal detected approximately {0:N1} km below current position, get closer for a better signal.", a.VDistance / 1000 + RandomDouble((2 * (a.VDistance / 1000) / 30), (4 * (a.VDistance / 1000) / 30))), 6f, ScreenMessageStyle.UPPER_CENTER);
			}
			else
			{
				ScreenMessages.PostScreenMessage(string.Format("Anomalous signal detected approximately {0:N1} km away to the {1}, get closer for a better signal.", a.VDistance / 1000 + RandomDouble((2 * (a.VDistance / 1000) / 30), (4 * (a.VDistance / 1000) / 30)), anomDirection), 6f, ScreenMessageStyle.UPPER_CENTER);
			}

			return false;
		}

		//Random number to fudge the distance estimate in the screen messages above; extent of fudging based on total distance to the anomaly, +- 2km at 30km away.
		private double RandomDouble(double min, double max)
		{
			System.Random randomd = new System.Random();
			double random = randomd.NextDouble();
			return (random * max) - min;
		}

		private string direction(double bearing)
		{
			if (bearing >= 0 && bearing < 22.5) return "North";
			else if (bearing >= 22.5 && bearing < 67.5) return "NorthEast";
			else if (bearing >= 67.5 && bearing < 112.5) return "East";
			else if (bearing >= 112.5 && bearing < 157.5) return "SouthEast";
			else if (bearing >= 157.5 && bearing < 202.5) return "South";
			else if (bearing >= 202.5 && bearing < 247.5) return "SouthWest";
			else if (bearing >= 247.5 && bearing < 292.5) return "West";
			else if (bearing >= 292.5 && bearing < 337.5) return "NorthWest";
			else if (bearing >= 337.5 && bearing < 360) return "North";
			else return "???";
		}

		#endregion

		#region experiment setup
		
		public override void gatherScienceData(bool silent = false)
		{
			if (canConduct())
			{
				if (!IsDeployed)
					deployEvent();

				getAnomValues();

				if (anomScienceInRange)
				{
					if (anomCloseRange)
						runExperiment(getSituation(), silent);
				}
				else
					ScreenMessages.PostScreenMessage("No anomalous signals detected.", 5f, ScreenMessageStyle.UPPER_CENTER);
			}
			else
				ScreenMessages.PostScreenMessage(failMessage, 5f, ScreenMessageStyle.UPPER_CENTER);
		}

		public override bool canConduct()
		{
			failMessage = "";
			if (Inoperable)
			{
				failMessage = "Experiment is no longer functional; must be reset at a science lab or returned to Kerbin";
				return false;
			}
			else if (Deployed)
			{
				failMessage = storageFullMessage;
				return false;
			}
			else if (storedScienceReports.Count > 0 && experimentLimit <= 1)
			{
				failMessage = storageFullMessage;
				return false;
			}

			return true;
		}

		public override string getBiome(ExperimentSituations s)
		{
			return anomalyCleanup(closestAnom);
		}

		internal static string anomalyCleanup(string anomName)
		{
			switch (anomName)
			{
				case "UFO":
				case "KSC":
				case "Cave":
				case "Face":
				case "Pyramids":
				case "Icehenge":
				case "Pyramid":
					return anomName;
				case "Randolith":
					return "Random Monolith";
				case "IslandAirfield":
					return "Island Airfield";
				case "KSC2":
					return "KSC 2";
				case "Monolith00":
					return "Monolith 1";
				case "Monolith01":
					return "Monolith 2";
				case "MSL":
					return "Mast Camera";
				case "Monolith02":
					return "Monolith 3";
				case "RockArch01":
					return "Rock Arch 2";
				case "ArmstrongMemorial":
					return "Armstrong Memorial";
				case "RockArch00":
					return "Rock Arch 1";
				case "DeadKraken":
					return "Dead Kraken";
				case "RockArch02":
					return "Rock Arch 3";
				default:
					return anomName;
			}
		}

		protected override string situationCleanup(ExperimentSituations expSit, string b)
		{
			if (expSit == ExperimentSituations.SrfLanded)
				return string.Format(" of the {0} from {1}'s surface", b, vessel.mainBody.theName);
			if (expSit == ExperimentSituations.FlyingLow)
				return string.Format(" while flying above the {0} on {1}", b, vessel.mainBody.theName);
			return "Dummy";
		}

		public override ExperimentSituations getSituation()
		{
			switch (vessel.situation)
			{
				case Vessel.Situations.LANDED:
				case Vessel.Situations.PRELAUNCH:
				case Vessel.Situations.SPLASHED:
					return ExperimentSituations.SrfLanded;
				default:
					return ExperimentSituations.FlyingLow;
			}
		}

		#endregion

	}
}
