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
using System.Collections;
using System.Linq;

namespace DMagic
{
	[KSPScenario(ScenarioCreationOptions.AddToExistingCareerGames | ScenarioCreationOptions.AddToExistingScienceSandboxGames | ScenarioCreationOptions.AddToNewCareerGames | ScenarioCreationOptions.AddToNewScienceSandboxGames, GameScenes.FLIGHT, GameScenes.SPACECENTER, GameScenes.TRACKSTATION)]
	class DMScienceScenario : ScenarioModule
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

		internal bool Recovered = false;
		internal DMTransmissionWatcher tranWatcher;
		internal DMRecoveryWatcher recoveryWatcher;

		internal List<DMScienceData> recoveredScienceList = new List<DMScienceData>();

		public override void OnSave(ConfigNode node)
		{
			DMUtils.DebugLog("Saving Science Scenario");
			ConfigNode results_node = new ConfigNode("Asteroid_Science");
			foreach (DMScienceData data in recoveredScienceList)
			{
				ConfigNode scienceResults_node = new ConfigNode("Science");
				//scienceResults_node.AddValue("id", data.id);
				scienceResults_node.AddValue("title", data.title);
				scienceResults_node.AddValue("bsv", data.basevalue);
				//scienceResults_node.AddValue("dsc", data.dataScale);
				scienceResults_node.AddValue("scv", data.scival);
				scienceResults_node.AddValue("sci", data.science);
				scienceResults_node.AddValue("cap", data.cap);
				scienceResults_node.AddValue("expNo", data.expNo);
				results_node.AddNode(scienceResults_node);
			}
			node.AddNode(results_node);
		}

		public override void OnLoad(ConfigNode node)
		{
			DMUtils.DebugLog("Loading Science Scenario");
			recoveredScienceList.Clear();
			ConfigNode results_node = node.GetNode("Asteroid_Science");
			if (results_node != null)
			{
				foreach (ConfigNode scienceResults_node in results_node.GetNodes("Science"))
				{
					//string id = scienceResults_node.GetValue("id");
					string title = scienceResults_node.GetValue("title");
					float bsv = float.Parse(scienceResults_node.GetValue("bsv"));
					//float dsc = Convert.ToSingle(scienceResults_node.GetValue("dsc"));
					float scv = float.Parse(scienceResults_node.GetValue("scv"));
					float sci = float.Parse(scienceResults_node.GetValue("sci"));
					float cap = float.Parse(scienceResults_node.GetValue("cap"));
					int eNo = int.Parse(scienceResults_node.GetValue("expNo"));
					RecordNewScience(title, bsv, scv, sci, cap, eNo);
				}
			}
			recoveryWatcher = gameObject.AddComponent<DMRecoveryWatcher>();
			if (HighLogic.LoadedSceneIsFlight)
			{
				tranWatcher = gameObject.AddComponent<DMTransmissionWatcher>();
				updateRemainingData();
			}
		}

		private void OnDestroy()
		{
			if (tranWatcher != null)
				Destroy(tranWatcher);
			Destroy(recoveryWatcher);
		}

		internal class DMScienceData
		{
			internal string title;
			internal int expNo;
			internal float scival, science, cap, basevalue;

			internal DMScienceData(string Title, float BaseVal, float Scv, float Sci, float Cap, int ENo)
			{
				title = Title;
				basevalue = BaseVal;
				scival = Scv;
				science = Sci;
				cap = Cap;
				expNo = ENo;
			}
		}

		internal void RecordNewScience(string title, float baseval, float scv, float sci, float cap, int eNo)
		{
			DMScienceData DMData = new DMScienceData(title, baseval, scv, sci, cap, eNo);
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
					DMSci.expNo = DMData.expNo;
					DMSci.scival = DMData.scival;
					break;
				}
			}
		}

		internal void submitDMScience(DMScienceData DMData, ScienceSubject sub)
		{
			DMData.scival = ScienceValue(DMData.expNo, DMData.scival);
			DMData.science += DMData.basevalue * sub.subjectValue * DMData.scival;
			DMData.expNo++;
			UpdateNewScience(DMData);
		}

		internal void RemoveDMScience(DMScienceData DMdata)
		{
			recoveredScienceList.Remove(DMdata);
		}

		private float ScienceValue(int i, float f)
		{
			float sciVal = 1f;
			if (i < 3) sciVal = f - 0.05f * (6 / i);
			else sciVal = f - 0.05f;
			return sciVal;
		}

		internal void updateRemainingData()
		{
			DMUtils.DebugLog("Updating Existing Data");
			List<ScienceData> dataList = new List<ScienceData>();
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
							sub.scientificValue = DMData.scival;
							sub.science = sub.scienceCap - (sub.scienceCap * sub.scientificValue);
						}
					}
				}
			}
		}

		//internal float SciSub(string s)
		//{
		//    switch (s[s.Length - 1])
		//    {
		//        case 'A':
		//            return 1.5f;
		//        case 'B':
		//            return 3f;
		//        case 'C':
		//            return 5f;
		//        case 'D':
		//            return 8f;
		//        case 'E':
		//            return 10f;
		//        default:
		//            return 30f;
		//    }
		//}

	}
}
