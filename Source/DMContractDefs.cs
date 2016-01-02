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
				public static int MinimumExpireDays = 1;

				public static int MaximumExpireDays = 7;

				public static int DeadlineDays = 2982;
			}

			public static class Funds
			{
				public static double BaseAdvance = 31000f;

				public static double BaseReward = 79000f;

				public static double BaseFailure;
			}

			public static class Science
			{
				public static float BaseReward;
			}

			public static class Reputation
			{
				public static float BaseReward = 22f;

				public static float BaseFailure = 11f;
			}

			public static int maxOffers = 2;
			public static int maxActive = 3;

			public static List<string> backStory = new List<string>(1) { "Something, Something, Something..." };
		}

		public static class DMAsteroid
		{
			public static class Expire
			{
				public static int MinimumExpireDays = 1;

				public static int MaximumExpireDays = 7;

				public static int DeadlineDays = 2982;
			}

			public static class Funds
			{
				public static double BaseAdvance = 31000f;

				public static double BaseReward = 79000f;

				public static double BaseFailure;
			}

			public static class Science
			{
				public static float BaseReward;
			}

			public static class Reputation
			{
				public static float BaseReward = 22f;

				public static float BaseFailure = 11f;
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
				public static int MinimumExpireDays = 1;

				public static int MaximumExpireDays = 7;

				public static int DeadlineDays = 2982;
			}

			public static class Funds
			{
				public static double BaseAdvance = 31000f;

				public static double BaseReward = 79000f;

				public static double BaseFailure;
			}

			public static class Science
			{
				public static float BaseReward;
			}

			public static class Reputation
			{
				public static float BaseReward = 22f;

				public static float BaseFailure = 11f;
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
				public static int MinimumExpireDays = 1;

				public static int MaximumExpireDays = 7;

				public static int DeadlineDays = 2982;
			}

			public static class Funds
			{
				public static double BaseAdvance = 31000f;

				public static double BaseReward = 79000f;

				public static double BaseFailure;
			}

			public static class Science
			{
				public static float BaseReward;
			}

			public static class Reputation
			{
				public static float BaseReward = 22f;

				public static float BaseFailure = 11f;
			}

			public static int maxOffers = 2;
			public static int maxActive = 4;

			public static double trivialEccentricityMultiplier = 0.2;
			public static double significantEccentricityMultiplier = 0.2;
			public static double exceptionalEccentricityMultiplier = 0.2;

			public static double trivialInclinationMultiplier = 0.2;
			public static double significantInclinationMultiplier = 0.2;
			public static double exceptionalInclinationMultiplier = 0.2;

			public static List<string> magParts = new List<string>(2) { "dmmagBoom", "dmUSMagBoom" };
			public static List<string> rpwsParts = new List<string>(2) { "rpwsAnt", "USRPWS" };

			public static List<string> backStory = new List<string>(1) { "Something, Something, Something..." };
		}

		public static class DMRecon
		{
			public static class Expire
			{
				public static int MinimumExpireDays = 1;

				public static int MaximumExpireDays = 7;

				public static int DeadlineDays = 2982;
			}

			public static class Funds
			{
				public static double BaseAdvance = 31000f;

				public static double BaseReward = 79000f;

				public static double BaseFailure;
			}

			public static class Science
			{
				public static float BaseReward;
			}

			public static class Reputation
			{
				public static float BaseReward = 22f;

				public static float BaseFailure = 11f;
			}

			public static int maxOffers = 2;
			public static int maxActive = 4;

			public static List<string> reconTrivialParts = new List<string>(1) { "dmReconSmall" };
			public static List<string> reconSignificantParts = new List<string>(1) { "dmSIGINT" };
			public static List<string> reconExceptionalParts = new List<string>(1) { "dmReconLarge" };

			public static List<string> backStory = new List<string>(1) { "Something, Something, Something..." };
		}
	}
}
