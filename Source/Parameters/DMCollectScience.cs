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
using DMagic.Contracts;

namespace DMagic.Parameters
{
	public class DMCollectScience : ContractParameter
	{
		private CelestialBody body;
		private ExperimentSituations scienceLocation;
		private DMScienceContainer scienceContainer;
		private float returnedScience;
		private string subject, name, biomeName, partName;
		private int type; //type 0: standard survey; type 1: long term survey; type 2: anomaly
		private bool registered;

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

			if (DMUtils.availableScience.ContainsKey("All"))
				DMUtils.availableScience["All"].TryGetValue(name, out scienceContainer);

			if (scienceContainer != null)
				partName = scienceContainer.SciPart;

			subject = string.Format("{0}@{1}{2}{3}", scienceContainer.Exp.id, body.name, scienceLocation, biomeName.Replace(" ", ""));
		}

		/// <summary>
		/// Used externally to return the name of the requested part
		/// </summary>
		/// <param name="cP">Instance of the requested Contract Parameter</param>
		/// <returns>Available Part name string</returns>
		public static string PartName(ContractParameter cP)
		{
			if (cP == null || cP.GetType() != typeof(DMCollectScience))
				return "";

			DMCollectScience Instance = (DMCollectScience)cP;
			return Instance.partName;
		}

		//Properties to be accessed by parent contract
		public CelestialBody Body
		{
			get { return body; }
		}

		public ExperimentSituations Situation
		{
			get { return scienceLocation; }
		}

		public string Biome
		{
			get { return biomeName; }
		}

		public string Subject
		{
			get { return subject; }
		}

		public DMScienceContainer Container
		{
			get { return scienceContainer; }
		}

		public string Name
		{
			get { return name; }
		}

		public float ReturnedScience
		{
			get { return returnedScience; }
		}

		protected override string GetHashString()
		{
			return body.name;
		}

		protected override string GetNotes()
		{
			if (type == 2)
				return "The Anomalous Signal Scanner can only be used from very close range; deploy and use the scanner to give an indication of range and direction to target";

			return base.GetNotes();
		}

		protected override string GetTitle()
		{
			if (scienceContainer == null)
				return "Whoops. Something bad happened here...";

			if (type == 2)
			{
				if (scienceLocation == ExperimentSituations.SrfLanded)
					return string.Format("{0} data from the surface near the anomalous signal", scienceContainer.Exp.experimentTitle);
				else if (scienceLocation == ExperimentSituations.FlyingLow)
					return string.Format("{0} data while flying above the anomalous signal", scienceContainer.Exp.experimentTitle);
				else
					return "Stupid Code Is Stupid";
			}
			else
			{
				if (!string.IsNullOrEmpty(biomeName))
				{
					if (scienceLocation == ExperimentSituations.InSpaceHigh)
						return string.Format("{0} data from high orbit above {1}'s {2}", scienceContainer.Exp.experimentTitle, body.theName, biomeName);
					else if (scienceLocation == ExperimentSituations.InSpaceLow)
						return string.Format("{0} data from low orbit above {1}'s {2}", scienceContainer.Exp.experimentTitle, body.theName, biomeName);
					else if (scienceLocation == ExperimentSituations.SrfLanded)
						return string.Format("{0} data from the surface at {1}'s {2}", scienceContainer.Exp.experimentTitle, body.theName, biomeName);
					else if (scienceLocation == ExperimentSituations.SrfSplashed)
						return string.Format("{0} data from the oceans at {1}'s {2}", scienceContainer.Exp.experimentTitle, body.theName, biomeName);
					else if (scienceLocation == ExperimentSituations.FlyingHigh)
						return string.Format("{0} data during high altitude flight over {1}'s {2}", scienceContainer.Exp.experimentTitle, body.theName, biomeName);
					else if (scienceLocation == ExperimentSituations.FlyingLow)
						return string.Format("{0} data during low altitude flight over {1}'s {2}", scienceContainer.Exp.experimentTitle, body.theName, biomeName);
				}
				else
				{
					if (scienceLocation == ExperimentSituations.InSpaceHigh)
						return string.Format("{0} data from high orbit above {1}", scienceContainer.Exp.experimentTitle, body.theName);
					else if (scienceLocation == ExperimentSituations.InSpaceLow)
						return string.Format("{0} data from low orbit above {1}", scienceContainer.Exp.experimentTitle, body.theName);
					else if (scienceLocation == ExperimentSituations.SrfLanded)
						return string.Format("{0} data from the surface of {1}", scienceContainer.Exp.experimentTitle, body.theName);
					else if (scienceLocation == ExperimentSituations.SrfSplashed)
						return string.Format("{0} data from the oceans of {1}", scienceContainer.Exp.experimentTitle, body.theName);
					else if (scienceLocation == ExperimentSituations.FlyingHigh)
						return string.Format("{0} data during high altitude flight at {1}", scienceContainer.Exp.experimentTitle, body.theName);
					else if (scienceLocation == ExperimentSituations.FlyingLow)
						return string.Format("{0} data during low altitude flight at {1}", scienceContainer.Exp.experimentTitle, body.theName);
				}
				return "Stupid Code Is Stupid";
			}
		}

