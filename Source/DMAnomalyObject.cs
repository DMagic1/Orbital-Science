#region license
/* DMagic Orbital Science - DMAnomalyObject
 * An object to hold anomaly data
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
using System.Linq;
using UnityEngine;

namespace DMagic
{
	public class DMAnomalyObject
	{
		private Vector3d worldLocation;
		private CelestialBody body;
		private double lat, lon, alt;
		private double Vdistance, Vheight, Vhorizontal;
		private double bearing;
		private string name;

		public DMAnomalyObject(PQSCity City)
		{
			name = City.name;
			try
			{
				body = FlightGlobals.Bodies.FirstOrDefault(b => b.name == City.transform.parent.name);
			}
			catch (Exception e)
			{
				DMUtils.Logging("Something Went Wrong Here: {0}", e);
			}
			if (body != null)
			{
				worldLocation = City.transform.position;
				lat = clampLat(body.GetLatitude(worldLocation));
				lon = clampLon(body.GetLongitude(worldLocation));
				alt = body.GetAltitude(worldLocation);
			}
		}

		public DMAnomalyObject(string n, CelestialBody b, double la, double lo, double a)
		{
			name = n;
			body = b;
			lat = la;
			lon = lo;
			alt = a;
		}

		private double clampLat(double l)
		{
			return (l + 180 + 90) % 180 - 90;
		}

		private double clampLon(double l)
		{
			return (l + 360 + 180) % 360 - 180;
		}

		public Vector3d WorldLocation
		{
			get { return worldLocation; }
			internal set { worldLocation = value; }
		}

		public CelestialBody Body
		{
			get { return body; }
		}

		public double Lat
		{
			get { return lat; }
			internal set
			{
				lat = clampLat(value);
			}
		}

		public double Lon
		{
			get { return lon; }
			internal set
			{
				lon = clampLon(value);
			}
		}

		public double Alt
		{
			get { return alt; }
		}

		public double VDistance
		{
			get { return Vdistance; }
			internal set
			{
				if (value >= 0)
					Vdistance = value;
			}
		}

		public double VHeight
		{
			get { return Vheight; }
			internal set
			{
				if (value >= 0)
					Vheight = value;
			}
		}

		public double VHorizontal
		{
			get { return Vhorizontal; }
			internal set
			{
				if (value >= 0)
					Vhorizontal = value;
				else
					Vhorizontal = double.MaxValue;
			}
		}

		public double Bearing
		{
			get { return bearing; }
			internal set { bearing = value; }
		}

		public string Name
		{
			get { return name; }
		}
	}
}
