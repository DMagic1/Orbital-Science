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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Contracts;
using Contracts.Parameters;
using DMagic.Contracts;

namespace DMagic.Parameters
{
	public class DMOrbitalParameters: ContractParameter
	{
		private DMLongOrbitParameter root;
		private List<Vessel> removeV = new List<Vessel>();
		private double orbitalParameter;
		private int type; //type 0 is eccentricity tracker; type 1 is inclination tracker

		public DMOrbitalParameters()
		{
		}

		internal DMOrbitalParameters(double Param, int Type, DMLongOrbitParameter r)
		{
			orbitalParameter = Param;
			type = Type;
			root = r;
			this.disableOnStateChange = false;
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

		protected override void OnSave(ConfigNode node)
		{
			node.AddValue("Type", type);
			node.AddValue("Orbital_Parameter", orbitalParameter.ToString("N3"));
		}

		protected override void OnLoad(ConfigNode node)
		{
			type = node.parse("Type", (int)1000);
			if (type == 1000)
			{
				DMUtils.Logging("Failed To Load Type-Variables; Mag Orbital Parameter Removed");
				this.Unregister();
				this.Parent.RemoveParameter(this);
				return;
			}

			if (type == 0)
				orbitalParameter = node.parse("Orbital_Parameter", (double)0.2);
			else
				orbitalParameter = node.parse("Orbital_Parameter", (double)20);

			this.disableOnStateChange = false;

			try
			{
				root = (DMLongOrbitParameter)Parent;
			}
			catch (Exception e)
			{
				this.Unregister();
				this.Parent.RemoveParameter(this);
				DMUtils.Logging("Could not find root long orbit parameter; removing DMReconOrbit Parameter\n{0}", e);
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

			if (root == null)
			{
				this.SetIncomplete();
				return;
			}

			for (int i = 0; i < root.VesselCount; i++)
			{
				Vessel v = root.GetVessel(i);

				if (v == null)
					continue;

				if (type == 0)
				{
					if (v.orbit.eccentricity > orbitalParameter && v.situation == Vessel.Situations.ORBITING)
					{
						this.SetComplete();
						return;
					}
				}
				else if (type == 1)
				{
					if (Math.Abs(v.orbit.inclination) > orbitalParameter && Math.Abs(v.orbit.inclination) < (180 - orbitalParameter) && v.situation == Vessel.Situations.ORBITING)
					{
						this.SetComplete();
						return;
					}
				}
			}
					
			this.SetIncomplete();
		}

	}
}
