/* DMagic Orbital Science - Magnetometer
 * Magnetosphere simulation.
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


using UnityEngine;
using System.Linq;
using System;

namespace DMagic
{

	public class DMMagBoomModule : PartModule
	{

		#region Fields and Setup

		[KSPField(guiActive = false, guiName = "Bt")]
		public string Bt;
		[KSPField(guiActive = false, guiName = "Inclination")]
		public string inc;
		[KSPField(guiActive = false, guiName = "Declination")]
		public string dec;
		[KSPField(guiActive = false, guiName = "Bh")]
		public string BhS;
		[KSPField(guiActive = false, guiName = "Bz")]
		public string BzS;

		//public string Br;
		//public string Btheta;
		//public string Bpsi;
		//public string BX;
		//public string BY;
		//public string BZ;

		//Development fields

		//[KSPField(guiActive = false, guiName = "X: ")]
		//public string sunX;
		//[KSPField(guiActive = false, guiName = "Z: ")]
		//public string sunZ;
		//[KSPField(guiActive = false, guiName = "Alt: ")]
		//public string lats;
		//[KSPField(guiActive = false, guiName = "nDay")]
		//public string nDays;
		//[KSPField(guiActive = false, guiName = "Long Shifted: ")]
		//public string lons;
		//[KSPField(guiActive = false, guiName = "Bh")]
		//public string Bhold;

		[KSPField]
		public bool runMagnetometer;
		[KSPField]
		public string resourceToUse = "ElectricCharge";
		[KSPField]
		public float resourceCost = 0;

		private DMModuleScienceAnimate primaryModule;
		private float lastUpdate = 0f;
		private float updateInterval = 0.2f;

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);
			if (part.FindModulesImplementing<DMModuleScienceAnimate>().Count > 0)
				primaryModule = part.FindModulesImplementing<DMModuleScienceAnimate>().First();
		}

		public override string GetInfo()
		{
			string info = base.GetInfo();
			info += "Requires:\n- " + resourceToUse + ": " + resourceCost.ToString() + "/s\n";
			return info;
		}

		MagVar magValues = new MagVar();

		private double[] getMag(double lat, double lon, double alt, long date, int i, double[] field)
		{
			return magValues.SGMagVar(lat, lon, alt, date, i, field);
		}

		#endregion

		#region Update

		public void Update()
		{
			float deltaTime = TimeWarp.deltaTime / Time.deltaTime;
			if (deltaTime > 10) deltaTime = 10;
			if (((Time.time * deltaTime) - lastUpdate) > updateInterval) {
				lastUpdate = Time.time;
				if (primaryModule.IsDeployed && runMagnetometer) {
					CelestialBody planetID = vessel.mainBody;
					int ID = planetID.flightGlobalsIndex;
					part.RequestResource(resourceToUse, resourceCost * TimeWarp.deltaTime);

					//double latDeg; // = (vessel.latitude + 90 + 180) % 180 - 90;
					//double lonDeg; // = (vessel.longitude + 180 + 360) % 360 - 180;
					double lat;
					double lon;
					double alt;
					double uDay;
					double nDay;
					int localDay;
					long date;

					//Paramaters for mag field model
					int i = 10;
					double[] field = new double[6];
					double lonShift;

					if (ID == 1 || ID == 2 || ID == 3 || ID == 5 || ID == 6 || ID == 7 || ID == 8 || ID == 9 || ID == 10 || ID == 11 || ID == 12 || ID == 13 || ID == 14) {
						if (ID == 9 || ID == 10 || ID == 11 || ID == 12 || ID == 14) {
							//For now the Joolian moons return values relative to Jool
							Vector3 vesselPosition = vessel.transform.position;
							alt = FlightGlobals.fetch.bodies[8].GetAltitude(vesselPosition) / 5000;
							lat = ((FlightGlobals.fetch.bodies[8].GetLatitude(vesselPosition) + 90 + 180) % 180 - 90) * Mathf.Deg2Rad;
							lon = ((FlightGlobals.fetch.bodies[8].GetLongitude(vesselPosition) + 180 + 360) % 360 - 180) * Mathf.Deg2Rad;
							planetID = FlightGlobals.fetch.bodies[8];
						}
						else if (ID == 2 || ID == 3) {
							Vector3 vesselPosition = vessel.transform.position;
							alt = FlightGlobals.fetch.bodies[1].GetAltitude(vesselPosition) / 1000;
							lat = ((FlightGlobals.fetch.bodies[1].GetLatitude(vesselPosition) + 90 + 180) % 180 - 90) * Mathf.Deg2Rad;
							lon = ((FlightGlobals.fetch.bodies[1].GetLongitude(vesselPosition) + 180 + 360) % 360 - 180) * Mathf.Deg2Rad;
							planetID = FlightGlobals.fetch.bodies[1];
						}
						else if (ID == 7) {
							Vector3 vesselPosition = vessel.transform.position;
							alt = FlightGlobals.fetch.bodies[6].GetAltitude(vesselPosition) / 1000;
							lat = ((FlightGlobals.fetch.bodies[6].GetLatitude(vesselPosition) + 90 + 180) % 180 - 90) * Mathf.Deg2Rad;
							lon = ((FlightGlobals.fetch.bodies[6].GetLongitude(vesselPosition) + 180 + 360) % 360 - 180) * Mathf.Deg2Rad;
							planetID = FlightGlobals.fetch.bodies[6];
						}
						else if (ID == 13) {
							Vector3 vesselPosition = vessel.transform.position;
							alt = FlightGlobals.fetch.bodies[5].GetAltitude(vesselPosition) / 1000;
							lat = ((FlightGlobals.fetch.bodies[5].GetLatitude(vesselPosition) + 90 + 180) % 180 - 90) * Mathf.Deg2Rad;
							lon = ((FlightGlobals.fetch.bodies[5].GetLongitude(vesselPosition) + 180 + 360) % 360 - 180) * Mathf.Deg2Rad;
							planetID = FlightGlobals.fetch.bodies[5];
						}
						else {
							lat = ((vessel.latitude + 90 + 180) % 180 - 90) * Mathf.Deg2Rad;
							lon = ((vessel.longitude + 180 + 360) % 360 - 180) * Mathf.Deg2Rad;
							alt = vessel.altitude / 1000;
							if (ID == 8) alt /= 5;
						}

						uDay = Planetarium.GetUniversalTime() / solarDay(planetID);
						nDay = uDay % 1;

						//Shift our current longitide to account for solar day - lonShift should equal zero when crossing solar noon, bring everything down to -Pi to Pi just to be safe
						//For reference, at time zero the sun is directly above -90.158 Deg West on Kerbin, I'm rounding that to -90, or -Pi/2
						lonShift = ((lon + longShift(planetID, nDay)) + Math.PI + Math.PI * 2) % (2 * Math.PI) - Math.PI;

						//Simulate magnetosphere distortion by solar wind with stretched torus shape, determine our position on the surface of the torus
						double radiusx = ((3.5 + (1 + 1 / Math.Cos(lonShift)) * Math.Cos(Math.PI + lonShift)) + (3.5 + (1.3 + 1 / Math.Cos(lonShift)) * Math.Cos(Math.PI + lonShift)) * Math.Cos(lat * 2)) * Math.Cos(lonShift);
						double radiusy = (0.75 + 0.9 * Math.Cos(lat * 2)) * Math.Sin(lonShift);
						double radiusz = (1 + 0.2 * Math.Cos(lonShift)) * Math.Sin(lat * 2);
						double Radius = Math.Sqrt((radiusx * radiusx) + (radiusy * radiusy) + (radiusz * radiusz));
						if (Radius == 0)
							Radius += 0.001;

						//Scale our altitude by our position on the simulated torus, ignore at altitudes below one planetary radius, ramp up quickly above high scaled altitude up to a max value
						if (alt > altScale(planetID)) {
							alt *= radScale(planetID) / Radius;
							if (alt < altScale(planetID))
								alt = altScale(planetID);
						}
						if (alt > altScale(planetID) * 2)
							alt *= Math.Pow((alt / (altScale(planetID) * 2)), 3);
						if (alt > altMax(planetID))
							alt = altMax(planetID);
					}
					else if (ID == 0) {
						lat = ((vessel.latitude + 90 + 180) % 180 - 90) * Mathf.Deg2Rad;
						lon = ((vessel.longitude + 180 + 360) % 360 - 180) * Mathf.Deg2Rad;
						alt = vessel.altitude / 50000;
						uDay = Planetarium.GetUniversalTime() / solarDay(planetID);
					}
					else {
						//For non-magnetic planets use our position relative to the sun to calculate alt, lat, and long
						Vector3 vesselPosition = vessel.transform.position;
						alt = FlightGlobals.fetch.bodies[0].GetAltitude(vesselPosition) / 50000;
						lat = ((FlightGlobals.fetch.bodies[0].GetLatitude(vesselPosition) + 90 + 180) % 180 - 90) * Mathf.Deg2Rad;
						lon = ((FlightGlobals.fetch.bodies[0].GetLongitude(vesselPosition) + 180 + 360) % 360 - 180) * Mathf.Deg2Rad;
						planetID = FlightGlobals.fetch.bodies[0];
						uDay = Planetarium.GetUniversalTime() / solarDay(planetID);
					}

					localDay = Convert.ToInt32(uDay);
					date = 2455197 + (localDay % 500) + ID * 25;

					//Send all of our modified parameters to the field model
					double[] magComp = getMag(lat, lon, alt, date, i, field);

					//Magnetic field components
					//double Brad = magComp[0];
					//double BPsi = magComp[2];
					//double BTheta = magComp[1];
					double Bx = magComp[3] * planetScale(planetID);
					double By = magComp[4] * planetScale(planetID);
					double Bz = magComp[5] * planetScale(planetID);

					//Calculate various magenetic field components based on 3-axis field strength 
					double Bh = Math.Sqrt((Bx * Bx) + (By * By));

					//Alter the magnetic field line vector when far away from the planet
					if (ID > 0) {
						if (alt > altScale(planetID) * 3) {
							if (ID == 8) {
								if (alt < (altMax(planetID) / 7)) {
									Bh /= (alt / (altScale(planetID) * 3));
									Bz *= (alt / (altScale(planetID) * 3));
								}
								else {
									Bh /= ((altMax(planetID) / 7) / (altScale(planetID) * 3));
									Bz *= ((altMax(planetID) / 7) / (altScale(planetID) * 3));
								}
							}
							else {
								if (alt < (altMax(planetID) / 2)) {
									Bh /= (alt / (altScale(planetID) * 3));
									Bz *= (alt / (altScale(planetID) * 3));
								}
								else {
									Bh /= ((altMax(planetID) / 2) / (altScale(planetID) * 3));
									Bz *= ((altMax(planetID) / 2) / (altScale(planetID) * 3));
								}
							}
						}
					}

					double Bti = Math.Sqrt((Bh * Bh) + (Bz * Bz));
					double dip = Math.Atan2(Bz, Bh);
					double decD;

					//Return 0 declination at magnetic poles
					if (Bx != 0.0 || By != 0.0) decD = Math.Atan2(By, Bx);
					else decD = 0.0;

					//Convert values for better display
					dip *= Mathf.Rad2Deg;
					decD *= Mathf.Rad2Deg;
					float Btf = (float)Bti;
					float incf = (float)dip;
					float decf = (float)decD;
					float Bzf = (float)Bz;
					float Bhf = (float)Bh;

					//float BRf = (float)Brad;
					//float BPsif = (float)BPsi;
					//float BThetaf = (float)BTheta;
					//float Bxf = (float)Bx;
					//float Byf = (float)By;

					//Display in right-click menu
					Bt = Btf.ToString("F2") + " nT";
					inc = incf.ToString("F2") + "Deg";
					dec = decf.ToString("F2") + "Deg";
					BhS = Bhf.ToString("F2") + " nT";
					BzS = Bzf.ToString("F2") + " nT";

					//Br = BRf.ToString("F2") + " nT";
					//Bpsi = BPsif.ToString("F2") + " nT";
					//Btheta = BThetaf.ToString("F2") + " nT";
					//BX = Bxf.ToString("F2") + " nT";
					//BY = Byf.ToString("F2") + " nT";
					//BZ = Bzf.ToString("F2") + " nT";

					Fields["Bt"].guiActive = primaryModule.IsDeployed;
					Fields["inc"].guiActive = alt < (altScale(planetID) / 2);
					Fields["dec"].guiActive = alt < (altScale(planetID) / 2);
					Fields["BhS"].guiActive = alt >= (altScale(planetID) / 2);
					Fields["BzS"].guiActive = alt >= (altScale(planetID) / 2);

					//Extra variables - used in development

					//nDays = nDay.ToString();
					//float altf = (float)alt;
					//float nDayf = (float)nDay;
					//Vector3 sunP = FlightGlobals.fetch.bodies[0].position;
					//Vector3 sunD = transform.InverseTransformPoint(sunP) - part.transform.localPosition;
					////lons = nDayf.ToString("F5");
					//double sunXd = sunD.x;
					//double sunZd = sunD.z;
					//sunX = sunXd.ToString();
					//sunZ = sunZd.ToString();
					//lats = altf.ToString("F4");
					//nDays = nDayf.ToString("F4");
					//radius = Radius.ToString();
					//float latf = (float)latDeg;
					//lats = (lon * Mathf.Rad2Deg).ToString();
					//lons = (lonShift * Mathf.Rad2Deg).ToString();
					//float lonf = (float)lonDeg;
					//lats = latf.ToString("F3") + " Deg";
					//altScaled = alt.ToString();
					//lons = lonf.ToString("F3") + " Deg";
					//lons = Btf.ToString("F2") + " nT";
					//lons = "Shifted long: " + (((lonShift * Mathf.Rad2Deg) + 180 + 360) % 360 - 180).ToString();
					//lons = "Scaled Bh: " + Bh.ToString();
					//Bznew = "Scaled Bz: " + Bz.ToString();

					//Fields["sunX"].guiActive = primaryModule.IsDeployed;
					//Fields["sunZ"].guiActive = primaryModule.IsDeployed;
					//Fields["lats"].guiActive = primaryModule.IsDeployed;
					//Fields["nDays"].guiActive = primaryModule.IsDeployed;
					//Fields["lons"].guiActive = primaryModule.IsDeployed;
					//Fields["Bhold"].guiActive = primaryModule.IsDeployed;
				}
				else {
					Fields["Bt"].guiActive = false;
					Fields["inc"].guiActive = false;
					Fields["dec"].guiActive = false;
					Fields["BhS"].guiActive = false;
					Fields["BzS"].guiActive = false;
				}
			}
		}

		#endregion

		#region Planet Scalars

		private double longShift(CelestialBody planet, double nDay)
		{
			double shift = 0;
			if (planet.flightGlobalsIndex == 1)
				shift = (0.5 * Math.PI) + (2 * nDay * Math.PI);
			if (planet.flightGlobalsIndex == 5)
				shift = (1.958985598 * Math.PI) + (2 * nDay * Math.PI);
			if (planet.flightGlobalsIndex == 6)
				shift = (1.767319403 * Math.PI) + (2 * nDay * Math.PI);
			if (planet.flightGlobalsIndex == 8)
				shift = (0.684750775 * Math.PI) + (2 * nDay * Math.PI);
			return shift;
		}

		private double altScale(CelestialBody planet)
		{
			double scale = planet.Radius / 1000;
			if (planet.flightGlobalsIndex == 0)
				scale = planet.Radius / 50000;
			if (planet.flightGlobalsIndex == 8)
				scale = planet.Radius / 5000;
			return scale;
		}

		private double altMax(CelestialBody planet)
		{
			double max = 1;
			if (planet.flightGlobalsIndex == 1)
				max = 40000;
			if (planet.flightGlobalsIndex == 5)
				max = 30000;
			if (planet.flightGlobalsIndex == 6)
				max = 15000;
			if (planet.flightGlobalsIndex == 8)
				max = 200000;
			return max;
		}

		private double solarDay(CelestialBody planet)
		{
			double solarDay = planet.rotationPeriod;
			if (planet.flightGlobalsIndex > 0)
				solarDay = planet.rotationPeriod / (1 - (planet.rotationPeriod / planet.orbit.period));
			return solarDay;
		}

		private double planetScale(CelestialBody planet)
		{
			double pScale = 1;
			if (planet.flightGlobalsIndex == 0)
				pScale = 5000;
			if (planet.flightGlobalsIndex == 1)
				pScale = 1;
			if (planet.flightGlobalsIndex == 5)
				pScale = 4;
			if (planet.flightGlobalsIndex == 6)
				pScale = 0.08;
			if (planet.flightGlobalsIndex == 8)
				pScale = 8;
			return pScale;
		}

		private double radScale(CelestialBody planet)
		{
			double rScale = 1;
			if (planet.flightGlobalsIndex == 1)
				rScale = 0.9;
			if (planet.flightGlobalsIndex == 5)
				rScale = 0.7;
			if (planet.flightGlobalsIndex == 6)
				rScale = 2;
			if (planet.flightGlobalsIndex == 8)
				rScale = 0.5;
			return rScale;
		}

		#endregion

	}
}
