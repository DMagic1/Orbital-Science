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
using Contracts;
using Contracts.Parameters;
using DMagic.Contracts;

namespace DMagic.Parameters
{
	public class DMLongOrbitParameter: ContractParameter
	{
		private double orbitTime, timeNeeded;
		private DMPartRequestParameter partRequest;

		public DMLongOrbitParameter()
		{
		}

		internal DMLongOrbitParameter(double Time)
		{
			orbitTime = 0;
			timeNeeded = Time;
		}

		public double TimeNeeded
		{
			get { return timeNeeded; }
		}

		public double OrbitTime
		{
			get { return orbitTime; }
		}

		protected override string GetTitle()
		{
			if (orbitTime <= 0 || this.State != ParameterState.Incomplete)
				return string.Format("Enter and maintain proper orbit for {0:N0} days", DMUtils.timeInDays(timeNeeded));
			else
				return string.Format("Maintain proper orbit for {0:N0} more days", DMUtils.timeInDays(timeNeeded - (Planetarium.GetUniversalTime() - orbitTime)));
		}

		protected override string GetNotes()
		{
			return "Vessels do not need to remain active throughout the period specified.";
		}

		protected override void OnSave(ConfigNode node)
		{
			node.AddValue("Time_Needed", timeNeeded.ToString("N2"));
			node.AddValue("Time_Completed", orbitTime.ToString("N2"));
		}

		protected override void OnLoad(ConfigNode node)
		{
			timeNeeded = node.parse("Time_Needed", (double)2160000);

			orbitTime = node.parse("Time_Completed", (double)-1);
			if (orbitTime < 0)
			{
				DMUtils.Logging("Failed To Load Orbit-Time Variables; Long Orbit Parameter Reset");
				orbitTime = 0;
			}

			try
			{
				partRequest = GetParameter<DMPartRequestParameter>();
			}
			catch (Exception e)
			{
				this.Unregister();
				this.Parent.RemoveParameter(this);
				DMUtils.Logging("Could not find child part request parameter; removing DMLongOrbit Parameter\n{0}", e);
				return;
			}
		}

		//Track our vessel's orbit
		protected override void OnUpdate()
		{
			if (this.Root.ContractState != Contract.State.Active)
				return;

			if (HighLogic.LoadedSceneIsEditor)
				return;

			if (AllChildParametersComplete())
			{
				if (orbitTime <= 0)
				{
					orbitTime = Planetarium.GetUniversalTime();
				}
				else
				{
					if ((Planetarium.GetUniversalTime() - orbitTime) >= timeNeeded)
					{
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

		public int VesselCount
		{
			get { return partRequest.VesselCount; }
		}

		public Vessel GetVessel(int index)
		{
			return partRequest.getVessel(index);
		}
	}
}
