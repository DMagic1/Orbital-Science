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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;

namespace DMagic
{
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	internal class DMConfigLoader: MonoBehaviour
	{
		private string[] WhiteList = new string[1] { "ScienceAlert" };

		private void Start()
		{
			initializeUtils();
			findAssemblies(WhiteList);
			configLoad();
		}

		private void configLoad()
		{
			//Load in global multipliers
			foreach (ConfigNode setNode in GameDatabase.Instance.GetConfigNodes("DM_CONTRACT_SETTINGS"))
			{
				if (setNode != null)
				{
					if (setNode.GetValue("name") == "Contract Settings")
					{

						float.TryParse(setNode.GetValue("Global_Science_Return"), out DMUtils.science);
						float.TryParse(setNode.GetValue("Global_Fund_Reward"), out DMUtils.reward);
						float.TryParse(setNode.GetValue("Global_Fund_Forward"), out DMUtils.forward);
						float.TryParse(setNode.GetValue("Global_Fund_Penalty"), out DMUtils.penalty);
						float.TryParse(setNode.GetValue("Global_Deadline"), out DMUtils.deadline);
						int.TryParse(setNode.GetValue("Max_Survey"), out DMUtils.maxSurvey);
						int.TryParse(setNode.GetValue("Max_Asteroid"), out DMUtils.maxAsteroid);
						int.TryParse(setNode.GetValue("Max_Anomaly"), out DMUtils.maxAnomaly);
						int.TryParse(setNode.GetValue("Max_Magnetic"), out DMUtils.maxMagnetic);

						DMUtils.Logging("Contract Variables Set; Science Reward: {0} ; Completion Reward: {1} ; Forward Amount: {2} ; Penalty Amount: {3} ; Deadline Length: {4}",
							DMUtils.science, DMUtils.reward, DMUtils.forward, DMUtils.penalty, DMUtils.deadline);
						DMUtils.Logging("Max Contract Variables Set: Survey: {0} ; Asteroid: {1} ; Anomaly: {2} ; Magnetic: {3}",
							DMUtils.maxSurvey, DMUtils.maxAsteroid, DMUtils.maxAnomaly, DMUtils.maxMagnetic);
						break;
					}
				}
			}

			//Load in experiment definitions
			foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("DM_CONTRACT_EXPERIMENT"))
			{
				if (node != null)
				{
					string name = "";
					string part = "";
					string agent = "";
					int sitMask = 0;
					int bioMask = 0;
					int type = 0;
					float transmit = 0;
					DMScienceContainer DMscience = null;
					ScienceExperiment exp = null;

					//Some apparently not impossible errors can cause duplicate experiments to be added to the R&D science experiment dictionary
					try
					{
						exp = ResearchAndDevelopment.GetExperiment(node.GetValue("experimentID"));
					}
					catch (Exception e)
					{
						Debug.LogError("[DM] Whoops. Something really wrong happened here; stopping this contract experiment from loading..." + e);
						continue;
					}
					if (exp != null)
					{
						if (!node.HasValue("name"))
							continue;
						name = node.GetValue("name");
						if (!int.TryParse(node.GetValue("sitMask"), out sitMask))
							continue;
						if (!int.TryParse(node.GetValue("bioMask"), out bioMask))
							continue;
						if (!int.TryParse(node.GetValue("type"), out type))
							continue;
						if (!float.TryParse(node.GetValue("xmitDataScalar"), out transmit))
							continue;
						if (node.HasValue("part"))
							part = node.GetValue("part");
						else
							part = "None";
						if (node.HasValue("agent"))
							agent = node.GetValue("agent");
						else
							agent = "Any";
						if (DMUtils.whiteListed)
						{
							exp.situationMask = (uint)sitMask;
							exp.biomeMask = (uint)bioMask;
						}

						DMscience = new DMScienceContainer(exp, sitMask, bioMask, (DMScienceType)type, part, agent, transmit);

						if (((DMScienceType)type & DMScienceType.Surface) == DMScienceType.Surface && !DMUtils.availableScience[DMScienceType.Surface.ToString()].ContainsKey(name))
							DMUtils.availableScience[DMScienceType.Surface.ToString()].Add(name, DMscience);
						if (((DMScienceType)type & DMScienceType.Aerial) == DMScienceType.Aerial && !DMUtils.availableScience[DMScienceType.Aerial.ToString()].ContainsKey(name))
							DMUtils.availableScience[DMScienceType.Aerial.ToString()].Add(name, DMscience);
						if (((DMScienceType)type & DMScienceType.Space) == DMScienceType.Space && !DMUtils.availableScience[DMScienceType.Space.ToString()].ContainsKey(name))
							DMUtils.availableScience[DMScienceType.Space.ToString()].Add(name, DMscience);
						if (((DMScienceType)type & DMScienceType.Biological) == DMScienceType.Biological && !DMUtils.availableScience[DMScienceType.Biological.ToString()].ContainsKey(name))
							DMUtils.availableScience[DMScienceType.Biological.ToString()].Add(name, DMscience);
						if (((DMScienceType)type & DMScienceType.Asteroid) == DMScienceType.Asteroid && !DMUtils.availableScience[DMScienceType.Asteroid.ToString()].ContainsKey(name))
							DMUtils.availableScience[DMScienceType.Asteroid.ToString()].Add(name, DMscience);
						if (((DMScienceType)type & DMScienceType.Anomaly) == DMScienceType.Anomaly && !DMUtils.availableScience[DMScienceType.Anomaly.ToString()].ContainsKey(name))
							DMUtils.availableScience[DMScienceType.Anomaly.ToString()].Add(name, DMscience);
						if (!DMUtils.availableScience["All"].ContainsKey(name))
							DMUtils.availableScience["All"].Add(name, DMscience);
						DMUtils.DebugLog("New Experiment: [{0}] Available For Contracts", name);
					}
				}
			}
			DMUtils.Logging("Successfully Added {0} New Experiments To Contract List", DMUtils.availableScience["All"].Count);
			DMUtils.DebugLog("Successfully Added {0} New Surface Experiments To Contract List", DMUtils.availableScience[DMScienceType.Surface.ToString()].Count);
			DMUtils.DebugLog("Successfully Added {0} New Aerial Experiments To Contract List", DMUtils.availableScience[DMScienceType.Aerial.ToString()].Count);
			DMUtils.DebugLog("Successfully Added {0} New Orbital Experiments To Contract List", DMUtils.availableScience[DMScienceType.Space.ToString()].Count);
			DMUtils.DebugLog("Successfully Added {0} New Biological Experiments To Contract List", DMUtils.availableScience[DMScienceType.Biological.ToString()].Count);
			DMUtils.DebugLog("Successfully Added {0} New Asteroid Experiments To Contract List", DMUtils.availableScience[DMScienceType.Asteroid.ToString()].Count);
			DMUtils.DebugLog("Successfully Added {0} New Anomaly Experiments To Contract List", DMUtils.availableScience[DMScienceType.Anomaly.ToString()].Count);

			//Load in custom contract descriptions
			foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("DM_SCIENCE_STORY_DEF"))
			{
				if (node != null)
				{
					foreach (ConfigNode storyNode in node.GetNodes("DM_SCIENCE_BACKSTORY"))
					{
						if (storyNode != null)
						{
							if (storyNode.HasValue("generic"))
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
							}
							if (storyNode.HasValue("survey"))
							{
								foreach (string so in storyNode.GetValues("survey"))
								{
									if (!string.IsNullOrEmpty(so))
									{
										string story_o = so.Replace("[", "{");
										story_o = story_o.Replace("]", "}");
										DMUtils.backStory["survey"].Add(story_o);
									}
								}
							}
							if (storyNode.HasValue("biological"))
							{
								foreach (string sb in storyNode.GetValues("biological"))
								{
									if (!string.IsNullOrEmpty(sb))
									{
										string story_b = sb.Replace("[", "{");
										story_b = story_b.Replace("]", "}");
										DMUtils.backStory["biological"].Add(story_b);
									}
								}
							}
							if (storyNode.HasValue("asteroid"))
							{
								foreach (string sb in storyNode.GetValues("asteroid"))
								{
									if (!string.IsNullOrEmpty(sb))
									{
										string story_b = sb.Replace("[", "{");
										story_b = story_b.Replace("]", "}");
										DMUtils.backStory["asteroid"].Add(story_b);
									}
								}
							}
							if (storyNode.HasValue("anomaly"))
							{
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
							if (storyNode.HasValue("magnetic"))
							{
								foreach (string sb in storyNode.GetValues("magnetic"))
								{
									if (!string.IsNullOrEmpty(sb))
									{
										string story_b = sb.Replace("[", "{");
										story_b = story_b.Replace("]", "}");
										DMUtils.backStory["magnetic"].Add(story_b);
									}
								}
							}
						}
					}
				}
			}

