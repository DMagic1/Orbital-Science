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
		private const string iconURL = "DMagicOrbitalScience/Icons/Waypoints/";

		private void Start()
		{
			initializeUtils();
			findAssemblies(WhiteList);
			configLoad();
			hackWaypointIcons();
		}

		private void configLoad()
		{
			//Load in global multipliers
			foreach (ConfigNode setNode in GameDatabase.Instance.GetConfigNodes("DM_CONTRACT_SETTINGS"))
			{
				if (setNode != null)
				{
					if (!float.TryParse(setNode.GetValue("Global_Science_Return"), out DMUtils.science))
						DMUtils.science = 1;
					if (!float.TryParse(setNode.GetValue("Global_Fund_Reward"), out DMUtils.reward))
						DMUtils.reward = 1;
					if (!float.TryParse(setNode.GetValue("Global_Fund_Forward"), out DMUtils.forward))
						DMUtils.forward = 1;
					if (!float.TryParse(setNode.GetValue("Global_Fund_Penalty"), out DMUtils.penalty))
						DMUtils.penalty = 1;
					if (!float.TryParse(setNode.GetValue("Global_Deadline"), out DMUtils.deadline))
						DMUtils.deadline = 1;
					if (!int.TryParse(setNode.GetValue("Max_Survey_Offered"), out DMUtils.maxSurveyOffered))
						DMUtils.maxSurveyOffered = 2;
					if (!int.TryParse(setNode.GetValue("Max_Survey_Active"), out DMUtils.maxSurveyActive))
						DMUtils.maxSurveyActive = 4;
					if (!int.TryParse(setNode.GetValue("Max_Asteroid_Offered"), out DMUtils.maxAsteroidOffered))
						DMUtils.maxAsteroidOffered = 1;
					if (!int.TryParse(setNode.GetValue("Max_Asteroid_Active"), out DMUtils.maxAsteroidActive))
						DMUtils.maxAsteroidActive = 3;
					if (!int.TryParse(setNode.GetValue("Max_Anomaly_Offered"), out DMUtils.maxAnomalyOffered))
						DMUtils.maxAnomalyOffered = 1;
					if (!int.TryParse(setNode.GetValue("Max_Anomaly_Active"), out DMUtils.maxAnomalyActive))
						DMUtils.maxAnomalyActive = 3;
					if (!int.TryParse(setNode.GetValue("Max_Magnetic_Offered"), out DMUtils.maxMagneticOffered))
						DMUtils.maxMagneticOffered = 2;
					if (!int.TryParse(setNode.GetValue("Max_Magnetic_Active"), out DMUtils.maxMagneticActive))
						DMUtils.maxMagneticActive = 4;


					DMUtils.Logging("Contract Variables Set; Science Reward: {0} ; Completion Reward: {1} ; Forward Amount: {2} ; Penalty Amount: {3} ; Deadline Length: {4}",
						DMUtils.science, DMUtils.reward, DMUtils.forward, DMUtils.penalty, DMUtils.deadline);
					DMUtils.Logging("Max Contract Variables Set: Survey Offers: {0}; Active: {1} -- Asteroid Offers: {2}; Active: {3} -- Anomaly Offers: {4}; Active: {5} -- Magnetic Offers: {6}; Active: {7}",
						DMUtils.maxSurveyOffered, DMUtils.maxSurveyActive, DMUtils.maxAsteroidOffered, DMUtils.maxAsteroidActive, DMUtils.maxAnomalyOffered, DMUtils.maxAnomalyActive, DMUtils.maxMagneticOffered, DMUtils.maxMagneticActive);

					break;
				}
				else
					DMUtils.Logging("Broken Config.....");
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

						foreach (var sciType in Enum.GetValues(typeof(DMScienceType)))
						{
							string typeString = ((DMScienceType)sciType).ToString();
							if (!string.IsNullOrEmpty(typeString))
							{
								if (DMUtils.availableScience.ContainsKey(typeString))
								{
									if ((DMScienceType)sciType == DMScienceType.All)
									{
										if (!DMUtils.availableScience[typeString].ContainsKey(name))
											DMUtils.availableScience[typeString].Add(name, DMscience);
									}
									else if (((DMScienceType)type & (DMScienceType)sciType) == (DMScienceType)sciType)
									{
										if (!DMUtils.availableScience[typeString].ContainsKey(name))
											DMUtils.availableScience[typeString].Add(name, DMscience);
									}
								}
							}
						}

						//DMUtils.DebugLog("New Experiment: [{0}] Available For Contracts", name);
					}
				}
			}

			DMUtils.Logging("Successfully Added {0} New Experiments To Contract List", DMUtils.availableScience["All"].Count);

			//foreach (var sciType in Enum.GetValues(typeof(DMScienceType)))
			//{
			//	string type = ((DMScienceType)sciType).ToString();
			//	if (!string.IsNullOrEmpty(type))
			//	{
			//		if (DMUtils.availableScience.ContainsKey(type))
			//		{
			//			if (type != "All")
			//				DMUtils.DebugLog("Successfully Added {0} New {1} Experiments To Contract List", DMUtils.availableScience[type].Count, type);
			//		}
			//	}
			//}

			//Load in custom contract descriptions
			foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("DM_SCIENCE_STORY_DEF"))
			{
				if (node != null)
				{
					foreach (ConfigNode storyNode in node.GetNodes("DM_SCIENCE_BACKSTORY"))
					{
						if (storyNode != null)
						{
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

			DMUtils.Logging("Added {0} New Survey Backstories; {1} New Asteroid Backstories; {2} New Anomaly Backstories; {3} New Magnetic Backstories To The List", DMUtils.backStory["survey"].Count, DMUtils.backStory["asteroid"].Count, DMUtils.backStory["anomaly"].Count, DMUtils.backStory["magnetic"].Count);
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

			foreach (var sciType in Enum.GetValues(typeof(DMScienceType)))
			{
				string type = ((DMScienceType)sciType).ToString();
				if (!string.IsNullOrEmpty(type))
				{
					if (!DMUtils.availableScience.ContainsKey(type))
						DMUtils.availableScience[type] = new Dictionary<string, DMScienceContainer>();
					else
						DMUtils.Logging("");
				}
				else
					DMUtils.Logging("");
			}

			DMUtils.backStory = new Dictionary<string, List<string>>();
			DMUtils.backStory.Add("survey", new List<string>());
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
					break;
				}
			}
		}

		private void hackWaypointIcons()
		{
			foreach (GameDatabase.TextureInfo ti in GameDatabase.Instance.databaseTexture.Where(t => t.name.StartsWith(iconURL)))
			{
				if (ti != null)
				{
					string s = ti.name.Remove(0, iconURL.Length);
					ti.name = "Squad/Contracts/Icons/" + s;
					DMUtils.Logging("DMagic Icon [{0}] Inserted Into FinePrint Database", s);
				}
			}
		}

	}
}
