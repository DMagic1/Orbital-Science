#region license
/* DMagic Orbital Science - DMCollectScience
 * Contract Parameter for science collection
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
using Contracts;
using Contracts.Parameters;

namespace DMagic
{
	public class DMCollectScience : ContractParameter
	{
		private CelestialBody body;
		private ExperimentSituations scienceLocation;
		private DMScienceContainer scienceContainer;
		private float returnedScience;
		private string subject, name, biomeName, partName;
		private int type; //type 0: standard survey; type 1: biological survey; type 2: anomaly

		public DMCollectScience()
		{
		}

		internal DMCollectScience(CelestialBody target, ExperimentSituations location, string BiomeName, string Name, int Type)
		{
			body = target;
			scienceLocation = location;
			name = Name;
			biomeName = BiomeName;
			type = Type;
			returnedScience = 0f;
			DMUtils.availableScience["All"].TryGetValue(name, out scienceContainer);
			partName = scienceContainer.sciPart;
			subject = string.Format("{0}@{1}{2}{3}", scienceContainer.exp.id, body.name, scienceLocation, biomeName.Replace(" ", ""));
		}

		/// <summary>
		/// Used externally to return the name of the requested part
		/// </summary>
		/// <param name="cP">Instance of the requested Contract Parameter</param>
		/// <returns>Available Part name string</returns>
		public static string PartName(ContractParameter cP)
		{
			DMCollectScience Instance = (DMCollectScience)cP;
			return Instance.partName;
		}

		//Properties to be accessed by parent contract
		internal CelestialBody Body
		{
			get { return body; }
			private set { }
		}

		internal ExperimentSituations Situation
		{
			get { return scienceLocation; }
			private set { }
		}

		internal string Biome
		{
			get { return biomeName; }
			private set { }
		}

		internal string Subject
		{
			get { return subject; }
			private set { }
		}

		internal DMScienceContainer Container
		{
			get { return scienceContainer; }
			private set { }
		}

		internal string Name
		{
			get { return name; }
			private set { }
		}

		internal float ReturnedScience
		{
			get { return returnedScience; }
			private set { }
		}

		protected override string GetHashString()
		{
			return body.name;
		}

		protected override string GetTitle()
		{
			if (type == 2)
			{
				if (scienceLocation == ExperimentSituations.SrfLanded)
					return string.Format("{0} data from the surface near the anomalous signal", scienceContainer.exp.experimentTitle);
				else if (scienceLocation == ExperimentSituations.FlyingLow)
					return string.Format("{0} data while flying above the anomalous signal", scienceContainer.exp.experimentTitle);
				else
					return "Stupid Code Is Stupid";
			}
			else
			{
				if (!string.IsNullOrEmpty(biomeName))
				{
					if (scienceLocation == ExperimentSituations.InSpaceHigh)
						return string.Format("{0} data from high orbit above {1}'s {2}", scienceContainer.exp.experimentTitle, body.theName, biomeName);
					else if (scienceLocation == ExperimentSituations.InSpaceLow)
						return string.Format("{0} data from low orbit above {1}'s {2}", scienceContainer.exp.experimentTitle, body.theName, biomeName);
					else if (scienceLocation == ExperimentSituations.SrfLanded)
						return string.Format("{0} data from the surface at {1}'s {2}", scienceContainer.exp.experimentTitle, body.theName, biomeName);
					else if (scienceLocation == ExperimentSituations.SrfSplashed)
						return string.Format("{0} data from the oceans at {1}'s {2}", scienceContainer.exp.experimentTitle, body.theName, biomeName);
					else if (scienceLocation == ExperimentSituations.FlyingHigh)
						return string.Format("{0} data during high altitude flight over {1}'s {2}", scienceContainer.exp.experimentTitle, body.theName, biomeName);
					else if (scienceLocation == ExperimentSituations.FlyingLow)
						return string.Format("{0} data during low altitude flight over {1}'s {2}", scienceContainer.exp.experimentTitle, body.theName, biomeName);
				}
				else
				{
					if (scienceLocation == ExperimentSituations.InSpaceHigh)
						return string.Format("{0} data from high orbit above {1}", scienceContainer.exp.experimentTitle, body.theName);
					else if (scienceLocation == ExperimentSituations.InSpaceLow)
						return string.Format("{0} data from low orbit above {1}", scienceContainer.exp.experimentTitle, body.theName);
					else if (scienceLocation == ExperimentSituations.SrfLanded)
						return string.Format("{0} data from the surface of {1}", scienceContainer.exp.experimentTitle, body.theName);
					else if (scienceLocation == ExperimentSituations.SrfSplashed)
						return string.Format("{0} data from the oceans of {1}", scienceContainer.exp.experimentTitle, body.theName);
					else if (scienceLocation == ExperimentSituations.FlyingHigh)
						return string.Format("{0} data during high altitude flight at {1}", scienceContainer.exp.experimentTitle, body.theName);
					else if (scienceLocation == ExperimentSituations.FlyingLow)
						return string.Format("{0} data during low altitude flight at {1}", scienceContainer.exp.experimentTitle, body.theName);
				}
				return "Stupid Code Is Stupid";
			}
		}

		protected override void OnRegister()
		{
			GameEvents.OnScienceRecieved.Add(scienceReceive);
			if (type == 2)
				DMUtils.OnAnomalyScience.Add(anomalyReceive);
		}

		protected override void OnUnregister()
		{
			GameEvents.OnScienceRecieved.Remove(scienceReceive);
			if (type == 2)
				DMUtils.OnAnomalyScience.Remove(anomalyReceive);
		}

		protected override void OnSave(ConfigNode node)
		{
			node.AddValue("Science_Subject", string.Format("{0}|{1}|{2}|{3}|{4}|{5}", type, name, body.flightGlobalsIndex, (int)scienceLocation, biomeName, returnedScience));
		}

		protected override void OnLoad(ConfigNode node)
		{
			if (DMScienceScenario.SciScenario != null)
				if (DMScienceScenario.SciScenario.contractsReload)
					DMUtils.resetContracts();
			int targetBodyID, targetSituation;
			string[] scienceString = node.GetValue("Science_Subject").Split('|');
			if (!int.TryParse(scienceString[0], out type))
			{
				DMUtils.Logging("Failed To Load Contract Parameter; Parameter Reset");
				type = 1;
			}
			name = scienceString[1];
			DMUtils.availableScience["All"].TryGetValue(name, out scienceContainer);
			if (scienceContainer == null)
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Removed");
				this.Unregister();
				this.Root.RemoveParameter(this);
			}
			else
				partName = scienceContainer.sciPart;
			if (int.TryParse(scienceString[2], out targetBodyID))
				body = FlightGlobals.Bodies[targetBodyID];
			else
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Removed");
				this.Unregister();
				this.Root.RemoveParameter(this);
			}
			if (int.TryParse(scienceString[3], out targetSituation))
				scienceLocation = (ExperimentSituations)targetSituation;
			else
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Removed");
				this.Unregister();
				this.Root.RemoveParameter(this);
			}
			biomeName = scienceString[4];
			if (!float.TryParse(scienceString[5], out returnedScience))
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Reset");
				returnedScience = 0;
			}
			subject = string.Format("{0}@{1}{2}{3}", scienceContainer.exp.id, body.name, scienceLocation, biomeName.Replace(" ", ""));
		}

		private void anomalyReceive(CelestialBody Body, string exp, string biome)
		{
			if (body == Body && exp == scienceContainer.exp.id && biomeName.Replace(" ", "") == biome)
				ScreenMessages.PostScreenMessage("Results From Anomalous Signal Recovered", 6f, ScreenMessageStyle.UPPER_CENTER);
		}

		private void scienceReceive(float sci, ScienceSubject sub)
		{
			if (type == 0 || type == 2)
			{
				if (!string.IsNullOrEmpty(biomeName))
				{
					if (sub.id == subject)
					{
						if (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION)
							base.SetComplete();
						else
						{
							returnedScience += sci;
							if (returnedScience >= scienceContainer.exp.baseValue * scienceContainer.transmit * sub.subjectValue * 0.3f)
							{
								ScreenMessages.PostScreenMessage("DMagic Orbital Science Survey Parameter Complete", 4f, ScreenMessageStyle.UPPER_CENTER);
								base.SetComplete();
							}
							else
								ScreenMessages.PostScreenMessage("This area has already been studied, try investigating another region to complete the contract", 6f, ScreenMessageStyle.UPPER_CENTER);
						}
					}
				}
				else
				{
					if (sub.id.Contains(subject))
					{
						if (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION)
							base.SetComplete();
						else
						{
							returnedScience += sci;
							if (returnedScience >= scienceContainer.exp.baseValue * scienceContainer.transmit * sub.subjectValue * 0.3f)
							{
								ScreenMessages.PostScreenMessage("DMagic Orbital Science Survey Parameter Complete", 4f, ScreenMessageStyle.UPPER_CENTER);
								base.SetComplete();
							}
							else
							{
								if (DMUtils.biomeRelevant(this.Situation, this.Container.bioMask))
									ScreenMessages.PostScreenMessage("This area has already been studied, try investigating another region to complete the contract", 6f, ScreenMessageStyle.UPPER_CENTER);
								else
									ScreenMessages.PostScreenMessage("Not enough science remaining; this experiment may need to be returned to Kerbin for credit", 6f, ScreenMessageStyle.UPPER_CENTER);
							}
						}
					}
				}
			}
			else if (type == 1)
			{
				if (sub.id.Contains(subject))
					base.SetComplete();
			}
		}

	}
}