			DMUtils.Logging("Added {0} New Generic Backstories; {1} New Survey Backstories; {2} New Biological Backstories; {3} New Asteroid Backstories; {4} New Anomaly Backstories; {5} New Magnetic Backstories To The List", DMUtils.backStory["generic"].Count, DMUtils.backStory["survey"].Count, DMUtils.backStory["biological"].Count, DMUtils.backStory["asteroid"].Count, DMUtils.backStory["anomaly"].Count, DMUtils.backStory["magnetic"].Count);
		}

		private void initializeUtils()
		{
			DMUtils.OnAnomalyScience = new EventData<CelestialBody, String, String>("OnAnomalyScience");
			DMUtils.OnAsteroidScience = new EventData<String, String>("OnAsteroidScience");

			var infoAtt = Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
			if (infoAtt != null)
			{
				DMUtils.version = infoAtt.InformationalVersion;
				DMUtils.Logging("DMagic Orbital Science Version: [{0}] Loaded", DMUtils.version);
			}
			else
			{
				DMUtils.version = "";
				DMUtils.Logging("Something Went Wrong Here... Version Not Set, Contracts Might Reset");
			}

			DMUtils.rand = new System.Random();

			DMUtils.availableScience = new Dictionary<string, Dictionary<string, DMScienceContainer>>();
			DMUtils.availableScience["All"] = new Dictionary<string, DMScienceContainer>();
			DMUtils.availableScience[DMScienceType.Surface.ToString()] = new Dictionary<string, DMScienceContainer>();
			DMUtils.availableScience[DMScienceType.Aerial.ToString()] = new Dictionary<string, DMScienceContainer>();
			DMUtils.availableScience[DMScienceType.Space.ToString()] = new Dictionary<string, DMScienceContainer>();
			DMUtils.availableScience[DMScienceType.Biological.ToString()] = new Dictionary<string, DMScienceContainer>();
			DMUtils.availableScience[DMScienceType.Asteroid.ToString()] = new Dictionary<string, DMScienceContainer>();
			DMUtils.availableScience[DMScienceType.Anomaly.ToString()] = new Dictionary<string, DMScienceContainer>();

			DMUtils.backStory = new Dictionary<string, List<string>>();
			DMUtils.backStory.Add("generic", new List<string>());
			DMUtils.backStory.Add("survey", new List<string>());
			DMUtils.backStory.Add("biological", new List<string>());
			DMUtils.backStory.Add("asteroid", new List<string>());
			DMUtils.backStory.Add("anomaly", new List<string>());
			DMUtils.backStory.Add("magnetic", new List<string>());
		}

		private void findAssemblies(string[] assemblies)
		{
			foreach (string name in assemblies)
			{
				AssemblyLoader.LoadedAssembly assembly = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.assembly.GetName().Name == name);
				if (assembly != null)
				{
					DMUtils.Logging("Assembly: {0} Found; Reactivating Experiment Properties", assembly.assembly.GetName().Name);
					DMUtils.whiteListed = true;
				}
			}
		}

	}
}
