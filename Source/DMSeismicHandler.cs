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
		private const string potato = "PotatoRoid";

		private void Start()
		{
			instance = this;

			bodyNameFixed = FlightGlobals.Bodies[16].bodyName;

			StartCoroutine(loadSensors());
		}

		private void OnDestroy()
		{

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

		private void addLoadedSeismometer(uint id, IDMSeismometer sensor)
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

		private void addProtoSeismometer(uint id, ProtoPartSnapshot pp, ProtoPartModuleSnapshot pm)
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

		public static ScienceData makeData(DMSeismometerValues sensor, ScienceExperiment exp, string expID, bool seismometerOnly, bool asteroid)
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

			float science = exp.baseValue * sub.dataScale * sensor.Score;

			if (asteroid) 
			{

				body.bodyName = bodyNameFixed;
				DMUtils.OnAsteroidScience.Fire(newAsteroid.AClass, expID);
				sub.title = exp.experimentTitle + string.Format(" from the surface of a {0} asteroid", newAsteroid.AType);
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

			return new ScienceData(science, 1f, 1f, sub.id, sub.title, false, sensor.ID);
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
