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
			initializeUtils();
			configLoad();
		}

		private void configLoad()
		{
			//Load in global multipliers
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
			//Load in experiment definitions
			foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("DM_CONTRACT_EXPERIMENT"))
			{
				string name, part, agent = "";
				int sitMask, bioMask, type = 0;
				DMScienceContainer DMscience = null;
				ScienceExperiment exp = ResearchAndDevelopment.GetExperiment(node.GetValue("experimentID"));
				if (exp != null)
				{
					name = node.GetValue("name");
					if (!int.TryParse(node.GetValue("sitMask"), out sitMask))
						continue;
					if (!int.TryParse(node.GetValue("bioMask"), out bioMask))
						continue;
					if (!int.TryParse(node.GetValue("type"), out type))
						continue;
					if (node.HasValue("part"))
						part = node.GetValue("part");
					else
						part = "None";
					if (node.HasValue("agent"))
						agent = node.GetValue("agent");
					else
						agent = "Any";
					DMscience = new DMScienceContainer(exp, sitMask, bioMask, (DMScienceType)type, part, agent);
					if (((DMScienceType)type & DMScienceType.Surface) == DMScienceType.Surface)
						DMUtils.availableScience[DMScienceType.Surface.ToString()].Add(name, DMscience);
					if (((DMScienceType)type & DMScienceType.Aerial) == DMScienceType.Aerial)
						DMUtils.availableScience[DMScienceType.Aerial.ToString()].Add(name, DMscience);
					if (((DMScienceType)type & DMScienceType.Space) == DMScienceType.Space)
						DMUtils.availableScience[DMScienceType.Space.ToString()].Add(name, DMscience);
					if (((DMScienceType)type & DMScienceType.Biological) == DMScienceType.Biological)
						DMUtils.availableScience[DMScienceType.Biological.ToString()].Add(name, DMscience);
					if (((DMScienceType)type & DMScienceType.Asteroid) == DMScienceType.Asteroid)
						DMUtils.availableScience[DMScienceType.Asteroid.ToString()].Add(name, DMscience);
					DMUtils.availableScience["All"].Add(name, DMscience);
					DMUtils.Logging("New Experiment: [{0}] Available For Contracts", name);
				}
			}
			DMUtils.Logging("Successfully Added {0} New Experiments To Contract List", DMUtils.availableScience["All"].Count);
			DMUtils.DebugLog("Successfully Added {0} New Surface Experiments To Contract List", DMUtils.availableScience[DMScienceType.Surface.ToString()].Count);
			DMUtils.DebugLog("Successfully Added {0} New Aerial Experiments To Contract List", DMUtils.availableScience[DMScienceType.Aerial.ToString()].Count);
			DMUtils.DebugLog("Successfully Added {0} New Orbital Experiments To Contract List", DMUtils.availableScience[DMScienceType.Space.ToString()].Count);
			DMUtils.DebugLog("Successfully Added {0} New Biological Experiments To Contract List", DMUtils.availableScience[DMScienceType.Biological.ToString()].Count);
			DMUtils.DebugLog("Successfully Added {0} New Asteroid Experiments To Contract List", DMUtils.availableScience[DMScienceType.Asteroid.ToString()].Count);
			//Load in custom contract descriptions
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
							DMUtils.backStory["generic"].Add(story);
						}
					}
					foreach (string so in storyNode.GetValues("survey"))
					{
						if (!string.IsNullOrEmpty(so))
						{
							string story_o = so.Replace("[", "{");
							story_o = story_o.Replace("]", "}");
							DMUtils.backStory["survey"].Add(story_o);
						}
					}
					foreach (string sb in storyNode.GetValues("biological"))
					{
						if (!string.IsNullOrEmpty(sb))
						{
							string story_b = sb.Replace("[", "{");
							story_b = story_b.Replace("]", "}");
							DMUtils.backStory["biological"].Add(story_b);
						}
					}
					foreach (string sb in storyNode.GetValues("asteroid"))
					{
						if (!string.IsNullOrEmpty(sb))
						{
							string story_b = sb.Replace("[", "{");
							story_b = story_b.Replace("]", "}");
							DMUtils.backStory["asteroid"].Add(story_b);
						}
					}
					foreach (string sb in storyNode.GetValues("anomaly"))
					{
						if (!string.IsNullOrEmpty(sb))
						{
							string story_b = sb.Replace("[", "{");
							story_b = story_b.Replace("]", "}");
							DMUtils.backStory["anomaly"].Add(story_b);
						}
					}
				}
			}
			DMUtils.Logging("Added {0} New Generic Backstories; {1} New Survey Backstories; {2} New Biological Backstories To The List", DMUtils.backStory["generic"].Count, DMUtils.backStory["survey"].Count, DMUtils.backStory["biological"].Count);
		}

		private void initializeUtils()
		{
			DMUtils.rand = new System.Random();
			DMUtils.availableScience = new Dictionary<string, Dictionary<string, DMScienceContainer>>();
			DMUtils.availableScience["All"] = new Dictionary<string, DMScienceContainer>();
			DMUtils.availableScience[DMScienceType.Surface.ToString()] = new Dictionary<string, DMScienceContainer>();
			DMUtils.availableScience[DMScienceType.Aerial.ToString()] = new Dictionary<string, DMScienceContainer>();
			DMUtils.availableScience[DMScienceType.Space.ToString()] = new Dictionary<string, DMScienceContainer>();
			DMUtils.availableScience[DMScienceType.Biological.ToString()] = new Dictionary<string, DMScienceContainer>();
			DMUtils.availableScience[DMScienceType.Asteroid.ToString()] = new Dictionary<string, DMScienceContainer>();

			DMUtils.backStory = new Dictionary<string, List<string>>();
			DMUtils.backStory["generic"] = new List<string>();
			DMUtils.backStory["survey"] = new List<string>();
			DMUtils.backStory["biological"] = new List<string>();
		}

		private void OnDestroy()
		{
		}

	}



}
