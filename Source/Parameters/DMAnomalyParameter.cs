#region license
/* DMagic Orbital Science - DMAnomalyParameter
 * Anomaly Contract Parameter
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
using Contracts;
using Contracts.Parameters;

namespace DMagic
{
	public class DMAnomalyParameter: ContractParameter
	{
		private CelestialBody body;
		private DMAnomalyObject city;
		private ExperimentSituations situation;
		private DMScienceContainer scienceContainer;
		private string name, subject, hash, partName;
		private bool collected = false;

		public DMAnomalyParameter()
		{
		}

		internal DMAnomalyParameter(CelestialBody Body, DMAnomalyObject City, ExperimentSituations Situation, string Name)
		{
			body = Body;
			situation = Situation;
			name = Name;
			city = City;
			DMUtils.availableScience["All"].TryGetValue(name, out scienceContainer);
			partName = scienceContainer.sciPart;
			subject = string.Format("{0}@{1}{2}", scienceContainer.exp.id, body.name, situation);
			hash = city.name;
		}

		/// <summary>
		/// Used externally to return the name of the requested part
		/// </summary>
		/// <param name="cP">Instance of the requested Contract Parameter</param>
		/// <returns>Available Part name string</returns>
		public static string PartName(ContractParameter cP)
		{
			DMAnomalyParameter aP = (DMAnomalyParameter)cP;
			return aP.partName;
		}

		internal DMAnomalyObject City
		{
			get
			{
				if (city != null)
					return city;
				else
					return null;
			}
			private set { }
		}

		internal CelestialBody Body
		{
			get { return body; }
			private set { }
		}

		internal ExperimentSituations Situation
		{
			get { return situation; }
			private set { }
		}

		internal string Name
		{
			get { return name; }
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

		protected override string GetHashString()
		{
			return hash;
		}

		protected override string GetTitle()
		{
			if (situation == ExperimentSituations.SrfLanded)
				return string.Format("{0} data from the surface near the anomalous signal", scienceContainer.exp.experimentTitle, body.theName);
			else if (situation == ExperimentSituations.InSpaceLow || situation == ExperimentSituations.FlyingLow)
				return string.Format("{0} data from above near the anomalous signal", scienceContainer.exp.experimentTitle, body.theName);
			else
				return "Fix Your Stupid Code Idiot";
		}

		protected override void OnRegister()
		{
			GameEvents.OnScienceRecieved.Add(anomalyScience);
			DMUtils.OnAnomalyScience.Add(monitorAnomScience);
		}

		protected override void OnUnregister()
		{
			GameEvents.OnScienceRecieved.Remove(anomalyScience);
			DMUtils.OnAnomalyScience.Remove(monitorAnomScience);
		}

		protected override void OnSave(ConfigNode node)
		{
			node.AddValue("Target_Anomaly", string.Format("{0}|{1}|{2}|{3}|{4}", hash, body.flightGlobalsIndex, name, (int)situation, collected));
		}

		protected override void OnLoad(ConfigNode node)
		{
			//if (DMScienceScenario.SciScenario != null)
			//	if (DMScienceScenario.SciScenario.contractsReload)
			//		DMUtils.resetContracts();
			int bodyID, sitID;
			string[] anomalyString = node.GetValue("Target_Anomaly").Split('|');
			hash = anomalyString[0];
			if (int.TryParse(anomalyString[1], out bodyID))
				body = FlightGlobals.Bodies[bodyID];
			else
			{
				DMUtils.Logging("Failed To Load Anomaly Contract Parameter; Parameter Removed");
				this.Unregister();
				this.Root.RemoveParameter(this);
			}
			name = anomalyString[2];
			DMUtils.availableScience["All"].TryGetValue(name, out scienceContainer);
			if (scienceContainer == null)
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Removed");
				this.Unregister();
				this.Root.RemoveParameter(this);
			}
			else
				partName = scienceContainer.sciPart;
			if (int.TryParse(anomalyString[3], out sitID))
				situation = (ExperimentSituations)sitID;
			else
			{
				DMUtils.Logging("Failed To Load Anomaly Contract Parameter; Parameter Removed");
				this.Unregister();
				this.Root.RemoveParameter(this);
			}
			if (!bool.TryParse(anomalyString[4], out collected))
			{
				DMUtils.Logging("Failed To Load Anomaly Contract Parameter; Reset Parameter");
				collected = false;
			}
			if (HighLogic.LoadedSceneIsFlight)
			{
				try
				{
					city = new DMAnomalyObject((UnityEngine.Object.FindObjectsOfType(typeof(PQSCity)) as PQSCity[]).FirstOrDefault(c => c.name == hash && c.transform.parent.name == body.name));
				}
				catch
				{
					DMUtils.Logging("Failed To Load Anomaly Contract Parameter; Parameter Removed");
					this.Unregister();
					this.Root.RemoveParameter(this);
				}
			}
			subject = string.Format("{0}@{1}{2}", scienceContainer.exp.id, body.name, situation);
		}

		private void monitorAnomScience(CelestialBody B, string s, string name)
		{
			if (FlightGlobals.currentMainBody == B)
			{
				if (s == scienceContainer.exp.id)
				{
					DMAnomalyList.updateAnomaly(FlightGlobals.ActiveVessel, city);
					DMUtils.Logging("Distance To Anomaly: {0} ; Altitude Above Anomaly: {1} ; Horizontal Distance To Anomaly: {2}", city.Vdistance, city.Vheight, city.Vhorizontal);

					//Draw a cone above the anomaly position up to 100km with a diameter of 60km at its widest
					if (city.Vdistance < 100000)
					{
						if (situation == ExperimentSituations.FlyingLow || situation == ExperimentSituations.InSpaceLow || situation == ExperimentSituations.FlyingHigh)
						{
							if (city.Vheight > 625 && city.Vheight < 100000)
							{
								double vHeight = city.Vheight;
								if (vHeight > 50000) vHeight = 50000;
								if (city.Vhorizontal < (60000 * (city.Vheight / 50000)))
								{
									ScreenMessages.PostScreenMessage("Results From Anomalous Signal Recovered", 6f, ScreenMessageStyle.UPPER_CENTER);
									collected = true;
								}
								else
									ScreenMessages.PostScreenMessage("Anomalous signal too weak, try again when closer", 6f, ScreenMessageStyle.UPPER_CENTER);
							}
							else if (city.Vheight < 625)
							{
								if (city.Vhorizontal < 750)
								{
									ScreenMessages.PostScreenMessage("Results From Anomalous Signal Recovered", 6f, ScreenMessageStyle.UPPER_CENTER);
									collected = true;
								}
								else
									ScreenMessages.PostScreenMessage("Anomalous signal too weak, try again when closer", 6f, ScreenMessageStyle.UPPER_CENTER);
							}
						}
						else if (situation == ExperimentSituations.SrfLanded)
						{
							if (city.Vhorizontal < 500)
							{
								ScreenMessages.PostScreenMessage("Results From Anomalous Signal Recovered", 6f, ScreenMessageStyle.UPPER_CENTER);
								collected = true;
							}
							else
								ScreenMessages.PostScreenMessage("Anomalous signal too weak, try again when closer", 6f, ScreenMessageStyle.UPPER_CENTER);
						}
					}
				}
			}
		}

		private void anomalyScience(float sci, ScienceSubject sub)
		{
			if (sub.id.Contains(subject))
			{
				if (collected)
					base.SetComplete();
			}
		}
	}
}
