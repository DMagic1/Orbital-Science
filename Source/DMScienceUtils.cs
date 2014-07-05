/* DMagic Orbital Science - Science Utilities
 * Common Science Experiment Methods
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

using System;
using UnityEngine;

namespace DMagic
{
	internal class DMScienceUtils
	{
		private const string bodyNameFixed = "Eeloo";
		private static DMAsteroidScience newAsteroid;

		private static float fixSubjectValue(ExperimentSituations s, CelestialBody b, float f, float boost)
		{
			float subV = f;
			if (s == ExperimentSituations.SrfLanded) subV = b.scienceValues.LandedDataValue;
			else if (s == ExperimentSituations.SrfSplashed) subV = b.scienceValues.SplashedDataValue;
			else if (s == ExperimentSituations.FlyingLow) subV = b.scienceValues.FlyingLowDataValue;
			else if (s == ExperimentSituations.FlyingHigh) subV = b.scienceValues.FlyingHighDataValue;
			else if (s == ExperimentSituations.InSpaceLow) subV = b.scienceValues.InSpaceLowDataValue;
			else if (s == ExperimentSituations.InSpaceHigh) subV = b.scienceValues.InSpaceHighDataValue;
			return subV * boost;
		}

		private static string getBiome(ExperimentSituations s, uint biomeMask, Vessel v)
		{
			if ((biomeMask & (uint)s) == 0)
				return "";
			else {
				switch (v.landedAt) {
					case "LaunchPad":
						return v.landedAt;
					case "Runway":
						return v.landedAt;
					case "KSC":
						return v.landedAt;
					default:
						return FlightGlobals.currentMainBody.BiomeMap.GetAtt(v.latitude * Mathf.Deg2Rad, v.longitude * Mathf.Deg2Rad).name;
				}
			}
		}

		private static ExperimentSituations getSituation(bool asteroid, Vessel v)
		{
			if (asteroid && DMAsteroidScience.asteroidGrappled()) return ExperimentSituations.SrfLanded;
			if (asteroid && DMAsteroidScience.asteroidNear()) return ExperimentSituations.InSpaceLow;
			switch (v.situation) {
				case Vessel.Situations.LANDED:
				case Vessel.Situations.PRELAUNCH:
					return ExperimentSituations.SrfLanded;
				case Vessel.Situations.SPLASHED:
					return ExperimentSituations.SrfSplashed;
				default:
					if (v.altitude < (v.mainBody.atmosphereScaleHeight * 1000 * Math.Log(1e6)) && v.mainBody.atmosphere) {
						if (v.altitude < v.mainBody.scienceValues.flyingAltitudeThreshold)
							return ExperimentSituations.FlyingLow;
						else
							return ExperimentSituations.FlyingHigh;
					}
					if (v.altitude < v.mainBody.scienceValues.spaceAltitudeThreshold)
						return ExperimentSituations.InSpaceLow;
					else
						return ExperimentSituations.InSpaceHigh;
			}
		}

		private static string situationCleanup(ExperimentSituations expSit, string b, bool asteroid, Vessel v)
		{
			if (asteroid && DMAsteroidScience.asteroidGrappled()) return " from the surface of a " + b + " asteroid";
			if (asteroid && DMAsteroidScience.asteroidNear()) return " while in space near a " + b + " asteroid";
			if (v.landedAt != "") return " from " + b;
			if (b == "") {
				switch (expSit) {
					case ExperimentSituations.SrfLanded:
						return " from  " + v.mainBody.theName + "'s surface";
					case ExperimentSituations.SrfSplashed:
						return " from " + v.mainBody.theName + "'s oceans";
					case ExperimentSituations.FlyingLow:
						return " while flying at " + v.mainBody.theName;
					case ExperimentSituations.FlyingHigh:
						return " from " + v.mainBody.theName + "'s upper atmosphere";
					case ExperimentSituations.InSpaceLow:
						return " while in space near " + v.mainBody.theName;
					default:
						return " while in space high over " + v.mainBody.theName;
				}
			}
			else {
				switch (expSit) {
					case ExperimentSituations.SrfLanded:
						return " from " + v.mainBody.theName + "'s " + b;
					case ExperimentSituations.SrfSplashed:
						return " from " + v.mainBody.theName + "'s " + b;
					case ExperimentSituations.FlyingLow:
						return " while flying over " + v.mainBody.theName + "'s " + b;
					case ExperimentSituations.FlyingHigh:
						return " from the upper atmosphere over " + v.mainBody.theName + "'s " + b;
					case ExperimentSituations.InSpaceLow:
						return " from space just above " + v.mainBody.theName + "'s " + b;
					default:
						return " while in space high over " + v.mainBody.theName + "'s " + b;
				}
			}
		}

		internal static bool canConduct(int experimentCount, int experimentLimit, uint situationMask, bool asteroidReports, Vessel v)
		{
			if (experimentCount < experimentLimit)
				if ((situationMask & (uint)getSituation(asteroidReports, v)) == 0)
					return false;
				else
					return true;
			else
				return false;
		}

		internal static ScienceData makeScience(bool asteroid, bool asteroidType, Vessel v, uint biomeMask, string experimentID, float xmitDataScalar, float boost)
		{
			ExperimentSituations vesselSituation = getSituation(asteroid, v);
			string biome = getBiome(vesselSituation, biomeMask, v);
			CelestialBody mainBody = v.mainBody;
			bool asteroids = false;

			//Check for asteroids and alter the biome and celestialbody values as necessary
			if (asteroid && DMAsteroidScience.asteroidGrappled() || asteroid && DMAsteroidScience.asteroidNear()) {
				newAsteroid = new DMAsteroidScience();
				asteroids = true;
				mainBody = newAsteroid.body;
				biome = "";
				if (asteroidType) biome = newAsteroid.aClass;
			}

			ScienceData data = null;
			ScienceExperiment exp = ResearchAndDevelopment.GetExperiment(experimentID);
			ScienceSubject sub = ResearchAndDevelopment.GetExperimentSubject(exp, vesselSituation, mainBody, biome);
			sub.title = exp.experimentTitle + situationCleanup(vesselSituation, biome, asteroid, v);

			if (asteroids) {
				sub.subjectValue = newAsteroid.sciMult;
				sub.scienceCap = exp.scienceCap * sub.subjectValue * 10;
				mainBody.bodyName = bodyNameFixed;
				asteroids = false;
			}
			else {
				sub.subjectValue = fixSubjectValue(vesselSituation, mainBody, sub.subjectValue, boost);
				sub.scienceCap = exp.scienceCap * sub.subjectValue;
			}

			if (sub != null)
				data = new ScienceData(exp.baseValue * sub.dataScale, xmitDataScalar, 0.5f, sub.id, sub.title);
			return data;
		}
	}
}
