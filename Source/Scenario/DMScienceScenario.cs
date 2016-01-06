#region license
/* DMagic Orbital Science - DM Science Scenario
 * Scenario Module to store science results
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

namespace DMagic.Scenario
{
	[KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT, GameScenes.SPACECENTER, GameScenes.TRACKSTATION, GameScenes.EDITOR)]
	public class DMScienceScenario : ScenarioModule
	{
		public static DMScienceScenario SciScenario
		{
			get { return instance; }
		}

		//Anomaly tracking object
		//internal DMAnomalyList anomalyList;

		//Master List for saved asteroid science data
		private Dictionary<string, DMScienceData> recoveredDMScience = new Dictionary<string,DMScienceData>();

		private static DMScienceScenario instance;

		public DMScienceData getDMScience(string title, bool warn = false)
		{
			if (recoveredDMScience.ContainsKey(title))
				return recoveredDMScience[title];
			else if (warn)
				DMUtils.Logging("Could not find DMScience of title [{0}]", title);

			return null;
		}

		public int RecoveredDMScienceCount
		{
			get { return recoveredDMScience.Count; }
		}

		public override void OnSave(ConfigNode node)
		{
			ConfigNode results_node = new ConfigNode("Asteroid_Science");
			foreach (DMScienceData data in recoveredDMScience.Values)
			{
				if (data == null)
					continue;

				try
				{
					ConfigNode scienceResults_node = new ConfigNode("DM_Science");
					scienceResults_node.AddValue("title", data.Title);
					scienceResults_node.AddValue("bsv", data.BaseValue);
					scienceResults_node.AddValue("scv", data.SciVal);
					scienceResults_node.AddValue("sci", data.Science);
					scienceResults_node.AddValue("cap", data.Cap);
					results_node.AddNode(scienceResults_node);
				}
				catch (Exception e)
				{
					Debug.LogWarning("[DMagic] Error Saving Asteroid Science Data: " + e);
				}
			}

			ConfigNode anomaly_node = new ConfigNode("Anomaly_Records");
			for (int i = 0; i < DMAnomalyList.AnomalyCount; i++)
			{
				DMAnomalyStorage anomStorage = DMAnomalyList.getAnomalyStorage(i);

				if (anomStorage == null)
					continue;

				ConfigNode anomalyList = new ConfigNode("DM_Anomaly_List");
				anomalyList.AddValue("Body", anomStorage.Body.flightGlobalsIndex);

				for (int j = 0; j < anomStorage.AnomalyCount; j++)
				{
					DMAnomalyObject anom = anomStorage.getAnomaly(j);

					if (anom == null)
						continue;

					ConfigNode anomaly = new ConfigNode("DM_Anomaly");
					anomaly.AddValue("Name", anom.Name);
					anomaly.AddValue("Lat", anom.Lat.ToString("N5"));
					anomaly.AddValue("Lon", anom.Lon.ToString("N5"));
					anomaly.AddValue("Alt", anom.Alt.ToString("N5"));
					anomalyList.AddNode(anomaly);
				}
				anomaly_node.AddNode(anomalyList);
			}

			node.AddNode(results_node);
			node.AddNode(anomaly_node);
		}

		public override void OnLoad(ConfigNode node)
		{
			instance = this;

			recoveredDMScience.Clear();
			ConfigNode results_node = node.GetNode("Asteroid_Science");
			if (results_node != null)
			{
				foreach (ConfigNode scienceResults_node in results_node.GetNodes("DM_Science"))
				{
					if (scienceResults_node == null)
						continue;

					string title = scienceResults_node.parse("title", "");
					if (string.IsNullOrEmpty(title))
						continue;

					float bsv = scienceResults_node.parse("bsv", (float)1);
					float scv = scienceResults_node.parse("scv", (float)1);
					float sci = scienceResults_node.parse("sci", (float)0);
					float cap = scienceResults_node.parse("cap", (float)1);

					RecordNewScience(title, bsv, scv, sci, cap);
				}
			}

			DMAnomalyList.clearAnomalies();
			ConfigNode anomaly_node = node.GetNode("Anomaly_Records");
			if (anomaly_node != null)
			{
				foreach (ConfigNode anomalyList in anomaly_node.GetNodes("DM_Anomaly_List"))
				{
					if (anomalyList == null)
						continue;

					CelestialBody body = anomalyList.parse("Body", (CelestialBody)null);
					if (body == null)
						continue;

					DMAnomalyStorage anomStorage = new DMAnomalyStorage(body);

					foreach (ConfigNode anomaly in anomalyList.GetNodes("DM_Anomaly"))
					{
						string name = anomaly.parse("Name", "");
						if (string.IsNullOrEmpty(name))
							continue;

						double lat = anomaly.parse("Lat", (double)0);
						double lon = anomaly.parse("Lon", (double)0);
						double alt = anomaly.parse("Alt", (double)0);

						anomStorage.addAnomaly(new DMAnomalyObject(name, body, lat, lon, alt));
					}
				}
			}
		}

		private void Start()
		{
			if (HighLogic.LoadedSceneIsFlight)
			{
				//anomalyList = gameObject.AddComponent<DMAnomalyList>();
				updateRemainingData();
			}
		}

		//private void OnDestroy()
		//{
		//	if (anomalyList != null)
		//		Destroy(anomalyList);
		//}

		private void addDMScience(DMScienceData data)
		{
			if (!recoveredDMScience.ContainsKey(data.Title))
				recoveredDMScience.Add(data.Title, data);
			else
				UpdateDMScience(data);
		}

		internal void RecordNewScience(string title, float baseval, float scv, float sci, float cap)
		{
			DMScienceData DMData = new DMScienceData(title, baseval, scv, sci, cap);
			addDMScience(DMData);
		}

		private void UpdateDMScience(DMScienceData DMData)
		{
			if (recoveredDMScience.ContainsKey(DMData.Title))
			{
				DMScienceData DMSci = recoveredDMScience[DMData.Title];
				DMSci.Science = DMData.Science;
				DMSci.SciVal = DMData.SciVal;
			}
		}

		internal void submitDMScience(DMScienceData DMData, float science)
		{
			DMData.Science = Math.Min(DMData.Science + science, DMData.Cap);
			DMData.SciVal = ScienceValue(DMData.Science, DMData.Cap);
			UpdateDMScience(DMData);
			if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready)
				updateRemainingData();
		}

		private void RemoveDMScience(DMScienceData DMdata)
		{
			if (recoveredDMScience.ContainsKey(DMdata.Title))
				recoveredDMScience.Remove(DMdata.Title);
		}

		private float ScienceValue(float sci, float cap)
		{
			float sciVal = 1f;
			if (cap == 0)
				sciVal = 0f;
			else
				sciVal = Math.Max(1f - (sci / cap), 0f);
			return sciVal;
		}

		private void updateRemainingData()
		{
			List<ScienceData> dataList = new List<ScienceData>();
			if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready)
			{
				foreach (IScienceDataContainer container in FlightGlobals.ActiveVessel.FindPartModulesImplementing<IScienceDataContainer>())
				{
					dataList.AddRange(container.GetData());
				}
				if (dataList.Count > 0)
				{
					foreach (ScienceData data in dataList)
					{
						DMScienceData DMData = getDMScience(data.title, true);
						if (DMData != null)
						{
							ScienceSubject sub = ResearchAndDevelopment.GetSubjectByID(data.subjectID);
							if (sub != null)
							{
								sub.scientificValue *= DMData.SciVal;
								sub.science = Math.Max(0f, Math.Min(sub.scienceCap, sub.scienceCap - (sub.scienceCap * sub.scientificValue)));
							}
						}
					}
				}
			}
		}

	}
}
