using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FinePrint.Utilities;

namespace DMagic
{
	public class DMAnomalyStorage
	{
		private CelestialBody body;
		private bool scanned;
		private float level;
		private Dictionary<string, DMAnomalyObject> anomalies = new Dictionary<string, DMAnomalyObject>();

		public DMAnomalyStorage(CelestialBody b, bool s = true)
		{
			body = b;
			scanned = s;
			level = CelestialUtilities.PlanetScienceRanking(b);
		}

		public void addAnomaly(DMAnomalyObject anom)
		{
			if (!anomalies.ContainsKey(anom.Name))
				anomalies.Add(anom.Name, anom);
		}

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

			return AnomalyCount > 0;
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

		public List<DMAnomalyObject> getAllAnomalies
		{
			get { return anomalies.Values.ToList(); }
		}

		public CelestialBody Body
		{
			get { return body; }
		}

		public bool Scanned
		{
			get { return scanned; }
		}

		public int AnomalyCount
		{
			get { return anomalies.Count; }
		}

		public float Level
		{
			get { return level; }
		}
	}
}
