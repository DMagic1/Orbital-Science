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
			double vAlt = v.mainBody.GetAltitude(vPos);
			a.Vdistance = (a.worldLocation - vPos).magnitude;
			a.Vheight = Math.Abs(vAlt - a.alt);
			a.Vhorizontal = Math.Sqrt((a.Vdistance * a.Vdistance) - (a.Vheight * a.Vheight));
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
