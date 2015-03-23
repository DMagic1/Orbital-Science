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
	class DMSoilMoisture: DMModuleScienceAnimate
	{

		private bool fullyDeployed = false;
		private bool rotating = false;

		private Transform dish;
		private const string dishTransform = "dishBase";

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);
			dish = part.FindModelTransform(dishTransform);
		}

		private void Update()
		{
			if (HighLogic.LoadedSceneIsFlight)
			{
				if (IsDeployed && fullyDeployed)
				{
					rotating = true;
					dishRotate();
				}

				if (!fullyDeployed && rotating)
					spinDishDown();
			}
		}

		public override void deployEvent()
		{
			StartCoroutine(deployEnumerator());
		}

		private IEnumerator deployEnumerator()
		{
			base.deployEvent();

			yield return new WaitForSeconds(anim[animationName].length);

			fullyDeployed = true;
		}

		public override void retractEvent()
		{
			StartCoroutine(retractEnumerator());
		}

		private IEnumerator retractEnumerator()
		{
			fullyDeployed = false;

			while (dish.localEulerAngles.z > 1)
				yield return null;

			base.deployEvent();
		}

		//Slowly rotate dish
		private void dishRotate()
		{
			dish.Rotate(Vector3.forward * Time.deltaTime * 20f);
		}

		private void spinDishDown()
		{
			if (dish.localEulerAngles.z > 1)
				dish.Rotate(Vector3.forward * Time.deltaTime * 30f);
			else
				rotating = false;
		}

	}
}
