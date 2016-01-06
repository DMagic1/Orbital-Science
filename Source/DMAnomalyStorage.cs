using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DMagic
{
	public class DMAnomalyStorage
	{
		private CelestialBody body;
		private bool scanned;
		//private bool updated;
		private Dictionary<string, DMAnomalyObject> anomalies = new Dictionary<string, DMAnomalyObject>();

		public DMAnomalyStorage(CelestialBody b, bool s = true)
		{
			body = b;
			scanned = s;
		}

		public void addAnomaly(DMAnomalyObject anom)
		{
			if (!anomalies.ContainsKey(anom.Name))
				anomalies.Add(anom.Name, anom);
		}

		//public void updateAnomalyCity(PQSCity city)
		//{
		//	DMAnomalyObject anom = anomalies.FirstOrDefault(a => a.Name == city.name);

		//	if (anom == null)
		//		return;

		//	anom.addPQSCity(city);
		//}

		public bool scanBody()
		{
			scanned = true;

			if (body.pqsController == null)
				return false;

			PQSCity[] Cities = body.pqsController.GetComponentsInChildren<PQSCity>();

			for (int i = 0; i < Cities.Length; i++)
			{
				PQSCity city = Cities[i];

				if (city == null)
					continue;

				if (city.transform.parent.name != body.name)
					continue;

				DMAnomalyObject anom = new DMAnomalyObject(city);

				addAnomaly(anom);
			}

			return true;
		}

		public DMAnomalyObject getAnomaly(string name)
		{
			if (anomalies.ContainsKey(name))
				return anomalies[name];

			return null;
		}

		public DMAnomalyObject getAnomaly(int index)
		{
			if (anomalies.Count > index)
				return anomalies.ElementAt(index).Value;

			return null;
		}

		public CelestialBody Body
		{
			get { return body; }
		}

		public bool Scanned
		{
			get { return scanned; }
		}

		//public bool Updated
		//{
		//	get { return updated; }
		//	set { updated = value; }
		//}

		public int AnomalyCount
		{
			get { return anomalies.Count; }
		}
	}
}
