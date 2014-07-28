#region license
/* DMagic Orbital Science - DMConfigLoader
 * Monobehaviour to load science contract paramaters at startup
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

using System.Collections.Generic;
using UnityEngine;

namespace DMagic
{
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	internal class DMConfigLoader: MonoBehaviour
	{

		private void Start()
		{
			DMUtils.rand = new System.Random();
			DMUtils.availableScience = new Dictionary<string,DMScienceContainer>();
			DMUtils.surfaceScience = new Dictionary<string,DMScienceContainer>();
			DMUtils.atmosphericScience = new Dictionary<string,DMScienceContainer>();
			DMUtils.orbitalScience = new Dictionary<string,DMScienceContainer>();
			DMUtils.storyList = new List<string>();
			DMUtils.DebugLog("Generating Global Random Number Generator");
			configLoad();
		}

		private void configLoad()
		{
			foreach (ConfigNode setNode in GameDatabase.Instance.GetConfigNodes("DM_CONTRACT_SETTINGS"))
				if (setNode.GetValue("name") == "Contract Settings")
				{
					DMUtils.science = float.Parse(setNode.GetValue("Global_Science_Return"));
					DMUtils.reward = float.Parse(setNode.GetValue("Global_Fund_Reward"));
					DMUtils.forward = float.Parse(setNode.GetValue("Global_Fund_Forward"));
					DMUtils.penalty = float.Parse(setNode.GetValue("Global_Fund_Penalty"));
					DMUtils.Logging("Contract Variables Set; Science Reward: {0} ; Completion Reward: {1} ; Forward Amount: {2} ; Penalty Amount: {3}",
						DMUtils.science, DMUtils.reward, DMUtils.forward, DMUtils.penalty);
					break;
				}
			foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("DM_CONTRACT_EXPERIMENT"))
			{
				string name, part, agent, expID = "";
				int sitMask, bioMask, type = 0;
				DMScienceContainer DMscience = null;
				expID = node.GetValue("experimentID");
				ScienceExperiment exp = ResearchAndDevelopment.GetExperiment(expID);
				if (exp != null)
				{
					name = node.GetValue("name");
					sitMask = int.Parse(node.GetValue("sitMask"));
					bioMask = int.Parse(node.GetValue("bioMask"));
					type = int.Parse(node.GetValue("type"));
					part = node.GetValue("part");
					if (node.HasValue("agent"))
						agent = node.GetValue("agent");
					else
						agent = "Any";
					DMscience = new DMScienceContainer(expID, exp, sitMask, bioMask, (DMScienceType)type, part, agent);
					if (((DMScienceType)type & DMScienceType.Surface) == DMScienceType.Surface)
						DMUtils.surfaceScience.Add(name, DMscience);
					if (((DMScienceType)type & DMScienceType.Aerial) == DMScienceType.Aerial)
						DMUtils.atmosphericScience.Add(name, DMscience);
					if (((DMScienceType)type & DMScienceType.Space) == DMScienceType.Space)
						DMUtils.orbitalScience.Add(name, DMscience);
					DMUtils.availableScience.Add(name, DMscience);
					DMUtils.Logging("New Experiment: [{0}] Available For Contracts", exp.experimentTitle);
				}
			}
			DMUtils.Logging("Successfully Added {0} New Experiments To Contract List", DMUtils.availableScience.Count);
			foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("DM_SCIENCE_STORY_DEF"))
			{
				foreach (ConfigNode storyNode in node.GetNodes("DM_SCIENCE_BACKSTORY"))
				{
					foreach (string st in storyNode.GetValues("generic"))
					{
						if (!string.IsNullOrEmpty(st))
						{
							string story = st.Replace("[", "{");
							story = story.Replace("]", "}");
							DMUtils.storyList.Add(story);
						}
					}
				}
			}
			DMUtils.Logging("Successfully Added {0} New Backstories to Story List", DMUtils.storyList.Count);
		}

		private void OnDestroy()
		{
		}

	}



}