		protected override void OnRegister()
		{
			if (registered)
				return;

			GameEvents.OnScienceRecieved.Add(scienceReceive);
			if (type == 2)
				DMUtils.OnAnomalyScience.Add(anomalyReceive);

			registered = true;
		}

		protected override void OnUnregister()
		{
			if (!registered)
				return;

			registered = false;

			GameEvents.OnScienceRecieved.Remove(scienceReceive);
			if (type == 2)
				DMUtils.OnAnomalyScience.Remove(anomalyReceive);
		}

		protected override void OnSave(ConfigNode node)
		{
			node.AddValue("Name", name);
			node.AddValue("Body", body.flightGlobalsIndex);
			node.AddValue("Situation", (int)scienceLocation);
			node.AddValue("Biome", biomeName);
			node.AddValue("Type", type);
			node.AddValue("Returned_Science", returnedScience);
		}

		protected override void OnLoad(ConfigNode node)
		{
			int targetSituation = node.parse("Situation", (int)65);
			if (targetSituation >= 65 || targetSituation <= 0)
			{
				removeThis("Failed To Load Situation Variables; Collect Science Parameter Removed");
				return;
			}
			scienceLocation = (ExperimentSituations)targetSituation;

			body = node.parse("Body", (CelestialBody)null);

			if (body == null)
			{
				removeThis("Failed To Load Target Body; Collect Science Parameter Removed");
				return;
			}

			type = node.parse("Type", (int)1000);
			if (type == 1000)
			{
				removeThis("Failed To Load Contract Type Value; Collect Science Parameter Reset");
				type = 1;
			}

			name = node.parse("Name", "");
			if (string.IsNullOrEmpty(name))
			{
				removeThis("Failed To Load Science Container Variables; Collect Science Parameter Removed");
				return;
			}

			if (!DMUtils.availableScience.ContainsKey("All"))
			{
				removeThis("Failed To Load Science Container Variables; Collect Science Parameter Removed");
				return;
			}

			DMUtils.availableScience["All"].TryGetValue(name, out scienceContainer);
			if (scienceContainer == null)
			{
				removeThis("Failed To Load Science Container Variables; Collect Science Parameter Removed");
				return;
			}
			else
				partName = scienceContainer.SciPart;

			biomeName = node.parse("Biome", "");

			returnedScience = node.parse("Returned_Science", (float)0);

			subject = string.Format("{0}@{1}{2}{3}", scienceContainer.Exp.id, body.name, scienceLocation, biomeName.Replace(" ", ""));
		}

		private void removeThis(string message)
		{
			this.Unregister();
			this.Parent.RemoveParameter(this);
			DMUtils.Logging(message);
		}

		private void anomalyReceive(CelestialBody Body, string exp, string biome)
		{
			if (this.Root.ContractState != Contract.State.Active)
				return;

			if (Body == null)
				return;

			if (body == Body && exp == scienceContainer.Exp.id && biomeName.Replace(" ", "") == biome)
				ScreenMessages.PostScreenMessage("Results From Anomalous Signal Recovered", 6f, ScreenMessageStyle.UPPER_CENTER);
		}

		private void scienceReceive(float sci, ScienceSubject sub, ProtoVessel pv, bool reverse)
		{
			if (this.Root.ContractState != Contract.State.Active)
				return;

			if (sub == null)
				return;

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
							if (returnedScience >= scienceContainer.Exp.baseValue * scienceContainer.Transmit * sub.subjectValue * 0.3f)
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
							if (returnedScience >= scienceContainer.Exp.baseValue * scienceContainer.Transmit * sub.subjectValue * 0.3f)
							{
								ScreenMessages.PostScreenMessage("DMagic Orbital Science Survey Parameter Complete", 4f, ScreenMessageStyle.UPPER_CENTER);
								base.SetComplete();
							}
							else
							{
								if (DMUtils.biomeRelevant(this.Situation, (int)this.Container.Exp.biomeMask))
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
