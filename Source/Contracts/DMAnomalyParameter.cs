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
	class DMAnomalyParameter: ContractParameter
	{
		private CelestialBody body;
		private PQSCity city;
		private ExperimentSituations situation;
		private DMScienceContainer scienceContainer;
		private Vessel v;
		private Vector3d anomPosition;
		private Vector3d recoveryPosition;
		private string name;
		private string subject;
		private string hash;
		private bool collected = false;

		public DMAnomalyParameter()
		{
		}

		internal DMAnomalyParameter(CelestialBody Body, PQSCity City, ExperimentSituations Situation, string Name)
		{
			body = Body;
			situation = Situation;
			name = Name;
			city = City;
			anomPosition = city.transform.position;
			DMUtils.availableScience["All"].TryGetValue(name, out scienceContainer);
			subject = string.Format("{0}@{1}{2}{3}", scienceContainer.exp.id, body.name, situation, "");
			hash = city.name;
		}

		internal PQSCity City
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
				return string.Format("Gather {0} data from the surface on the anomalous signal emanating from {1}", scienceContainer.exp.experimentTitle, body.theName);
			else if (situation == ExperimentSituations.InSpaceLow || situation == ExperimentSituations.FlyingLow)
				return string.Format("Gather {0} data from above on the anomalous signal emanating from {1}", scienceContainer.exp.experimentTitle, body.theName);
			else
				return "Fix Your Stupid Code Idiot";
		}

		protected override void OnRegister()
		{
			GameEvents.OnScienceRecieved.Add(anomalyScience);
		}

		protected override void OnUnregister()
		{
			GameEvents.OnScienceRecieved.Remove(anomalyScience);
		}

		protected override void OnSave(ConfigNode node)
		{
			DMUtils.DebugLog("Saving Anomaly Parameter");
			node.AddValue("Target_Anomaly", string.Format("{0}|{1}|{2}|{3}|{4}", hash, body.flightGlobalsIndex, name, (int)situation, collected));
		}

		protected override void OnLoad(ConfigNode node)
		{
			DMUtils.DebugLog("Loading Anomaly Parameter");
			DMUtils.newExp = "";
			int bodyID, sitID;
			string[] anomalyString = node.GetValue("Target_Anomaly").Split('|');
			hash = anomalyString[0];
			if (int.TryParse(anomalyString[1], out bodyID))
				body = FlightGlobals.Bodies[bodyID];
			else
			{
				DMUtils.Logging("Failed To Load Anomaly Contract Parameter; Parameter Removed");
				this.Root.RemoveParameter(this);
			}
			name = anomalyString[2];
			DMUtils.availableScience["All"].TryGetValue(name, out scienceContainer);
			if (int.TryParse(anomalyString[3], out sitID))
				situation = (ExperimentSituations)sitID;
			else
			{
				DMUtils.Logging("Failed To Load Anomaly Contract Parameter; Parameter Removed");
				this.Root.RemoveParameter(this);
			}
			if (!bool.TryParse(anomalyString[4], out collected))
			{
				DMUtils.Logging("Failed To Load Anomaly Contract Parameter; Parameter Removed");
				this.Root.RemoveParameter(this);
			}
			if (HighLogic.LoadedSceneIsFlight)
			{
				try
				{
					city = (UnityEngine.Object.FindObjectsOfType(typeof(PQSCity)) as PQSCity[]).FirstOrDefault(c => c.name == hash);
					anomPosition = city.transform.position;
				}
				catch
				{
					DMUtils.Logging("Failed To Load Anomaly Contract Parameter; Parameter Removed");
					this.Root.RemoveParameter(this);
				}
				v = FlightGlobals.ActiveVessel;
			}
			subject = string.Format("{0}@{1}{2}{3}", scienceContainer.exp.id, body.name, situation, "");
		}

		protected override void OnUpdate()
		{
			if (this.Root.ContractState == Contract.State.Active && HighLogic.LoadedSceneIsFlight && FlightGlobals.ready)
			{
				if (setExp(DMUtils.newExp))
				{
					DMUtils.DebugLog("Checking Distance To Anomaly");
					//Calculate distance to the anomaly on science collection
					if (v.mainBody == body)
					{
						recoveryPosition = v.transform.position;
						double valt = v.mainBody.GetAltitude(recoveryPosition);
						double anomAlt = v.mainBody.GetAltitude(anomPosition);
						double verticalD = anomAlt - valt;
						double totalD = (anomPosition - recoveryPosition).magnitude;
						double horizantalD = Math.Sqrt((totalD * totalD) - (verticalD * verticalD));

						//Draw a cone above the anomaly position up to 100km with a diametere of 15km at its widest
						if (situation == ExperimentSituations.FlyingLow || situation == ExperimentSituations.InSpaceLow || situation == ExperimentSituations.FlyingHigh)
						{
							if (Math.Abs(verticalD) > 1000 && verticalD < 100000)
							{
								if (horizantalD < (15000 * (verticalD / 100000)))
								{
									ScreenMessages.PostScreenMessage("Results from Anomalous Signal recovered", 6f, ScreenMessageStyle.UPPER_CENTER);
									collected = true;
								}
								else
									ScreenMessages.PostScreenMessage("No anomalies detected in this area, try again when closer", 6f, ScreenMessageStyle.UPPER_CENTER);
							}
							else if (Math.Abs(verticalD) < 1000)
							{
								if (horizantalD < 150)
								{
									ScreenMessages.PostScreenMessage("Results from Anomalous Signal recovered", 6f, ScreenMessageStyle.UPPER_CENTER);
									collected = true;
								}
								else
									ScreenMessages.PostScreenMessage("No anomalies detected in this area, try again when closer", 6f, ScreenMessageStyle.UPPER_CENTER);
							}
						}
						else if (situation == ExperimentSituations.SrfLanded)
							if (horizantalD < 50)
							{
								ScreenMessages.PostScreenMessage("Results from Anomalous Signal recovered", 6f, ScreenMessageStyle.UPPER_CENTER);
								collected = true;
							}
							else
								ScreenMessages.PostScreenMessage("No anomalies detected in this area, try again when closer", 6f, ScreenMessageStyle.UPPER_CENTER);
					}
					DMUtils.newExp = "";
				}
			}
		}

		//Event triggered by an experiment activating
		private bool setExp(string s)
		{
			if (string.IsNullOrEmpty(s))
				return false;
			else
			{
				DMUtils.DebugLog("Matching Experiment Names");
				if (s == scienceContainer.exp.id)
					return true;
				else
					return false;
			}
		}

		private void anomalyScience(float sci, ScienceSubject sub)
		{
			if (collected)
			{
				string clippedSub = sub.id.Replace("@", "");
				string clippedTargetSub = subject.Replace("@", "");
				DMUtils.DebugLog("Comparing New Strings [{0}] And [{1}]", clippedSub, clippedTargetSub);
				if (clippedSub.StartsWith(clippedTargetSub))
				{
					DMUtils.DebugLog("Anomaly Contract Complete");
					base.SetComplete();
				}
			}
		}
	}
}
