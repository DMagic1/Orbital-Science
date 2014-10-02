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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DMagic
{
	internal class DMAnomalyList : MonoBehaviour
	{
		internal static List<DMAnomalyObject> anomObjects = new List<DMAnomalyObject>();
		private static bool scannerUpdating;
		private static bool magUpdating;

		private void Start()
		{
			GameEvents.onVesselSOIChanged.Add(SOIChange);
			pqsBuild(FlightGlobals.currentMainBody);
		}

		private void OnDestroy()
		{
			GameEvents.onVesselSOIChanged.Remove(SOIChange);
		}

		internal static bool ScannerUpdating
		{
			get { return scannerUpdating; }
			set { scannerUpdating = value; }
		}

		internal static bool MagUpdating
		{
			get { return magUpdating; }
			set { magUpdating = value; }
		}

		private void SOIChange(GameEvents.HostedFromToAction<Vessel, CelestialBody> VB)
		{
			pqsBuild(VB.to);
		}

		private void pqsBuild(CelestialBody body)
		{
			scannerUpdating = false;
			magUpdating = false;
			anomObjects.Clear();
			PQSCity[] Cities = FindObjectsOfType(typeof(PQSCity)) as PQSCity[];
			foreach (PQSCity anomalyObject in Cities)
			{
				if (anomalyObject.transform.parent.name == FlightGlobals.currentMainBody.name)
					anomObjects.Add(new DMAnomalyObject(anomalyObject));
			}
		}

		internal static void updateAnomaly(Vessel v, DMAnomalyObject a)
		{
			Vector3d vPos = v.transform.position;
			a.worldLocation = a.city.transform.position;
			//Calculate vectors from CBody position to object positions
			Vector3d vSurfaceV = v.mainBody.GetRelSurfacePosition(vPos);
			Vector3d aSurfaceV = v.mainBody.GetRelSurfacePosition(a.worldLocation);
			//Project vessel vector onto anomaly vector, use magnitude to determine height above or below anomaly
			a.Vheight = Vector3d.Project(vSurfaceV, aSurfaceV).magnitude - aSurfaceV.magnitude;
			a.Vdistance = (a.worldLocation - vPos).magnitude;
			if (Math.Abs(a.Vheight) <= a.Vdistance)
				a.Vhorizontal = Math.Sqrt((a.Vdistance * a.Vdistance) - (a.Vheight * a.Vheight));
			else
				a.Vhorizontal = double.MaxValue; //This should never happen...
			
		}

		internal static void bearing(Vessel v, DMAnomalyObject a)
		{
			double vlat = v.latitude;
			double vlon = v.longitude;
			double longdiff = (a.lon - vlon) * Mathf.Deg2Rad;
			double y = Math.Sin(longdiff) * Math.Cos(Mathf.Deg2Rad * a.lat);
			double x = Math.Cos(Mathf.Deg2Rad * vlat) * Math.Sin(Mathf.Deg2Rad * a.lat) - Math.Sin(Mathf.Deg2Rad * vlat) * Math.Cos(Mathf.Deg2Rad * a.lat) * Math.Cos(longdiff);
			double aBearing = (Math.Atan2(y, x) * Mathf.Rad2Deg + 360) % 360;
			a.bearing = aBearing;
		}
	}
}
