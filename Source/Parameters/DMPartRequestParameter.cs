using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Contracts;
using FinePrint.Contracts.Parameters;

namespace DMagic.Parameters
{
	public class DMPartRequestParameter : ContractParameter
	{
		private Dictionary<Guid, Vessel> suitableVessels = new Dictionary<Guid, Vessel>();
		private Dictionary<int, List<string>> requiredParts = new Dictionary<int, List<string>>();
		private List<string> partTitles = new List<string>();
		private CelestialBody targetBody;
		private string vesselNames;
		private bool updatingVesselState;

		public DMPartRequestParameter() { }

		public DMPartRequestParameter(Dictionary<int, List<string>> parts, CelestialBody body)
		{
			requiredParts = parts;
			targetBody = body;
			this.disableOnStateChange = false;
		}

		private void getPartTitles()
		{
			for (int i = 0; i < requiredParts.Count; i++)
			{
				string s = requiredParts.ElementAt(i).Value.FirstOrDefault();
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
				return suitableVessels.ElementAt(index).Value;

			return null;
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

		protected override string GetTitle()
		{
			if (requiredParts.Count == 1)
				return string.Format("Have the following part onboard: {0}", partTitles[0]);

			string[] parts = new string[partTitles.Count];

			for (int i = 0; i < partTitles.Count; i++)
				parts[i] = partTitles[i] + ",";			

			return string.Format("Have the following parts onboard: {0}", string.Concat(parts).TrimEnd(','));
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

			node.AddValue("Body", targetBody.flightGlobalsIndex);
			node.AddValue("Requested_Parts", DMUtils.stringConcat(requiredParts));
			node.AddValue("Vessels", vessels);
		}

		protected override void OnLoad(ConfigNode node)
		{
			targetBody = node.parse("Body", (CelestialBody)null);
			if (targetBody == null)
			{
				DMUtils.Logging("Failed To Load Target Body; DM Part Request Parameter Removed");
				this.Unregister();
				this.Parent.RemoveParameter(this);
				return;
			}

			string parts = node.parse("Requested_Parts", "");
			if (string.IsNullOrEmpty(parts))
			{
				DMUtils.Logging("Failed To Required Parts List; DM Part Request Parameter Removed");
				this.Unregister();
				this.Parent.RemoveParameter(this);
				return;
			}

			requiredParts = DMUtils.stringSplit(parts);

			vesselNames = node.parse("Vessels", "");
			if (!string.IsNullOrEmpty(vesselNames) && !HighLogic.LoadedSceneIsEditor)
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
								if (vesselEquipped(FlightGlobals.ActiveVessel, FlightGlobals.currentMainBody))
									addVessel(FlightGlobals.ActiveVessel);
							}
						}
					}
				}
			}

			this.disableOnStateChange = false;
		}

		private void addVessel(Vessel v)
		{
			if (!suitableVessels.ContainsKey(v.id))
				suitableVessels.Add(v.id, v);
			else
				DMUtils.Logging("Vessel: [{0}] Already Included In DM Part Request List", v.name);
		}

		private void removeVessel(Vessel v)
		{
			if (suitableVessels.ContainsKey(v.id))
				suitableVessels.Remove(v.id);
		}

		private bool vesselEquipped(Vessel v, CelestialBody b)
		{
			//If the vessels enters orbit around the correct body and has the right parts set to inOrbit
			if (v.situation == Vessel.Situations.ORBITING)
			{
				for (int i = 0; i < requiredParts.Count; i++)
				{
					List<string> parts = requiredParts.ElementAt(i).Value;

					if (parts.Count <= 0)
						return false;

					if (!DMUtils.vesselHasPart(v, parts))
						return false;
				}

				return true;
			}

			return false;
		}

		private void vesselOrbit(Vessel v, CelestialBody b)
		{
			if (b != targetBody)
				return;

			if (vesselEquipped(v, b))
				addVessel(v);
		}

		private void dockCheck(GameEvents.FromToAction<Part, Part> Parts)
		{
			if (Parts.from.vessel.mainBody == targetBody)
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

			if (vesselEquipped(FlightGlobals.ActiveVessel, targetBody))
				addVessel(FlightGlobals.ActiveVessel);
			else
				removeVessel(FlightGlobals.ActiveVessel);
		}

		private void newVesselCheck(Vessel v)
		{
			if (suitableVessels.Count > 0)
			{
				Vessel V = v;

				if (V.Parts.Count <= 1)
					return;

				if (V.mainBody == targetBody)
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
			if (vesselEquipped(newV, targetBody))
				addVessel(newV);
			//If the currently active, hopefully old, vessel retains the proper instruments
			else if (vesselEquipped(FlightGlobals.ActiveVessel, targetBody))
				addVessel(FlightGlobals.ActiveVessel);
			//If the proper instruments are spread across the two vessels
			else
				removeVessel(FlightGlobals.ActiveVessel);
		}

	}

}
