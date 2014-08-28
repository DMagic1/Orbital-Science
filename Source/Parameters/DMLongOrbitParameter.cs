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
		private double orbitTime, timeNeeded;

		public DMLongOrbitParameter()
		{
		}

		internal DMLongOrbitParameter(CelestialBody Body, double Time)
		{
			body = Body;
			orbitTime = 0;
			timeNeeded = Time;
		}

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

		internal double OrbitTime
		{
			get { return orbitTime; }
			private set { }
		}

		protected override string GetHashString()
		{
			return body.name;
		}

		protected override string GetTitle()
		{
			return string.Format("Enter and maintain proper orbit around {0} for {1:N0} days", body.theName, DMUtils.timeInDays(timeNeeded));
		}

		protected override string GetNotes()
		{
			return "Vessel, or vessels, must be equipped with both magnetometer and RPWS instruments; vessels do not need to remain active throughout the period specified.";
		}

		protected override void OnSave(ConfigNode node)
		{
			node.AddValue("Orbital_Parameter", string.Format("{0}|{1:N1}|{2:N1}", body.flightGlobalsIndex, orbitTime, timeNeeded));
		}

		protected override void OnLoad(ConfigNode node)
		{
			//DMUtils.DebugLog("Loading Long Orbital Parameter");
			int target;
			string[] orbitString = node.GetValue("Orbital_Parameter").Split('|');
			if (int.TryParse(orbitString[0], out target))
				body = FlightGlobals.Bodies[target];
			else
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Removed");
				this.Root.RemoveParameter(this);
			}
			if (!double.TryParse(orbitString[1], out orbitTime))
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Reset");
				orbitTime = 0;
			}
			if (!double.TryParse(orbitString[2], out timeNeeded))
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Reset to Default Value Of 100 Days");
				timeNeeded = 2160000;
			}
		}

		//Track our vessel's orbit
		protected override void OnUpdate()
		{
			if (this.Root.ContractState == Contract.State.Active && !HighLogic.LoadedSceneIsEditor)
			{
				if (AllChildParametersComplete())
				{
					if (orbitTime == 0)
					{
						DMUtils.DebugLog("Setting time to {0:N2}", Planetarium.GetUniversalTime());
						orbitTime = Planetarium.GetUniversalTime();
					}
					else
					{
						if ((Planetarium.GetUniversalTime() - orbitTime) >= timeNeeded)
						{
							DMUtils.DebugLog("Survey Complete Ater {0:N2} Amount of Time", Planetarium.GetUniversalTime() - orbitTime);
							this.DisableOnStateChange = true;
							foreach (ContractParameter cP in this.AllParameters)
								cP.DisableOnStateChange = true;
							this.SetComplete();
						}
					}
				}
				//if the vessel falls out of the specified orbit reset the timer
				else
					orbitTime = 0;
			}
		}

	}
}
