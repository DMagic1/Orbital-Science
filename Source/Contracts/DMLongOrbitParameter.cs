#region license
/* DMagic Orbital Science - DMLongOrbitParameter
 * Parameter To Track Long Lasting Orbit
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
	class DMLongOrbitParameter: ContractParameter
	{
		private CelestialBody body;
		private Vessel vessel, newVessel;
		private string vName;
		private bool inOrbit, goodOrbit, modifiedByDocking, modifiedByUnDocking;
		private int timer;
		private double orbitTime, timeNeeded, eccentricity, inclination, lastGoodTime;
		private DMMagneticSurveyContract rootContract;

		public DMLongOrbitParameter()
		{
		}

		internal DMLongOrbitParameter(CelestialBody Body, double Time, double Eccen, double Inc)
		{
			body = Body;
			vessel = null;
			vName = "";
			inOrbit = false;
			goodOrbit = false;
			orbitTime = 0;
			timeNeeded = Time;
			eccentricity = Eccen;
			inclination = Inc;
			this.disableOnStateChange = false;
		}

		//Properties to be accessed by parent contract
		internal CelestialBody Body
		{
			get { return body; }
			private set { }
		}

		internal double TimeNeeded
		{
			get { return timeNeeded; }
			private set { }
		}

		internal bool InOrbit
		{
			get { return inOrbit; }
			private set { }
		}

		internal bool GoodOrbit
		{
			get { return goodOrbit; }
			private set { }
		}

		internal double OrbitTime
		{
			get { return orbitTime; }
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
			return string.Format("Enter orbit around {0}; maintain proper orbit for {1:N0} days", body.theName, DMUtils.timeInDays(timeNeeded));
		}

		protected override void OnRegister()
		{
			GameEvents.VesselSituation.onOrbit.Add(vesselOrbit);
			GameEvents.onPartCouple.Add(dockCheck);
			GameEvents.onVesselCreate.Add(newVesselCheck);
			//GameEvents.onVesselWasModified.Add(vesselModified);
		}

		protected override void OnUnregister()
		{
			GameEvents.VesselSituation.onOrbit.Remove(vesselOrbit);
			GameEvents.onPartCouple.Remove(dockCheck);
			GameEvents.onVesselCreate.Remove(newVesselCheck);
			//GameEvents.onVesselWasModified.Remove(vesselModified);
		}

		protected override void OnSave(ConfigNode node)
		{
			DMUtils.DebugLog("Saving Long Orbital Parameter");
			if (HighLogic.LoadedSceneIsEditor)
				node.AddValue("Orbital_Parameter", string.Format("{0}|{1}|{2}|{3}|{4:N1}|{5:N1}|{6:N3}|{7:N3}", body.flightGlobalsIndex, vName, inOrbit, goodOrbit, orbitTime, timeNeeded, eccentricity, inclination));
			else if (vessel != null)
				node.AddValue("Orbital_Parameter", string.Format("{0}|{1}|{2}|{3}|{4:N1}|{5:N1}|{6:N3}|{7:N3}", body.flightGlobalsIndex, vessel.vesselName, inOrbit, goodOrbit, orbitTime, timeNeeded, eccentricity, inclination));
			else
				node.AddValue("Orbital_Parameter", string.Format("{0}|{1}|{2}|{3}|{4:N1}|{5:N1}|{6:N3}|{7:N3}", body.flightGlobalsIndex, vName, inOrbit, goodOrbit, orbitTime, timeNeeded, eccentricity, inclination));
		}

		protected override void OnLoad(ConfigNode node)
		{
			DMUtils.DebugLog("Loading Long Orbital Parameter");
			int target;
			string[] orbitString = node.GetValue("Orbital_Parameter").Split('|');
			if (int.TryParse(orbitString[0], out target))
				body = FlightGlobals.Bodies[target];
			else
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Removed");
				this.Root.RemoveParameter(this);
			}
			vName = orbitString[1];
			DMUtils.DebugLog("Loaded Planet Target And Vessel Name");
			if (!bool.TryParse(orbitString[2], out inOrbit))
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Reset");
				inOrbit = false;
			}
			if (!bool.TryParse(orbitString[3], out goodOrbit))
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Reset");
				goodOrbit = false;
			}
			if (double.TryParse(orbitString[4], out lastGoodTime))
				orbitTime = lastGoodTime;
			else
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Reset");
				orbitTime = Planetarium.GetUniversalTime();
			}
			if (!double.TryParse(orbitString[5], out timeNeeded))
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Removed");
				this.Root.RemoveParameter(this);
			}
			if (!double.TryParse(orbitString[6], out eccentricity))
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Removed");
				this.Root.RemoveParameter(this);
			}
			if (!double.TryParse(orbitString[7], out inclination))
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Removed");
				this.Root.RemoveParameter(this);
			}
			DMUtils.DebugLog("Loaded Double Precision Variables");

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
							goodOrbit = false;
							vName = "";
							orbitTime = Planetarium.GetUniversalTime();
						}
					}
				}
				rootContract = (DMMagneticSurveyContract)this.Root;
			}
			this.disableOnStateChange = false;
		}

		//Track our vessel's orbit
		protected override void OnUpdate()
		{
			if (rootContract.ContractState == Contract.State.Active && !HighLogic.LoadedSceneIsEditor && rootContract.Loaded)
			{
				if (!modifiedByUnDocking && !modifiedByDocking)
				{
					if (inOrbit)
					{
						//if the vessel's orbit matches our parameters start a timer
						if (vessel.situation == Vessel.Situations.ORBITING && rootContract.Eccentric && rootContract.Inclined)
						{
							if (!goodOrbit)
							{
								DMUtils.DebugLog("Setting time to {0:N2}", Planetarium.GetUniversalTime());
								goodOrbit = true;
								orbitTime = Planetarium.GetUniversalTime();
								lastGoodTime = orbitTime;
							}
							else //Once the timer is started measure if enough time has passed to complete the parameter
							{
								if ((Planetarium.GetUniversalTime() - orbitTime) >= timeNeeded)
								{
									DMUtils.DebugLog("Survey Complete Ater {0:N2} Amount of Time", Planetarium.GetUniversalTime() - orbitTime);
									this.DisableOnStateChange = true;
									this.SetComplete();
								}
							}
						}
						//if the vessel falls out of the specified orbit reset the timer
						else if (goodOrbit)
						{
							DMUtils.DebugLog("Vessel Moved Out Of Proper Orbit; Inclination: {0} ; Eccentricity: {1}", vessel.orbit.inclination, vessel.orbit.eccentricity);
							goodOrbit = false;
							orbitTime = Planetarium.GetUniversalTime();
						}
						else if (vessel.situation == Vessel.Situations.SUB_ORBITAL || vessel.situation == Vessel.Situations.FLYING)
						{
							{
								DMUtils.DebugLog("Vessel Breaking Orbit");
								inOrbit = false;
								goodOrbit = false;
								orbitTime = Planetarium.GetUniversalTime();
							}
						}
						//If the vessel is orbiting the wrong body reset the timer and conditions
						if (vessel.mainBody != body)
						{
							DMUtils.DebugLog("Vessel Orbiting Wrong Celestial Body");
							inOrbit = false;
							goodOrbit = false;
							orbitTime = Planetarium.GetUniversalTime();
						}
					}
				}
				else
				{
					if (timer < 45)
					{
						timer++;
					}
					else
					{
						if (modifiedByDocking)
						{
							if (VesselEquipped(FlightGlobals.ActiveVessel))
							{
								DMUtils.DebugLog("Docked Vessel Assigned: {0}", FlightGlobals.ActiveVessel.vesselName);
								vessel = FlightGlobals.ActiveVessel;
								inOrbit = true;
								goodOrbit = true;
								orbitTime = lastGoodTime;
								vName = vessel.vesselName;
							}
							else
							{
								DMUtils.DebugLog("Vessel Not Properly Equipped");
								vName = "";
								inOrbit = false;
								goodOrbit = false;
								vessel = null;
								orbitTime = Planetarium.GetUniversalTime();
							}
						}
						if (modifiedByUnDocking)
						{
							//If the new vessel retains the proper instruments
							if (VesselEquipped(newVessel))
							{
								DMUtils.DebugLog("New Vessel Assigned: {0}", newVessel.vesselName);
								vessel = newVessel;
								inOrbit = true;
								goodOrbit = true;
								orbitTime = lastGoodTime;
								vName = vessel.vesselName;
							}
							//If the currently active, hopefully old, vessel retains the proper instruments
							else if (VesselEquipped(FlightGlobals.ActiveVessel))
							{
								DMUtils.DebugLog("Old Vessel Assigned");
								vessel = FlightGlobals.ActiveVessel;
								inOrbit = true;
								goodOrbit = true;
								orbitTime = lastGoodTime;
								vName = vessel.vesselName;
							}
							//If the proper instruments are spread across the two vessels
							else
							{
								DMUtils.DebugLog("No Vessels Assigned");
								inOrbit = false;
								goodOrbit = false;
								orbitTime = Planetarium.GetUniversalTime();
							}
						}
						modifiedByDocking = false;
						modifiedByUnDocking = false;
						timer = 0;
					}

				}
			}
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
							DMUtils.DebugLog("Long Orbit - Successfully Entered Orbit");
							inOrbit = true;
							vessel = v;
							vName = vessel.vesselName;
						}
						else
						{
							inOrbit = false;
							goodOrbit = false;
							orbitTime = Planetarium.GetUniversalTime();
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
		//                goodOrbit = true;
		//                orbitTime = lastGoodTime;
		//                vName = vessel.vesselName;
		//            }
		//            else
		//            {
		//                DMUtils.DebugLog("Vessel No Longer Properly Equipped");
		//                vName = "";
		//                inOrbit = false;
		//                goodOrbit = false;
		//                vessel = null;
		//                orbitTime = Planetarium.GetUniversalTime();
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
						newVessel = V;
						modifiedByUnDocking = true;
						timer = 0;
					}
				}
			}
		}

			//            //If the new vessel retains the proper instruments
			//            if (VesselEquipped(newV))
			//            {
			//                DMUtils.DebugLog("New Vessel Assigned: {0}", newV.vesselName);
			//                vessel = newV;
			//                inOrbit = true;
			//                goodOrbit = true;
			//                orbitTime = lastGoodTime;
			//                vName = vessel.vesselName;
			//            }
			//            //If the currently active, hopefully old, vessel retains the proper instruments
			//            else if (VesselEquipped(FlightGlobals.ActiveVessel))
			//            {
			//                DMUtils.DebugLog("Old Vessel Assigned");
			//                vessel = FlightGlobals.ActiveVessel;
			//                inOrbit = true;
			//                goodOrbit = true;
			//                orbitTime = lastGoodTime;
			//                vName = vessel.vesselName;
			//            }
			//            //If the proper instruments are spread across the two vessels
			//            else
			//            {
			//                DMUtils.DebugLog("No Vessels Assigned");
			//                inOrbit = false;
			//                goodOrbit = false;
			//                orbitTime = Planetarium.GetUniversalTime();
			//            }
			//        }
			//    }
			//}
		//}

	}
}
