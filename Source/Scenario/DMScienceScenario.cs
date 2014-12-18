﻿#region license
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
using System.Collections;
using System.Linq;

namespace DMagic
{
	[KSPScenario(ScenarioCreationOptions.AddToExistingCareerGames | ScenarioCreationOptions.AddToExistingScienceSandboxGames | ScenarioCreationOptions.AddToNewCareerGames | ScenarioCreationOptions.AddToNewScienceSandboxGames, GameScenes.FLIGHT, GameScenes.SPACECENTER, GameScenes.TRACKSTATION, GameScenes.EDITOR)]
	public class DMScienceScenario : ScenarioModule
	{
		internal static DMScienceScenario SciScenario
		{
			get
			{
				Game g = HighLogic.CurrentGame;
				if (g == null)
				{
					return null;
				}
				try
				{
					var DMmod = g.scenarios.FirstOrDefault(m => m.moduleName == typeof(DMScienceScenario).Name);
					if (DMmod != null)
						return (DMScienceScenario)DMmod.moduleRef;
					else
						return null;
				}
				catch (Exception e)
				{
					DMUtils.Logging("Could Not Load DMScienceScenario: {0}", e);
					return null;
				}
			}
		}

		[KSPField(isPersistant = true)]
		public string DMagicVersion = "v0.84";
		[KSPField(isPersistant = true)]
		public bool contractsReload = false;

		internal DMTransmissionWatcher tranWatcher;
		internal DMRecoveryWatcher recoveryWatcher;
		internal DMAnomalyList anomalyList;

		internal List<DMScienceData> recoveredScienceList = new List<DMScienceData>();

		public override void OnSave(ConfigNode node)
		{
			DMUtils.DebugLog("Saving Science Scenario");
			ConfigNode results_node = new ConfigNode("Asteroid_Science");
			foreach (DMScienceData data in recoveredScienceList)
			{
				ConfigNode scienceResults_node = new ConfigNode("DM_Science");
				scienceResults_node.AddValue("title", data.title);
				scienceResults_node.AddValue("bsv", data.basevalue);
				scienceResults_node.AddValue("scv", data.scival);
				scienceResults_node.AddValue("sci", data.science);
				scienceResults_node.AddValue("cap", data.cap);
				results_node.AddNode(scienceResults_node);
			}
			node.AddNode(results_node);
		}

		public override void OnLoad(ConfigNode node)
		{
			if (DMagicVersion != DMUtils.version)
			{
				DMUtils.DebugLog("[DM] New DMagic Version Detected; Resetting Contracts");
				DMagicVersion = DMUtils.version;
				contractsReload = true;
			}
			else
				contractsReload = false;

			DMUtils.DebugLog("Loading Science Scenario");
			recoveredScienceList.Clear();
			ConfigNode results_node = node.GetNode("Asteroid_Science");
			if (results_node != null)
			{
				foreach (ConfigNode scienceResults_node in results_node.GetNodes("DM_Science"))
				{
					string title = scienceResults_node.GetValue("title");
					float bsv = float.Parse(scienceResults_node.GetValue("bsv"));
					float scv = float.Parse(scienceResults_node.GetValue("scv"));
					float sci = float.Parse(scienceResults_node.GetValue("sci"));
					float cap = float.Parse(scienceResults_node.GetValue("cap"));
					RecordNewScience(title, bsv, scv, sci, cap);
				}
			}
			if (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION)
				recoveryWatcher = gameObject.AddComponent<DMRecoveryWatcher>();
			if (HighLogic.LoadedSceneIsFlight)
			{
				tranWatcher = gameObject.AddComponent<DMTransmissionWatcher>();
				anomalyList = gameObject.AddComponent<DMAnomalyList>();
				updateRemainingData();
			}
		}

		private void OnDestroy()
		{
			if (tranWatcher != null)
				Destroy(tranWatcher);
			if (anomalyList != null)
				Destroy(anomalyList);
			if (recoveryWatcher != null)
				Destroy(recoveryWatcher);
		}

		internal class DMScienceData
		{
			internal string title;
			internal float scival, science, cap, basevalue;

			internal DMScienceData(string Title, float BaseVal, float Scv, float Sci, float Cap)
			{
				title = Title;
				basevalue = BaseVal;
				scival = Scv;
				science = Sci;
				cap = Cap;
			}
		}

		internal void RecordNewScience(string title, float baseval, float scv, float sci, float cap)
		{
			DMScienceData DMData = new DMScienceData(title, baseval, scv, sci, cap);
			recoveredScienceList.Add(DMData);
			DMUtils.DebugLog("Adding new DMData to list");
		}

		private void UpdateNewScience(DMScienceData DMData)
		{
			foreach (DMScienceData DMSci in recoveredScienceList)
			{
				if (DMSci.title == DMData.title)
				{
					DMSci.science = DMData.science;
					DMSci.scival = DMData.scival;
					break;
				}
			}
		}

		internal void submitDMScience(DMScienceData DMData, float science)
		{
			DMData.science = Math.Min(DMData.science + science, DMData.cap);
			DMData.scival = ScienceValue(DMData.science, DMData.cap);
			UpdateNewScience(DMData);
			if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready)
				updateRemainingData();
		}

		internal void RemoveDMScience(DMScienceData DMdata)
		{
			recoveredScienceList.Remove(DMdata);
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

		internal void updateRemainingData()
		{
			DMUtils.DebugLog("Updating Existing Data");
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
						foreach (DMScienceScenario.DMScienceData DMData in recoveredScienceList)
						{
							if (DMData.title == data.title)
							{
								ScienceSubject sub = ResearchAndDevelopment.GetSubjectByID(data.subjectID);
								sub.scientificValue *= DMData.scival;
								sub.science = Math.Max(0f, sub.scienceCap - (sub.scienceCap * sub.scientificValue));
							}
						}
					}
				}
			}
		}

	}
}
