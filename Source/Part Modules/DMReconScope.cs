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
		private Dictionary<int, GameObject> cannisters = new Dictionary<int, GameObject>();

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);

			if (!string.IsNullOrEmpty(loopingAnimName))
				loopingAnim = part.FindModelAnimators(loopingAnimName)[0];
		}

		private void setCannisterObjects()
		{
			if (string.IsNullOrEmpty(filmCannisterName))
				return;

			for (int i = 0; i < experimentLimit; i++)
			{
				string s = filmCannisterName + "00" + i.ToString();

				Transform t = part.FindModelTransform(s);

				if (t == null)
					continue;

				GameObject g = t.gameObject;

				if (g == null)
					continue;

				if (cannisters.ContainsKey(i))
					continue;

				cannisters.Add(i, g);
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
				return; ;

			GameObject g = cannisters[i];

			if (g == null)
				return;

			g.SetActive(false);
		}

		new protected void DumpData(ScienceData data)
		{
			base.DumpData(data);

			int i = experimentsReturned - 1;

			if (!cannisters.ContainsKey(i))
				return; ;

			GameObject g = cannisters[i];

			if (g == null)
				return;

			g.SetActive(false);
		}

		new public void ReturnData(ScienceData data)
		{
			base.ReturnData(data);

			int i = experimentsReturned;

			if (!cannisters.ContainsKey(i))
				return; ;

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
					continue; ;

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

			StartCoroutine(startLooping(waitForAnimationTime));
		}

		private IEnumerator startLooping(float time)
		{
			yield return new WaitForSeconds(time);

			startLoopingAnimation(1f);
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
			if (windingDown)
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
			if (loopingAnim == null)
				return;

			loopingAnim[loopingAnimName].speed *= 1.3f;
			loopingAnim[loopingAnimName].wrapMode = WrapMode.Clamp;
		}

		private float getTimeRemaining()
		{
			float f = 0;

			if (loopingAnim == null)
				return f;

			float animLength = loopingAnim[loopingAnimName].length / loopingAnim[loopingAnimName].speed;

			float animTime = loopingAnim[loopingAnimName].normalizedTime;

			f = animLength - (animLength * animTime);

			return f;
		}

		protected override bool canConduct()
		{
			if (!base.canConduct())
				return false;

			scanPlanet(vessel.mainBody);

			return true;
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
