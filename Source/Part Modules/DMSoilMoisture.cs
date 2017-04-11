#region license
/* DMagic Orbital Science - Soil Moisture
 * Some Soil Moisture-Specific Code On Top Of DMModuleScienceAnimate
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

using System.Collections;
using UnityEngine;

namespace DMagic.Part_Modules
{
	class DMSoilMoisture: DMModuleScienceAnimate, IScalarModule
	{
		[KSPField(isPersistant = true)]
		public bool allowTransmission = true;
		[KSPField(guiActive = true, guiName = "Science Transmission")]
		public string scienceTransmission = "Enabled";

		private bool fullyDeployed = false;
		private bool rotating = false;

		private Transform dish;
		private const string dishTransform = "armBase";

		private float scalar;
		private float deployScalar;
		private float scalarStep;
		private bool moving;

		EventData<float> onStop;
		EventData<float, float> onMove;

		public override void OnStart(PartModule.StartState state)
		{
			onStop = new EventData<float>("SoilMoisture_" + part.flightID + "_OnStop");
			onMove = new EventData<float, float>("SoilMoisture_" + part.flightID + "_OnMove");

			base.OnStart(state);
			dish = part.FindModelTransform(dishTransform);
			if (IsDeployed)
			{
				fullyDeployed = true;
				isLocked = true;
				deployScalar = 1;
				scalar = 1;
			}

			Events["ToggleScienceTransmission"].guiName = allowTransmission ? "Disable Science Transmission" : "Enable Science Transmission";
			scienceTransmission = allowTransmission ? "Enabled" : "Disabled";

			if (anim != null && anim[animationName] != null)
				scalarStep = 1 / anim[animationName].length;
		}

		protected override void Update()
		{
			base.Update();

			if (HighLogic.LoadedSceneIsFlight)
			{
				if (IsDeployed && fullyDeployed)
				{
					rotating = true;
					dishRotate();
				}

				if (!fullyDeployed && rotating)
					spinDishDown();

				if (!moving)
					return;

				if (scalar >= 0.95f)
				{
					isLocked = true;
					moving = false;
					deployEvent();
					onStop.Fire(anim[animationName].normalizedTime);
				}
				else if (scalar <= 0.05f)
				{
					isLocked = false;
					moving = false;
					retractEvent();
					onStop.Fire(anim[animationName].normalizedTime);
				}
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
			base.deployEvent();

			yield return new WaitForSeconds((1 - anim[animationName].normalizedTime) * anim[animationName].length);

			fullyDeployed = true;
			isLocked = true;
			deployScalar = 1;
			scalar = 1;
		}

		public override void retractEvent()
		{
			if (IsDeployed && !fullyDeployed)
				StopCoroutine("deployEnumerator");

			StartCoroutine("retractEnumerator");
		}

		private IEnumerator retractEnumerator()
		{
			fullyDeployed = false;
			isLocked = false;
			deployScalar = 0;
			scalar = 0;

			if (dish != null)
			{
				while (dish.localEulerAngles.z > 1)
					yield return null;
			}

			base.retractEvent();
		}

		//Slowly rotate dish
		private void dishRotate()
		{
			if (dish != null)
				dish.Rotate(Vector3.forward * Time.deltaTime * 20f);
		}

		private void spinDishDown()
		{
			if (dish != null)
			{
				if (dish.localEulerAngles.z > 1)
					dish.Rotate(Vector3.forward * Time.deltaTime * 50f);
				else
					rotating = false;
			}
		}

		[KSPEvent(guiActive = true, guiActiveEditor = true, active = true)]
		public void ToggleScienceTransmission()
		{
			allowTransmission = !allowTransmission;

			Events["ToggleScienceTransmission"].guiName = allowTransmission ? "Disable Science Transmission" : "Enable Science Transmission";
			scienceTransmission = allowTransmission ? "Enabled" : "Disabled";
		}

		#region IScalar

		public bool CanMove
		{
			get
			{
				if (anim.IsPlaying(animationName))
				{
					scalar = anim[animationName].normalizedTime;
					deployScalar = scalar;
				}

				if (deployScalar < 0.95f)
					isLocked = false;

				return true;
			}
		}

		public float GetScalar
		{
			get { return scalar; }
		}

		public EventData<float, float> OnMoving
		{
			get { return onMove; }
		}

		public EventData<float> OnStop
		{
			get { return OnStop; }
		}

		public string ScalarModuleID
		{
			get { return "dmsoil"; }
		}

		public bool IsMoving()
		{
			if (anim == null)
				return false;

			if (anim.isPlaying && anim[animationName] != null && anim[animationName].speed != 0f)
				return true;

			return moving;
		}

		public void SetScalar(float t)
		{
			if (isLocked)
			{
				scalar = t;
				deployScalar = 1;
				moving = false;

				return;
			}

			anim[animationName].speed = 0f;
			anim[animationName].enabled = true;

			moving = true;

			t = Mathf.MoveTowards(scalar, t, scalarStep * Time.deltaTime);

			anim[animationName].normalizedTime = t;
			anim.Blend(animationName);
			scalar = t;
			deployScalar = scalar;
		}

		public void SetUIRead(bool state)
		{

		}

		public void SetUIWrite(bool state)
		{

		}

		#endregion
	}
}
