#region license
/* DMagic Orbital Science - Rover Goo and Mat
 * Some Rover Goo Pod and Mat Bay-Specific Code On Top Of DMModuleScienceAnimate
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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DMagic.Part_Modules
{
	class DMRoverGooMat : DMModuleScienceAnimate
	{

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);
			if (!IsDeployed && anim2 != null && anim != null)
				sampleAnimator(0f, 0f, 1f);
		}

		protected override void onLabReset()
		{
			base.onLabReset();

			if (IsDeployed)
				sampleAnimator(-2f, 1f, (anim2[sampleAnim].length / 2f));
		}

		public override void deployEvent()
		{
			if (anim2 != null && anim != null)
				sampleAnimator(anim[animationName].length);
			base.deployEvent();
		}

		public override void retractEvent()
		{
			if (anim2 != null)
				sampleAnimator(0f);
			base.retractEvent();
		}

		private IEnumerator waitForSamples(float waitTime)
		{
			yield return new WaitForSeconds(waitTime);
			if (IsDeployed)
				sampleAnimator(1.5f, 0f, (experimentNumber * (1f / experimentLimit)) * (anim2[sampleAnim].length) / 1.5f);
			else
				sampleAnimator(-2f, (experimentNumber * 1f / experimentLimit), (experimentNumber * (1f / experimentLimit)) * (anim2[sampleAnim].length) / 2f);
		}

		private IEnumerator stopAnimSample(float timer)
		{
			yield return new WaitForSeconds(timer);
			anim2[sampleAnim].enabled = false;
		}

		private void sampleAnimator(float waitTime)
		{
			if (IsDeployed)
			{
				if (anim2.IsPlaying(sampleAnim))
				{
					StopCoroutine("stopAnimSample");
					anim2[sampleAnim].speed = -1.5f;
				}
				else if (anim.IsPlaying(animationName))
					StopCoroutine("waitForSamples");
				else
					StartCoroutine("waitForSamples", waitTime);
			}
			else
			{
				if (anim2.IsPlaying(sampleAnim))
				{
					StopCoroutine("stopAnimSample");
					anim2[sampleAnim].speed = 1.5f;
					StartCoroutine("stopAnimSample", (1 - (anim2[sampleAnim].time / ((experimentNumber * (1f / experimentLimit)) * anim2[sampleAnim].length))) * ((experimentNumber * (1f / experimentLimit)) * (anim2[sampleAnim].length / 1.5f)));
				}
				else
					StartCoroutine("waitForSamples", waitTime);
			}
		}

		private void sampleAnimator(float speed, float time, float timer)
		{
			anim2[sampleAnim].speed = speed;
			anim2[sampleAnim].normalizedTime = time;
			anim2.Blend(sampleAnim);
			StartCoroutine("stopAnimSample", timer);
		}

	}
}
