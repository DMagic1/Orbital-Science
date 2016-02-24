#region license
/* DMagic Orbital Science - DMSIGINT
 * Extension Part Module to handle some custom aspects of the breakable SIGINT dish
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
	public class DMSIGINT : DMBreakablePart, IDMSurvey
	{
		private readonly string[] dishTransformNames = new string[7] { "dish_Armature.000", "dish_Armature.001", "dish_Armature.002", "dish_Armature.003", "focalColumn", "dishCenter", "focalHead" };
		private readonly string[] dishMeshNames = new string[7] { "dish_Mesh.000", "dish_Mesh.001", "dish_Mesh.002", "dish_Mesh.003", "focalColumn", "dishCenter", "focalHead" };

		private List<Transform> dishTransforms = new List<Transform>();
		private List<GameObject> dishObjects = new List<GameObject>();

		public override void OnStart(PartModule.StartState state)
		{
			assignTransforms();
			assignObjects();

			base.OnStart(state);

			if (state != StartState.Editor)
				return;

			setTransformState(false);

			DMUtils.DebugLog("Turning off skinned mesh components...");

			for (int i = 0; i < dishObjects.Count; i++)
			{
				GameObject obj = dishObjects[i];

				if (obj == null)
					continue;

				DMUtils.DebugLog("looking for skinned mesh components...");

				List<Renderer> renderers = obj.GetComponentsInChildren<Renderer>(true).ToList();

				if (renderers.Count == 0)
					return;

				for (int j = 0; j < renderers.Count; j++)
				{
					Renderer r = renderers[j];

					if (r == null)
						continue;

					if (r is SkinnedMeshRenderer)
					{
						DMUtils.DebugLog("Setting skinned mesh component null...");
						r = null;
					}
				}
			}
		}

		private void assignTransforms()
		{
			for (int i = 0; i < dishTransformNames.Length; i++)
			{
				string s = dishTransformNames[i];

				if (string.IsNullOrEmpty(s))
					continue;

				Transform t = part.FindModelTransform(s);

				if (t == null)
					continue;

				dishTransforms.Add(t);
			}
		}

		private void assignObjects()
		{
			for (int i = 0; i < dishMeshNames.Length; i++)
			{
				string s = dishMeshNames[i];

				if (string.IsNullOrEmpty(s))
					continue;

				Transform t = part.FindModelTransform(s);

				if (t == null)
					continue;

				GameObject obj = t.gameObject;

				if (obj == null)
					continue;

				dishObjects.Add(obj);
			}
		}

		public override void editorDeployEvent()
		{
			Events["editorDeployEvent"].active = false;
			Events["editorRetractEvent"].active = false;
			return;
		}

		public override void editorRetractEvent()
		{
			Events["editorDeployEvent"].active = false;
			Events["editorRetractEvent"].active = false;
			return;
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
					if (vessel.altitude < vessel.mainBody.scienceValues.spaceAltitudeThreshold * 10)
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

		protected override void getGameObjects()
		{
			if (!breakable)
				return;

			for (int i = 0; i < dishTransforms.Count; i++)
			{
				Transform t = dishTransforms[i];

				if (t == null)
					continue;

				GameObject obj = t.gameObject;

				if (obj == null)
					continue;

				breakableObjects.Add(obj);
			}			
		}

		protected override void setTransformState(bool on)
		{
			if (!breakable)
				return;

			DMUtils.DebugLog("Setting Transform State To {0}", on);

			for (int i = 0; i < dishObjects.Count; i++)
			{
				GameObject obj = dishObjects[i];

				if (obj == null)
					continue;

				DMUtils.DebugLog("Setting False...");

				obj.SetActive(on);
			}
		}
	}
}
