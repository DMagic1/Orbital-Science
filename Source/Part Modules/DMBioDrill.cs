﻿#region license
/* DMagic Orbital Science - Bio Drill
 * Some Drill-Specific Code On Top Of DMModuleScienceAnimate
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
using UnityEngine;

namespace DMagic.Part_Modules
{
	class DMBioDrill: DMModuleScienceAnimate
	{

		[KSPField]
		public string verticalDrill = "";

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);
			anim = DMUtils.GetAnimation(part, verticalDrill);
		}

		public override bool canConduct()
		{
			if (base.canConduct())
			{
				if (vessel.mainBody.bodyName == "Eve" || vessel.mainBody.bodyName == "Kerbin" || vessel.mainBody.bodyName == "Duna" || vessel.mainBody.bodyName == "Laythe" || vessel.mainBody.bodyName == "Bop" || vessel.mainBody.bodyName == "Vall" || vessel.mainBody.bodyName == "Slate" || vessel.mainBody.atmosphere)
					return true;

				failMessage = customFailMessage;
			}

			return false;
		}

		public override void gatherScienceData(bool silent = false)
		{
			if (vessel.mainBody.bodyName == "Eve")
				base.scienceBoost = 2f;
			else
				base.scienceBoost = 1f;
			if (anim.IsPlaying(verticalDrill))
				return;

			base.gatherScienceData(silent);
		}

		public override void deployEvent()
		{
			startDrill();
		}

		//Determine drill orientation relative to parent part, set angle to -90 to 90.
		private void startDrill()
		{
			double cosineAngle = 0;
			double processedRot = 0;
			cosineAngle = Mathf.Rad2Deg * Math.Acos(Vector3d.Dot(part.transform.up, part.parent.transform.up));
			if (cosineAngle > 180)
				cosineAngle = 360 - cosineAngle;
			if (cosineAngle > 90)
				cosineAngle -= 180;
			processedRot = Math.Abs(cosineAngle);
			if (processedRot < 90 && processedRot >= 50)
				base.primaryAnimator(animSpeed, 0f, WrapMode.Default, verticalDrill, anim);
			else
				base.primaryAnimator(animSpeed, 0f, WrapMode.Default, animationName, anim);
			if (HighLogic.LoadedSceneIsEditor)
				print("Current rot is: " + Math.Round(processedRot, 2).ToString() + ". Values between 0 and 50 play the horizontal drill animation, values between 50 and 90 play the vertical animation.");
		}

	}
}
