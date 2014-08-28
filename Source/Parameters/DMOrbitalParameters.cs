#region license
/* DMagic Orbital Science - DMOrbitalParameters
 * Parameter To Track Orbital Parameters of a Specific Vessel
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
	class DMOrbitalParameters: ContractParameter
	{
		private CelestialBody body;
		private Vessel newV;
		private List<Vessel> suitableV = new List<Vessel>();
		private List<Vessel> removeV = new List<Vessel>();
		private string vName;
		private double orbitalParameter;
		private int type, timer; //type 0 is eccentricity tracker; type 1 is inclination tracker
		private bool modifiedByDocking, modifiedByUnDocking;

		public DMOrbitalParameters()
		{
		}

		internal DMOrbitalParameters(CelestialBody Body, double Param, int Type)
		{
			body = Body;
			orbitalParameter = Param;
			type = Type;
			this.disableOnStateChange = false;
		}

		//Properties to be accessed by parent contract
		internal CelestialBody Body
		{
			get { return body; }
			private set { }
		}

		internal double OrbitalParameter
		{
			get { return orbitalParameter; }
			private set { }
		}

		protected override string GetHashString()
		{
			return body.name;
		}

		protected override string GetTitle()
		{
			if (type == 0)
				return string.Format("Orbit with at least {0:N2} eccentricity", orbitalParameter);
			else if (type == 1)
				return string.Format("Orbit of at least {0:N1}° inclination", orbitalParameter);
			else
				return "Stupid things";
		}

		protected override void OnRegister()
		{
			GameEvents.VesselSituation.onOrbit.Add(vesselOrbit);
			GameEvents.onVesselCreate.Add(newVesselCheck);
			GameEvents.onPartCouple.Add(dockCheck);
		}

		protected override void OnUnregister()
		{
			GameEvents.VesselSituation.onOrbit.Remove(vesselOrbit);
			GameEvents.onVesselCreate.Remove(newVesselCheck);
			GameEvents.onPartCouple.Remove(dockCheck);
		}

		protected override void OnSave(ConfigNode node)
		{
			if (HighLogic.LoadedSceneIsEditor)
				node.AddValue("Orbital_Parameter", string.Format("{0}|{1}|{2}|{3:N3}", type, body.flightGlobalsIndex, vName, orbitalParameter));
			else if (suitableV.Count > 0)
			{
				vName = stringConcat(suitableV);
				node.AddValue("Orbital_Parameter", string.Format("{0}|{1}|{2}|{3:N3}", type, body.flightGlobalsIndex, vName, orbitalParameter));
			}
			else
				node.AddValue("Orbital_Parameter", string.Format("{0}|{1}|{2}|{3:N3}", type, body.flightGlobalsIndex, vName, orbitalParameter));
		}

		private string stringConcat(List<Vessel> source)
		{
			int i = source.Count;
			if (i == 0)
				return "";
			string[] s = new string[i];
			for (int j = 0; j < i; j++)
			{
				s[j] = source[j].id.ToString() + ",";
			}
			return string.Concat(s).TrimEnd(',');
		}

		private List<Guid> stringSplitGuid(string source)
		{
			if (source == "")
				return new List<Guid>();
			string[] s = source.Split(',');
			List<Guid> id = new List<Guid>();
			for (int j = 0; j < s.Length; j++)
			{
				try
				{
					Guid g = new Guid(s[j]);
					id.Add(g);
				}
				catch (Exception e)
				{
					DMUtils.Logging("Guid invalid: {0}", e);
				}
			}
			return id;
		}

		protected override void OnLoad(ConfigNode node)
		{
			if (DMScienceScenario.SciScenario.contractsReload)
				DMUtils.resetContracts();
			int target;
			string[] orbitString = node.GetValue("Orbital_Parameter").Split('|');
			if (!int.TryParse(orbitString[0], out type))
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Removed");
				this.Unregister();
				this.Root.RemoveParameter(this);
			}
			if (int.TryParse(orbitString[1], out target))
				body = FlightGlobals.Bodies[target];
			else
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Removed");
				this.Unregister();
				this.Root.RemoveParameter(this);
			}
			vName = orbitString[2];
			if (!double.TryParse(orbitString[3], out orbitalParameter))
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Removed");
				this.Unregister();
				this.Root.RemoveParameter(this);
			}
			if (!HighLogic.LoadedSceneIsEditor)
			{
				if (!string.IsNullOrEmpty(vName))
				{
					List<Guid> ids = stringSplitGuid(vName);
					if (ids.Count > 0)
					{
						foreach (Guid id in ids)
						{
							try
							{
								Vessel V = FlightGlobals.Vessels.FirstOrDefault(v => v.id == id);
								suitableV.Add(V);
								DMUtils.DebugLog("Vessel {0} Loaded", V.vesselName);
							}
							catch
							{
								DMUtils.Logging("Failed To Load Vessel; Parameter Reset");
								if (HighLogic.LoadedSceneIsFlight)
								{
									DMUtils.Logging("Checking If Currently Loaded Vessel Is Appropriate");
									vesselOrbit(FlightGlobals.ActiveVessel, FlightGlobals.currentMainBody);
								}
							}
						}
					}
				}
			}
			this.disableOnStateChange = false;
		}

		private void vesselOrbit(Vessel v, CelestialBody b)
		{
			if (v == FlightGlobals.ActiveVessel)
			{
				//If the vessels enters orbit around the correct body and has the right parts set to inOrbit
				if (b == body && v.situation == Vessel.Situations.ORBITING)
				{
					DMUtils.DebugLog("Vessel Mainbody {0} Matches {1}, Checking For Instruments On: ", v.mainBody.name, body.name, v.vesselName);
					if (VesselEquipped(v))
					{
						DMUtils.DebugLog("OP Successfully Entered Orbit");
						if (!suitableV.Contains(v))
							suitableV.Add(v);
					}
				}
				else
					DMUtils.DebugLog("Vessel Mainbody {0} Does Not Match: {1}", v.mainBody.name, body.name);
			}
		}

		private bool VesselEquipped(Vessel v)
		{
			if (v == null)
				return false;
			Part magPart = v.Parts.FirstOrDefault(p => p.name == "dmmagBoom" || p.name == "dmUSMagBoom");
			Part rpwsPart = v.Parts.FirstOrDefault(r => r.name == "rpwsAnt" || r.name == "USRPWS");
			if (magPart != null && rpwsPart != null)
				return true;
			else
				return false;
		}

		private void dockCheck(GameEvents.FromToAction<Part, Part> Parts)
		{
			DMUtils.DebugLog("Dock Event");
			if (suitableV.Count > 0)
			{
				DMUtils.DebugLog("Docking To Mag Surveyor");
				if (Parts.from.vessel.mainBody == body)
				{
					DMUtils.DebugLog("Mainbody Matches");
					modifiedByDocking = true;
					timer = 0;
				}
			}
		}

		private void newVesselCheck(Vessel v)
		{
			if (suitableV.Count > 0)
			{
				DMUtils.DebugLog("New Vessel Created");
				Vessel V = v;
				if (V.Parts.Count <= 1)
					return;
				if (V.mainBody == body)
				{
					DMUtils.DebugLog("Mainbody Matches");
					newV = V;
					modifiedByUnDocking = true;
					timer = 0;
				}
			}
		}

		//Track our vessel's orbit
		protected override void OnUpdate()
		{
			if (this.Root.ContractState == Contract.State.Active && !HighLogic.LoadedSceneIsEditor)
			{
				if (!modifiedByUnDocking && !modifiedByDocking)
				{
					if (suitableV.Count > 0)
					{
						bool complete = false;
						removeV.Clear();
						foreach (Vessel v in suitableV)
						{
							if (v.mainBody != body)
							{
								DMUtils.DebugLog("Vessel Orbiting Wrong Celestial Body");
								removeV.Add(v);
							}
							else if (type == 0)
							{
								if (v.orbit.eccentricity > orbitalParameter && v.situation == Vessel.Situations.ORBITING)
									complete = true;
								else if (v.situation != Vessel.Situations.ORBITING)
									removeV.Add(v);
							}
							else if (type == 1)
							{
								if (Math.Abs(v.orbit.inclination) > orbitalParameter && Math.Abs(v.orbit.inclination) < (180 - orbitalParameter) && v.situation == Vessel.Situations.ORBITING)
									complete = true;
								else if (v.situation != Vessel.Situations.ORBITING)
									removeV.Add(v);
							}
						}
						if (removeV.Count > 0)
						{
							foreach (Vessel V in removeV)
								suitableV.Remove(V);
						}
						if (complete)
							this.SetComplete();
						else
							this.SetIncomplete();
					}
					else
						this.SetIncomplete();
				}
				else
				{
					if (timer < 30)
					{
						timer++;
					}
					else
					{
						if (modifiedByDocking)
						{
							DMUtils.DebugLog("Vessel Modified By Docking");
							if (VesselEquipped(FlightGlobals.ActiveVessel))
							{
								DMUtils.DebugLog("Docked Vessel Assigned: {0}", FlightGlobals.ActiveVessel.vesselName);
								if (!suitableV.Contains(FlightGlobals.ActiveVessel))
									suitableV.Add(FlightGlobals.ActiveVessel);
							}
							else
							{
								DMUtils.DebugLog("Vessel No Longer Properly Equipped");
								if (suitableV.Contains(FlightGlobals.ActiveVessel))
									suitableV.Remove(FlightGlobals.ActiveVessel);
							}
						}
						if (modifiedByUnDocking)
						{
							//If the new vessel retains the proper instruments
							if (VesselEquipped(newV))
							{
								DMUtils.DebugLog("New Vessel Assigned");
								if (!suitableV.Contains(newV))
									suitableV.Add(newV);
							}
							//If the currently active, hopefully old, vessel retains the proper instruments
							else if (VesselEquipped(FlightGlobals.ActiveVessel))
							{
								if (!suitableV.Contains(FlightGlobals.ActiveVessel))
									suitableV.Add(FlightGlobals.ActiveVessel);
								DMUtils.DebugLog("Old Vessel Assigned");
							}
							//If the proper instruments are spread across the two vessels
							else
							{
								if (suitableV.Contains(FlightGlobals.ActiveVessel))
									suitableV.Remove(FlightGlobals.ActiveVessel);
								DMUtils.DebugLog("No Vessels Assigned");
							}
						}
						modifiedByUnDocking = false;
						modifiedByDocking = false;
						timer = 0;
					}
				}
			}
		}

	}
}
