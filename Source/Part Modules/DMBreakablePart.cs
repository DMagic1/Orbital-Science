#region license
/* DMagic Orbital Science - DMBreakablePart
 * Extension Part Module to handle parts with breakable objects
 *
 * Copyright (c) 2016, David Grandy <david.grandy@gmail.com>
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DMagic.Part_Modules
{
	public class DMBreakablePart : DMModuleScienceAnimate
	{
		protected List<GameObject> breakableObjects = new List<GameObject>();
		private Transform baseTransform;
		private Transform forwardTransform;

		[KSPField(isPersistant = true)]
		public bool broken;
		[KSPField]
		public bool breakable = true;
		[KSPField]
		public float breakingForce = 5;
		//[KSPField(guiActive = true)]
		//public string BForce = "";
		[KSPField]
		public bool fixable = true;
		[KSPField]
		public int fixLevel = 1;
		[KSPField]
		public string baseTransfromName = "";
		[KSPField]
		public string forwardTransformName = "";
		[KSPField]
		public float componentDrag = 1f;
		[KSPField]
		public float componentMass = 0.1f;

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);

			if (!string.IsNullOrEmpty(baseTransfromName))
				baseTransform = part.FindModelTransform(baseTransfromName);

			if (!string.IsNullOrEmpty(forwardTransformName))
				forwardTransform = part.FindModelTransform(forwardTransformName);

			if (broken)
			{
				setTransformState(false);
				base.Events["retractEvent"].active = base.IsDeployed && showEndEvent;
				base.Events["deployEvent"].active = false;
			}

			Events["fixPart"].active = fixable && breakable && broken;
			Events["fixPart"].unfocusedRange = interactionRange;
		}

		public override string GetInfo()
		{
			string info = base.GetInfo();

			if (breakable)
			{
				info += "Breakable: <color=#15b01a>V</color>\n";

				info += "Fixable: " + (fixable ? "<color=#15b01a>V</color>\n" : "<color=#e50000>X</color>\n");

				if (fixable)
					info += string.Format("Engineer Level To Fix: {0}\n", fixLevel);
			}

			return info;
		}

		public override void OnFixedUpdate()
		{
			base.OnFixedUpdate();

			if (!breakable)
				return;

			//BForce = breakingForce.ToString("F2");

			if (broken)
				return;

			checkForces();
		}

		public override bool canConduct()
		{
			if (broken)
			{
				failMessage = "This instrument is broken; the experiment can't be conducted";
				return false;
			}

			return base.canConduct();
		}

		public override void deployEvent()
		{
			base.deployEvent();

			if (broken && !oneWayAnimation)
				base.Events["retractEvent"].active = true;
		}

		protected virtual void setTransformState(bool on)
		{
			if (!breakable)
				return;

			if (baseTransform == null)
				return;

			if (baseTransform.gameObject == null)
				return;

			baseTransform.gameObject.SetActive(on);
		}

		private void checkForces()
		{
			if (!HighLogic.LoadedSceneIsFlight)
				return;

			if (vessel == null)
				return;

			if (vessel.atmDensity <= 0)
				return;

			if (!IsDeployed)
				return;

			if (forwardTransform == null)
				return;

			if (anim != null)
			{
				if (anim.IsPlaying(animationName))
					return;
			}

			if (vessel.HoldPhysics)
				return;

			if (shieldedState())
				return;

			float velocity = Mathf.Abs(Vector3.Dot(vessel.srf_velocity.normalized, forwardTransform.forward.normalized));
			if (velocity < 0.0001f)
				velocity = 0.0001f;
			float pressure = velocity * (float)(part.dynamicPressurekPa + part.submergedDynamicPressurekPa);
			if (pressure > breakingForce)
			{
				DMUtils.Logging("Breakable Part {0} - Breaking Force Exceded", part.name);
				onBreak();
			}
		}

		//Handle FAR shielded state; from RemoteTech:
		//https://github.com/RemoteTechnologiesGroup/RemoteTech/blob/develop/src/RemoteTech/Modules/ModuleRTAntenna.cs#L427-L481
		private bool shieldedState()
		{
			PartModule FARPartModule = GetFARModule();

			if (FARPartModule != null)
			{
				try
				{
					FieldInfo fi = FARPartModule.GetType().GetField("isShielded");
					return (bool)(fi.GetValue(FARPartModule));
				}
				catch (Exception e)
				{
					DMUtils.Logging("GetShieldedState: {0}", e);
				}
			}

			return part.ShieldedFromAirstream;
		}

		private PartModule GetFARModule()
		{
			if (part.Modules.Contains("FARBasicDragModel"))
			{
				return part.Modules["FARBasicDragModel"];
			}
			else if (part.Modules.Contains("FARWingAerodynamicModel"))
			{
				return part.Modules["FARWingAerodynamicModel"];
			}
			else if (part.Modules.Contains("FARControlSys"))
			{
				return part.Modules["FARControlSys"];
			}
			else
			{
				return null;
			}
		}

		//[KSPEvent(guiActive = true, active = true, guiName = "Break")]
		//public void breakStuff()
		//{
		//	onBreak();
		//}

		//[KSPEvent(guiActive = true, active = true)]
		//public void breakAdd()
		//{
		//	breakingForce += 0.05f;
		//}

		//[KSPEvent(guiActive = true, active = true)]
		//public void breakMinus()
		//{
		//	breakingForce -= 0.05f;
		//}

		private void breakObjects()
		{
			if (broken)
				return;

			if (part.packed)
				return;

			DMUtils.DebugLog("Breaking Objects...");

			getGameObjects();

			for (int i = 0; i < breakableObjects.Count; i++)
			{
				GameObject o = breakableObjects[i];

				if (o == null)
					continue;

				Rigidbody r = o.AddComponent<Rigidbody>();

				if (r == null)
					continue;

				DMUtils.DebugLog("Breaking Object [{0}]...", o.name);

				Rigidbody partRigid = part.GetComponent<Rigidbody>();

				Vector3 randomAngular = new Vector3((float)DMUtils.rand.NextDouble() * 3, (float)DMUtils.rand.NextDouble() * 3, (float)DMUtils.rand.NextDouble() * 3);
				DMUtils.DebugLog("Random Angular: [{0:F4}]", randomAngular);
				DMUtils.DebugLog("Old Angular: [{0:F4}]", r.angularVelocity);
				r.angularVelocity = partRigid.angularVelocity + randomAngular;
				DMUtils.DebugLog("New Angular: [{0:F4}]", r.angularVelocity);
				Vector3 randomVel = new Vector3(((float)DMUtils.rand.NextDouble() * 8) - 4, ((float)DMUtils.rand.NextDouble() * 8) - 4, ((float)DMUtils.rand.NextDouble() * 8) - 4);
				DMUtils.DebugLog("Random Velocity: [{0:F4}]", randomVel);
				Vector3 localCOM = vessel.findWorldCenterOfMass() - partRigid.worldCenterOfMass;
				DMUtils.DebugLog("Old Velocity: [{0:F4}]", r.velocity);
				r.velocity = partRigid.velocity + randomVel + Vector3.Cross(localCOM, r.angularVelocity);
				DMUtils.DebugLog("New Velocity: [{0:F4}]", r.velocity);
				r.mass = componentMass;
				r.useGravity = false;
				o.transform.parent = null;
				physicalObject p = o.AddComponent<physicalObject>();
				r.drag = componentDrag;

				var colliders = o.GetComponentsInChildren<Collider>();

				if (colliders.Length <= 0)
					continue;

				for (int j = 0; j < colliders.Length; j++)
				{
					Collider c = colliders[j];

					if (c == null)
						continue;

					if (!c.enabled)
						continue;

					DMUtils.DebugLog("Setting Collider [{0}] Inactive", c.name);

					c.enabled = false;
				}
			}

			StartCoroutine(breakablePartsRemove());

			if (IsDeployed && oneShot && !oneWayAnimation)
			{
				isLocked = false;
				base.Events["retractEvent"].active = true;
			}
		}

		private IEnumerator breakablePartsRemove()
		{
			yield return new WaitForSeconds(0.75f);

			setTransformState(false);
		}

		protected virtual void getGameObjects()
		{
			getChildren(baseTransform);
		}

		private void getChildren(Transform t)
		{
			if (t == null)
				return;

			for (int i = 0; i < t.childCount; i++)
			{
				Transform tChild = t.GetChild(i);

				if (tChild == null)
					continue;

				GameObject obj = tChild.gameObject;

				if (obj == null)
					continue;

				if (obj.GetComponent<MeshRenderer>() == null && obj.GetComponent<SkinnedMeshRenderer>() == null)
					continue;

				breakableObjects.Add(obj);

				getChildren(tChild);
			}
		}

		[KSPEvent(guiActive = false, guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Fix", active = false)]
		public void fixPart()
		{
			if (!fixable)
				return;

			if (!breakable)
				return;

			if (!broken)
			{
				Events["fixPart"].active = false;
				return;
			}

			Vessel v = FlightGlobals.ActiveVessel;

			if (v == null)
				return;

			if (!v.isEVA)
				return;

			if (v.parts[0] == null)
				return;

			if (v.parts[0].protoModuleCrew.Count <= 0)
				return;

			if (v.parts[0].protoModuleCrew[0].experienceTrait.TypeName != "Engineer")
			{
				ScreenMessages.PostScreenMessage(string.Format("An engineer of at least level [{0}] is required to repair this instrument.", fixLevel), 6f, ScreenMessageStyle.UPPER_CENTER);
				return;
			}

			if (v.parts[0].protoModuleCrew[0].experienceLevel < fixLevel)
			{
				ScreenMessages.PostScreenMessage(string.Format("An engineer of at least level [{0}] is required to repair this instrument.", fixLevel), 6f, ScreenMessageStyle.UPPER_CENTER);
				return;
			}

			onFix();
		}

		protected virtual void onBreak()
		{
			breakObjects();

			broken = true;

			if (fixable)
				Events["fixPart"].active = true;
		}

		protected virtual void onFix()
		{
			broken = false;

			setTransformState(true);

			Events["fixPart"].active = false;
		}
	}
}
