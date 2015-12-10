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
		public string camAnimate = null;
		[KSPField]
		public string foundAnimate = null;
		[KSPField]
		public float resourceCost = 0f;

		private string closestAnom = null;
		private bool anomCloseRange, anomInRange, camDeployed, rotating, closeRange, fullyDeployed = false;
		private Animation animSecondary;
		private Transform cam, dish;
		private const string camTransform = "camBase";
		private const string dishTransform = "radarBaseArmNode0";

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);
			animSecondary = part.FindModelAnimators(camAnimate)[0];
			animSecondary = part.FindModelAnimators(foundAnimate)[0];
			if (IsDeployed)
				fullyDeployed = true;
			base.labDataBoost = 0.45f;
			base.Events["CollectDataExternalEvent"].active = false;
			if (!HighLogic.LoadedSceneIsEditor)
			{
				cam = part.FindModelTransform(camTransform);
				dish = part.FindModelTransform(dishTransform);
			}
		}

		protected override void Update()
		{
			base.Update();

			if (HighLogic.LoadedSceneIsFlight)
			{
				if (DMScienceScenario.SciScenario == null)
					return;

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

				if (DMScienceScenario.SciScenario.anomalyList != null)
				{
					if (IsDeployed)
						DMScienceScenario.SciScenario.anomalyList.ScannerUpdating = true;
					else if (DMScienceScenario.SciScenario.anomalyList.ScannerUpdating)
						DMScienceScenario.SciScenario.anomalyList.ScannerUpdating = false;
				}

				if (!fullyDeployed && rotating)
					spinDishDown();
			}
		}

		new private void OnDestroy()
		{
			if (DMScienceScenario.SciScenario != null)
			{
				if (DMScienceScenario.SciScenario.anomalyList != null)
					DMScienceScenario.SciScenario.anomalyList.ScannerUpdating = false;
			}
		}

		#region animators

		public void newSecondaryAnimator(string animName, float dishSpeed, float dishTime, WrapMode wrap)
		{
			if (animSecondary != null)
			{
				animSecondary[animName].speed = dishSpeed;
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

		private void inRange()
		{
			bool anomInRange = false;

			foreach (DMAnomalyObject anom in DMScienceScenario.SciScenario.anomalyList.anomObjects())
			{
				DMAnomalyList.updateAnomaly(vessel, anom);
				if (anom.VDistance < 50000)
				{
					if (anom.VHorizontal < (11000 * (1 - anom.VHeight / 6000)))
					{
						anomInRange = true;
						if (anom.VHorizontal < (10000 * (1 - anom.VHeight / 5000)))
						{
							if (!camDeployed)
							{
								newSecondaryAnimator(camAnimate, 1f, 0f, WrapMode.Default);
								camDeployed = true;
								if (anom.VDistance < 250)
								{
									newSecondaryAnimator(foundAnimate, 1f, 0f, WrapMode.PingPong);
									closeRange = true;
									break;
								}
								else
								{
									closeRange = false;
								}
							}
							if (camDeployed)
							{
								camRotate(anom.WorldLocation);
								if (anom.VDistance < 250 && closeRange == false)
								{
									newSecondaryAnimator(foundAnimate, 1f, 0f, WrapMode.PingPong);
									closeRange = true;
									break;
								}
								if (anom.VDistance >= 275 && closeRange == true)
								{
									animSecondary[foundAnimate].wrapMode = WrapMode.Default;
									closeRange = false;
								}
							}
						}
					}
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

		private void getAnomValues()
		{
			anomCloseRange = false;
			anomInRange = false;
			closestAnom = "";
			foreach (DMAnomalyObject anom in DMScienceScenario.SciScenario.anomalyList.anomObjects())
			{
				DMAnomalyList.updateAnomaly(vessel, anom);
				if (anom.VDistance < 100000)
				{
					if (anom.VHorizontal < (30000 * (1 - anom.VHeight / 15000)))	//Determine cutoff distance on sliding scale based on altitude above the anomaly.
					{
						DMAnomalyList.bearing(vessel, anom);			//Calculate the bearing to the anomaly from the current vessel position.
						string anomDirection = direction(anom.Bearing);			//Get cardinal directions based on the bearing.
						anomInRange = true;
						DMUtils.Logging("Anomaly: {0} is at bearing: {1:N1} deg at a distance of {2:N1}m.", anom.Name, anom.Bearing, anom.VDistance);
						if (anom.VDistance < 250)           //Scanning range distance for science experiment.
						{
							closestAnom = anom.Name;
							anomCloseRange = true;
							break;
						}
						else if (anom.VHeight > 10000)				//Use alternate message when more than 10km above the anomaly.
						{
							ScreenMessages.PostScreenMessage(string.Format("Anomalous signal detected approximately {0:N1} km below current position, get closer for a better signal", anom.VDistance / 1000 + RandomDouble((2 * (anom.VDistance / 1000) / 30), (4 * (anom.VDistance / 1000) / 30))), 6f, ScreenMessageStyle.UPPER_CENTER);
						}
						else
						{
							ScreenMessages.PostScreenMessage(string.Format("Anomalous signal detected approximately {0:N1} km away to the {1}, get closer for a better signal.", anom.VDistance / 1000 + RandomDouble((2 * (anom.VDistance / 1000) / 30), (4 * (anom.VDistance / 1000) / 30)), anomDirection), 6f, ScreenMessageStyle.UPPER_CENTER);
						}
					}
				}
			}
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
				if (anomInRange)
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

		protected override string getBiome(ExperimentSituations s)
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

		protected override ExperimentSituations getSituation()
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

		protected override void onComplete(ScienceData data)
		{
			data.transmitValue = 0.95f;
			base.onComplete(data);
		}

		#endregion

	}
}
