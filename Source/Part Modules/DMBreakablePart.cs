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
using System.Collections.Generic;
using System.Linq;
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
		[KSPField]
		public bool fixable = true;
		[KSPField]
		public int fixLevel = 1;
		[KSPField]
		public string baseTransfromName = "";
		[KSPField]
		public string forwardTransformName = "";
		[KSPField]
		public float componentDrag = 0.5f;
		[KSPField]
		public float componentMass = 0.001f;

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
			}

			Events["fixPart"].active = fixable && breakable;
			Events["fixPart"].unfocusedRange = interactionRange;
		}

		public override void OnFixedUpdate()
		{
			base.OnFixedUpdate();

			if (!breakable)
				return;

			checkForces();
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

			if (!IsDeployed)
				return;

			if (forwardTransform == null)
				return;

			if (anim != null)
			{
				if (anim.IsPlaying(animationName))
					return;
			}

			if (part.ShieldedFromAirstream)
				return;

			float velocity = Mathf.Abs(Vector3.Dot(vessel.srf_velocity.normalized, forwardTransform.forward.normalized));
			if (velocity < 0.0001f)
				velocity = 0.0001f;
			float pressure = velocity * (float)(part.dynamicPressurekPa + part.submergedDynamicPressurekPa);
			if (pressure > breakingForce)
			{
				DMUtils.Logging("Breakable Part {0} - Breaking Force Exceded", part.partName);
				breakObjects();
			}

		}

		private void breakObjects()
		{
			if (broken)
				return;

			if (part.packed)
				return;

			getGameObjects();

			for (int i = 0; i < breakableObjects.Count; i++)
			{
				GameObject o = breakableObjects[i];

				if (o == null)
					continue;

				Rigidbody r = o.AddComponent<Rigidbody>();

				if (r == null)
					continue;

				Vector3 randomAngular = new Vector3((float)DMUtils.rand.NextDouble() * 3, (float)DMUtils.rand.NextDouble() * 3, (float)DMUtils.rand.NextDouble() * 3);
				r.angularVelocity = part.rigidbody.angularVelocity + randomAngular;
				Vector3 randomVel = new Vector3(((float)DMUtils.rand.NextDouble() * 4) - 2, ((float)DMUtils.rand.NextDouble() * 4) - 2, ((float)DMUtils.rand.NextDouble() * 4) - 2);
				Vector3 localCOM = vessel.findWorldCenterOfMass() - part.rigidbody.worldCenterOfMass;
				r.velocity = part.rigidbody.velocity + randomVel + Vector3.Cross(localCOM, rigidbody.angularVelocity);
				r.mass = componentMass;
				r.useGravity = false;
				o.transform.parent = null;
				physicalObject p = o.AddComponent<physicalObject>();
				r.drag = componentDrag;
			}

			if (IsDeployed && oneShot && !oneWayAnimation)
				base.Events["retractEvent"].active = true;
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

				if (obj.GetComponent<Collider>() == null)
					continue;

				breakableObjects.Add(obj);

				getChildren(tChild);
			}
		}

		[KSPEvent(guiActive = false, guiActiveUnfocused = true, externalToEVAOnly = true, unfocusedRange = 5f, active = true)]
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

			onFix();
		}

		protected void onBreak()
		{
			breakObjects();

			broken = true;
		}

		protected void onFix()
		{
			broken = false;

			setTransformState(true);
		}
	}
}
