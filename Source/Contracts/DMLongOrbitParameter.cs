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
		private double orbitTime, timeNeeded, eccentricity;

		public DMLongOrbitParameter()
		{
		}

		internal DMLongOrbitParameter(CelestialBody Body, double Time, double Eccen)
		{
			body = Body;
			vessel = null;
			vName = "";
			inOrbit = false;
			goodOrbit = false;
			orbitTime = 0;
			timeNeeded = Time;
			eccentricity = Eccen;
		}

		//Properties to be accessed by parent contract
		internal CelestialBody Body
		{
			get { return body; }
			private set { }
		}

		internal Vessel Vessel
		{
			get
			{
				if (HighLogic.LoadedScene != GameScenes.EDITOR)
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
			return "Stupid Code Is Stupid";
		}

		protected override void OnRegister()
		{
			GameEvents.VesselSituation.onOrbit.Add(vesselOrbit);
		}

		protected override void OnUnregister()
		{
			GameEvents.VesselSituation.onOrbit.Remove(vesselOrbit);
		}

		protected override void OnSave(ConfigNode node)
		{
			DMUtils.DebugLog("Saving Long Orbital Parameter");
			node.AddValue("Orbital_Paramater", string.Format("{0}|{1}|{2}|{3}|{4:N1}|{5:N3}", body.flightGlobalsIndex, vName, inOrbit, goodOrbit, orbitTime, eccentricity));
		}

		protected override void OnLoad(ConfigNode node)
		{
			DMUtils.DebugLog("Loading Long Ortbital Parameter");
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
			if (!bool.TryParse(orbitString[2], out inOrbit))
			{
				DMUtils.Logging("Failed To Load Vessel; Parameter Removed");
				this.Root.RemoveParameter(this);
			}
			if (!bool.TryParse(orbitString[3], out goodOrbit))
			{
				DMUtils.Logging("Failed To Load Vessel; Parameter Removed");
				this.Root.RemoveParameter(this);
			}
			if (!double.TryParse(orbitString[4], out orbitTime))
			{
				DMUtils.Logging("Failed To Load Vessel; Parameter Removed");
				this.Root.RemoveParameter(this);
			}
			if (!double.TryParse(orbitString[5], out eccentricity))
			{
				DMUtils.Logging("Failed To Load Vessel; Parameter Removed");
				this.Root.RemoveParameter(this);
			}
			if (HighLogic.LoadedScene != GameScenes.EDITOR)
			{
				if (!string.IsNullOrEmpty(vName))
				{
					try
					{
						vessel = FlightGlobals.Vessels.FirstOrDefault(v => v.vesselName == vName);
					}
					catch
					{
						DMUtils.Logging("Failed To Load Vessel; Parameter Removed");
						this.Root.RemoveParameter(this);
					}
				}
			}
		}

		protected override void OnUpdate()
		{
			if (inOrbit)
			{
				if (vessel.orbit.eccentricity > eccentricity && vessel.situation == Vessel.Situations.ORBITING)
				{
					goodOrbit = true;
					orbitTime = Planetarium.GetUniversalTime();
				}
				else
				{
					goodOrbit = false;
					orbitTime = 0;
				}
			}
		}

		private void vesselOrbit(Vessel v, CelestialBody b)
		{
			if (!inOrbit)
			{
				if (b == body)
				{
					var magPart = v.Parts.FirstOrDefault(p => p.partName == "dmmagBoom" || p.partName == "dmUSMagBoom");
					var rpwsPart = v.Parts.FirstOrDefault(r => r.partName == "rpwsAnt" || r.partName == "USRPWS");
					if (magPart != null & rpwsPart != null)
					{
						inOrbit = true;
						vessel = v;
					}
				}
			}
		}

	}
}
