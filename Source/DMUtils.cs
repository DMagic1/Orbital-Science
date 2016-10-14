#region license
/* DMagic Orbital Science - DMUtils
 * Static utilities class for various methods
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
using System.Text.RegularExpressions;
using UnityEngine;
using Contracts;
using DMagic.Contracts;
using DMagic.Parameters;
using FinePrint.Utilities;

namespace DMagic
{
	internal static class DMUtils
	{
		internal static System.Random rand;
		internal static Dictionary<string, Dictionary<string, DMScienceContainer>> availableScience;
		internal static string version = "v0.9.2";
		internal static EventData<CelestialBody, String, String> OnAnomalyScience;
		internal static EventData<String, String> OnAsteroidScience;
		internal static bool whiteListed = false;

		private static Regex openBracket = new Regex(@"\[(?=\d+:?\w?\d?\])");
		private static Regex closeBraket = new Regex(@"(?<=\{\d+:?\w?\d?)\]");
		private static Regex newLines = new Regex(@"\\n");

		internal static float fixSubjectVal(ExperimentSituations s, float f, CelestialBody body)
		{
			float subV = f;
			if (s == ExperimentSituations.SrfLanded) subV = body.scienceValues.LandedDataValue;
			else if (s == ExperimentSituations.SrfSplashed) subV = body.scienceValues.SplashedDataValue;
			else if (s == ExperimentSituations.FlyingLow) subV = body.scienceValues.FlyingLowDataValue;
			else if (s == ExperimentSituations.FlyingHigh) subV = body.scienceValues.FlyingHighDataValue;
			else if (s == ExperimentSituations.InSpaceLow) subV = body.scienceValues.InSpaceLowDataValue;
			else if (s == ExperimentSituations.InSpaceHigh) subV = body.scienceValues.InSpaceHighDataValue;
			return subV;
		}

		internal static Vessel.Situations convertSit(ExperimentSituations s)
		{
			switch (s)
			{
				case ExperimentSituations.SrfLanded:
					return Vessel.Situations.LANDED;
				case ExperimentSituations.SrfSplashed:
					return Vessel.Situations.SPLASHED;
				case ExperimentSituations.FlyingLow:
					return Vessel.Situations.FLYING;
				case ExperimentSituations.FlyingHigh:
					return Vessel.Situations.SUB_ORBITAL;
				case ExperimentSituations.InSpaceLow:
					return Vessel.Situations.ORBITING;
				default:
					return Vessel.Situations.ESCAPING;
			}
		}

		internal static string stringConcat(List<Vessel> source)
		{
			int i = source.Count;
			if (i == 0)
				return "";
			string[] s = new string[i];
			for (int j = 0; j < i; j++)
			{
				if (source[j] != null)
					s[j] = source[j].id.ToString() + "|";
			}
			return string.Concat(s).TrimEnd('|');
		}

		internal static string stringConcat(DictionaryValueList<int, List<string>> source)
		{
			if (source.Count == 0)
				return "";

			string[] result = new string[source.Count];
			for (int i = 0; i < source.Count; i++)
			{
				List<string> group = source.At(i);

				if (group.Count == 0)
				{
					result[i] = "|";
					continue;
				}

				string[] s = new string[group.Count];

				for (int j = 0; j < group.Count; j++)
				{
					s[j] = group[j] + ",";
				}

				result[i] = string.Concat(s).TrimEnd(',') + "|";
			}

			return string.Concat(result).TrimEnd('|');
		}

		internal static List<string> formatFixStringList(List<string> source)
		{
			List<string> fixedList = new List<string>();

			for (int i = 0; i < source.Count(); i++)
			{
				string s = source[i];

				s = openBracket.Replace(s, "{");
				s = closeBraket.Replace(s, "}");
				s = newLines.Replace(s, Environment.NewLine);

				fixedList.Add(s);
			}

			return fixedList;
		}

		internal static DictionaryValueList<int, List<string>> stringSplit(string source)
		{
			DictionaryValueList<int, List<string>> result = new DictionaryValueList<int, List<string>>();

			string[] groups = source.Split('|');

			if (groups.Length == 0)
				return result;

			for (int i = 0; i < groups.Length; i++)
			{
				string[] s = groups[i].Split(',');

				List<string> t = new List<string>();

				for (int j= 0; j < s.Length; j++)
				{
					t.Add(s[j]);
				}

				result.Add(i, t);
			}

			return result;
		}

		internal static bool partAvailable(List<string> parts)
		{
			for (int i = 0; i < parts.Count; i++)
			{
				AvailablePart aPart = PartLoader.getPartInfoByName(parts[i].Replace('_','.'));
				if (aPart == null)
					continue;
				if (!ResearchAndDevelopment.PartModelPurchased(aPart))
					continue;

				return true;
			}

			return false;
		}

		internal static bool vesselHasPart(Vessel v, List<string> titles)
		{
			if (v == null)
				return false;

			if (titles.Count <= 0)
				return false;

			if (v.loaded)
			{
				for (int i = 0; i < v.Parts.Count; i++)
				{
					Part p = v.Parts[i];

					if (p == null)
						continue;

					for (int j = 0; j < titles.Count; j++)
					{
						string title = titles[j];

						if (VesselUtilities.GetPartName(p) == title.Replace('_', '.'))
							return true;
					}
				}
			}
			else
			{
				for (int i = 0; i < v.protoVessel.protoPartSnapshots.Count; i++)
				{
					ProtoPartSnapshot pp = v.protoVessel.protoPartSnapshots[i];

					if (pp == null)
						continue;

					for (int j = 0; j < titles.Count; j++)
					{
						string title = titles[j];

						if (pp.partName == title.Replace('_', '.'))
							return true;
					}
				}
			}

			return false;
		}

		internal static float asteroidSubjectVal(int i)
		{
			switch (i)
			{
				case 0:
					return 2f;
				case 1:
					return 3f;
				case 2:
					return 5f;
				case 3:
					return 7f;
				case 4:
					return 9f;
				default:
					return 1f;
			}
		}

		internal static string sizeHash(int i)
		{
			switch (i)
			{
				case 0:
					return "Class A";
				case 1:
					return "Class B";
				case 2:
					return "Class C";
				case 3:
					return "Class D";
				case 4:
					return "Class E";
				default:
					return "Class Unholy";
			}
		}

		internal static double bearing(double lat1, double lon1, double lat2, double lon2)
		{
			double longdiff = (lon2 - lon1) * Mathf.Deg2Rad;
			double y = Math.Sin(longdiff) * Math.Cos(Mathf.Deg2Rad * lat2);
			double x = Math.Cos(Mathf.Deg2Rad * lat1) * Math.Sin(Mathf.Deg2Rad * lat2) - Math.Sin(Mathf.Deg2Rad * lat1) * Math.Cos(Mathf.Deg2Rad * lat2) * Math.Cos(longdiff);
			double Bearing = (Math.Atan2(y, x) * Mathf.Rad2Deg + 360) % 360;
			return Bearing;
		}

		internal static double timeInDays(double D)
		{
			D /= KSPUtil.dateTimeFormatter.Day;

			return D;
		}

		internal static double fixLatShift(double lat)
		{
			return (lat + 180 + 90) % 180 - 90;
		}

		internal static double fixLonShift(double lon)
		{
			return (lon + 360 + 180) % 360 - 180;
		}

		#region Debug Logging

		[System.Diagnostics.Conditional("DEBUG")]
		internal static void DebugLog(string s, params object[] stringObjects)
		{
			Logging(s, stringObjects);
		}

		internal static void Logging(string s, params object[] stringObjects)
		{
			s = string.Format(s, stringObjects);
			string finalLog = string.Format("[DMOS] {0}", s);
			Debug.Log(finalLog);
		}

		#endregion

		#region Random parameters

		//Return a list of valid experiment situations based on the experiment parameters
		internal static List<ExperimentSituations> availableSituations(ScienceExperiment exp, int i, CelestialBody b)
		{
			List<ExperimentSituations> expSitList = new List<ExperimentSituations>();
			if (((ExperimentSituations)i & ExperimentSituations.FlyingHigh) == ExperimentSituations.FlyingHigh && b.atmosphere)
				expSitList.Add(ExperimentSituations.FlyingHigh);
			if (((ExperimentSituations)i & ExperimentSituations.FlyingLow) == ExperimentSituations.FlyingLow && b.atmosphere)
				expSitList.Add(ExperimentSituations.FlyingLow);
			if (((ExperimentSituations)i & ExperimentSituations.InSpaceHigh) == ExperimentSituations.InSpaceHigh)
			{
				if (!exp.requireAtmosphere)
					expSitList.Add(ExperimentSituations.InSpaceHigh);
				else if (b.atmosphere)
					expSitList.Add(ExperimentSituations.InSpaceHigh);
			}
			if (((ExperimentSituations)i & ExperimentSituations.InSpaceLow) == ExperimentSituations.InSpaceLow)
			{
				if (!exp.requireAtmosphere)
					expSitList.Add(ExperimentSituations.InSpaceLow);
				else if (b.atmosphere)
					expSitList.Add(ExperimentSituations.InSpaceLow);
			}
			if (((ExperimentSituations)i & ExperimentSituations.SrfLanded) == ExperimentSituations.SrfLanded && b.pqsController != null)
			{
				if (!exp.requireAtmosphere && exp.id != "dmbiodrillscan")
					expSitList.Add(ExperimentSituations.SrfLanded);
				else if (b.atmosphere)
					expSitList.Add(ExperimentSituations.SrfLanded);
			}
			if (((ExperimentSituations)i & ExperimentSituations.SrfSplashed) == ExperimentSituations.SrfSplashed && b.ocean && b.pqsController != null)
			{
				if (!exp.requireAtmosphere)
					expSitList.Add(ExperimentSituations.SrfSplashed);
				else if (b.atmosphere)
					expSitList.Add(ExperimentSituations.SrfSplashed);
			}
			return expSitList;
		}

		internal static List<ExperimentSituations> availableSituationsLimited(ScienceExperiment exp, int i, CelestialBody b)
		{
			List<ExperimentSituations> expSitList = new List<ExperimentSituations>();
			if (((ExperimentSituations)i & ExperimentSituations.FlyingLow) == ExperimentSituations.FlyingLow && b.atmosphere)
				expSitList.Add(ExperimentSituations.FlyingLow);
			if (((ExperimentSituations)i & ExperimentSituations.InSpaceLow) == ExperimentSituations.InSpaceLow)
			{
				if (!exp.requireAtmosphere)
					expSitList.Add(ExperimentSituations.InSpaceLow);
				else if (b.atmosphere)
					expSitList.Add(ExperimentSituations.InSpaceLow);
			}
			if (((ExperimentSituations)i & ExperimentSituations.SrfLanded) == ExperimentSituations.SrfLanded && b.pqsController != null)
			{
				if (!exp.requireAtmosphere && exp.id != "dmbiodrillscan")
					expSitList.Add(ExperimentSituations.SrfLanded);
				else if (b.atmosphere)
					expSitList.Add(ExperimentSituations.SrfLanded);
			}
			return expSitList;
		}

		internal static bool biomeRelevant(ExperimentSituations s, int i)
		{
			if ((i & (int)s) == 0)
				return false;
			else
				return true;
		}

		internal static List<string> fetchBiome(CelestialBody b, ScienceExperiment exp, ExperimentSituations sit)
		{
			List<string> s = new List<string>();
			if (b.BiomeMap == null)
			{
				s.Add("");
				return s;
			}
			else
			{
				for (int j = 0; j < b.BiomeMap.Attributes.Length; j++)
				{
					string bName = b.BiomeMap.Attributes[j].name;
					string subId = string.Format("{0}@{1}{2}{3}", exp.id, b.name, sit, bName.Replace(" ", ""));

					if (ResearchAndDevelopment.GetSubjects().Any(a => a.id == subId))
					{
						ScienceSubject subB = ResearchAndDevelopment.GetSubjectByID(subId);
						if (subB.scientificValue > 0.5f)
							s.Add(bName);
					}
					else
						s.Add(bName);
				}
			}
			return s;
		}

		internal static List<string> fetchBiome(CelestialBody b)
		{
			List<string> s = new List<string>();
			if (b.BiomeMap == null)
			{
				s.Add("");
				return s;
			}
			else
			{
				for (int j = 0; j < b.BiomeMap.Attributes.Length; j++)
				{
					string bName = b.BiomeMap.Attributes[j].name;
					s.Add(bName);
				}
			}
			return s;
		}

#endregion

	}

}
