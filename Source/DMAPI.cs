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

		/// <summary>
		/// Use to determine whether an experiment can be conducted at this time. This returns the same value as the internal check used when an experiment is deplyed from the right-click menu.
		/// </summary>
		/// <param name="isc">The science experiment module must be cast as an IScienceDataContianer.</param>
		/// <returns>Returns true if the experiment can be performed; will return false if the science module is not of the right type.</returns>
		public static bool experimentCanConduct(IScienceDataContainer isc)
		{
			if (isc == null)
				return false;

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

		/// <summary>
		/// Uses the internal method for conducting an experiment; the experiment cannot be forced and must first pass the "canConduct". All associated animations and other functions will be called. Optinally run the experiment without opening the results window.
		/// </summary>
		/// <param name="isc">The science experiment module must be cast as an IScienceDataContianer.</param>
		/// <param name="silent">Set to true to prevent the science results dialog from opening.</param>
		/// <returns>Returns true if the science module is of the right type and the gather science method is called.</returns>
		public static bool deployDMExperiment(IScienceDataContainer isc, bool silent = false)
		{
			if (isc == null)
				return false;

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

		/// <summary>
		/// Calls the internal method for getting the Experiment Situation for a certain experiment.
		/// </summary>
		/// <param name="mse">The science experiment module must be cast as a ModuleScienceExperiment.</param>
		/// <returns>Returns the Experiment Situation value for that experiment; returns InSpaceHigh if the experiment is not of the right type.</returns>
		public static ExperimentSituations getExperimentSituation(ModuleScienceExperiment mse)
		{
			if (mse == null)
				return ExperimentSituations.InSpaceHigh;

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

		/// <summary>
		/// Calls the internal method for getting the biome for a certain experiment.
		/// </summary>
		/// <param name="mse">The science experiment module must be cast as a ModuleScienceExperiment.</param>
		/// <param name="sit">The current Experiment Situation value; see getExperimentSituation.</param>
		/// <returns>Returns the biome string for that experiment; returns an empty string if the experiment is not of the right type.</returns>
		public static string getBiome(ModuleScienceExperiment mse, ExperimentSituations sit)
		{
			if (mse == null)
				return "";

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

		/// <summary>
		/// Check if an experiment can be conducted on asteroids.
		/// </summary>
		/// <param name="mse">The science experiment module must be cast as a ModuleScienceExperiment.</param>
		/// <returns>Returns false if the module is not of the right type or if the experiment cannot be conducted with asteroids.</returns>
		public static bool isAsteroidExperiment(ModuleScienceExperiment mse)
		{
			if (mse == null)
				return false;

			Type t = mse.GetType();

			if (!t.IsSubclassOf(typeof(DMModuleScienceAnimate)))
				return false;

			DMModuleScienceAnimate DMMod = (DMModuleScienceAnimate)mse;
			return DMMod.asteroidReports;
		}

		/// <summary>
		/// Check if an experiment can be conducted on asteroids.
		/// </summary>
		/// <param name="dms">The science experiment module must be cast as a DMModuleScienceAnimate.</param>
		/// <returns>Returns false if the module is not of the right type or if the experiment cannot be conducted with asteroids.</returns>
		public static bool isAsteroidExperiment(DMModuleScienceAnimate dms)
		{
			if (dms == null)
				return false;

			return dms.asteroidReports;
		}

		/// <summary>
		/// Check to see if an asteroid is within loading distance.
		/// </summary>
		/// <returns>Returns true if an asteroid is detected within loading distance (2.5km)</returns>
		public static bool isAsteroidNear()
		{
			return DMAsteroidScience.AsteroidNear;
		}

		/// <summary>
		/// Check to see if an asteroid is within loading distance and an experiment is valid for asteroid science.
		/// </summary>
		/// <param name="mse">The science experiment module must be cast as a ModuleScienceExperiment.</param>
		/// <returns>Returns false if the module is not of the right type, if the experiment cannot be conducted with asteroids, or if no asteroids are within loading distance (2.5km)</returns>
		public static bool isAsteroidNear(ModuleScienceExperiment mse)
		{
			if (!isAsteroidExperiment(mse))
				return false;

			return isAsteroidNear();
		}

		/// <summary>
		/// Check to see if an asteroid is grappled to the current vessel.
		/// </summary>
		/// <returns>Returns true if an asteroid is attached to the current vessel.</returns>
		public static bool isAsteroidGrappled()
		{
			return DMAsteroidScience.AsteroidGrappled;
		}

		/// <summary>
		/// Check to see if an asteroid is grappled to the current vessel and an experiment is valid for asteroid science
		/// </summary>
		/// <param name="mse">The science experiment module must be cast as a ModuleScienceExperiment.</param>
		/// <returns>Returns false if the module is not of the right type, if the experiment cannot be conducted with asteroids, or if no asteroid is attached to the current vessel.</returns>
		public static bool isAsteroidGrappled(ModuleScienceExperiment mse)
		{
			if (!isAsteroidExperiment(mse))
				return false;

			return isAsteroidGrappled();
		}

		/// <summary>
		/// Get the ScienceSubject for an asteroid experiment.
		/// </summary>
		/// <param name="mse">The science experiment module must be cast as a ModuleScienceExperiment.</param>
		/// <returns>Returns the ScienceSubject for that specific asteroid and experiment; returns null if the module is not of the right type, the experiment is not suitable for astroids, if no asteroids are detected, or if the current asteroid situation is not suitable for the experiment.</returns>
		public static ScienceSubject getAsteroidSubject(ModuleScienceExperiment mse)
		{
			if (mse == null)
				return null;

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

		/// <summary>
		/// Get the ScienceSubject for an asteroid experiment.
		/// </summary>
		/// <param name="mse">The science experiment module must be cast as a ModuleScienceExperiment.</param>
		/// <param name="sit">The current Experiment Situation value; see the getExperimentSituation method above.</param>
		/// <returns>Returns the ScienceSubject for that specific asteroid, experiment, and ExperimentSituation; returns null if the module is not of the right type, the experiment is not suitable for astroids, if no asteroids are detected, or if the current asteroid situation is not suitable for the experiment.</returns>
		public static ScienceSubject getAsteroidSubject(ModuleScienceExperiment mse, ExperimentSituations sit)
		{
			if (mse == null)
				return null;

			Type t = mse.GetType();

			if (!t.IsSubclassOf(typeof(DMModuleScienceAnimate)))
				return null;

			DMModuleScienceAnimate DMMod = (DMModuleScienceAnimate)mse;

			if (!isAsteroidExperiment(DMMod))
				return null;

			if (DMMod.scienceExp == null)
				return null;

			if (sit == ExperimentSituations.InSpaceLow)
			{
				if (!isAsteroidNear())
					return null;
			}
			else if (sit == ExperimentSituations.SrfLanded)
			{
				if (!isAsteroidGrappled())
					return null;
			}
			else
				return null;

			if ((DMMod.scienceExp.situationMask & (int)sit) == 0)
				return null;

			DMAsteroidScience newAsteroid = new DMAsteroidScience();
			ScienceSubject sub = new ScienceSubject(DMMod.scienceExp, sit, newAsteroid.Body, newAsteroid.AType + newAsteroid.ASeed.ToString());
			sub.subjectValue = newAsteroid.SciMult;
			return sub;
		}

		/// <summary>
		/// Get the science amount recovered from a certain astroid type and experiment.
		/// </summary>
		/// <param name="title">The Science Subject title (not the ID) for the asteroid type and experiment.</param>
		/// <returns>Returns the amount of science already recovered; does not included science that has not been transmitted or recovered; returns 0 if no results for this subjuect are found.</returns>
		public static float getDMScienceRecoveredValue(string title)
		{
			if (DMScienceScenario.SciScenario == null)
				return 0;

			DMScienceData DMS = DMScienceScenario.SciScenario.getDMScience(title);

			if (DMS == null)
				return 0;

			return DMS.Science;
		}

		/// <summary>
		/// Get the science amount recovered from a certain astroid type and experiment.
		/// </summary>
		/// <param name="sub">The Science Subject for the asteroid type and experiment.</param>
		/// <returns>Returns the amount of science already recovered; does not included science that has not been transmitted or recovered; returns 0 if no results for this subjuect are found.</returns>
		public static float getDMScienceRecoveredValue(ScienceSubject sub)
		{
			if (sub == null)
				return 0;

			return getDMScienceRecoveredValue(sub.title);
		}

		/// <summary>
		/// Get the science amount recovered from a certain astroid type and experiment. This method must first determine the science subject and will only work if the experiment is properly suited for asteroid science and there is an asteroid nearby or on the vessel.
		/// </summary>
		/// <param name="mse">The science experiment module must be cast as a ModuleScienceExperiment.</param>
		/// <returns>Returns the amount of science already recovered; does not included science that has not been transmitted or recovered; returns 0 if no results for this subjuect are found or if the subject is not valid.</returns>
		public static float getDMScienceRecoveredValue(ModuleScienceExperiment mse)
		{
			return getDMScienceRecoveredValue(getAsteroidSubject(mse));
		}

		/// <summary>
		/// Get the science amount recovered from a certain astroid type and experiment. This method must first determine the science subject and will only work if the experiment is properly suited for asteroid science and there is an asteroid nearby or on the vessel.
		/// </summary>
		/// <param name="mse">The science experiment module must be cast as a ModuleScienceExperiment.</param>
		/// <param name="sit">The current Experiment Situation value; see the getExperimentSituation method above.</param>
		/// <returns>Returns the amount of science already recovered; does not included science that has not been transmitted or recovered; returns 0 if no results for this subjuect are found or if the subject is not valid.</returns>
		public static float getDMScienceRecoveredValue(ModuleScienceExperiment mse, ExperimentSituations sit)
		{
			return getDMScienceRecoveredValue(getAsteroidSubject(mse, sit));
		}

		/// <summary>
		/// Get the value of the next science result for a given experiment and science subject.
		/// </summary>
		/// <param name="mse">The science experiment module must be cast as a ModuleScienceExperiment.</param>
		/// <param name="sub">The Science Subject for the asteroid type and experiment.</param>
		/// <param name="xmit">The transmission percentage from 0-1; use 1 for recovered results.</param>
		/// <returns>Returns a float representing the science value for the next set of results; returns 0 if the experiment is not of the right type, or is not an asteroid experiment.</returns>
		public static float getNextDMScienceValue(ModuleScienceExperiment mse, ScienceSubject sub, float xmit)
		{
			if (sub == null)
				return 0;

			if (mse == null)
				return 0;

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
				return ResearchAndDevelopment.GetNextScienceValue(DMMod.scienceExp.baseValue * DMMod.scienceExp.dataScale, sub, xmit);
			}
			else
			{
				float sci = ResearchAndDevelopment.GetNextScienceValue(DMMod.scienceExp.baseValue * DMMod.scienceExp.dataScale, sub, xmit);

				float oldSciVal = 0f;
				if (sub.scienceCap != 0)
					oldSciVal = Math.Max(0f, 1f - ((sub.science - sci) / sub.scienceCap));
				return sub.subjectValue * DMS.BaseValue * DMS.SciVal * oldSciVal * xmit;
			}
		}

		/// <summary>
		/// Get the value of the next science result for a given experiment and science subject.
		/// </summary>
		/// <param name="exp">The science experiment; results will not be valid if the experiment cannot be used for asteroids.</param>
		/// <param name="sub">The Science Subject for the asteroid type and experiment.</param>
		/// <param name="xmit">The transmission percentage from 0-1; use 1 for recovered results.</param>
		/// <returns>Returns a float representing the science value for the next set of results.</returns>
		public static float getNextDMScienceValue(ScienceExperiment exp, ScienceSubject sub, float xmit)
		{
			if (sub == null)
				return 0;

			if (exp == null)
				return 0;

			if (DMScienceScenario.SciScenario == null)
				return 0;

			DMScienceData DMS = DMScienceScenario.SciScenario.getDMScience(sub.title);

			if (DMS == null)
			{
				return ResearchAndDevelopment.GetNextScienceValue(exp.baseValue * exp.dataScale, sub, xmit);
			}
			else
			{
				float sci = ResearchAndDevelopment.GetNextScienceValue(exp.baseValue * exp.dataScale, sub, xmit);

				float oldSciVal = 0f;
				if (sub.scienceCap != 0)
					oldSciVal = Math.Max(0f, 1f - ((sub.science - sci) / sub.scienceCap));
				return sub.subjectValue * DMS.BaseValue * DMS.SciVal * oldSciVal * xmit;
			}
		}

		/// <summary>
		/// Get the value of the next science result for a given experiment and science subject.
		/// </summary>
		/// <param name="mse">The science experiment module must be cast as a ModuleScienceExperiment.</param>
		/// <param name="sub">The Science Subject for the asteroid type and experiment.</param>
		/// <param name="data">The science data amount for the results; from ScienceData dataAmount.</param>
		/// <param name="xmit">The transmission percentage from 0-1; use 1 for recovered results.</param>
		/// <returns>Returns a float representing the science value for the next set of results; returns 0 if the experiment is not of the right type, or is not an asteroid experiment.</returns>
		public static float getNextDMScienceValue(ModuleScienceExperiment mse, ScienceSubject sub, float data, float xmit)
		{
			if (mse == null)
				return 0;

			Type t = mse.GetType();

			if (!t.IsSubclassOf(typeof(DMModuleScienceAnimate)))
				return 0;

			DMModuleScienceAnimate DMMod = (DMModuleScienceAnimate)mse;

			if (!isAsteroidExperiment(DMMod))
				return 0;

			return getNextDMScienceValue(sub, data, xmit);
		}

		/// <summary>
		/// Get the value of the next science result for a given science subject; may be invalid if the science subject does not come from an asteoid-specific experiment.
		/// </summary>
		/// <param name="sub">The Science Subject for the asteroid type and experiment.</param>
		/// <param name="data">The science data amount for the results; from ScienceData dataAmount.</param>
		/// <param name="xmit">The transmission percentage from 0-1; use 1 for recovered results.</param>
		/// <returns>Returns a float representing the science value for the next set of results; returns 0 if subject is null.</returns>
		public static float getNextDMScienceValue(ScienceSubject sub, float data, float xmit)
		{
			if (sub == null)
				return 0;

			if (DMScienceScenario.SciScenario == null)
				return 0;

			DMScienceData DMS = DMScienceScenario.SciScenario.getDMScience(sub.title);

			if (DMS == null)
			{
				return ResearchAndDevelopment.GetNextScienceValue(data, sub, xmit);
			}
			else
			{
				float sci = ResearchAndDevelopment.GetNextScienceValue(data, sub, xmit);

				float oldSciVal = 0f;
				if (sub.scienceCap != 0)
					oldSciVal = Math.Max(0f, 1f - ((sub.science - sci) / sub.scienceCap));
				return sub.subjectValue * DMS.BaseValue * DMS.SciVal * oldSciVal * xmit;
			}
		}

		/// <summary>
		/// Get the Sci Value for a given asteroid science subject; this is analogous to the Science Data scientificValue; a 0-1 float representing the fraction of science for a certain asteroid type still remaining.
		/// </summary>
		/// <param name="title">The Science Subject title (not the ID) for the asteroid type and experiment.</param>
		/// <returns>A 0-1 float representing the fraction of science remaining for an asteroid type; returns 1 if the subject is not found.</returns>
		public static float getDMScienceSciValue(string title)
		{
			if (DMScienceScenario.SciScenario == null)
				return 0;

			DMScienceData DMS = DMScienceScenario.SciScenario.getDMScience(title);

			if (DMS == null)
				return 1;

			return DMS.SciVal;
		}

		/// <summary>
		/// Get the Sci Value for a given asteroid science subject; this is analogous to the Science Data scientificValue; a 0-1 float representing the fraction of science for a certain asteroid type still remaining.
		/// </summary>
		/// <param name="sub">The Science Subject for the asteroid type and experiment.</param>
		/// <returns>A 0-1 float representing the fraction of science remaining for an asteroid type; returns 1 if the subject is not found, returns 0 if the subject is null.</returns>
		public static float getDMScienceSciValue(ScienceSubject sub)
		{
			if (sub == null)
				return 0;

			return getDMScienceSciValue(sub.title);
		}

		/// <summary>
		/// Get the Sci Value for a given asteroid science subject; this is analogous to the Science Data scientificValue; a 0-1 float representing the fraction of science for a certain asteroid type still remaining. This method must first determine the science subject and will only work if the experiment is properly suited for asteroid science and there is an asteroid nearby or on the vessel.
		/// </summary>
		/// <param name="mse">The science experiment module must be cast as a ModuleScienceExperiment.</param>
		/// <returns>A 0-1 float representing the fraction of science remaining for an asteroid type; returns 1 if the subject is not found, returns 0 if the subject is null.</returns>
		public static float getDMScienceSciValue(ModuleScienceExperiment mse)
		{
			return getDMScienceSciValue(getAsteroidSubject(mse));
		}

		/// <summary>
		/// Get the Sci Value for a given asteroid science subject; this is analogous to the Science Data scientificValue; a 0-1 float representing the fraction of science for a certain asteroid type still remaining. This method must first determine the science subject and will only work if the experiment is properly suited for asteroid science and there is an asteroid nearby or on the vessel.
		/// </summary>
		/// <param name="mse">The science experiment module must be cast as a ModuleScienceExperiment.</param>
		/// <param name="sit">The current Experiment Situation value; see the getExperimentSituation method above.</param>
		/// <returns>A 0-1 float representing the fraction of science remaining for an asteroid type; returns 1 if the subject is not found, returns 0 if the subject is null.</returns>
		public static float getDMScienceSciValue(ModuleScienceExperiment mse, ExperimentSituations sit)
		{
			return getDMScienceSciValue(getAsteroidSubject(mse, sit));
		}

		/// <summary>
		/// Get the science cap for a certain asteroid type and experiment. This represents the total available science, and is analogous to the science subject scienceCap value.
		/// </summary>
		/// <param name="title">The Science Subject title (not the ID) for the asteroid type and experiment.</param>
		/// <returns>A float representing the total science available for an asteroid type and experiment. Returns 0 if no valid subject is found.</returns>
		public static float getDMScienceCap(string title)
		{
			if (DMScienceScenario.SciScenario == null)
				return 0;

			DMScienceData DMS = DMScienceScenario.SciScenario.getDMScience(title);

			if (DMS == null)
				return 0;

			return DMS.Cap;
		}

		/// <summary>
		/// Get the science cap for a certain asteroid type and experiment. This represents the total available science, and is analogous to the science subject scienceCap value.
		/// </summary>
		/// <param name="sub">The Science Subject for the asteroid type and experiment.</param>
		/// <returns>A float representing the total science available for an asteroid type and experiment. Returns 0 if no valid subject is found.</returns>
		public static float getDMScienceCap(ScienceSubject sub)
		{
			if (sub == null)
				return 0;

			return getDMScienceCap(sub.title);
		}

		/// <summary>
		/// Get the science cap for a certain asteroid type and experiment. This represents the total available science, and is analogous to the science subject scienceCap value. This method must first determine the science subject and will only work if the experiment is properly suited for asteroid science and there is an asteroid nearby or on the vessel.
		/// </summary>
		/// <param name="mse">The science experiment module must be cast as a ModuleScienceExperiment.</param>
		/// <returns>A float representing the total science available for an asteroid type and experiment. Returns 0 if no valid subject is found.</returns>
		public static float getDMScienceCap(ModuleScienceExperiment mse)
		{
			return getDMScienceCap(getAsteroidSubject(mse));
		}

		/// <summary>
		/// Get the science cap for a certain asteroid type and experiment. This represents the total available science, and is analogous to the science subject scienceCap value. This method must first determine the science subject and will only work if the experiment is properly suited for asteroid science and there is an asteroid nearby or on the vessel.
		/// </summary>
		/// <param name="mse">The science experiment module must be cast as a ModuleScienceExperiment.</param>
		/// <param name="sit">The current Experiment Situation value; see the getExperimentSituation method above.</param>
		/// <returns>A float representing the total science available for an asteroid type and experiment. Returns 0 if no valid subject is found.</returns>
		public static float getDMScienceCap(ModuleScienceExperiment mse, ExperimentSituations sit)
		{
			return getDMScienceCap(getAsteroidSubject(mse, sit));
		}



	}
}
