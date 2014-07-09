#region license
/* DMagic Orbital Science - Asteroid Science
 * Class to setup asteroid science data
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
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DMagic
{
	internal class DMAsteroidScience
	{
		private static Vessel asteroidVessel;
		internal string aClass = null;
		internal string aType = null;
		internal float sciMult = 1f;
		internal CelestialBody body = null;
		internal int ID = 0;

		internal DMAsteroidScience()
		{
			body = FlightGlobals.Bodies[16];
			body.bodyName = "Asteroid";
			asteroidValues();
		}

		//Alter some of the values to give us asteroid specific results based on asteroid class and current situation
		private void asteroidValues()
		{
			if (asteroidNear()) {
				ModuleAsteroid asteroidM = asteroidVessel.FindPartModulesImplementing<ModuleAsteroid>().First();
				aClass = asteroidClass(asteroidM.prefabBaseURL);
				ID = asteroidM.seed;
				sciMult = asteroidValue(aClass);
			}
			else if (asteroidGrappled()) {
				ModuleAsteroid asteroidM = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleAsteroid>().First();
				aClass = asteroidClass(asteroidM.prefabBaseURL);
				ID = asteroidM.seed;
				sciMult = asteroidValue(aClass) * 1.5f;
			}
		}

		private string asteroidClass(string s)
		{
			switch (s[s.Length - 1]) {
				case 'A':
					return "Class A";
				case 'B':
					return "Class B";
				case 'C':
					return "Class C";
				case 'D':
					return "Class D";
				case 'E':
					return "Class E";
				default:
					return "Class Unholy";
			}
		}

		private static float asteroidValue(string aclass)
		{
			switch (aclass) {
				case "Class A":
					return 1f;
				case "Class B":
					return 2f;
				case "Class C":
					return 4f;
				case "Class D":
					return 6f;
				case "Class E":
					return 8f;
				case "Class Unholy":
					return 15f;
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
			foreach (Vessel v in vesselList) {
				if (v != FlightGlobals.ActiveVessel) {
					if (v.FindPartModulesImplementing<ModuleAsteroid>().Count > 0) {
						Part asteroidPart = v.FindPartModulesImplementing<ModuleAsteroid>().First().part;
						Vector3 asteroidPosition = asteroidPart.transform.position;
						Vector3 vesselPosition = FlightGlobals.ActiveVessel.transform.position;
						double distance = (asteroidPosition - vesselPosition).magnitude;
						if (distance < 2000) {
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
