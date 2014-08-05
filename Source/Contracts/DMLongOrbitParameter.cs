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
		private Vessel vessel;
		private string vName;
		private bool inOrbit, goodOrbit;
		private double orbitTime, timeNeeded;
		private DMMagneticSurveyContract rootContract;

		public DMLongOrbitParameter()
		{
		}

		internal DMLongOrbitParameter(CelestialBody Body, double Time)
		{
			body = Body;
			vessel = null;
			vName = "";
			inOrbit = false;
			goodOrbit = false;
			orbitTime = 0;
			timeNeeded = Time;
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
			GameEvents.VesselSituation.onEscape.Add(vesselEscapeOrbit);
		}

		protected override void OnUnregister()
		{
			GameEvents.VesselSituation.onOrbit.Remove(vesselOrbit);
			GameEvents.VesselSituation.onEscape.Remove(vesselEscapeOrbit);
		}

		protected override void OnSave(ConfigNode node)
		{
			DMUtils.DebugLog("Saving Long Orbital Parameter");
			node.AddValue("Orbital_Parameter", string.Format("{0}|{1}|{2}|{3}|{4:N1}|{5:N1}", body.flightGlobalsIndex, vName, inOrbit, goodOrbit, orbitTime, timeNeeded));
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
				DMUtils.Logging("Failed To Load Variables; Parameter Removed");
				this.Root.RemoveParameter(this);
			}
			if (!bool.TryParse(orbitString[3], out goodOrbit))
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Removed");
				this.Root.RemoveParameter(this);
			}
			if (!double.TryParse(orbitString[4], out orbitTime))
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Removed");
				this.Root.RemoveParameter(this);
			}
			if (!double.TryParse(orbitString[5], out timeNeeded))
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
						DMUtils.DebugLog("Vessel {0} Loaded", vessel.vesselName);
					}
					catch
					{
						DMUtils.Logging("Failed To Load Vessel; Parameter Removed");
						this.Root.RemoveParameter(this);
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
						}
						else //Once the timer is started measure if enough time has passed to complete the parameter
						{
							if ((Planetarium.GetUniversalTime() - orbitTime) >= timeNeeded)
							{
								DMUtils.DebugLog("Survey Complete Ater {0:N2} Amount of Time", Planetarium.GetUniversalTime() - orbitTime);
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
		}

		private void vesselOrbit(Vessel v, CelestialBody b)
		{
			if (v == FlightGlobals.ActiveVessel)
			{
				if (!inOrbit)
				{
					//If the vessels enters orbit around the correct body and has the right parts set to inOrbit
					if (b == body)
					{
						DMUtils.DebugLog("Vessel Mainbody {0} Matches {1}, Checking For Instruments", v.mainBody.name, body.name);
						Part magPart = v.Parts.FirstOrDefault(p => p.name == "dmmagBoom" || p.name == "dmUSMagBoom");
						Part rpwsPart = v.Parts.FirstOrDefault(r => r.name == "rpwsAnt" || r.name == "USRPWS");
						if (magPart != null && rpwsPart != null)
						{
							DMUtils.DebugLog("Long Orbit - Successfully Entered Orbit");
							inOrbit = true;
							vessel = v;
							vName = vessel.vesselName;
						}
					}
					else
						DMUtils.DebugLog("Vessel Mainbody {0} Does Not Match: {1}", v.mainBody.name, body.name);
				}
			}
		}

		private void vesselEscapeOrbit(Vessel v, CelestialBody b)
		{
			if (v == FlightGlobals.ActiveVessel)
			{
				if (inOrbit)
				{
					DMUtils.DebugLog("Vessel Escaping Orbit");
					inOrbit = false;
					goodOrbit = false;
					orbitTime = Planetarium.GetUniversalTime();
				}
			}
		}

	}
}
