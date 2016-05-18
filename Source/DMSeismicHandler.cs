#region license
/* DMagic Orbital Science - Seismic Handler
 * Monobehaviour for tracking and updating all active seismic sensors and hammers
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
using DMagic.Part_Modules;
using DMagic.Scenario;
using UnityEngine;

namespace DMagic
{
	public interface IDMSeismometer
	{
	}

	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class DMSeismicHandler : MonoBehaviour
	{
		private static DMSeismicHandler instance;
		public static DMSeismicHandler Instance
		{
			get { return instance; }
		}

		public static float nearPodThreshold = 500;
		public static float nearPodMinDistance = 10;
		public static float nearPodMaxDistance = 2500;
		public static float farPodThreshold = 4000;
		public static float farPodMinDistance = 2500;
		public static float farPodMaxDistance = 15000;
		public static float podMinAngle = 20;
		public static float podAngleThreshold = 90;

		private Dictionary<uint, DMSeismometerValues> seismometers = new Dictionary<uint, DMSeismometerValues>();
		private Dictionary<uint, DMSeismometerValues> hammers = new Dictionary<uint, DMSeismometerValues>();

		private static string bodyNameFixed = "Eeloo";

		private void Start()
		{
			instance = this;

			GameEvents.onPartDie.Add(onPartDestroyed);

			if (FlightGlobals.Bodies.Count >= 17)
				bodyNameFixed = FlightGlobals.Bodies[16].bodyName;

			StartCoroutine(loadSensors());
		}

		private void OnDestroy()
		{
			GameEvents.onPartDie.Remove(onPartDestroyed);
		}

		private void Update()
		{
			if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready)
				updatePositions();
		}

		private IEnumerator loadSensors()
		{
			while (!FlightGlobals.ready)
				yield return null;

			for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
			{
				Vessel v = FlightGlobals.Vessels[i];

				if (v == null)
					continue;

				if (v.mainBody != FlightGlobals.ActiveVessel.mainBody)
					continue;

				if (v.loaded)
				{
					for (int j = 0; j < v.Parts.Count; j++)
					{
						Part p = v.Parts[j];

						if (p == null)
							continue;

						IDMSeismometer sensor = p.FindModuleImplementing<IDMSeismometer>();

						if (sensor != null)
							addLoadedSeismometer(p.flightID, sensor);
					}
				}
				else
				{
					ProtoVessel pv = v.protoVessel;

					for (int j = 0; j < v.protoVessel.protoPartSnapshots.Count; j++)
					{
						ProtoPartSnapshot p = v.protoVessel.protoPartSnapshots[j];

						if (p == null)
							continue;

						for (int k = 0; k < p.modules.Count; k++)
						{
							ProtoPartModuleSnapshot m = p.modules[k];

							if (m == null)
								continue;

							if (m.moduleName == "DMSeismicSensor" || m.moduleName == "DMSeismicHammer")
								addProtoSeismometer(p.flightID, p, m);
						}
					}
				}
			}
		}

		public void addLoadedSeismometer(uint id, IDMSeismometer sensor)
		{
			if (sensor.GetType() == typeof(DMSeismicSensor))
			{
				if (!seismometers.ContainsKey(id))
				{
					DMSeismicSensor s = (DMSeismicSensor)sensor;
					DMSeismometerValues v = new DMSeismometerValues(s.vessel, s.part.protoPartSnapshot, s.snapshot, false);
					seismometers.Add(id, v);
				}
			}
			else if (sensor.GetType() == typeof(DMSeismicHammer))
			{
				if (!hammers.ContainsKey(id))
				{
					DMSeismicHammer h = (DMSeismicHammer)sensor;
					DMSeismometerValues v = new DMSeismometerValues(h.vessel, h.part.protoPartSnapshot, h.snapshot, true);
					hammers.Add(id, v);
				}
			}
		}

		public void addProtoSeismometer(uint id, ProtoPartSnapshot pp, ProtoPartModuleSnapshot pm)
		{
			if (pm.moduleName == "DMSeismicSensor")
			{
				if (!seismometers.ContainsKey(id))
				{
					DMSeismometerValues v = new DMSeismometerValues(pp.pVesselRef.vesselRef, pp, pm, false);
					seismometers.Add(id, v);
				}
			}
			else if (pm.moduleName == "DMSeismicHammer")
			{
				if (!hammers.ContainsKey(id))
				{
					DMSeismometerValues v = new DMSeismometerValues(pp.pVesselRef.vesselRef, pp, pm, true);
					hammers.Add(id, v);
				}
			}
		}

		public void removeSeismometer(uint id)
		{
			if (seismometers.ContainsKey(id))
				seismometers.Remove(id);
		}

		public void removeHammer(uint id)
		{
			if (hammers.ContainsKey(id))
				hammers.Remove(id);
		}

		private void onPartDestroyed(Part p)
		{
			if (p == null)
				return;

			removeSeismometer(p.flightID);
			removeHammer(p.flightID);
		}

		public DMSeismometerValues getSeismicSensor(uint id)
		{
			if (seismometers.ContainsKey(id))
				return seismometers[id];

			return null;
		}

		public DMSeismometerValues getSeismicHammer(uint id)
		{
			if (hammers.ContainsKey(id))
				return hammers[id];

			return null;
		}

		private void updatePositions()
		{
			for (int i = 0; i < hammers.Count; i++)
			{
				DMSeismometerValues h = hammers.ElementAt(i).Value;

				if (h == null)
					continue;

				if (!h.Hammer)
				{
					h.removeAllSensors();
					continue;
				}

				if (!h.Armed)
				{
					h.removeAllSensors();
					continue;
				}

				if (h.ProtoPartRef == null)
				{
					h.removeAllSensors();
					h.updateScore();
					continue;
				}

				if (h.VesselRef == null)
				{
					h.removeAllSensors();
					h.updateScore();
					continue;
				}

				if (!h.VesselRef.LandedOrSplashed)
				{
					h.removeAllSensors();
					h.updateScore();
					continue;
				}

				for (int j = 0; j < seismometers.Count; j++)
				{
					DMSeismometerValues s = seismometers.ElementAt(j).Value;

					if (s == null)
						continue;

					if (s.Hammer)
					{
						removeSensors(h, s);
						continue;
					}

					if (!s.Armed)
					{
						removeSensors(h, s);
						continue;
					}

					if (s.ProtoPartRef == null)
					{
						removeSensors(h, s);
						continue;
					}

					if (s.VesselRef == null)
					{
						removeSensors(h, s);
						continue;
					}

					if (!s.VesselRef.LandedOrSplashed)
					{
						removeSensors(h, s);
						continue;
					}

					if (s.VesselRef == h.VesselRef)
					{
						removeSensors(h, s);
						continue;
					}

					double distance = Math.Abs((h.VesselRef.GetWorldPos3D() - s.VesselRef.GetWorldPos3D()).magnitude);

					if (distance > farPodMaxDistance + 1000)
					{
						removeSensors(h, s);
						continue;
					}
					else if (distance < farPodMaxDistance)
					{
						float angle = (float)DMUtils.bearing(h.VesselRef.latitude, h.VesselRef.longitude, s.VesselRef.latitude, s.VesselRef.longitude);

						h.addSensor(s.ID, new Vector2((float)distance, angle));

						s.addSensor(h.ID, new Vector2());
					}
				}
				h.updateScore();
			}

			for (int j = 0; j < seismometers.Count; j++)
			{
				DMSeismometerValues s = seismometers.ElementAt(j).Value;

				if (s == null)
					continue;

				if (s.Hammer)
					continue;

				if (!s.Armed)
					continue;

				s.updateScore();
			}
		}

		private void removeSensors(DMSeismometerValues hammer, DMSeismometerValues pod)
		{
			hammer.removeSensor(pod.ID);

			pod.removeSensor(hammer.ID);
		}

		public static ScienceData makeData(DMSeismometerValues sensor, float score, ScienceExperiment exp, string expID, bool seismometerOnly, bool asteroid)
		{
			if (sensor == null || exp == null || sensor.VesselRef == null || sensor.VesselRef.mainBody == null)
				return null;

			Vessel v = sensor.VesselRef;
			CelestialBody body = v.mainBody;
			string biome = ((int)exp.biomeMask & (int)ExperimentSituations.SrfLanded) == 0 ? "" : ScienceUtil.GetExperimentBiome(body, v.latitude, v.longitude);

			DMAsteroidScience newAsteroid = null;

			if (asteroid)
			{
				newAsteroid = new DMAsteroidScience();
				body = newAsteroid.Body;
				biome = newAsteroid.AType + newAsteroid.ASeed.ToString();
			}

			ScienceSubject sub = ResearchAndDevelopment.GetExperimentSubject(exp, ExperimentSituations.SrfLanded, body, biome);

			if (sub == null)
			{
				Debug.LogError("[DM] Something Went Wrong Here; Null Seismometer Subject Returned; Please Report This On The KSP Forum With Output.log Data");
				return null;
			}

			float science = exp.baseValue * sub.dataScale * score;

			if (asteroid) 
			{
				string b = newAsteroid.AType;

				string a = "a";
				if (b == "Icy-Organic")
					a = "an";

				string c = " asteroid";
				if (b == "Comet")
					c = "";

				body.bodyName = bodyNameFixed;
				DMUtils.OnAsteroidScience.Fire(newAsteroid.AClass, expID);
				sub.title = exp.experimentTitle + string.Format(" from the surface of {0} {1}{2}", a, b, c);
				registerDMScience(newAsteroid, exp, sub);
			}
			else
			{
				if (sub.science > 0)
				{
					sub.scientificValue = 1f;

					if (sub.science >= ((science / sub.dataScale) * sub.subjectValue))
						sub.scientificValue = 0f;
					else
						sub.scientificValue = 1 - ((sub.science / sub.subjectValue) / (science / sub.dataScale));
				}
				DMUtils.OnAnomalyScience.Fire(body, expID, biome);
				sub.title = exp.experimentTitle + string.Format(" from {0}'s {1}", body.theName, biome);
			}

			return new ScienceData(science, 1f, v.VesselValues.ScienceReturn.value, sub.id, sub.title, false, sensor.ID);
		}

		private static void registerDMScience(DMAsteroidScience newAst, ScienceExperiment exp, ScienceSubject sub)
		{
			if (HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX)
				return;

			DMScienceData DMData = null;

			DMScienceData DMScience = DMScienceScenario.SciScenario.getDMScience(sub.title);
			if (DMScience != null)
			{
				sub.scientificValue *= DMScience.SciVal;
				DMData = DMScience;
			}

			if (DMData == null)
			{
				float astSciCap = exp.scienceCap * 40f;
				DMScienceScenario.SciScenario.RecordNewScience(sub.title, exp.baseValue, 1f, 0f, astSciCap);
				sub.scientificValue = 1f;
			}

			sub.subjectValue = newAst.SciMult;
			sub.scienceCap = exp.scienceCap * sub.subjectValue;
			sub.science = Math.Max(0f, Math.Min(sub.scienceCap, sub.scienceCap - (sub.scienceCap * sub.scientificValue)));
		}

	}
}
