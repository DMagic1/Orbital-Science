#region license
/* DMagic Orbital Science - DMReconScope
 * Extension Part Module to handle recon cameras
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
using UnityEngine;

namespace DMagic.Part_Modules
{
	public class DMReconScope : DMModuleScienceAnimate, IDMSurvey
	{
		[KSPField]
		public string loopingAnimName = "";
		[KSPField]
		public string filmCannisterName = "cannister";

		private Animation loopingAnim;
		private bool windingDown;
		private bool windingUp;
		private Dictionary<int, GameObject> cannisters = new Dictionary<int, GameObject>();

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);

			if (!string.IsNullOrEmpty(loopingAnimName))
				loopingAnim = part.FindModelAnimators(loopingAnimName)[0];

			if (IsDeployed)
				startLoopingAnimation(1f);

			setCannisterObjects();
		}

		private void setCannisterObjects()
		{
			if (string.IsNullOrEmpty(filmCannisterName))
				return;

			for (int i = 0; i < experimentLimit; i++)
			{
				string s = filmCannisterName + ".00" + i.ToString();

				Transform t = part.FindModelTransform(s);

				if (t == null)
					continue;

				GameObject g = t.gameObject;

				if (g == null)
					continue;

				if (cannisters.ContainsKey(i))
					continue;

				cannisters.Add(i, g);

				if (experimentsReturned > i)
					g.SetActive(false);
			}
		}

		protected override void DumpAllData(List<ScienceData> data)
		{
			base.DumpAllData(data);

			for (int i = 1; i <= data.Count; i++)
			{
				int j = experimentsReturned - i;

				if (!cannisters.ContainsKey(j))
					continue;

				GameObject g = cannisters[j];

				if (g == null)
					continue;

				g.SetActive(false);
			}
		}

		protected override void DumpInitialData(ScienceData data)
		{
			base.DumpInitialData(data);

			int i = experimentsReturned - 1;

			if (!cannisters.ContainsKey(i))
				return;

			GameObject g = cannisters[i];

			if (g == null)
				return;

			g.SetActive(false);
		}

		protected override void DumpDataOverride(ScienceData data)
		{
			base.DumpDataOverride(data);

			int i = experimentsReturned - 1;

			if (!cannisters.ContainsKey(i))
				return;

			GameObject g = cannisters[i];

			if (g == null)
				return;

			g.SetActive(false);
		}

		override protected void ReturnDataOverRide(ScienceData data)
		{
			base.ReturnData(data);

			int i = experimentsReturned;

			if (!cannisters.ContainsKey(i))
				return;

			GameObject g = cannisters[i];

			if (g == null)
				return;

			g.SetActive(true);
		}

		protected override void onLabReset()
		{
			base.onLabReset();

			for (int i = 0; i < cannisters.Count; i++)
			{
				if (!cannisters.ContainsKey(i))
					continue;

				GameObject g = cannisters[i];

				if (g == null)
					continue;

				g.SetActive(true);
			}
		}

		public override void deployEvent()
		{
			if (windingDown)
				return;

			base.deployEvent();

			if (HighLogic.LoadedSceneIsEditor)
				return;

			StartCoroutine(startLooping(waitForAnimationTime));
		}

		private IEnumerator startLooping(float time)
		{
			windingUp = true;

			yield return new WaitForSeconds(time);

			startLoopingAnimation(1f);

			windingUp = false;
		}

		private void startLoopingAnimation(float speed)
		{
			if (loopingAnim == null)
				return;

			loopingAnim[loopingAnimName].speed = speed;
			loopingAnim[loopingAnimName].wrapMode = WrapMode.Loop;
			loopingAnim[loopingAnimName].normalizedTime = 0f;
			loopingAnim.Blend(loopingAnimName, 1f);
		}

		public override void retractEvent()
		{
			if (windingUp)
				return;

			StartCoroutine(stopLooping());
		}

		private IEnumerator stopLooping()
		{
			windingDown = true;

			stopLoopingAnimation();

			float time = getTimeRemaining();

			yield return new WaitForSeconds(time);

			windingDown = false;

			base.retractEvent();
		}

		private void stopLoopingAnimation()
		{
			if (HighLogic.LoadedSceneIsEditor)
				return;

			if (loopingAnim == null)
				return;

			loopingAnim[loopingAnimName].speed *= 1.3f;
			loopingAnim[loopingAnimName].wrapMode = WrapMode.Clamp;
		}

		private float getTimeRemaining()
		{
			if (HighLogic.LoadedSceneIsEditor)
				return 0;

			float f = 0;

			if (loopingAnim == null)
				return f;

			float animLength = loopingAnim[loopingAnimName].length / loopingAnim[loopingAnimName].speed;

			float animTime = loopingAnim[loopingAnimName].normalizedTime;

			f = animLength - (animLength * animTime);

			return f;
		}

		protected override void runExperiment(ExperimentSituations sit, bool silent)
		{
			base.runExperiment(sit, silent);

			scanPlanet(vessel.mainBody);
		}

		protected override ExperimentSituations getSituation()
		{
			switch (vessel.situation)
			{
				case Vessel.Situations.LANDED:
				case Vessel.Situations.PRELAUNCH:
					return ExperimentSituations.SrfLanded;
				case Vessel.Situations.SPLASHED:
					return ExperimentSituations.SrfSplashed;
				default:
					if (vessel.altitude < vessel.mainBody.atmosphereDepth && vessel.mainBody.atmosphere)
					{
						if (vessel.altitude < vessel.mainBody.scienceValues.flyingAltitudeThreshold)
							return ExperimentSituations.FlyingLow;
						else
							return ExperimentSituations.FlyingHigh;
					}
					if (vessel.altitude < vessel.mainBody.scienceValues.spaceAltitudeThreshold * 5)
						return ExperimentSituations.InSpaceLow;
					else
						return ExperimentSituations.InSpaceHigh;
			}
		}

		protected override string getBiome(ExperimentSituations s)
		{
			if ((bioMask & (int)s) == 0)
				return "";

			if (DMUtils.fixLatShift(vessel.latitude) > 0)
				return "NorthernHemisphere";
			else
				return "SouthernHemisphere";
		}

		public void scanPlanet(CelestialBody b)
		{
			DMAnomalyStorage anom = DMAnomalyList.getAnomalyStorage(b.name);

			if (anom == null)
				anom = new DMAnomalyStorage(b, false);

			if (anom.Scanned)
				return;

			if (anom.scanBody())
				DMAnomalyList.addAnomalyStorage(b.name, anom);
		}
	}
}
