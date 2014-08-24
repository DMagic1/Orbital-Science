#region license
/* DMagic Orbital Science - DM Recovery Watcher
 * Monobehaviour to watch for science data recovery
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

namespace DMagic
{
	internal class DMRecoveryWatcher : MonoBehaviour
	{

		private void Start()
		{
			DMUtils.DebugLog("Starting Recovery Watcher");
			GameEvents.onVesselRecovered.Add(ProtoRecoveryWatcher);
			//GameEvents.OnVesselRecoveryRequested.Add(RecoveryWatcher);
		}

		private void OnDestroy()
		{
			DMUtils.DebugLog("Destroying Recovery Watcher");
			GameEvents.onVesselRecovered.Remove(ProtoRecoveryWatcher);
			//GameEvents.OnVesselRecoveryRequested.Remove(RecoveryWatcher);
		}

		//private void RecoveryWatcher(Vessel v)
		//{
		//	DMUtils.DebugLog("Vessel Recovery Triggered");
		//	List<ScienceData> dataList = new List<ScienceData>();
		//	if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready)
		//	{
		//		foreach (IScienceDataContainer container in v.FindPartModulesImplementing<IScienceDataContainer>())
		//			dataList.AddRange(container.GetData());
		//		foreach (ScienceData data in dataList)
		//		{
		//			foreach (DMScienceScenario.DMScienceData DMData in DMScienceScenario.SciScenario.recoveredScienceList)
		//			{
		//				if (DMData.title == data.title)
		//				{
		//					ScienceSubject sub = ResearchAndDevelopment.GetSubjectByID(data.subjectID);
		//					sub.scientificValue = DMData.scival;
		//					DMScienceScenario.SciScenario.submitDMScience(DMData, sub);
		//				}
		//			}
		//		}
		//		DMScienceScenario.Recovered = true;
		//	}
		//}

		private void ProtoRecoveryWatcher(ProtoVessel v)
		{
			DMUtils.DebugLog("ProtoVessel Recovery Triggered");
			float totalRecoveredScience = 0;
			float totalDMScience = 0;
			foreach (ProtoPartSnapshot snap in v.protoPartSnapshots)
			{
				foreach (ProtoPartModuleSnapshot msnap in snap.modules)
				{
					if (msnap.moduleValues.HasNode("ScienceData"))
					{
						foreach (ConfigNode dataNode in msnap.moduleValues.GetNodes("ScienceData"))
						{
							ScienceData data = new ScienceData(dataNode);
							if (data != null)
							{
								DMUtils.DebugLog("Found Data In Recovered Vessel");
								foreach (DMScienceScenario.DMScienceData DMData in DMScienceScenario.SciScenario.recoveredScienceList)
								{
									if (DMData.title == data.title)
									{
										ScienceSubject sub = ResearchAndDevelopment.GetSubjectByID(data.subjectID);
										totalRecoveredScience += ResearchAndDevelopment.GetScienceValue(data.dataAmount, sub, 1f);
										totalDMScience += sub.subjectValue * DMData.basevalue * DMData.scival;
										DMScienceScenario.SciScenario.submitDMScience(DMData, sub);
									}
								}
							}
						}
					}
				}
			}
			float extraScience = totalRecoveredScience - totalDMScience;
			Debug.LogWarning(string.Format("Add/Remove {0} Science From R&D Center After Asteroid Calculations", extraScience));
			ResearchAndDevelopment.Instance.Science -= extraScience;
		}
	}
}
