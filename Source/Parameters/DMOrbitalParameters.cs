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
using DMagic.Contracts;

namespace DMagic.Parameters
{
	public class DMOrbitalParameters: ContractParameter
	{
		private DMMagneticSurveyContract root;
		private Vessel newV;
		private Dictionary<Guid, Vessel> suitableVessels = new Dictionary<Guid, Vessel>();
		private List<Vessel> removeV = new List<Vessel>();
		private string vName;
		private double orbitalParameter;
		private int type, timer; //type 0 is eccentricity tracker; type 1 is inclination tracker
		private bool modifiedByDocking, modifiedByUnDocking;

		public DMOrbitalParameters()
		{
		}

		internal DMOrbitalParameters(double Param, int Type)
		{
			orbitalParameter = Param;
			type = Type;
			this.disableOnStateChange = false;
		}

		//Properties to be accessed by parent contract

		public double OrbitalParameter
		{
			get { return orbitalParameter; }
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

		protected override void OnSave(ConfigNode node)
		{
			if (HighLogic.LoadedSceneIsEditor)
				node.AddValue("Orbital_Parameter", string.Format("{0}|{1}|{2:N3}", type, vName, orbitalParameter));
			else if (suitableVessels.Count > 0)
			{
				List<Vessel> suitableV = suitableVessels.Values.ToList();
				vName = stringConcat(suitableV);
				node.AddValue("Orbital_Parameter", string.Format("{0}|{1}|{2:N3}", type, vName, orbitalParameter));
			}
			else
				node.AddValue("Orbital_Parameter", string.Format("{0}|{1}|{2:N3}", type, "", orbitalParameter));
		}

		private string stringConcat(List<Vessel> source)
		{
			int i = source.Count;
			if (i == 0)
				return "";
			string[] s = new string[i];
			for (int j = 0; j < i; j++)
			{
				if (source[j] != null)
					s[j] = source[j].id.ToString() + ",";
			}
			return string.Concat(s).TrimEnd(',');
		}

		private List<Guid> stringSplitGuid(string source)
		{
			if (source == "")
				return new List<Guid>();
			string[] s = source.Split(',');
			List<Guid> id = new List<Guid>();
			for (int j = 0; j < s.Length; j++)
			{
				try
				{
					Guid g = new Guid(s[j]);
					id.Add(g);
				}
				catch (Exception e)
				{
					DMUtils.Logging("Guid invalid: {0}", e);
				}
			}
			return id;
		}

		protected override void OnLoad(ConfigNode node)
		{
			string[] orbitString = node.GetValue("Orbital_Parameter").Split('|');
			if (!int.TryParse(orbitString[0], out type))
			{
				DMUtils.Logging("Failed To Load Type-Variables; Mag Orbital Parameter Removed");
				this.Unregister();
				this.Parent.RemoveParameter(this);
				return;
			}
			vName = orbitString[1];
			if (!double.TryParse(orbitString[2], out orbitalParameter))
			{
				DMUtils.Logging("Failed To Load Orbital-Variables; Mag Orbital Parameter Reset");
				if (type == 0)
					orbitalParameter = 0.2;
				else
					orbitalParameter = 20;
			}
			if (!HighLogic.LoadedSceneIsEditor)
			{
				if (!string.IsNullOrEmpty(vName))
				{
					List<Guid> ids = stringSplitGuid(vName);
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
								DMUtils.Logging("Failed To Load Vessel; Mag Orbital Parameter Reset");
								if (HighLogic.LoadedSceneIsFlight)
								{
									DMUtils.Logging("Checking If Currently Loaded Vessel Is Appropriate");
									vesselOrbit(FlightGlobals.ActiveVessel, FlightGlobals.currentMainBody);
								}
							}
						}
					}
				}
			}
			this.disableOnStateChange = false;

			root = (DMMagneticSurveyContract)this.Root;
		}

		private void addVessel(Vessel v)
		{
			if (!suitableVessels.ContainsKey(v.id))
				suitableVessels.Add(v.id, v);
			else
				DMUtils.Logging("Magnetic Survey Vessel: [{0}] Already Included In List", v.name);
		}

		private void removeVessel(Vessel v)
		{
			if (suitableVessels.ContainsKey(v.id))
				suitableVessels.Remove(v.id);
		}

		private void vesselOrbit(Vessel v, CelestialBody b)
		{
			if (v == FlightGlobals.ActiveVessel)
			{
				//If the vessels enters orbit around the correct body and has the right parts set to inOrbit
				if (b == root.Body && v.situation == Vessel.Situations.ORBITING)
				{
					if (VesselEquipped(v))
					{
						addVessel(v);
					}
				}
			}
		}

		private bool VesselEquipped(Vessel v)
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

		private void dockCheck(GameEvents.FromToAction<Part, Part> Parts)
		{
			if (Parts.from.vessel.mainBody == root.Body)
			{
				modifiedByDocking = true;
				timer = 0;
			}
		}

		private void newVesselCheck(Vessel v)
		{
			if (suitableVessels.Count > 0)
			{
				Vessel V = v;
				if (V.Parts.Count <= 1)
					return;
				if (V.mainBody == root.Body)
				{
					newV = V;
					modifiedByUnDocking = true;
					timer = 0;
				}
			}
		}

		//Track our vessel's orbit
		protected override void OnUpdate()
		{
			if (this.Root.ContractState == Contract.State.Active && !HighLogic.LoadedSceneIsEditor)
			{
				if (!modifiedByUnDocking && !modifiedByDocking)
				{
					if (suitableVessels.Count > 0)
					{
						bool complete = false;
						removeV.Clear();
						foreach (Vessel v in suitableVessels.Values)
						{
							if (v.mainBody != root.Body)
							{
								removeV.Add(v);
							}
							else if (type == 0)
							{
								if (v.orbit.eccentricity > orbitalParameter && v.situation == Vessel.Situations.ORBITING)
									complete = true;
								else if (v.situation != Vessel.Situations.ORBITING)
									removeV.Add(v);
							}
							else if (type == 1)
							{
								if (Math.Abs(v.orbit.inclination) > orbitalParameter && Math.Abs(v.orbit.inclination) < (180 - orbitalParameter) && v.situation == Vessel.Situations.ORBITING)
									complete = true;
								else if (v.situation != Vessel.Situations.ORBITING)
									removeV.Add(v);
							}
						}
						if (removeV.Count > 0)
						{
							foreach (Vessel V in removeV)
							{
								removeVessel(V);
							}
						}
						if (complete)
							this.SetComplete();
						else
							this.SetIncomplete();
					}
					else
						this.SetIncomplete();
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
							if (VesselEquipped(FlightGlobals.ActiveVessel))
							{
								addVessel(FlightGlobals.ActiveVessel);
							}
							else
							{
								removeVessel(FlightGlobals.ActiveVessel);
							}
						}
						if (modifiedByUnDocking)
						{
							//If the new vessel retains the proper instruments
							if (VesselEquipped(newV))
							{
								addVessel(newV);
							}
							//If the currently active, hopefully old, vessel retains the proper instruments
							else if (VesselEquipped(FlightGlobals.ActiveVessel))
							{
								addVessel(FlightGlobals.ActiveVessel);
							}
							//If the proper instruments are spread across the two vessels
							else
							{
								removeVessel(FlightGlobals.ActiveVessel);
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
