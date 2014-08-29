#region license
/* DMagic Orbital Science - DMAsteroidParameter
 * Contract Parameter for asteroid science
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

namespace DMagic
{
	public class DMAsteroidParameter : CollectScience
	{
		private ExperimentSituations scienceLocation;
		private DMScienceContainer scienceContainer;
		private string subject, name, aSize, partName;
		private bool collected;
		private int size;

		public DMAsteroidParameter()
		{
		}

		internal DMAsteroidParameter(int Size, ExperimentSituations Location, string Name)
		{
			scienceLocation = Location;
			name = Name;
			size = Size;
			aSize = DMUtils.sizeHash(size);
			collected = false;
			DMUtils.availableScience["All"].TryGetValue(name, out scienceContainer);
			partName = scienceContainer.sciPart;
			subject = string.Format("{0}@Asteroid{1}{2}", scienceContainer.exp.id, scienceLocation, "");
		}

		/// <summary>
		/// Used externally to return the name of the requested part
		/// </summary>
		/// <param name="cP">Instance of the requested Contract Parameter</param>
		/// <returns>Available Part name string</returns>
		public static string PartName(ContractParameter cP)
		{
			DMAsteroidParameter Instance = (DMAsteroidParameter)cP;
			return Instance.partName;
		}

		//Properties to be accessed by parent contract
		internal ExperimentSituations Situation
		{
			get { return scienceLocation; }
			private set { }
		}

		internal string Subject
		{
			get { return subject; }
			private set { }
		}

		internal DMScienceContainer Container
		{
			get { return scienceContainer; }
			private set { }
		}

		internal string Name
		{
			get { return name; }
			private set { }
		}

		protected override string GetHashString()
		{
			return aSize;
		}

		protected override string GetTitle()
		{
			if (scienceLocation == ExperimentSituations.InSpaceLow)
				return string.Format("{0} data from in space near a {1} asteroid", scienceContainer.exp.experimentTitle, aSize);
			else if (scienceLocation == ExperimentSituations.SrfLanded)
				return string.Format("{0} data while grappled to a {1} asteroid", scienceContainer.exp.experimentTitle, aSize);
			else
				return "Stupid Code Is Stupid";
		}

		protected override void OnRegister()
		{
			GameEvents.OnScienceRecieved.Add(scienceRecieve);
			DMUtils.OnAsteroidScience.Add(asteroidMonitor);
		}

		protected override void OnUnregister()
		{
			GameEvents.OnScienceRecieved.Remove(scienceRecieve);
			DMUtils.OnAsteroidScience.Remove(asteroidMonitor);
		}

		protected override void OnSave(ConfigNode node)
		{
			node.AddValue("Science_Subject", string.Format("{0}|{1}|{2}|{3}", name, size, (int)scienceLocation, collected));
		}

		protected override void OnLoad(ConfigNode node)
		{
			if (DMScienceScenario.SciScenario != null)
				if (DMScienceScenario.SciScenario.contractsReload)
					DMUtils.resetContracts();
			int targetLocation;
			string[] scienceString = node.GetValue("Science_Subject").Split('|');
			name = scienceString[0];
			DMUtils.availableScience["All"].TryGetValue(name, out scienceContainer);
			partName = scienceContainer.sciPart;
			if (int.TryParse(scienceString[1], out size))
				aSize = DMUtils.sizeHash(size);
			else
			{
				DMUtils.Logging("Failed To Load Contract Parameter; Parameter Removed");
				this.Unregister();
				this.Root.RemoveParameter(this);
			}
			if (int.TryParse(scienceString[2], out targetLocation))
				scienceLocation = (ExperimentSituations)targetLocation;
			else
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Removed");
				this.Unregister();
				this.Root.RemoveParameter(this);
			}
			if (!bool.TryParse(scienceString[3], out collected))
			{
				DMUtils.Logging("Failed To Load Variables; Parameter Reset");
				collected = false;
			}
			subject = string.Format("{0}@Asteroid{1}", scienceContainer.exp.id, scienceLocation);
		}

		private void asteroidMonitor(string size, string exp)
		{
			if (!collected)
			{
				if (size == aSize && exp == scienceContainer.exp.id)
				{
					ScreenMessages.PostScreenMessage("Asteroid Science Results Collected", 6f, ScreenMessageStyle.UPPER_CENTER);
					collected = true;
				}
			}
		}

		private void scienceRecieve(float sci, ScienceSubject sub)
		{
			if (sub.id.Contains(subject))
			{
				if (collected)
					base.SetComplete();
				else
					DMUtils.Logging("Data Not Collected From Correctly Sized Asteroid");
			}
		}

	}
}
