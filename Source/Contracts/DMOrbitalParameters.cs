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
		private Vessel vessel, newV;
		private string vName;
		private double orbitalParameter;
		private int type, timer; //type 0 is eccentricity tracker; type 1 is inclination tracker
		private bool inOrbit, modifiedByDocking, modifiedByUnDocking;

		public DMOrbitalParameters()
		{
		}

		internal DMOrbitalParameters(CelestialBody Body, double Param, int Type)
		{
			body = Body;
			vessel = null;
			vName = "";
			inOrbit = false;
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

		internal Vessel Vessel
		{
			get
			{
				if (!HighLogic.LoadedSceneIsEditor)
					return vessel;
				else
					return null;
			}
			private set { }
		}

		internal bool VesselEquipped(Vessel v)
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

		protected override string GetHashString()
		{
			return body.name;
		}

		protected override string GetTitle()
		{
			if (type == 0)
				return string.Format("Orbit with at least {0:N2} eccentricity", orbitalParameter);
			else if (type == 1)
				return string.Format("Orbit of at least {0:N1} degrees inclination", orbitalParameter);
			else
				return "Stupid things";
		}

		protected override void OnRegister()
		{
			GameEvents.VesselSituation.onOrbit.Add(vesselOrbit);
			GameEvents.onVesselCreate.Add(newVesselCheck);
			GameEvents.onPartCouple.Add(dockCheck);
			//GameEvents.onVesselWasModified.Add(vesselModified);
		}

		protected override void OnUnregister()
		{
			GameEvents.VesselSituation.onOrbit.Remove(vesselOrbit);
			GameEvents.onVesselCreate.Remove(newVesselCheck);
			GameEvents.onPartCouple.Remove(dockCheck);
			//GameEvents.onVesselWasModified.Remove(vesselModified);
		}

		protected override void OnSave(ConfigNode node)
		{
			DMUtils.DebugLog("Saving Long Orbital Parameter");
			if (HighLogic.LoadedSceneIsEditor)
				node.AddValue("Orbital_Parameter", string.Format("{0}|{1}|{2}|{3:N3}|{4}", type, body.flightGlobalsIndex, vName, orbitalParameter, inOrbit));
			else if (vessel != null)
				node.AddValue("Orbital_Parameter", string.Format("{0}|{1}|{2}|{3:N3}|{4}", type, body.flightGlobalsIndex, vessel.vesselName, orbitalParameter, inOrbit));
			else
				node.AddValue("Orbital_Parameter", string.Format("{0}|{1}|{2}|{3:N3}|{4}", type, body.flightGlobalsIndex, vName, orbitalParameter, inOrbit));
		}

		protected override void OnLoad(ConfigNode node)
		{
			DMUtils.DebugLog("Loading Orbital Parameter");
			int target;
			string[] orbitString = node.GetValue("Orbital_Parameter").Split('|');
			if (!int.TryParse(orbitString[0], out type))
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Removed");
				this.Root.RemoveParameter(this);
			}
			if (int.TryParse(orbitString[1], out target))
				body = FlightGlobals.Bodies[target];
			else
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Removed");
				this.Root.RemoveParameter(this);
			}
			vName = orbitString[2];
			if (!double.TryParse(orbitString[3], out orbitalParameter))
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Removed");
				this.Root.RemoveParameter(this);
			}
			if (!bool.TryParse(orbitString[4], out inOrbit))
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Reset");
				inOrbit = false;
			}
			if (!HighLogic.LoadedSceneIsEditor)
			{
				if (!string.IsNullOrEmpty(vName))
				{
					try
					{
						vessel = FlightGlobals.Vessels.FirstOrDefault(v => v.vesselName == vName);
						//if (vessel != null)
							DMUtils.DebugLog("Vessel {0} Loaded", vessel.vesselName);
					}
					catch
					{
						DMUtils.Logging("Failed To Load Vessel; Parameter Reset");
						inOrbit = false;
						if (HighLogic.LoadedSceneIsFlight)
						{
							DMUtils.Logging("Checking If Currently Loaded Vessel Is Appropriate");
							vesselOrbit(FlightGlobals.ActiveVessel, FlightGlobals.currentMainBody);
						}
						else
						{
							vName = "";
							this.SetIncomplete();
						}
					}
				}
			}
			this.disableOnStateChange = false;
		}

		internal void setStateChangeDisable()
		{
			this.disableOnStateChange = true;
			this.SetComplete();
			this.enabled = false;
		}

		private void vesselOrbit(Vessel v, CelestialBody b)
		{
			if (v == FlightGlobals.ActiveVessel)
			{
				if (!inOrbit)
				{
					//If the vessels enters orbit around the correct body and has the right parts set to inOrbit
					if (b == body && v.situation == Vessel.Situations.ORBITING)
					{
						DMUtils.DebugLog("Vessel Mainbody {0} Matches {1}, Checking For Instruments", v.mainBody.name, body.name);
						if (VesselEquipped(v))
						{
							DMUtils.DebugLog("OP Successfully Entered Orbit");
							inOrbit = true;
							vessel = v;
							vName = vessel.vesselName;
						}
						else
						{
							inOrbit = false;
							this.SetIncomplete();
						}
					}
					else
						DMUtils.DebugLog("Vessel Mainbody {0} Does Not Match: {1}", v.mainBody.name, body.name);
				}
			}
		}

		private void dockCheck(GameEvents.FromToAction<Part, Part> Parts)
		{
			DMUtils.DebugLog("Dock Event");
			if (inOrbit)
			{
				DMUtils.DebugLog("Docking To Mag Surveyor");
				Vessel fromV = Parts.from.vessel;
				if (fromV.mainBody == body)
				{
					DMUtils.DebugLog("Mainbody Matches");
					Vessel toV = Parts.to.vessel;
					if (fromV == vessel || toV == vessel)
					{
						modifiedByDocking = true;
						timer = 0;
					}
				}
			}
		}

		//private void vesselModified(Vessel v)
		//{
		//    if (inOrbit)
		//    {
		//        if (modifiedByDocking)
		//        {
		//            DMUtils.DebugLog("Vessel Modified By Docking");
		//            if (VesselEquipped(v))
		//            {
		//                DMUtils.DebugLog("Docked Vessel Assigned: {0}", v.vesselName);
		//                vessel = v;
		//                inOrbit = true;
		//                this.SetComplete();
		//                vName = vessel.vesselName;
		//            }
		//            else
		//            {
		//                DMUtils.DebugLog("Vessel No Longer Properly Equipped");
		//                vName = "";
		//                vessel = null;
		//                inOrbit = false;
		//                this.SetIncomplete();
		//            }
		//            modifiedByDocking = false;
		//        }
		//    }
		//}

		private void newVesselCheck(Vessel v)
		{
			if (inOrbit)
			{
				DMUtils.DebugLog("New Vessel Created");
				Vessel V = v;
				if (V.Parts.Count <= 1)
					return;
				if (V.mainBody == body)
				{
					DMUtils.DebugLog("Mainbody Matches");
					if (FlightGlobals.ActiveVessel == vessel || V == vessel)
					{
						DMUtils.DebugLog("Matching Vessel Located");
						newV = V;
						modifiedByUnDocking = true;
						timer = 0;
					}
				}
			}
		}

		//                //If the new vessel retains the proper instruments
		//                if (VesselEquipped(newV))
		//                {
		//                    DMUtils.DebugLog("New Vessel Assigned");
		//                    vessel = newV;
		//                    inOrbit = true;
		//                    this.SetComplete();
		//                    vName = vessel.vesselName;
		//                }
		//                //If the currently active, hopefully old, vessel retains the proper instruments
		//                else if (VesselEquipped(FlightGlobals.ActiveVessel))
		//                {
		//                    inOrbit = true;
		//                    this.SetComplete();
		//                    DMUtils.DebugLog("Old Vessel Assigned");
		//                    vessel = FlightGlobals.ActiveVessel;
		//                    vName = vessel.vesselName;
		//                }
		//                //If the proper instruments are spread across the two vessels
		//                else
		//                {
		//                    DMUtils.DebugLog("No Vessels Assigned");
		//                    inOrbit = false;
		//                    this.SetIncomplete();
		//                }
		//            }
		//        }
		//    }
		//}

		//Track our vessel's orbit
		protected override void OnUpdate()
		{
			if (this.Root.ContractState == Contract.State.Active && !HighLogic.LoadedSceneIsEditor)
			{
				if (!modifiedByUnDocking && !modifiedByDocking)
				{
					if (inOrbit)
					{
						if (type == 0)
						{
							if (vessel.orbit.eccentricity > orbitalParameter && vessel.situation == Vessel.Situations.ORBITING)
								this.SetComplete();
							else if (vessel.situation == Vessel.Situations.ESCAPING || vessel.situation == Vessel.Situations.SUB_ORBITAL)
							{
								inOrbit = false;
								this.SetIncomplete();
							}
							else
								this.SetIncomplete();
						}
						else if (type == 1)
						{
							if (Math.Abs(vessel.orbit.inclination) > orbitalParameter && Math.Abs(vessel.orbit.inclination) < (180 - orbitalParameter) && vessel.situation == Vessel.Situations.ORBITING)
								this.SetComplete();
							else if (vessel.situation == Vessel.Situations.ESCAPING || vessel.situation == Vessel.Situations.SUB_ORBITAL)
							{
								inOrbit = false;
								this.SetIncomplete();
							}
							else
								this.SetIncomplete();
						}
						if (this.State == ParameterState.Complete)
						{
							if (vessel.mainBody != body)
							{
								DMUtils.DebugLog("Vessel Orbiting Wrong Celestial Body");
								inOrbit = false;
								this.SetIncomplete();
							}
						}
					}
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
								vessel = FlightGlobals.ActiveVessel;
								inOrbit = true;
								this.SetComplete();
								vName = vessel.vesselName;
							}
							else
							{
								DMUtils.DebugLog("Vessel No Longer Properly Equipped");
								vName = "";
								vessel = null;
								inOrbit = false;
								this.SetIncomplete();
							}
						}
						if (modifiedByUnDocking)
						{
							//If the new vessel retains the proper instruments
							if (VesselEquipped(newV))
							{
								DMUtils.DebugLog("New Vessel Assigned");
								vessel = newV;
								inOrbit = true;
								this.SetComplete();
								vName = vessel.vesselName;
							}
							//If the currently active, hopefully old, vessel retains the proper instruments
							else if (VesselEquipped(FlightGlobals.ActiveVessel))
							{
								inOrbit = true;
								this.SetComplete();
								DMUtils.DebugLog("Old Vessel Assigned");
								vessel = FlightGlobals.ActiveVessel;
								vName = vessel.vesselName;
							}
							//If the proper instruments are spread across the two vessels
							else
							{
								DMUtils.DebugLog("No Vessels Assigned");
								inOrbit = false;
								this.SetIncomplete();
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
