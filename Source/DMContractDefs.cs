#region license
/* DMagic Orbital Science - DMContractDefs
 * Class to store contract configuration values
 *
 * Copyright (c) 2016, David Grandy <david.grandy@gmail.com>
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

namespace DMagic
{
	public static class DMContractDefs
	{
		public static class DMAnomaly
		{
			public static class Expire
			{
				public static int MinimumExpireDays = 4;

				public static int MaximumExpireDays = 10;

				public static float DeadlineYears = 1.5f;
			}

			public static class Funds
			{
				public static float BaseAdvance = 20000;

				public static float BaseReward = 24000;

				public static float BaseFailure = 20000;

				public static float ParamReward = 8000;

				public static float ParamFailure = 0;
			}

			public static class Science
			{
				public static float BaseReward = 0;

				public static float ParamReward = 5;

				public static float SecondaryReward = 0.25f;
			}

			public static class Reputation
			{
				public static float BaseReward = 7;

				public static float BaseFailure = 6;

				public static float ParamReward = 0;

				public static float ParamFailure = 0;
			}

			public static int maxOffers = 2;
			public static int maxActive = 3;

			public static int TrivialReconLevelRequirement = 0;
			public static int SignificantReconLevelRequirement = 1;
			public static int ExceptionalReconLevelRequirement = 1;

			public static float TrivialAnomalyLevel = 0f;
			public static float SignificantAnomalyLevel = 0.3f;
			public static float ExceptionalAnomalyLevel = 0.6f;

			public static List<string> backStory = new List<string>(1) { "Something, Something, Something..." };
		}

		public static class DMAsteroid
		{
			public static class Expire
			{
				public static int MinimumExpireDays = 4;

				public static int MaximumExpireDays = 10;

				public static float DeadlineYears = 3.8f;
			}

			public static class Funds
			{
				public static float BaseAdvance = 8000;

				public static float BaseReward = 9500;

				public static float BaseFailure = 7000;

				public static float ParamReward = 5000;

				public static float ParamFailure = 0;
			}

			public static class Science
			{
				public static float BaseReward = 0;

				public static float ParamReward = 0.25f;
			}

			public static class Reputation
			{
				public static float BaseReward = 8;

				public static float BaseFailure = 6;

				public static float ParamReward = 0;

				public static float ParamFailure = 0;
			}

			public static int maxOffers = 2;
			public static int maxActive = 3;

			public static int trivialScienceRequests = 3;
			public static int significantScienceRequests = 4;
			public static int exceptionalScienceRequests = 6;

			public static List<string> backStory = new List<string>(1) { "Something, Something, Something..." };
		}

		public static class DMSurvey
		{
			public static class Expire
			{
				public static int MinimumExpireDays = 4;

				public static int MaximumExpireDays = 10;

				public static float DeadlineYears = 1.7f;
			}

			public static class Funds
			{
				public static float BaseAdvance = 8500;

				public static float BaseReward = 10500;

				public static float BaseFailure = 7500;

				public static float ParamReward = 3000;

				public static float ParamFailure = 0;
			}

			public static class Science
			{
				public static float BaseReward = 0;

				public static float ParamReward = 0.25f;
			}

			public static class Reputation
			{
				public static float BaseReward = 12;

				public static float BaseFailure = 10;

				public static float ParamReward = 0;

				public static float ParamFailure = 0;
			}

			public static int maxOffers = 2;
			public static int maxActive = 4;

			public static int trivialScienceRequests = 4;
			public static int significantScienceRequests = 6;
			public static int exceptionalScienceRequests = 8;

			public static List<string> backStory = new List<string>(1) { "Something, Something, Something..." };
		}

		public static class DMMagnetic
		{
			public static class Expire
			{
				public static int MinimumExpireDays = 4;

				public static int MaximumExpireDays = 10;

				public static float DeadlineModifier = 3.7f;
			}

			public static class Funds
			{
				public static float BaseAdvance = 21000;

				public static float BaseReward = 25000;

				public static float BaseFailure = 23000;

				public static float ParamReward = 4000;

				public static float ParamFailure = 0;
			}

			public static class Science
			{
				public static float BaseReward = 24;

				public static float ParamReward = 2;
			}

			public static class Reputation
			{
				public static float BaseReward = 8;

				public static float BaseFailure = 7;

				public static float ParamReward = 0;

				public static float ParamFailure = 0;
			}

			public static int maxOffers = 2;
			public static int maxActive = 4;

			public static double trivialTimeModifier = 100;
			public static double significantTimeModifier = 150;
			public static double exceptionalTimeModifier = 200;

			public static double trivialEccentricityMultiplier = 0.2;
			public static double significantEccentricityMultiplier = 0.35;
			public static double exceptionalEccentricityMultiplier = 0.5;

			public static double trivialInclinationMultiplier = 20;
			public static double significantInclinationMultiplier = 40;
			public static double exceptionalInclinationMultiplier = 60;

			public static string magnetometerExperimentTitle = "Magnetometer Scan";
			public static string rpwsExperimentTitle = "Radio Plasma Wave Scan";

			public static bool useVesselWaypoints = true;

			public static List<string> magParts = new List<string>(2) { "dmmagBoom", "dmUSMagBoom" };
			public static List<string> rpwsParts = new List<string>(2) { "rpwsAnt", "USRPWS" };

			public static List<string> backStory = new List<string>(1) { "Something, Something, Something..." };
		}

		public static class DMRecon
		{
			public static class Expire
			{
				public static int MinimumExpireDays = 4;

				public static int MaximumExpireDays = 10;

				public static float DeadlineModifier = 3.9f;
			}

			public static class Funds
			{
				public static float BaseAdvance = 50000;

				public static float BaseReward = 60000;

				public static float BaseFailure = 38000;

				public static float ParamReward = 8000;

				public static float ParamFailure = 0;
			}

			public static class Science
			{
				public static float BaseReward = 10;

				public static float ParamReward = 2;
			}

			public static class Reputation
			{
				public static float BaseReward = 12;

				public static float BaseFailure = 10;

				public static float ParamReward = 0;

				public static float ParamFailure = 0;
			}

			public static int maxOffers = 2;
			public static int maxActive = 4;

			public static double trivialTimeModifier = 50;
			public static double significantTimeModifier = 100;
			public static double exceptionalTimeModifier = 150;

			public static bool useVesselWaypoints = true;

			public static List<string> reconTrivialParts = new List<string>(1) { "dmReconSmall" };
			public static List<string> reconSignificantParts = new List<string>(3) { "dmSIGINT", "dmSIGINT.Small", "dmSIGINT.End" };
			public static List<string> reconExceptionalParts = new List<string>(1) { "dmReconLarge" };

			public static string trivialExperimentTitle = "Recon Scan";
			public static string significantExperimentTitle = "SIGINT Scan";
			public static string exceptionalExperimentTitle = "Recon Scan";

			public static List<string> backStory = new List<string>(1) { "Something, Something, Something..." };
		}
	}
}
