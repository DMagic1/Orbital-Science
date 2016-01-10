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
				public static int MinimumExpireDays = 5;

				public static int MaximumExpireDays = 15;

				public static float DeadlineYears = 1.5f;
			}

			public static class Funds
			{
				public static float BaseAdvance = 20000;

				public static float BaseReward = 24000;

				public static float BaseFailure = 20000;

				public static float ParamReward = 12000;

				public static float ParamFailure = 0;
			}

			public static class Science
			{
				public static float BaseReward = 0;

				public static float ParamReward = 6;

				public static float SecondaryReward = 0.25f;
			}

			public static class Reputation
			{
				public static float BaseReward = 8;

				public static float BaseFailure = 9;

				public static float ParamReward = 0;

				public static float ParamFailure = 0;
			}

			public static int maxOffers = 2;
			public static int maxActive = 3;

			public static int TrivialReconLevelRequirement = 0;
			public static int SignificantReconLevelRequirement = 1;
			public static int ExceptionalReconLevelRequirement = 1;

			public static List<string> backStory = new List<string>(1) { "Something, Something, Something..." };
		}

		public static class DMAsteroid
		{
			public static class Expire
			{
				public static int MinimumExpireDays = 1;

				public static int MaximumExpireDays = 7;

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
				public static float BaseReward = 1.5f;

				public static float BaseFailure = 1.5f;

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
				public static int MinimumExpireDays = 5;

				public static int MaximumExpireDays = 15;

				public static float DeadlineYears = 1.7f;
			}

			public static class Funds
			{
				public static float BaseAdvance = 8500;

				public static float BaseReward = 10500;

				public static float BaseFailure = 7500;

				public static float ParamReward = 3500;

				public static float ParamFailure = 0;
			}

			public static class Science
			{
				public static float BaseReward = 0;

				public static float ParamReward = 0.2f;
			}

			public static class Reputation
			{
				public static float BaseReward = 1.9f;

				public static float BaseFailure = 1.5f;

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
				public static int MinimumExpireDays = 5;

				public static int MaximumExpireDays = 15;

				public static float DeadlineModifier = 3.7f;
			}

			public static class Funds
			{
				public static float BaseAdvance = 35000;

				public static float BaseReward = 40000;

				public static float BaseFailure = 28000;

				public static float ParamReward = 5000;

				public static float ParamFailure = 0;
			}

			public static class Science
			{
				public static float BaseReward = 15;

				public static float ParamReward = 2f;
			}

			public static class Reputation
			{
				public static float BaseReward = 8f;

				public static float BaseFailure = 7f;

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

			public static bool useVesselWaypoints = true;

			public static List<string> magParts = new List<string>(2) { "dmmagBoom", "dmUSMagBoom" };
			public static List<string> rpwsParts = new List<string>(2) { "rpwsAnt", "USRPWS" };

			public static List<string> backStory = new List<string>(1) { "Something, Something, Something..." };
		}

		public static class DMRecon
		{
			public static class Expire
			{
				public static int MinimumExpireDays = 5;

				public static int MaximumExpireDays = 15;

				public static float DeadlineModifier = 3.7f;
			}

			public static class Funds
			{
				public static float BaseAdvance = 35000;

				public static float BaseReward = 40000;

				public static float BaseFailure = 28000;

				public static float ParamReward = 5000;

				public static float ParamFailure = 0;
			}

			public static class Science
			{
				public static float BaseReward = 20;

				public static float ParamReward = 2f;
			}

			public static class Reputation
			{
				public static float BaseReward = 10;

				public static float BaseFailure = 12f;

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
			public static List<string> reconSignificantParts = new List<string>(1) { "dmSIGINT" };
			public static List<string> reconExceptionalParts = new List<string>(1) { "dmReconLarge" };

			public static List<string> backStory = new List<string>(1) { "Something, Something, Something..." };
		}
	}
}
