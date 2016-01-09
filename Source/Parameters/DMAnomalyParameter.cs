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
using DMagic.Scenario;
using DMagic.Contracts;

namespace DMagic.Parameters
{
	public class DMAnomalyParameter: ContractParameter
	{
		private ExperimentSituations situation;
		private DMScienceContainer scienceContainer;
		private DMAnomalyContract root;
		private string name, partName;
		private bool collected = false;

		public DMAnomalyParameter()
		{
		}

		internal DMAnomalyParameter(ExperimentSituations Situation, string Name)
		{
			situation = Situation;
			name = Name;
			DMUtils.availableScience["All"].TryGetValue(name, out scienceContainer);
			partName = scienceContainer.SciPart;
		}

		/// <summary>
		/// Used externally to return the name of the requested part
		/// </summary>
		/// <param name="cP">Instance of the requested Contract Parameter</param>
		/// <returns>Available Part name string</returns>
		public static string PartName(ContractParameter cP)
		{
			if (cP == null || cP.GetType() != typeof(DMAnomalyParameter))
				return "";

			DMAnomalyParameter aP = (DMAnomalyParameter)cP;
			return aP.partName;
		}

		public ExperimentSituations Situation
		{
			get { return situation; }
		}

		public string Name
		{
			get { return name; }
		}

		public DMScienceContainer Container
		{
			get { return scienceContainer; }
		}

		protected override string GetHashString()
		{
			return name;
		}

		protected override string GetTitle()
		{
			if (situation == ExperimentSituations.SrfLanded)
				return string.Format("{0} data from the surface near the anomalous signal", scienceContainer.Exp.experimentTitle);
			else if (situation == ExperimentSituations.InSpaceLow || situation == ExperimentSituations.FlyingLow)
				return string.Format("{0} data from above near the anomalous signal", scienceContainer.Exp.experimentTitle);
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
			node.AddValue("Name", name);
			node.AddValue("Situation", (int)situation);
			node.AddValue("Collected", collected);
		}

		protected override void OnLoad(ConfigNode node)
		{
			int sitID = node.parse("Situation", (int)65);
			if (sitID >= 65 || sitID <= 0)
			{
				DMUtils.Logging("Failed To Load Anomaly Contract Situation Value; Parameter Set To Complete");
				this.SetComplete();
			}
			situation = (ExperimentSituations)sitID;

			collected = node.parse("Collected", (bool)true);
			if (collected)
				this.SetComplete();

			name = node.parse("Name", "");
			if (string.IsNullOrEmpty(name))
			{
				DMUtils.Logging("Failed To Load Anomaly Contract Science Container Variables; Parameter Set To Complete");
				this.SetComplete();
			}

			DMUtils.availableScience["All"].TryGetValue(name, out scienceContainer);
			if (scienceContainer == null)
			{
				DMUtils.Logging("Failed To Load Anomaly Contract Science Container Variables; Parameter Set To Complete");
				this.SetComplete();
			}
			else
				partName = scienceContainer.SciPart;

			try
			{
				root = (DMAnomalyContract)this.Root;
			}
			catch (Exception e)
			{
				this.Unregister();
				this.Parent.RemoveParameter(this);
				DMUtils.Logging("Could not find root anomaly contract; removing DMAnomalyParameter\n{0}", e);
				return;
			}
		}

		private void monitorAnomScience(CelestialBody B, string s, string name)
		{
			if (FlightGlobals.currentMainBody == B)
			{
				if (s == scienceContainer.Exp.id)
				{
					DMAnomalyList.updateAnomaly(FlightGlobals.ActiveVessel, root.TargetAnomaly);
					DMUtils.Logging("Distance To Anomaly: {0} ; Altitude Above Anomaly: {1} ; Horizontal Distance To Anomaly: {2}", root.TargetAnomaly.VDistance, root.TargetAnomaly.VHeight, root.TargetAnomaly.VHorizontal);

					//Draw a cone above the anomaly position up to 100km with a diameter of 60km at its widest
					if (root.TargetAnomaly.VDistance < 100000)
					{
						if (situation == ExperimentSituations.FlyingLow || situation == ExperimentSituations.InSpaceLow || situation == ExperimentSituations.FlyingHigh)
						{
							if (root.TargetAnomaly.VHeight > 625 && root.TargetAnomaly.VHeight < 100000)
							{
								double vHeight = root.TargetAnomaly.VHeight;
								if (vHeight > 50000) vHeight = 50000;
								if (root.TargetAnomaly.VHorizontal < (60000 * (root.TargetAnomaly.VHeight / 50000)))
								{
									ScreenMessages.PostScreenMessage("Results From Anomalous Signal Recovered", 6f, ScreenMessageStyle.UPPER_CENTER);
									collected = true;
								}
								else
									ScreenMessages.PostScreenMessage("Anomalous signal too weak, try again when closer", 6f, ScreenMessageStyle.UPPER_CENTER);
							}
							else if (root.TargetAnomaly.VHeight < 625)
							{
								if (root.TargetAnomaly.VHorizontal < 750)
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
							if (root.TargetAnomaly.VHorizontal < 500)
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

		private void anomalyScience(float sci, ScienceSubject sub, ProtoVessel pv, bool reverse)
		{
			if (sub.id.Contains(string.Format("{0}@{1}{2}", scienceContainer.Exp.id, root.TargetAnomaly.Body.name, situation)))
			{
				if (collected)
					base.SetComplete();
			}
		}
	}
}
