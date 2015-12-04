#region license
/* DMagic Orbital Science - Seismic Hammer
 * Science Module For Seismic Hammer Experiment
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DMagic.Part_Modules
{
	class DMSeismicHammer : DMBasicScienceModule, IDMSeismometer
	{
		[KSPField]
		public string hammerAnimation = "";
		[KSPField(isPersistant = true)]
		public float baseExperimentValue = 0.4f;
		[KSPField(guiActive = false)]
		public string scoreString = "0%";

		private Animation Anim;
		private string failMessage;
		private Transform RotationTransform;
		private Transform ExtensionTransform;
		private bool dryRun = true;
		private DMSeismometerValues values;
	
		private const string rotationTransformName = "RotationTransform";
		private const string extensionTransformName = "ThumperCasing";
		private const string potato = "PotatoRoid";

		public override void OnStart(PartModule.StartState state)
		{
			if (!string.IsNullOrEmpty(hammerAnimation))
				Anim = part.FindModelAnimators(hammerAnimation)[0];
			RotationTransform = part.FindModelTransform(rotationTransformName);
			ExtensionTransform = part.FindModelTransform(extensionTransformName);

			base.OnStart(state);

			if (state == StartState.Editor)
				return;

			Events["hammerEvent"].unfocusedRange = interactionRange;
			Events["hammerEvent"].guiName = "Hammer Test";

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
				values = DMSeismicHandler.Instance.getSeismicHammer(part.flightID);
		}

		protected override void EventsCheck()
		{
			base.EventsCheck();

			Events["DeployExperiment"].active = IsDeployed;
		}

		#region Animator

		//Controls the main, door-opening animation
		private void animator(float speed, float time, Animation a, string name)
		{
			if (a != null)
			{
				a[name].speed = speed;
				if (!a.IsPlaying(name))
				{
					a[name].normalizedTime = time;
					a.Blend(name, 1f);
				}
			}
		}

		private void rotation(float angle, float time = 1f)
		{
			RotationTransform.localRotation = Quaternion.RotateTowards(RotationTransform.localRotation, Quaternion.AngleAxis(angle, Vector3.right), time);
		}

		private void extension(Vector3 direction, float time = 1f)
		{
			ExtensionTransform.Translate(direction * time);
		}

		public override void deployEvent()
		{
			base.deployEvent();
			Events["hammerEvent"].active = true;
			Fields["scoreString"].guiActive = true;
			if (values != null)
				values.Armed = true;
		}

		public override void retractEvent()
		{
			base.retractEvent();
			Events["hammerEvent"].active = false;
			Fields["scoreString"].guiActive = false;
			if (values != null)
				values.Armed = false;
		}

		[KSPEvent(guiActive = true, guiActiveUnfocused = true, externalToEVAOnly = true, active = false)]
		public void hammerEvent()
		{
			if (values == null)
			{
				DMUtils.Logging("Something went wrong here; the seismic hammer was not properly initialized...");
				return;
			}

			if (!values.Armed)
				return;

			animator(1f, 0f, Anim, hammerAnimation);

			StartCoroutine(RunThumper());
		}

		#endregion

		#region Science Setup

		public override void DeployExperiment()
		{
			if (!canConduct())
			{
				ScreenMessages.PostScreenMessage(failMessage, 5f, ScreenMessageStyle.UPPER_CENTER);
				return;
			}

			if (!IsDeployed)
				deployEvent();

			dryRun = false;

			hammerEvent();
		}

		//This is where the primary animator runs
		private IEnumerator RunThumper()
		{
			float distance = 0f;

			float angle = 0;
			float originalAngle = RotationTransform.localEulerAngles.x;
			Vector3d hammerLine = new Vector3d();

			if (values.OnAsteroid)
			{
				var asteroids = vessel.FindPartModulesImplementing<ModuleAsteroid>();

				ModuleAsteroid m = null;
				float astDistance = 2000;

				for (int i = 0; i < asteroids.Count; i++)
				{
					ModuleAsteroid ast = asteroids[i];

					if (ast == null)
						continue;

					float d = (RotationTransform.position - ast.part.transform.position).magnitude;

					if (d < astDistance)
					{
						astDistance = d;
						m = ast;
					}
				}

				if (m == null)
				{
					animator(-1f, 1f, Anim, hammerAnimation);
					dryRun = true;

					ScreenMessages.PostScreenMessage("Seismic Hammer can't find an asteroid to impact...", 6f, ScreenMessageStyle.UPPER_CENTER);
					yield break;
				}

				hammerLine = RotationTransform.InverseTransformPoint(m.part.transform.position);
			}
			else
			{
				//First we draw a line from the rotation transform object to the point on the surface directly below it
				Vector3d surfacePos = vessel.mainBody.GetWorldSurfacePosition(vessel.mainBody.GetLatitude(RotationTransform.position), vessel.mainBody.GetLongitude(RotationTransform.position), vessel.pqsAltitude);
				hammerLine = RotationTransform.InverseTransformPoint(surfacePos);
			}

			//Calculate the angle on the Z axis
			angle = Mathf.Atan2((float)hammerLine.y, (float)hammerLine.z) * Mathf.Rad2Deg;

			//Make sure the angle is within a normal range
			angle = normalizeAngle(angle);

			DMUtils.DebugLog("Hammer Angle: {0:N7}", angle);

			//Clamp the rotation between maximum limits for the model
			angle = Mathf.Clamp(angle, -30, 90);

			//Reverse the angle to compensate for initial transform rotation
			angle *= -1;

			DMUtils.DebugLog("Clamped Angle: {0:N7}", angle);

			//Wait while the primary animator is playing so that the hammer transform can clear the base
			while (Anim.IsPlaying(hammerAnimation) && Anim[hammerAnimation].normalizedTime < 0.03f)
			{
				yield return null;
			}

			DMUtils.DebugLog("Hammer Starting Angle: {0:N7}", RotationTransform.localEulerAngles.x);

			//Cache the original rotation angle of the transform and calculate the target angle
			float newAngle = fixAngle(originalAngle + angle);

			DMUtils.DebugLog("New Angle: {0:N7}", newAngle);

			//Rotate the transform while the primary animator is playing; only allow this to go on for a certain amount of time
			while (Anim.IsPlaying(hammerAnimation) && Anim[hammerAnimation].normalizedTime < 0.341f)
			{
				//Rotation check is dependent on the sign of the target rotation angle
				if (angle > 0)
				{
					while (RotationTransform.localEulerAngles.x < newAngle)
					{
						//DMUtils.Logging("Rotation positive: {0:N7}", RotationTransform.localEulerAngles.x);
						rotation(angle, TimeWarp.deltaTime * 28f);
						yield return null;
					}
				}
				else if (angle < 0)
				{
					while (fixAngle(RotationTransform.localEulerAngles.x) > newAngle)
					{
						//DMUtils.Logging("Rotation negative: {0:N7}", RotationTransform.localEulerAngles.x);
						rotation(angle, TimeWarp.deltaTime * 28f);
						yield return null;
					}
				}
				yield return null;
			}

			//Take any changes to the rescale factor into account *need to add tweakscale reference too*
			float scale = part.rescaleFactor;

			//After the transform is rotated and pointing at the surface draw a ray from the extension transform; check for impacts on the terrain
			if (!rayImpact(values.OnAsteroid, ExtensionTransform, scale, out distance))
			{
				//If no impact is detected within the distance limit stop the animation, reverse the rotation, and cancel the coroutine
				DMUtils.DebugLog("Hammer Failed: Distance: {0:N3}", distance);
				animator(-1f, 1f, Anim, hammerAnimation);
				dryRun = true;

				ScreenMessages.PostScreenMessage("Seismic Hammer can't impact the surface from here...", 6f, ScreenMessageStyle.UPPER_CENTER);

				while (Anim.IsPlaying(hammerAnimation))
				{
					if (angle > 0)
					{
						while (RotationTransform.localEulerAngles.x > originalAngle)
						{
							if (RotationTransform.localEulerAngles.x > originalAngle)
								rotation(0, TimeWarp.deltaTime * 30f);
							yield return null;
						}
					}
					else
					{
						while (fixAngle(RotationTransform.localEulerAngles.x) < fixAngle(originalAngle))
						{
							if (fixAngle(RotationTransform.localEulerAngles.x) < fixAngle(originalAngle))
								rotation(0, TimeWarp.deltaTime * 30f);
							yield return null;
						}
					}
					yield return null;
				}
				rotation(0);

				yield break;
			}

			DMUtils.DebugLog("Hammer Hit: Distance: {0:N3}", distance);

			//We have to subtract the length of the impact hammer from the impact distance, leaving only the extension transform length
			distance -= (1.5f * scale);

			//Transform translation does not take the part scale into account, so we need to convert the distance back into the unscaled dimensions
			distance /= scale;

			distance = Math.Max(0, distance);

			DMUtils.DebugLog("New Distance: {0:N3}", distance);

			//Cache the original transform position and calculate the target position
			Vector3 originalPosition = ExtensionTransform.localPosition;

			float targetPosition = originalPosition.z - distance;

			DMUtils.DebugLog("Drill Original Position: {0:N3}", originalPosition.z);

			DMUtils.DebugLog("Drill Target Position: {0:N3}", targetPosition);

			//While the animation is playing translate the extension transform out
			while (Anim.IsPlaying(hammerAnimation) && Anim[hammerAnimation].normalizedTime < 0.68f)
			{
				if (distance > 0)
				{
					while (ExtensionTransform.localPosition.z > targetPosition)
					{
						//DMUtils.Logging("Drill Position: {0:N3}", ExtensionTransform.localPosition.z);
						extension(Vector3.back, TimeWarp.deltaTime);
						yield return null;
					}
				}
				yield return null;
			}

			//If this is a real run gather science data, then reset the flag
			if (!dryRun)
				getScienceData(values.OnAsteroid);

			dryRun = true;

			//After the experiment has been collected reverse the rotation and translation
			while (Anim.IsPlaying(hammerAnimation))
			{
				if (angle > 0)
				{
					while (ExtensionTransform.localPosition.z < originalPosition.z || RotationTransform.localEulerAngles.x > originalAngle)
					{
						//DMUtils.Logging("Rotation Angle Back positive: {0:N3}", RotationTransform.localEulerAngles.x);
						//DMUtils.Logging("Drill Position Back: {0:N3}", ExtensionTransform.localPosition.z);
						if (distance > 0)
						{
							if (ExtensionTransform.localPosition.z < originalPosition.z)
								extension(Vector3.forward, TimeWarp.deltaTime);
						}
						if (RotationTransform.localEulerAngles.x > originalAngle)
							rotation(0, TimeWarp.deltaTime * 20f);
						yield return null;
					}
				}
				else
				{
					while (ExtensionTransform.localPosition.z < originalPosition.z || fixAngle(RotationTransform.localEulerAngles.x) < fixAngle(originalAngle))
					{
						//DMUtils.Logging("Rotation Angle Back negative: {0:N3}", RotationTransform.localEulerAngles.x);
						//DMUtils.Logging("Drill Position Back: {0:N3}", ExtensionTransform.localPosition.z);
						if (distance > 0)
						{
							if (ExtensionTransform.localPosition.z < originalPosition.z)
								extension(Vector3.forward, TimeWarp.deltaTime);
						}
						if (fixAngle(RotationTransform.localEulerAngles.x) < fixAngle(originalAngle))
							rotation(0, TimeWarp.deltaTime * 20f);
						yield return null;
					}
				}
				yield return null;
			}

			//Reset the transform positions after the primary animation has completed; this corrects and timestep errors
			rotation(0);
			ExtensionTransform.localPosition = originalPosition;
		}

		private float normalizeAngle(float a)
		{
			a = a % 360;
			a -= 180;
			if (a <= -180)
				a += 360;
			return a;
		}

		private float fixAngle(float a)
		{
			a = a % 360;

			if (a <= 0)
				a += 360;

			return a;
		}

		private bool rayImpact(bool b, Transform t, float s, out float d)
		{
			RaycastHit hit = new RaycastHit();
			Vector3 p = t.position;
			Ray r = new Ray(p, -1f * t.forward);
			d = 0f;

			Physics.Raycast(r, out hit, 3.7f * s);
			if (hit.collider != null)
			{
				if (b)
				{
					string obj = hit.collider.attachedRigidbody.gameObject.name;
					d = hit.distance;
					return (obj.StartsWith(potato));
				}
				else
				{
					Transform hitT = hit.collider.transform;
					int i = 0; //Just to prevent this from getting stuck in a loop
					while (hitT != null && i < 200)
					{
						if (hitT.name.Contains(vessel.mainBody.name))
						{
							d = hit.distance;
							return true;
						}
						hitT = hitT.parent;
						i++;
					}
				}
			}
			return false;
		}

		private void getScienceData(bool asteroid)
		{
			ScienceData data = DMSeismicHandler.makeData(values, exp, experimentID, false, asteroid);

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
