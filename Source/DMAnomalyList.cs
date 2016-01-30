#region license
/* DMagic Orbital Science - DMAnomalyList
 * An class to update and store info on anomalies
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
 *  
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DMagic
{
	public static class DMAnomalyList
	{
		private static bool scannerUpdating;
		private static Dictionary<string, DMAnomalyStorage> anomalies = new Dictionary<string, DMAnomalyStorage>();

		public static void addAnomalyStorage(string body, DMAnomalyStorage anom)
		{
			if (!anomalies.ContainsKey(body))
				anomalies.Add(body, anom);
		}

		public static DMAnomalyObject getAnomalyObject(string body, string city)
		{
			if (!anomalies.ContainsKey(body))
			{
				DMUtils.Logging("No anomaly of name [{0}] found for body [{1}]", city, body);
				return null;
			}

			return anomalies[body].getAnomaly(city);
		}

		public static DMAnomalyStorage getAnomalyStorage(int index)
		{
			if (anomalies.Count > index)
				return anomalies.ElementAt(index).Value;

			return null;
		}

		public static DMAnomalyStorage getAnomalyStorage(string body)
		{
			if (anomalies.ContainsKey(body))
				return anomalies[body];

			return null;
		}

		public static void clearAnomalies()
		{
			anomalies.Clear();
		}

		public static bool ScannerUpdating
		{
			get { return scannerUpdating; }
			internal set { scannerUpdating = value; }
		}

		public static int AnomalyCount
		{
			get { return anomalies.Count; }
		}

		public static List<CelestialBody> getScannedBodies
		{
			get
			{
				List<CelestialBody> bodies = new List<CelestialBody>();

				for (int i = 0; i < anomalies.Count; i++)
				{
					CelestialBody c = FlightGlobals.Bodies.FirstOrDefault(a => a.name == anomalies.ElementAt(i).Key);

					if (c == null)
						continue;

					bodies.Add(c);
				}

				return bodies;
			}
		}

		public static void updateCoordinates(CelestialBody b)
		{
			if (anomalies.ContainsKey(b.name))
			{
				for (int i = 0; i < anomalies[b.name].AnomalyCount; i++)
				{
					DMAnomalyObject anom = anomalies[b.name].getAnomaly(i);

					if (anom == null)
						continue;

					anom.WorldLocation = b.GetWorldSurfacePosition(anom.Lat, anom.Lon, anom.Alt);
					anom.Lat = b.GetLatitude(anom.WorldLocation);
					anom.Lon = b.GetLongitude(anom.WorldLocation);
				}
			}
		}

		public static void updateAnomaly(Vessel v, DMAnomalyObject a)
		{
			Vector3d vPos = v.transform.position;
			a.WorldLocation = v.mainBody.GetWorldSurfacePosition(a.Lat, a.Lon, a.Alt);

			a.Lat = v.mainBody.GetLatitude(a.WorldLocation);
			a.Lon = v.mainBody.GetLongitude(a.WorldLocation);

			//Calculate vectors from CBody position to object positions
			Vector3d anomBody = v.mainBody.position - a.WorldLocation;
			Vector3d vesselBody = v.mainBody.position - v.transform.position;

			//Project vessel vector onto anomaly vector
			Vector3d projectedV = Vector3d.Project(vesselBody, anomBody);

			//Calculate height above or below anomaly by drawing a line between the projected vector and the anomaly vector
			//Take the magnitude of that line, which equals the height
			a.VHeight = (anomBody - projectedV).magnitude;
			a.VDistance = (a.WorldLocation - vPos).magnitude;
			a.VHorizontal = Math.Sqrt((a.VDistance * a.VDistance) - (a.VHeight * a.VHeight));
		}

		public static void bearing(Vessel v, DMAnomalyObject a)
		{
			a.Bearing = DMUtils.bearing(v.latitude, v.longitude, a.Lat, a.Lon);
		}
	}
}
