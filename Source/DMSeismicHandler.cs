using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DMagic.Part_Modules;
using UnityEngine;

namespace DMagic
{
	public interface IDMSeismometer
	{
		void updatePosition();
		void addSeismometer(uint id, IDMSeismometer sensor, float distance);
	}

	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class DMSeismicHandler : MonoBehaviour
	{
		private DMSeismicHandler instance;

		private Dictionary<uint, DMSeismicSensor> seismometers = new Dictionary<uint, DMSeismicSensor>();
		private Dictionary<uint, DMSeismicHammer> hammers = new Dictionary<uint, DMSeismicHammer>();



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

				if (!h.vessel.Landed && h.vessel.heightFromTerrain > 1000)
					continue;

				for (int j = 0; j < seismometers.Count; j++)
				{
					DMSeismicSensor s = seismometers.ElementAt(j).Value;

					if (s == null)
						continue;

					if (!s.vessel.Landed && s.vessel.heightFromTerrain > 1000)
						continue;

					float distance = Math.Abs((h.part.transform.position - s.part.transform.position).magnitude);

					if (distance < 15000)
					{
						h.addSeismometer(s.part.flightID, s, distance);

						s.addSeismometer(h.part.flightID, h, distance);
					}
				}
			}
		}		

		public DMSeismicHandler Instance
		{
			get { return instance; }
		}

	}
}
