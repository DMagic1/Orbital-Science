using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DMagic.Part_Modules;
using UnityEngine;

namespace DMagic
{

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

						DMSeismicSensor sensor = p.FindModuleImplementing<DMSeismicSensor>();

						if (sensor != null)
							addSeismometer(p.flightID, sensor);

						DMSeismicHammer hammer = p.FindModuleImplementing<DMSeismicHammer>();

						if (hammer != null)
							addHammer(p.flightID, hammer);
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

							if (m.moduleName == "DMSeismicSensor")
							{
								if (m.moduleRef.GetType() == typeof(DMSeismicSensor))
								{
									addSeismometer(p.flightID, (DMSeismicSensor)m.moduleRef);
									continue;
								}
							}

							if (m.moduleName == "DMSeismicHammer")
							{
								if (m.moduleRef.GetType() == typeof(DMSeismicHammer))
								{
									addHammer(p.flightID, (DMSeismicHammer)m.moduleRef);
									continue;
								}
							}
						}
					}
				}
			}
		}

		private void addSeismometer(uint id, DMSeismicSensor sensor)
		{
			if (!seismometers.ContainsKey(id))
				seismometers.Add(id, sensor);
		}

		private void addHammer(uint id, DMSeismicHammer hammer)
		{
			if (!hammers.ContainsKey(id))
				hammers.Add(id, hammer);
		}
		

		public DMSeismicHandler Instance
		{
			get { return instance; }
		}

	}
}
