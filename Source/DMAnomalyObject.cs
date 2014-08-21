

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DMagic
{
	internal class DMAnomalyObject
	{
		internal PQSCity city;
		internal Vector3d worldLocation;
		internal CelestialBody body;
		internal double lat, lon, alt;
		internal double Vdistance, Vheight, Vhorizontal;
		internal double bearing;
		internal string name;

		internal DMAnomalyObject(PQSCity City)
		{
			city = City;
			name = city.name;
			try
			{
				body = FlightGlobals.Bodies.FirstOrDefault(b => b.name == city.transform.parent.name);
			}
			catch (Exception e)
			{
				DMUtils.Logging("Something Went Wrong Here: {0}", e);
			}
			if (body != null)
			{
				worldLocation = city.transform.position;
				lat = body.GetLatitude(worldLocation);
				lon = body.GetLongitude(worldLocation);
				alt = body.GetAltitude(worldLocation);
			}
		}
	}
}
