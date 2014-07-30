#region license
/* DMagic Orbital Science - DMAnomalyParameter
 * Anomaly Contract Parameter
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
	class DMAnomalyParameter: ContractParameter
	{
		private CelestialBody body;
		private PQSCity city;
		private ExperimentSituations situation;
		private DMScienceContainer scienceContainer;
		private Vector3d anomPosition;
		private string name;
		private string subject;

		public DMAnomalyParameter()
		{
		}

		internal DMAnomalyParameter(CelestialBody Body, PQSCity City, ExperimentSituations Situation, string Name)
		{
			body = Body;
			situation = Situation;
			name = Name;
			city = City;
			anomPosition = city.transform.position;
			DMUtils.availableScience["All"].TryGetValue(name, out scienceContainer);
			subject = string.Format("{0}@{1}{2}{3}", scienceContainer.exp.id, body.name, situation, city.name.Replace(" ", ""));
		}

		protected override string GetHashString()
		{
			return "";
		}

		protected override string GetTitle()
		{
			return "Stupid Code Is Stupid";
		}

		protected override void OnRegister()
		{
			GameEvents.OnScienceRecieved.Add(anomalyScience);
		}

		protected override void OnUnregister()
		{
			GameEvents.OnScienceRecieved.Remove(anomalyScience);
		}

		protected override void OnSave(ConfigNode node)
		{
			DMUtils.DebugLog("Saving Anomaly Parameter");
			
		}

		protected override void OnLoad(ConfigNode node)
		{
			DMUtils.DebugLog("Loading Anomaly Parameter");
		}

		private void anomalyScience(float sci, ScienceSubject sub)
		{
		}
	}
}
