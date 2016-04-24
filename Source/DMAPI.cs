#region license
/* DMagic Orbital Science - DMAPI
 * Static utilities class for interacting with other mods
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
using DMagic.Part_Modules;
using DMagic.Scenario;

namespace DMagic
{
	public static class DMAPI
	{

		public static bool experimentCanConduct(IScienceDataContainer isc)
		{
			Type t = isc.GetType();

			if (t == typeof(DMAnomalyScanner))
			{
				DMAnomalyScanner DMMod = (DMAnomalyScanner)isc;
				return DMMod.canConduct();
			}
			else if (t == typeof(DMBioDrill))
			{
				DMBioDrill DMMod = (DMBioDrill)isc;
				return DMMod.canConduct();
			}
			else if (t == typeof(DMSIGINT))
			{
				DMSIGINT DMMod = (DMSIGINT)isc;
				return DMMod.canConduct();
			}
			else if (t == typeof(DMXRayDiffract))
			{
				DMXRayDiffract DMMod = (DMXRayDiffract)isc;
				return DMMod.canConduct();
			}
			else if (t.IsSubclassOf(typeof(DMModuleScienceAnimate)))
			{
				DMModuleScienceAnimate DMMod = (DMModuleScienceAnimate)isc;
				return DMMod.canConduct();
			}			
			else if (t == typeof(DMSeismicHammer))
			{
				DMSeismicHammer DMMod = (DMSeismicHammer)isc;
				return DMMod.canConduct();
			}
			else if (t == typeof(DMSeismicSensor))
			{
				DMSeismicSensor DMMod = (DMSeismicSensor)isc;
				return DMMod.canConduct();
			}
			else if (t == typeof(DMAsteroidScanner))
			{
				DMAsteroidScanner DMMod = (DMAsteroidScanner)isc;
				return DMMod.canConduct();
			}

			return false;
		}

		public static bool deployDMExperiment(IScienceDataContainer isc, bool silent = false)
		{
			Type t = isc.GetType();		

			if (t == typeof(DMAnomalyScanner))
			{
				DMAnomalyScanner DMMod = (DMAnomalyScanner)isc;
				DMMod.gatherScienceData(silent);
				return true;
			}
			else if (t == typeof(DMBioDrill))
			{
				DMBioDrill DMMod = (DMBioDrill)isc;
				DMMod.gatherScienceData(silent);
				return true;
			}
			else if (t.IsSubclassOf(typeof(DMModuleScienceAnimate)))
			{
				DMModuleScienceAnimate DMMod = (DMModuleScienceAnimate)isc;
				DMMod.gatherScienceData(silent);
				return true;
			}
			else if (t == typeof(DMSeismicHammer))
			{
				DMSeismicHammer DMMod = (DMSeismicHammer)isc;
				DMMod.gatherScienceData(silent);
				return true;
			}
			else if (t == typeof(DMSeismicSensor))
			{
				DMSeismicSensor DMMod = (DMSeismicSensor)isc;
				DMMod.gatherScienceData(silent);
				return true;
			}
			else if (t == typeof(DMAsteroidScanner))
			{
				DMAsteroidScanner DMMod = (DMAsteroidScanner)isc;
				DMMod.gatherScienceData(silent);
				return true;
			}

			return false;
		}

		public static ExperimentSituations getExperimentSituation(ModuleScienceExperiment mse)
		{
			Type t = mse.GetType();

			if (t == typeof(DMAnomalyScanner))
			{
				DMAnomalyScanner DMMod = (DMAnomalyScanner)mse;
				return DMMod.getSituation();
			}
			else if (t == typeof(DMBathymetry))
			{
				DMBathymetry DMMod = (DMBathymetry)mse;
				return DMMod.getSituation();
			}
			else if (t == typeof(DMReconScope))
			{
				DMReconScope DMMod = (DMReconScope)mse;
				return DMMod.getSituation();
			}
			else if (t == typeof(DMSIGINT))
			{
				DMSIGINT DMMod = (DMSIGINT)mse;
				return DMMod.getSituation();
			}
			else if (t.IsSubclassOf(typeof(DMModuleScienceAnimate)))
			{
				DMModuleScienceAnimate DMMod = (DMModuleScienceAnimate)mse;
				return DMMod.getSituation();
			}

			return ExperimentSituations.InSpaceHigh;
		}

		public static string getBiome(ModuleScienceExperiment mse, ExperimentSituations sit)
		{
			Type t = mse.GetType();

			if (t == typeof(DMAnomalyScanner))
			{
				DMAnomalyScanner DMMod = (DMAnomalyScanner)mse;
				return DMMod.getBiome(sit);
			}
			else if (t == typeof(DMBathymetry))
			{
				DMBathymetry DMMod = (DMBathymetry)mse;
				return DMMod.getBiome(sit);
			}
			else if (t == typeof(DMReconScope))
			{
				DMReconScope DMMod = (DMReconScope)mse;
				return DMMod.getBiome(sit);
			}
			else if (t == typeof(DMSIGINT))
			{
				DMSIGINT DMMod = (DMSIGINT)mse;
				return DMMod.getBiome(sit);
			}
			else if (t.IsSubclassOf(typeof(DMModuleScienceAnimate)))
			{
				DMModuleScienceAnimate DMMod = (DMModuleScienceAnimate)mse;
				return DMMod.getBiome(sit);
			}

			return "";
		}

		public static bool isAsteroidExperiment(ModuleScienceExperiment mse)
		{
			Type t = mse.GetType();

			if (!t.IsSubclassOf(typeof(DMModuleScienceAnimate)))
				return false;

			DMModuleScienceAnimate DMMod = (DMModuleScienceAnimate)mse;
			return DMMod.asteroidReports;
		}

		public static bool isAsteroidExperiment(DMModuleScienceAnimate dms)
		{
			return dms.asteroidReports;
		}

		public static bool isAsteroidNear(ModuleScienceExperiment mse)
		{
			if (!isAsteroidExperiment(mse))
				return false;

			return DMAsteroidScience.AsteroidNear;
		}

		public static bool isAsteroidGrappled(ModuleScienceExperiment mse)
		{
			if (!isAsteroidExperiment(mse))
				return false;

			return DMAsteroidScience.AsteroidGrappled;
		}

		public static ScienceSubject getAsteroidSubject(ModuleScienceExperiment mse)
		{
			Type t = mse.GetType();

			if (!t.IsSubclassOf(typeof(DMModuleScienceAnimate)))
				return null;

			DMModuleScienceAnimate DMMod = (DMModuleScienceAnimate)mse;

			if (!isAsteroidExperiment(DMMod))
				return null;

			if (DMMod.scienceExp == null)
				return null;

			if (DMAsteroidScience.AsteroidGrappled)
			{
				if ((DMMod.scienceExp.situationMask & (int)ExperimentSituations.SrfLanded) == 0)
					return null;

				DMAsteroidScience newAsteroid = new DMAsteroidScience();
			    ScienceSubject sub = new ScienceSubject(DMMod.scienceExp, ExperimentSituations.SrfLanded, newAsteroid.Body, newAsteroid.AType + newAsteroid.ASeed.ToString());
				sub.subjectValue = newAsteroid.SciMult;
				return sub;
			}
			else if (DMAsteroidScience.AsteroidNear)
			{
				if ((DMMod.scienceExp.situationMask & (int)ExperimentSituations.InSpaceLow) == 0)
					return null;

				DMAsteroidScience newAsteroid = new DMAsteroidScience();
				ScienceSubject sub = new ScienceSubject(DMMod.scienceExp, ExperimentSituations.InSpaceLow, newAsteroid.Body, newAsteroid.AType + newAsteroid.ASeed.ToString());
				sub.subjectValue = newAsteroid.SciMult;
				return sub;
			}

			return null;
		}

		public static ScienceSubject getAsteroidSubject(ModuleScienceExperiment mse, ExperimentSituations sit)
		{
			Type t = mse.GetType();

			if (!t.IsSubclassOf(typeof(DMModuleScienceAnimate)))
				return null;

			DMModuleScienceAnimate DMMod = (DMModuleScienceAnimate)mse;

			if (!isAsteroidExperiment(DMMod))
				return null;

			if (DMMod.scienceExp == null)
				return null;

			if ((DMMod.scienceExp.situationMask & (int)sit) == 0)
				return null;

			DMAsteroidScience newAsteroid = new DMAsteroidScience();
			ScienceSubject sub = new ScienceSubject(DMMod.scienceExp, sit, newAsteroid.Body, newAsteroid.AType + newAsteroid.ASeed.ToString());
			sub.subjectValue = newAsteroid.SciMult;
			return sub;
		}

		public static float getDMScienceRecoveredValue(string title)
		{
			if (DMScienceScenario.SciScenario == null)
				return 0;

			DMScienceData DMS = DMScienceScenario.SciScenario.getDMScience(title);

			if (DMS == null)
				return 0;

			return DMS.Science;
		}

		public static float getDMScienceRecoveredValue(ScienceSubject sub)
		{
			if (sub == null)
				return 0;

			return getDMScienceRecoveredValue(sub.title);
		}

		public static float getDMScienceRecoveredValue(ModuleScienceExperiment mse)
		{
			return getDMScienceRecoveredValue(getAsteroidSubject(mse));
		}

		public static float getDMScienceRecoveredValue(ModuleScienceExperiment mse, ExperimentSituations sit)
		{
			return getDMScienceRecoveredValue(getAsteroidSubject(mse, sit));
		}

		public static float getNextDMScienceValue(ModuleScienceExperiment mse, ScienceSubject sub)
		{
			Type t = mse.GetType();

			if (!t.IsSubclassOf(typeof(DMModuleScienceAnimate)))
				return 0;

			DMModuleScienceAnimate DMMod = (DMModuleScienceAnimate)mse;

			if (!isAsteroidExperiment(DMMod))
				return 0;

			if (DMMod.scienceExp == null)
				return 0;

			if (DMScienceScenario.SciScenario == null)
				return 0;

			DMScienceData DMS = DMScienceScenario.SciScenario.getDMScience(sub.title);

			if (DMS == null)
			{
				return sub.subjectValue * DMMod.scienceExp.baseValue;
			}
			else
			{
				return sub.subjectValue * DMMod.scienceExp.baseValue * DMS.SciVal * sub.scientificValue;
			}
		}

		public static float getNextDMScienceValue(ScienceExperiment exp, ScienceSubject sub)
		{
			if (DMScienceScenario.SciScenario == null)
				return 0;

			DMScienceData DMS = DMScienceScenario.SciScenario.getDMScience(sub.title);

			if (DMS == null)
			{
				return sub.subjectValue * exp.baseValue;
			}
			else
			{
				return sub.subjectValue * exp.baseValue * DMS.SciVal * sub.scientificValue;
			}
		}

		public static float getDMScienceSciValue(string title)
		{
			if (DMScienceScenario.SciScenario == null)
				return 0;

			DMScienceData DMS = DMScienceScenario.SciScenario.getDMScience(title);

			if (DMS == null)
				return 0;

			return DMS.SciVal;
		}

		public static float getDMScienceSciValue(ScienceSubject sub)
		{
			if (sub == null)
				return 0;

			return getDMScienceSciValue(sub.title);
		}

		public static float getDMScienceSciValue(ModuleScienceExperiment mse)
		{
			return getDMScienceSciValue(getAsteroidSubject(mse));
		}

		public static float getDMScienceSciValue(ModuleScienceExperiment mse, ExperimentSituations sit)
		{
			return getDMScienceSciValue(getAsteroidSubject(mse, sit));
		}

		public static float getDMScienceCap(string title)
		{
			if (DMScienceScenario.SciScenario == null)
				return 0;

			DMScienceData DMS = DMScienceScenario.SciScenario.getDMScience(title);

			if (DMS == null)
				return 0;

			return DMS.Cap;
		}

		public static float getDMScienceCap(ScienceSubject sub)
		{
			if (sub == null)
				return 0;

			return getDMScienceCap(sub.title);
		}

		public static float getDMScienceCap(ModuleScienceExperiment mse)
		{
			return getDMScienceCap(getAsteroidSubject(mse));
		}

		public static float getDMScienceCap(ModuleScienceExperiment mse, ExperimentSituations sit)
		{
			return getDMScienceCap(getAsteroidSubject(mse, sit));
		}



	}
}
