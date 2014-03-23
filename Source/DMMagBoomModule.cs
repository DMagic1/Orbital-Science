/* DMagic Orbital Science - Magnetometer
 * Magnetosphere simulation.
 *
 * Copyright (C) 2014  David Grandy

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *  
 *  
 */


using UnityEngine;
using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace DMagic
{
    //Inherit default science experiment module.
    public class DMMagBoomModule : PartModule
    {        
        [KSPField(guiActive = false, guiName = "Bt")]
        public string Bt;
        [KSPField(guiActive = false, guiName = "Inclination")]
        public string inc;
        [KSPField(guiActive = false, guiName = "Declination")]
        public string dec;
        //[KSPField(guiActive = false, guiName = "Radius")]
        //public string radius;
        //[KSPField(guiActive = false, guiName = "Alt")]
        //public string altScaled;
        //[KSPField(guiActive = false, guiName = "Lon")]
        //public string lons;

        [KSPField]
        public bool runMagnetometer;
        [KSPField]
        public string resourceToUse = "ElectricCharge";
        [KSPField]
        public float resourceCost = 0;

        private DMModuleScienceAnimate primaryModule;

        //Kerbin solar day length
        private static double sDay = 21650.81276574;

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            this.part.force_activate();
            if (part.FindModulesImplementing<DMModuleScienceAnimate>().Count > 1)
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
        
        public override void OnUpdate()
        {
            base.OnUpdate();

            //Only functional on Kerbin for now
            if (vessel.mainBody.name == "Kerbin" && runMagnetometer)
            {
                Fields["Bt"].guiActive = primaryModule.IsDeployed;
                Fields["inc"].guiActive = primaryModule.IsDeployed;
                Fields["dec"].guiActive = primaryModule.IsDeployed;
                //Fields["radius"].guiActive = primaryModule.IsDeployed;
                //Fields["altScaled"].guiActive = primaryModule.IsDeployed;
                //Fields["lons"].guiActive = primaryModule.IsDeployed;

                if (primaryModule.IsDeployed)
                {
                    part.RequestResource(resourceToUse, resourceCost * Time.deltaTime);

                    double latDeg = (vessel.latitude + 90 + 180) % 180 - 90;
                    double lonDeg = (vessel.longitude + 180 + 360) % 360 - 180;
                    double lat = latDeg * Mathf.Deg2Rad;
                    double lon = lonDeg * Mathf.Deg2Rad;
                    double alt = vessel.altitude / 1000;

                    //Get universal time in seconds and calculate our time during the current, normalized solar day
                    double uTime = Planetarium.GetUniversalTime();
                    double uDay = uTime / sDay;
                    double nDay = uDay % 1;

                    //Change the simulation day to add some variability, start with Jan 1 2010 in Julian Date format
                    int localDay = Convert.ToInt32(uDay);
                    long date = 2455197 + (localDay % 500);

                    //Paramaters for mag field model
                    int i = 10;
                    double[] field = new double[6];

                    //Shift our current longitide to account for solar day - lonShift should equal zero when crossing solar noon, bring everything down to -Pi to Pi just to be safe
                    //For reference, at time zero the sun is directly above -90.158 Deg West on Kerbin, I'm rounding that to -90, or -Pi/2
                    double lonShift = ((lon + (Math.PI / 2) * ((1 + 4 * nDay))) + Math.PI + Math.PI * 2) % (2 * Math.PI) - Math.PI;

                    //Simulate magnetosphere distortion by solar wind with stretched torus shape, determine our position on the surface of the torus
                    double radiusx = ((3.5 + (1 + 1 / Math.Cos(lonShift)) * Math.Cos(Math.PI + lonShift)) + (3.5 + (1.3 + 1 / Math.Cos(lonShift)) * Math.Cos(Math.PI + lonShift)) * Math.Cos(lat * 2)) * Math.Cos(lonShift);
                    double radiusy = (0.75 + 0.9 * Math.Cos(lat * 2)) * Math.Sin(lonShift);
                    double radiusz = (1 + 0.2 * Math.Cos(lonShift)) * Math.Sin(lat * 2);
                    double Radius = Math.Sqrt((radiusx * radiusx) + (radiusy * radiusy) + (radiusz * radiusz));
                    if (Radius == 0) Radius += 0.001;

                    //Scale our altitude by our position on the simulated torus, ignore at altitudes below 250km, ramp up quickly above high scaled altitude up to a max value
                    if (alt > 250)
                    {
                        alt *= 1 / Radius;
                        if (alt < 250) alt = 250;
                    }                    
                    if (alt > 500) alt *= Math.Pow((alt / 500), 3);
                    if (alt > 5000) alt = 5000;

                    //Send all of our modified parameters to the field model
                    double[] magComp = getMag(lat, lon, alt, date, i, field);

                    //Magnetic field components
                    double Bx = magComp[3];
                    double By = magComp[4];
                    double Bz = magComp[5];

                    //Calculate various magenetic field components based on 3-axis field strength 
                    double Bh = Math.Sqrt((Bx * Bx) + (By * By));
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

                    //Display in right-click menu
                    Bt = Btf.ToString("F2") + " nT";
                    inc = incf.ToString("F2") + " Deg";
                    dec = decf.ToString("F2") + " Deg";

                    //Extra variables - used in development
                    //float altf = (float)alt;
                    //float nDayf = (float)nDay;
                    //Vector3 sunP = FlightGlobals.fetch.bodies[0].position;
                    //Vector3 sunD = transform.InverseTransformPoint(sunP) - part.transform.localPosition;
                    //lons = nDayf.ToString("F5");
                    //nDays = nDayf.ToString("F4");
                    //radius = Radius.ToString();
                    //float latf = (float)latDeg;
                    //float lonf = (float)lonDeg;
                    //lats = latf.ToString("F3") + " Deg";
                    //altScaled = alt.ToString();
                    //lons = lonf.ToString("F3") + " Deg";
                    //lons = Btf.ToString("F2") + " nT";
                    //lons = "Shifted long: " + (((lonShift * Mathf.Rad2Deg) + 180 + 360) % 360 - 180).ToString();
                }
            }
        }
        
    }
}
