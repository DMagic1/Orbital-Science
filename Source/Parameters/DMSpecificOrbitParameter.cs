#region license
/* DMagic Orbital Science - DMSpecificOrbitParameter
 * Class to track multiple vessels on specific orbits
 *
 * Copyright (c) 2016, David Grandy <david.grandy@gmail.com>
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
using DMagic.Contracts;
using Contracts;
using FinePrint.Utilities;
using FinePrint.Contracts.Parameters;

namespace DMagic.Parameters
{
	public class DMSpecificOrbitParameter : SpecificOrbitParameter
	{
		private CelestialBody body;
		private OrbitType type;
		private double inc, ecc, sma, lan, aop, mae, epo, deviation;
		private bool orbitLoaded;
		private bool orbitTested;
		private DMLongOrbitParameter root;

		public DMSpecificOrbitParameter() { }

		public DMSpecificOrbitParameter(OrbitType orbitType, double inclination, double eccentricity, double semi, double la, double argumentOfPeriapsis, double meanAnomalyAtEpoch, double epoch, CelestialBody targetBody, double deviationWindow, DMLongOrbitParameter r) : base(orbitType, inclination, eccentricity, semi, la, argumentOfPeriapsis, meanAnomalyAtEpoch, epoch, targetBody, deviationWindow)
		{
			type = orbitType;
			body = targetBody;
			inc = inclination;
			ecc = eccentricity;
			sma = semi;
			lan = la;
			aop = argumentOfPeriapsis;
			mae = meanAnomalyAtEpoch;
			epo = epoch;
			deviation = deviationWindow;
			root = r;
			disableOnStateChange = false;
			setupOrbit();
			testOrbit();
		}

		private void setupOrbit()
		{
			orbitDriver = new OrbitDriver();
			orbitDriver.orbit = new Orbit();
			orbitDriver.orbit.referenceBody = body;
			orbitDriver.orbit.inclination = inc;
			orbitDriver.orbit.eccentricity = ecc;
			orbitDriver.orbit.argumentOfPeriapsis = aop;
			orbitDriver.orbit.semiMajorAxis = sma;
			orbitDriver.orbit.LAN = lan;
			orbitDriver.orbit.meanAnomalyAtEpoch = mae;
			orbitDriver.orbit.epoch = epo;
			orbitDriver.orbit.Init();

			orbitLoaded = true;
		}

		protected override void OnUpdate()
		{
			if (this.Root.ContractState != Contract.State.Active)
				return;

			if (HighLogic.LoadedSceneIsEditor)
				return;

			if (!orbitTested)
			{
				orbitLoaded = testOrbit();
				orbitTested = true;
			}

			if (!orbitLoaded)
			{
				this.SetIncomplete();
				return;
			}

			if (orbitDriver.orbit == null)
			{
				this.SetIncomplete();
				return;
			}

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

				if (VesselUtilities.VesselAtOrbit(orbitDriver.orbit, deviationWindow, v))
				{
					this.SetComplete();
					return;
				}
			}

			this.SetIncomplete();
		}

		protected override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			try
			{
				root = (DMLongOrbitParameter)Parent;
			}
			catch (Exception e)
			{
				loadFail("Could not find root long orbit parameter; removing DMSpecific Orbit Parameter\n" + e.ToString());
				return;
			}

			if (HighLogic.LoadedScene == GameScenes.SPACECENTER && this.Root.ContractState == Contract.State.Active)
			{
				body = node.parse("TargetBody", (CelestialBody)null);
				if (body == null)
				{
					loadFail("Failed To Load Target Body; DMSpecific Orbit Parameter Removed");
					return;
				}

				int oType = node.parse("orbitType", (int)1000);
				if (oType == 1000)
				{
					loadFail("Failed To Load Orbit Type; DMSpecific Orbit Parameter Removed");
					return;
				}
				type = (OrbitType)oType;

				inc = node.parse("inclination", (double)90);
				ecc = node.parse("eccentricity", (double)0);
				sma = node.parse("sma", (double)0);
				aop = node.parse("argumentOfPeriapsis", (double)0);
				mae = node.parse("meanAnomalyAtEpoch", (double)0);
				epo = node.parse("epoch", (double)0);
				lan = node.parse("lan", (double)0);
				deviation = node.parse("deviationWindow", (double)10);
				setupOrbit();
			}

			if (this.Root.ContractState == Contract.State.Active)
				orbitLoaded = testOrbit();
		}

		private bool testOrbit()
		{
			try
			{
				double d = orbitDriver.orbit.inclination;
				orbitTested = true;
				DMUtils.DebugLog("Orbit Checks Out...");
				return true;
			}
			catch (Exception e)
			{
				Debug.LogError("[DM] Error detected in setting up long term recon orbit parameter; deactivating\n" + e.StackTrace);
				return false;
			}
		}		

		private void loadFail(string message)
		{
			this.Unregister();
			this.Parent.RemoveParameter(this);
			DMUtils.Logging(message);
		}

	}
}
