/* DMagic Orbital Science - Asteroid Science
 * Class to setup asteroid science data
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
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE 
 * GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF 
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT 
 * OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *  
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DMagic
{
    class AsteroidScience
    {
        private static Vessel asteroidVessel;

        //Let's make us some asteroid science
        //First construct a new celestial body from an asteroid

        internal static CelestialBody Asteroid()
        {
            //Inherit values for the CelestialBody from an existing body, Eeloo in this case to minimize the risk of screwing something up
            CelestialBody AsteroidBody = FlightGlobals.fetch.bodies[16];
            AsteroidBody.bodyName = "Asteroid";
            asteroidValues(AsteroidBody);
            return AsteroidBody;
        }

        //Alter some of the values to give us asteroid specific results based on asteroid class and current situation
        private static void asteroidValues(CelestialBody body)
        {
            if (asteroidNear())
            {
                Part asteroidPart = asteroidVessel.FindPartModulesImplementing<ModuleAsteroid>().First().part;
                body.bodyDescription = asteroidClass(asteroidPart.mass);
                body.scienceValues.InSpaceLowDataValue = asteroidValue(body.bodyDescription);
            }
            else if (asteroidGrappled())
            {
                Part asteroidPart = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleAsteroid>().First().part;
                body.bodyDescription = asteroidClass(asteroidPart.mass);
                body.scienceValues.LandedDataValue = asteroidValue(body.bodyDescription) * 1.5f;
            }
        }
        
        //Need to figure out accurate mass ranges
        private static string asteroidClass(float mass)
        {
            if (mass < 10f) return "Class A";
            if (mass >= 10f && mass < 50f) return "Class B";
            if (mass >= 50f && mass < 200f) return "Class C";
            if (mass >= 200f && mass < 750f) return "Class D";
            if (mass >= 750f && mass < 3000f) return "Class E";
            return "Class Unholy";
        }

        private static float asteroidValue(string aclass)
        {
            switch (aclass)
            {
                case "Class A":
                    return 1.5f;
                case "Class B":
                    return 3f;
                case "Class C":
                    return 5f;
                case "Class D":
                    return 8f;
                case "Class E":
                    return 10f;
                case "Class Unholy":
                    return 30f;
                default:
                    return 1f;
            }
        }

        //Are we attached to the asteroid, check if an asteroid part is on our vessel
        internal static bool asteroidGrappled()
        {
            if (FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleAsteroid>().Count > 0) return true;
            else return false;
        }

        //Are we near the asteroid, cycle through existing vessels, only target asteroids within 2km
        internal static bool asteroidNear()
        {
            List<Vessel> vesselList = FlightGlobals.fetch.vessels;
            foreach (Vessel v in vesselList)
            {
                if (v != FlightGlobals.ActiveVessel)
                {
                    if (v.FindPartModulesImplementing<ModuleAsteroid>().Count > 0)
                    {
                        Part asteroidPart = v.FindPartModulesImplementing<ModuleAsteroid>().First().part;
                        Vector3 asteroidPosition = asteroidPart.transform.position;
                        Vector3 vesselPosition = FlightGlobals.ActiveVessel.transform.position;
                        double distance = (asteroidPosition - vesselPosition).magnitude;
                        if (distance < 2000)
                        {
                            asteroidVessel = v;
                            return true;
                        }
                        else continue;
                    }
                }
            }
            return false;
        }


    }
}
