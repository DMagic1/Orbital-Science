#region license
/* DMagic Orbital Science - DMAnomalyStorage
 * Class to store information about anomalies for a specific planet
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
using FinePrint.Utilities;

namespace DMagic
{
	public class DMAnomalyStorage
	{
		private CelestialBody body;
		private bool scanned;
		private float level;
		private DictionaryValueList<string, DMAnomalyObject> anomalies = new DictionaryValueList<string, DMAnomalyObject>();

		public DMAnomalyStorage(CelestialBody b, bool s = true)
		{
			body = b;
			scanned = s;
			level = CelestialUtilities.PlanetScienceRanking(b);
		}

		public void addAnomaly(DMAnomalyObject anom)
		{
			if (!anomalies.Contains(anom.Name))
				anomalies.Add(anom.Name, anom);
		}

		public bool scanBody()
		{
			scanned = true;

			if (body.pqsController == null)
				return false;

			PQSSurfaceObject[] Cities = body.pqsSurfaceObjects;

			for (int i = 0; i < Cities.Length; i++)
			{
				PQSSurfaceObject city = Cities[i];

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
			if (anomalies.Contains(name))
				return anomalies[name];

			return null;
		}

		public DMAnomalyObject getAnomaly(int index)
		{
			if (anomalies.Count > index)
				return anomalies.At(index);

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
