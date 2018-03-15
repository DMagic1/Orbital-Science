#region license
/* DMagic Orbital Science - DMPartRequestParameter
 * Class to track vessels with specific parts
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
using Contracts;
using DMagic.Contracts;
using FinePrint.Contracts.Parameters;
using FinePrint;
using FinePrint.Utilities;

namespace DMagic.Parameters
{
	public class DMPartRequestParameter : WaypointParameter
	{
		private DictionaryValueList<Guid, Vessel> suitableVessels = new DictionaryValueList<Guid, Vessel>();
		private DictionaryValueList<Vessel, Waypoint> wps = new DictionaryValueList<Vessel, Waypoint>();
		private DictionaryValueList<int, List<string>> requiredParts = new DictionaryValueList<int, List<string>>();
		private List<string> partTitles = new List<string>();
		private string vesselNames;
		private bool updatingVesselState;
		private bool waypointsOn;
		private bool useWaypoints;
		private bool registered;

		public DMPartRequestParameter() { }

		public DMPartRequestParameter(DictionaryValueList<int, List<string>> parts, bool b, CelestialBody body)
		{
			requiredParts = parts;
			TargetBody = body;
			useWaypoints = b;
			getPartTitles();
			this.disableOnStateChange = false;
		}

		private void getPartTitles()
		{
			for (int i = 0; i < requiredParts.Count; i++)
			{
				string s = requiredParts.At(i).FirstOrDefault();
				AvailablePart aPart = PartLoader.getPartInfoByName(s);
				if (aPart == null)
					continue;
				partTitles.Add(aPart.title);
			}
		}

		public int VesselCount
		{
			get { return suitableVessels.Count; }
		}

		public Vessel getVessel(int index)
		{
			if (suitableVessels.Count > index)
				return suitableVessels.At(index);

			return null;
		}

		protected override void OnRegister()
		{
			if (registered)
				return;

            GameEvents.onVesselSituationChange.Add(vesselOrbit);
			//GameEvents.VesselSituation.onOrbit.Add(vesselOrbit);
			GameEvents.onVesselCreate.Add(newVesselCheck);
			GameEvents.onPartCouple.Add(dockCheck);
			GameEvents.onVesselSOIChanged.Add(soiChange);

			registered = true;
		}

		protected override void OnUnregister()
		{
			suitableVessels.Clear();
			CleanupWaypoints();

			if (!registered)
				return;

			registered = false;

            GameEvents.onVesselSituationChange.Remove(vesselOrbit);
            //GameEvents.VesselSituation.onOrbit.Remove(vesselOrbit);
			GameEvents.onVesselCreate.Remove(newVesselCheck);
			GameEvents.onPartCouple.Remove(dockCheck);
			GameEvents.onVesselSOIChanged.Remove(soiChange);
		}

		protected override string GetTitle()
		{
			if (partTitles.Count <= 0)
				return "Whoops. Something bad happened here...";

			if (requiredParts.Count == 1)
				return string.Format("Have the following part onboard: {0}", partTitles[0]);

			string[] parts = new string[partTitles.Count];

			for (int i = 0; i < partTitles.Count; i++)
				parts[i] = " " + partTitles[i] + ",";			

			return string.Format("Have the following parts onboard: {0}", string.Concat(parts).TrimEnd(',').TrimStart(' '));
		}

		protected override string GetNotes()
		{
			return "Only vessels in orbit around the target planet are tracked.";
		}

		protected override void OnUpdate()
		{
			if (this.Root.ContractState != Contract.State.Active)
				return;
			
			if (HighLogic.LoadedSceneIsEditor)
				return;

			if (updatingVesselState)
				return;

			if (suitableVessels.Count > 0)
				this.SetComplete();
			else
				this.SetIncomplete();
		}

		protected override void OnSave(ConfigNode node)
		{
			string vessels = "";

			if (HighLogic.LoadedSceneIsEditor)
				vessels = vesselNames;
			else if (suitableVessels.Count <= 0)
				vessels = "";
			else
				vessels = DMUtils.stringConcat(suitableVessels.Values.ToList());

			node.AddValue("Body", TargetBody.flightGlobalsIndex);
			node.AddValue("Requested_Parts", DMUtils.stringConcat(requiredParts));
			node.AddValue("Vessels", vessels);
		}

		protected override void OnLoad(ConfigNode node)
		{
			TargetBody = node.parse("Body", (CelestialBody)null);
			if (TargetBody == null)
			{
				DMUtils.Logging("Failed To Load Target Body; DM Part Request Parameter Removed");
				this.Unregister();
				this.Parent.RemoveParameter(this);
				return;
			}

			if (Root.GetType() == typeof(DMReconContract))
				useWaypoints = DMContractDefs.DMRecon.useVesselWaypoints;
			else if (Root.GetType() == typeof(DMMagneticSurveyContract))
				useWaypoints = DMContractDefs.DMMagnetic.useVesselWaypoints;
			else
				useWaypoints = false;

			string parts = node.parse("Requested_Parts", "");
			if (string.IsNullOrEmpty(parts))
			{
				DMUtils.Logging("Failed To Required Parts List; DM Part Request Parameter Removed");
				this.Unregister();
				this.Parent.RemoveParameter(this);
				return;
			}

			requiredParts = DMUtils.stringSplit(parts);

			getPartTitles();

			vesselNames = node.parse("Vessels", "");

			if (!HighLogic.LoadedSceneIsEditor && this.Root.ContractState == Contract.State.Active)
			{
				if (!string.IsNullOrEmpty(vesselNames))
				{
					List<Guid> ids = node.parse("Vessels", new List<Guid>());
					if (ids.Count > 0)
					{
						foreach (Guid id in ids)
						{
							try
							{
								Vessel V = FlightGlobals.Vessels.FirstOrDefault(v => v.id == id);
								addVessel(V);
								DMUtils.DebugLog("Vessel {0} Loaded", V.vesselName);
							}
							catch
							{
								DMUtils.Logging("Failed To Load Vessel; DM Part Request Parameter Reset");
								if (HighLogic.LoadedSceneIsFlight)
								{
									DMUtils.Logging("Checking If Currently Loaded Vessel Is Appropriate");
									if (vesselEquipped(FlightGlobals.ActiveVessel))
										addVessel(FlightGlobals.ActiveVessel);
								}
							}
						}
					}
				}
				else
				{
					for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
					{
						Vessel V = FlightGlobals.Vessels[i];

						if (V == null)
							continue;

						if (V.mainBody == null)
							continue;

						if (V.mainBody != TargetBody)
							continue;

						if (!vesselEquipped(V))
							continue;

						if (!suitableVessels.Contains(V.id))
							addVessel(V);
					}
				}
			}

			this.disableOnStateChange = false;
		}

		public override void UpdateWaypoints(bool focused)
		{
			if (!useWaypoints)
				return;

			var enumerator = wps.GetDictEnumerator();

			while (enumerator.MoveNext())
			{
				var pair = enumerator.Current;

				Vessel v = pair.Key;

				if (v == null)
					continue;

				Waypoint wp = pair.Value;

				if (wp == null)
				{
					wp = setupNewWaypoint(v);
					wps[v] = wp;
				}

				Orbit o;

				if (v.loaded)
					o = v.orbit;
				else
					o = v.protoVessel.orbitSnapShot.Load();

				wp.celestialName = TargetBody.GetName();
				wp.isOnSurface = false;
				wp.orbitPosition = o.getPositionAtUT(Planetarium.GetUniversalTime());
			}
		}

		public override void CleanupWaypoints()
		{
			if (!waypointsOn || !useWaypoints)
				return;

			foreach (Waypoint w in wps.Values)
			{
				if (w == null)
					continue;

				WaypointManager.RemoveWaypoint(w);
			}

			waypointsOn = false;
			wps.Clear();
		}

		private Waypoint setupNewWaypoint(Vessel v)
		{
			if (!useWaypoints)
				return null;

			waypointsOn = true;

			Waypoint wp = new Waypoint();

			wp.celestialName = TargetBody.GetName();
			wp.latitude = 0;
			wp.longitude = 0;
			wp.altitude = 0;
			wp.index = 0;
			wp.id = "dmVessel";
			wp.iconSize = 48;
			wp.blocksInput = false;
			wp.seed = SystemUtilities.SuperSeed(this.Root);
			wp.isOnSurface = false;
			wp.isNavigatable = false;
			wp.enableMarker = false;
			wp.enableTooltip = false;
			wp.landLocked = false;
			wp.name = v.vesselName;
			wp.contractReference = this.Root;

			WaypointManager.AddWaypoint(wp);

			return wp;
		}

		private void removeWaypoint(Vessel v)
		{
			if (wps.Contains(v))
			{
				Waypoint wp = wps[v];

				WaypointManager.RemoveWaypoint(wp);

				wps.Remove(v);
			}

			if (wps.Count <= 0)
				waypointsOn = false;
		}

		private void addVessel(Vessel v)
		{
			if (!suitableVessels.Contains(v.id))
				suitableVessels.Add(v.id, v);
			else
				DMUtils.Logging("Vessel: [{0}] Already Included In DM Part Request List", v.name);

			if (!useWaypoints)
				return;

			if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedScene == GameScenes.TRACKSTATION)
			{
				if (!wps.Contains(v))
					wps.Add(v, setupNewWaypoint(v));
			}
		}

		private void removeVessel(Vessel v)
		{
			if (suitableVessels.Contains(v.id))
				suitableVessels.Remove(v.id);

			if (!useWaypoints)
				return;

			removeWaypoint(v);
		}

		private bool vesselEquipped(Vessel v)
		{
			//If the vessels enters orbit around the correct body and has the right parts set to inOrbit
			if ((v.loaded ? v.situation : v.protoVessel.situation) == Vessel.Situations.ORBITING)
			{
				for (int i = 0; i < requiredParts.Count; i++)
				{
					List<string> parts = requiredParts.At(i);

					if (parts.Count <= 0)
						return false;

					if (!DMUtils.vesselHasPart(v, parts))
						return false;
				}

				return true;
			}

			return false;
		}

        private void vesselOrbit(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> vs)
        {
            if (this.Root.ContractState != Contract.State.Active)
                return;

            if (vs.to != Vessel.Situations.ORBITING)
                return;

            if (vs.host == null)
                return;

            if (vs.host.mainBody == null)
                return;

            if (vs.host.mainBody != TargetBody)
                return;

            if (!vesselEquipped(vs.host))
                return;

            if (!suitableVessels.Contains(vs.host.id))
                addVessel(vs.host);
        }
        
		private void soiChange(GameEvents.HostedFromToAction<Vessel, CelestialBody> action)
		{
			if (this.Root.ContractState != Contract.State.Active)
				return;

			if (action.host == null)
				return;

			if (action.from == null || action.to == null)
				return;

			if (!suitableVessels.Contains(action.host.id))
				return;

			if (action.to != TargetBody)
				removeVessel(action.host);
		}

		private void dockCheck(GameEvents.FromToAction<Part, Part> Parts)
		{
			if (this.Root.ContractState != Contract.State.Active)
				return;

			if (Parts.from == null)
				return;

			if (Parts.from.vessel == null)
				return;

			if (Parts.from.vessel.mainBody == null)
				return;

			if (Parts.from.vessel.mainBody == TargetBody)
				ContractSystem.Instance.StartCoroutine(waitForDockCheck());
		}

		IEnumerator waitForDockCheck()
		{
			int timer = 0;
			updatingVesselState = true;

			while (timer < 45)
			{
				timer++;
				yield return null;
			}

			updatingVesselState = false;

			if (vesselEquipped(FlightGlobals.ActiveVessel))
				addVessel(FlightGlobals.ActiveVessel);
			else
				removeVessel(FlightGlobals.ActiveVessel);
		}

		private void newVesselCheck(Vessel v)
		{
			if (this.Root.ContractState != Contract.State.Active)
				return;

			if (v == null)
				return;

			if (suitableVessels.Count > 0)
			{
				Vessel V = v;

				if (V.Parts.Count <= 1)
					return;

				if (V.mainBody == TargetBody)
					ContractSystem.Instance.StartCoroutine(waitForNewVesselCheck(V));
			}
		}

		IEnumerator waitForNewVesselCheck(Vessel newV)
		{
			int timer = 0;
			updatingVesselState = true;

			while (timer < 45)
			{
				timer++;
				yield return null;
			}

			updatingVesselState = false;

			//If the new vessel retains the proper instruments
			if (vesselEquipped(newV))
				addVessel(newV);
			//If the currently active, hopefully old, vessel retains the proper instruments
			else if (vesselEquipped(FlightGlobals.ActiveVessel))
				addVessel(FlightGlobals.ActiveVessel);
			//If the proper instruments are spread across the two vessels
			else
				removeVessel(FlightGlobals.ActiveVessel);
		}

	}

}
