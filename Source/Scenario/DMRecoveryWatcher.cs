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
using UnityEngine;

namespace DMagic.Scenario
{
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	internal class DMRecoveryWatcher : MonoBehaviour
	{
		private static bool loaded = false;

		private void Awake()
		{
			if (!loaded)
			{
				GameEvents.OnScienceRecieved.Add(RecoveryWatcher);
				loaded = true;
			}
		}

		private void Start()
		{
			DontDestroyOnLoad(this);
		}

		private void OnDestroy()
		{
			GameEvents.OnScienceRecieved.Remove(RecoveryWatcher);
		}

		private void RecoveryWatcher(float sci, ScienceSubject sub, ProtoVessel pv)
		{
			if (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION)
			{
				float DMScience = sci;
				DMUtils.DebugLog("Science Data Recovered For {0} Science", sci);

				DMScienceData DMData = DMScienceScenario.SciScenario.getDMScience(sub.title);
				if (DMData != null)
				{
					float oldSciVal = 0f;
					if (sub.scienceCap != 0)
						oldSciVal = Math.Max(0f, 1f - ((sub.science - sci) / sub.scienceCap));
					DMScience = sub.subjectValue * DMData.BaseValue * DMData.SciVal * oldSciVal;
					DMScienceScenario.SciScenario.submitDMScience(DMData, DMScience);
				}
				if (DMScience != sci)
				{
					float extraScience = sci - DMScience;
					Debug.LogWarning(string.Format("[DMagic Orbital Science] [Asteroid Science Retrieval] Remove {0} Science From R&D Center After Asteroid Calculations", extraScience));
					DMUtils.DebugLog("Remove {0} Science From R&D Center: From {1} To {2}", extraScience, ResearchAndDevelopment.Instance.Science, ResearchAndDevelopment.Instance.Science - extraScience);
					ResearchAndDevelopment.Instance.AddScience(-1f * extraScience, TransactionReasons.ScienceTransmission);
				}
			}
		}

	}
}
