/* DMagic Orbital Science - Magnetometer
 * Magnetosphere simulation.
 *
 * Copyright (c) 2014, David Grandy
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
using System.Collections;
using System.Collections.Generic;

namespace DMagic
{
    
    public class DMMagBoomModule : PartModule
    {        
        //[KSPField(guiActive = false, guiName = "Bt")]
        public string Bt;
        //[KSPField(guiActive = false, guiName = "Inclination")]
        public string inc;
        //[KSPField(guiActive = false, guiName = "Declination")]
        public string dec;
        

        //public string Br;
        //public string Btheta;
        //public string Bpsi;
        //public string BX;
        //public string BY;
        //public string BZ;

        //Development fields

        [KSPField(guiActive = false, guiName = "X: ")]
        public string sunX;
        [KSPField(guiActive = false, guiName = "Z: ")]
        public string sunZ;
        [KSPField(guiActive = false, guiName = "Long: ")]
        public string lats;
        [KSPField(guiActive = false, guiName = "nDay")]
        public string nDays;
        [KSPField(guiActive = false, guiName = "Long Shifted: ")]
        public string lons;
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
            this.part.force_activate();
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

        public double[] getMag(double lat, double lon, double alt, long date, int i, double[] field)
        {
            return magValues.SGMagVar(lat, lon, alt, date, i, field);
        }

        public void Update()
        {
            if ((Time.time - lastUpdate) > updateInterval)
            {
                lastUpdate = Time.time;

                if (runMagnetometer) // && planetID.flightGlobalsIndex == 0 || runMagnetometer && planetID.flightGlobalsIndex == 5 || runMagnetometer && planetID.flightGlobalsIndex == 6 || runMagnetometer && planetID.flightGlobalsIndex == 0)
                {
                    
                    //Fields["Bt"].guiActive = primaryModule.IsDeployed;
                    //Fields["inc"].guiActive = primaryModule.IsDeployed;
                    //Fields["dec"].guiActive = primaryModule.IsDeployed;
                    Fields["sunX"].guiActive = primaryModule.IsDeployed;
                    Fields["sunZ"].guiActive = primaryModule.IsDeployed;
                    Fields["lats"].guiActive = primaryModule.IsDeployed;
                    Fields["nDays"].guiActive = primaryModule.IsDeployed;
                    Fields["lons"].guiActive = primaryModule.IsDeployed;
                    //Fields["Bhold"].guiActive = primaryModule.IsDeployed;

                    if (primaryModule.IsDeployed)
                    {
                        CelestialBody planetID = vessel.mainBody;
                        part.RequestResource(resourceToUse, resourceCost * Time.deltaTime);

                        double latDeg = (vessel.latitude + 90 + 180) % 180 - 90;
                        double lonDeg = (vessel.longitude + 180 + 360) % 360 - 180;
                        double lat = latDeg * Mathf.Deg2Rad;
                        double lon = lonDeg * Mathf.Deg2Rad;
                        double alt = vessel.altitude / 1000;

                        //Get universal time in seconds and calculate our time during the normalized solar day
                        double uTime = Planetarium.GetUniversalTime();
                        double uDay = uTime / solarDay(planetID);
                        double nDay = uDay % 1;

                        //Change the simulation day to add some variability, start with Jan 1 2010 in Julian Date format
                        int localDay = Convert.ToInt32(uDay);
                        long date = 2455197 + (localDay % 500) + planetID.flightGlobalsIndex * 25;

                        //Paramaters for mag field model
                        int i = 10;
                        double[] field = new double[6];
                        double lonShift = 1;


                        if (planetID.flightGlobalsIndex == 1 || planetID.flightGlobalsIndex == 5 || planetID.flightGlobalsIndex == 6 || planetID.flightGlobalsIndex == 8)
                        {
                            //Shift our current longitide to account for solar day - lonShift should equal zero when crossing solar noon, bring everything down to -Pi to Pi just to be safe
                            //For reference, at time zero the sun is directly above -90.158 Deg West on Kerbin, I'm rounding that to -90, or -Pi/2
                            lonShift = ((lon + longShift(planetID, nDay)) + Math.PI + Math.PI * 2) % (2 * Math.PI) - Math.PI;

                            //Simulate magnetosphere distortion by solar wind with stretched torus shape, determine our position on the surface of the torus
                            double radiusx = ((3.5 + (1 + 1 / Math.Cos(lonShift)) * Math.Cos(Math.PI + lonShift)) + (3.5 + (1.3 + 1 / Math.Cos(lonShift)) * Math.Cos(Math.PI + lonShift)) * Math.Cos(lat * 2)) * Math.Cos(lonShift);
                            double radiusy = (0.75 + 0.9 * Math.Cos(lat * 2)) * Math.Sin(lonShift);
                            double radiusz = (1 + 0.2 * Math.Cos(lonShift)) * Math.Sin(lat * 2);
                            double Radius = Math.Sqrt((radiusx * radiusx) + (radiusy * radiusy) + (radiusz * radiusz));
                            if (Radius == 0) Radius += 0.001;

                            //Scale our altitude by our position on the simulated torus, ignore at altitudes below 600km, ramp up quickly above high scaled altitude up to a max value
                            if (alt > altScale(planetID))
                            {
                                alt *= 1 / Radius;
                                if (alt < altScale(planetID)) alt = altScale(planetID);
                            }
                            if (alt > altScale(planetID) * 2) alt *= Math.Pow((alt / (altScale(planetID) * 2)), 3);
                            if (alt > altMax(planetID)) alt = altMax(planetID);
                        }
                        else if (planetID.flightGlobalsIndex == 0)
                        {
                            alt /= 10;
                        }
                        else
                        {
                            Vector3 planetPosition = planetID.position;
                            alt = FlightGlobals.fetch.bodies[0].GetAltitude(planetPosition) / 10000;
                            lat = ((FlightGlobals.fetch.bodies[0].GetLatitude(planetPosition) + 90 + 180) % 180 - 90) * Mathf.Deg2Rad;
                            lon = ((FlightGlobals.fetch.bodies[0].GetLongitude(planetPosition) + 180 + 360) % 360 - 180) * Mathf.Deg2Rad;
                        }

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
                        //Bzold = "Bz: " + Bz.ToString();
                        //Bhold = "Bh: " + Bh.ToString();

                        //Alter the magnetic field line vector when far away from Kerbin
                        if (planetID.flightGlobalsIndex == 1 || planetID.flightGlobalsIndex == 5 || planetID.flightGlobalsIndex == 6 || planetID.flightGlobalsIndex == 8)
                        {
                            if (alt > altScale(planetID) * 3)
                            {
                                Bh /= (alt / (altScale(planetID) * 3));
                                Bz *= (alt / (altScale(planetID) * 3));
                                if (alt > altMax(planetID) / 2)
                                {
                                    Bz /= (alt / (altMax(planetID) / 2));
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
                        //float BRf = (float)Brad;
                        //float BPsif = (float)BPsi;
                        //float BThetaf = (float)BTheta;
                        //float Bxf = (float)Bx;
                        //float Byf = (float)By;
                        //float Bzf = (float)Bz;

                        //Display in right-click menu
                        Bt = Btf.ToString("F2") + " nT";
                        inc = incf.ToString("F2") + "Deg";
                        dec = decf.ToString("F2") + "Deg";
                        //Br = BRf.ToString("F2") + " nT";
                        //Bpsi = BPsif.ToString("F2") + " nT";
                        //Btheta = BThetaf.ToString("F2") + " nT";
                        //BX = Bxf.ToString("F2") + " nT";
                        //BY = Byf.ToString("F2") + " nT";
                        //BZ = Bzf.ToString("F2") + " nT";

                        //Extra variables - used in development

                        nDays = nDay.ToString();
                        //float altf = (float)alt;
                        //float nDayf = (float)nDay;
                        Vector3 sunP = FlightGlobals.fetch.bodies[0].position;
                        Vector3 sunD = transform.InverseTransformPoint(sunP) - part.transform.localPosition;
                        //lons = nDayf.ToString("F5");
                        double sunXd = sunD.x;
                        double sunZd = sunD.z;
                        sunX = sunXd.ToString();
                        sunZ = sunZd.ToString();

                        //nDays = nDayf.ToString("F4");
                        //radius = Radius.ToString();
                        //float latf = (float)latDeg;
                        lats = lonDeg.ToString();
                        lons = (lonShift * Mathf.Rad2Deg).ToString();
                        //float lonf = (float)lonDeg;
                        //lats = latf.ToString("F3") + " Deg";
                        //altScaled = alt.ToString();
                        //lons = lonf.ToString("F3") + " Deg";
                        //lons = Btf.ToString("F2") + " nT";
                        //lons = "Shifted long: " + (((lonShift * Mathf.Rad2Deg) + 180 + 360) % 360 - 180).ToString();
                        //lons = "Scaled Bh: " + Bh.ToString();
                        //Bznew = "Scaled Bz: " + Bz.ToString();
                    }
                }
                //else
                //{
                //    //Fields["Bt"].guiActive = false;
                //    //Fields["inc"].guiActive = false;
                //    //Fields["dec"].guiActive = false;
                //    Fields["sunX"].guiActive = false;
                //    Fields["sunZ"].guiActive = false;
                //    Fields["lats"].guiActive = false;
                //    Fields["nDays"].guiActive = false;
                //    Fields["lons"].guiActive = false;
                //    //Fields["Bhold"].guiActive = false;
                //}
            }
        }

        private double longShift(CelestialBody planet, double nDay)
        {
            double shift = 0;
            if (planet.flightGlobalsIndex == 1) shift = (0.5 * Math.PI) + (2 * nDay * Math.PI);
            if (planet.flightGlobalsIndex == 5) shift = (1.958985598 * Math.PI) + (2 * nDay * Math.PI);
            if (planet.flightGlobalsIndex == 6) shift = (1.767319403 * Math.PI) + (2 * nDay * Math.PI);
            if (planet.flightGlobalsIndex == 8) shift = 1;
            return shift;
        }

        private double altScale(CelestialBody planet)
        {
            double scale = planet.Radius / 1000;
            return scale;
        }

        private double altMax(CelestialBody planet)
        {
            double max = 1;
            if (planet.flightGlobalsIndex == 0) max = 100000000;
            if (planet.flightGlobalsIndex == 1) max = 20000;
            if (planet.flightGlobalsIndex == 5) max = 10000;
            if (planet.flightGlobalsIndex == 6) max = 5000;
            if (planet.flightGlobalsIndex == 8) max = 100000;
            return max;
        }

        private double solarDay(CelestialBody planet)
        {
            double solarDay = planet.rotationPeriod;
            if (planet.flightGlobalsIndex > 0) solarDay = planet.rotationPeriod / (1 - (planet.rotationPeriod / planet.orbit.period));
            return solarDay;
        }

        private double planetScale(CelestialBody planet)
        {
            double pScale = 100;
            if (planet.flightGlobalsIndex == 1) pScale = 1;
            if (planet.flightGlobalsIndex == 5) pScale = 4;
            if (planet.flightGlobalsIndex == 6) pScale = 0.1;
            if (planet.flightGlobalsIndex == 8) pScale = 10;
            return pScale;
        }
        
    }
}
