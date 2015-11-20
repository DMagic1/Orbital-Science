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
		void addSeismometer(IDMSeismometer sensor, Vector2 vector = new Vector2());
		void removeSeismometer(IDMSeismometer sensor);
		void updateScore();
		float experimentScore { get; set; }
	}

	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class DMSeismicHandler : MonoBehaviour
	{
		private static DMSeismicHandler instance;

		private Dictionary<uint, DMSeismicSensor> seismometers = new Dictionary<uint, DMSeismicSensor>();
		private Dictionary<uint, DMSeismicHammer> hammers = new Dictionary<uint, DMSeismicHammer>();

		private const string bodyNameFixed = "Eeloo";


		private void Start()
		{
			instance = this;

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
							addSeismometer(p.flightID, sensor);
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
								addSeismometer(p.flightID, (IDMSeismometer)m.moduleRef);
						}
					}
				}
			}
		}

		private void addSeismometer(uint id, IDMSeismometer sensor)
		{
			if (sensor.GetType() == typeof(DMSeismicSensor))
			{
				if (!seismometers.ContainsKey(id))
					seismometers.Add(id, (DMSeismicSensor)sensor);
			}
			else if (sensor.GetType() == typeof(DMSeismicHammer))
			{
				if (!hammers.ContainsKey(id))
					hammers.Add(id, (DMSeismicHammer)sensor);
			}
		}

		private void updatePositions()
		{
			for (int i = 0; i < hammers.Count; i++)
			{
				DMSeismicHammer h = hammers.ElementAt(i).Value;

				if (h == null)
					continue;

				if (h.part == null)
					continue;

				if (h.vessel == null)
					continue;

				if (!h.vessel.Landed && h.vessel.heightFromTerrain > 1000)
					continue;

				for (int j = 0; j < seismometers.Count; j++)
				{
					DMSeismicSensor s = seismometers.ElementAt(j).Value;

					if (s == null)
						continue;

					if (s.part == null)
						continue;

					if (s.vessel == null)
						continue;

					if (!s.vessel.Landed && s.vessel.heightFromTerrain > 1000)
						continue;

					float distance = Math.Abs((h.part.transform.position - s.part.transform.position).magnitude);

					if (distance < 15000)
					{
						float angle = (float)DMUtils.bearing(h.vessel.latitude, h.vessel.longitude, s.vessel.latitude, s.vessel.longitude);

						h.addSeismometer(s, new Vector2(distance, angle));

						s.addSeismometer(h);
					}
					else if (distance > 16000)
					{
						h.removeSeismometer(s);

						s.removeSeismometer(h);
					}

					s.updateScore();
				}

				h.updateScore();
			}
		}		

		public static DMSeismicHandler Instance
		{
			get { return instance; }
		}

		public static ScienceData makeData(IDMSeismometer sensor, uint partID, ScienceExperiment exp, string expID, CelestialBody body, Vessel v, bool seismometerOnly, bool asteroid)
		{
			if (sensor == null || exp == null || body == null || v == null)
				return null;

			string biome = ScienceUtil.GetExperimentBiome(body, v.latitude, v.longitude);

			DMAsteroidScience newAsteroid = null;
			bool asteroids = false;

			if (asteroid)
			{
				newAsteroid = new DMAsteroidScience();
				asteroids = true;
				body = newAsteroid.Body;
				biome = newAsteroid.AType + newAsteroid.ASeed.ToString();
			}

			ScienceSubject sub = ResearchAndDevelopment.GetExperimentSubject(exp, ExperimentSituations.SrfLanded, body, biome);

			if (sub == null)
			{

				return null;
			}

			if (asteroids)
			{
				DMUtils.OnAsteroidScience.Fire(newAsteroid.AClass, expID);
				sub.title = exp.experimentTitle + string.Format(" from the surface of a {0} asteroid", newAsteroid.AType);
				registerDMScience(newAsteroid, exp, sub);
				body.bodyName = bodyNameFixed;
			}
			else
			{
				DMUtils.OnAnomalyScience.Fire(body, expID, biome);
				sub.title = exp.experimentTitle + string.Format(" from {0}'s {1}", body.theName, biome);
				sub.scienceCap = exp.scienceCap * sub.subjectValue;
			}

			return new ScienceData(exp.baseValue * sub.dataScale * sensor.experimentScore, 1f, 1f, sub.id, sub.title, false, partID);
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
