#region license
/* DMagic Orbital Science - Module Science Animate
 * Generic Part Module For Animated Science Experiments
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
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DMagic.Scenario;
using KSP.UI.Screens.Flight.Dialogs;
using Experience.Effects;

namespace DMagic.Part_Modules
{
	public class DMModuleScienceAnimate : ModuleScienceExperiment, IScienceDataContainer
	{
		#region Fields

		[KSPField]
		public string customFailMessage = "";
		[KSPField]
		public string deployingMessage = "";
		[KSPField(isPersistant = true)]
		public bool IsDeployed;
		[KSPField(isPersistant = true)]
		public bool isLocked;
		[KSPField]
		public string animationName = "";
		[KSPField]
		public string sampleAnim = "";
		[KSPField]
		public string indicatorAnim = "";
		[KSPField]
		public string sampleEmptyAnim = "";
		[KSPField]
		public float animSpeed = 1f;
		[KSPField]
		public string endEventGUIName = "Retract";
		[KSPField]
		public bool showEndEvent = true;
		[KSPField]
		public string startEventGUIName = "Deploy";
		[KSPField]
		public bool showStartEvent = true;
		[KSPField]
		public string toggleEventGUIName = "Toggle";
		[KSPField]
		public bool showToggleEvent = false;
		[KSPField]
		public bool showEditorEvents = true;

		[KSPField]
		public bool experimentAnimation = true;
		[KSPField]
		public bool experimentWaitForAnimation = false;
		[KSPField]
		public float waitForAnimationTime = -1;
		[KSPField]
		public int keepDeployedMode = 0;
		[KSPField]
		public bool oneWayAnimation = false;
		[KSPField]
		public bool oneShot = false;
		[KSPField]
		public string resourceExperiment = "ElectricCharge";
		[KSPField]
		public float resourceExpCost = 0;
		[KSPField]
		public bool asteroidReports = false;
		[KSPField]
		public bool asteroidTypeDependent = false;
		[KSPField(isPersistant = true)]
		public int experimentNumber = 0;
		[KSPField(isPersistant = true)]
		public int experimentsReturned = 0;
		[KSPField]
		public string storageFullMessage = "No more samples can be collected";
		[KSPField]
		public bool USScience = false;
		[KSPField]
		public bool USStock = false;
		[KSPField]
		public string bayAnimation = "";
		[KSPField]
		public string looperAnimation = "";
		[KSPField]
		public bool primary = true;
		[KSPField]
		public int sitMask = 0;
		[KSPField]
		public int bioMask = 0;
		[KSPField]
		public int experimentLimit = 1;
		[KSPField]
		public bool externalDeploy = false;
		[KSPField]
		public int resetLevel = 0;
		[KSPField]
		public float totalScienceLevel = 1;

		protected Animation anim;
		protected Animation anim2;
		private Animation anim3;
		private Animation anim4;
		internal ScienceExperiment scienceExp;
		protected bool resourceOn;
		private int dataIndex;
		private List<ScienceData> scienceReports = new List<ScienceData>();
		protected List<ScienceData> storedScienceReports = new List<ScienceData>();
		private List<DMEnviroSensor> enviroList = new List<DMEnviroSensor>();
		private List<DMModuleScienceAnimate> primaryList = new List<DMModuleScienceAnimate>();
		private DMModuleScienceAnimate primaryModule = null;
		private string bodyNameFixed = "Eeloo";
		private bool lastInOperableState;
		protected float scienceBoost = 1f;
		protected string failMessage = "";
		private ExperimentsResultDialog resultsDialog;

		/// <summary>
		/// For external use to determine if a module can conduct science
		/// </summary>
		/// <param name="MSE">The base ModuleScienceExperiment instance</param>
		/// <returns>True if the experiment can be conducted under current conditions</returns>
		public static bool conduct(ModuleScienceExperiment MSE)
		{
			if (MSE.GetType() != typeof(DMModuleScienceAnimate))
				return false;

			DMModuleScienceAnimate DMMod = (DMModuleScienceAnimate)MSE;
			try
			{
				return DMMod.canConduct();
			}
			catch (Exception e)
			{
				Debug.LogWarning("[DMOS] Error in casting ModuleScienceExperiment to DMModuleScienceAnimate; Invalid Part Module... : " + e);
				return false;
			}
		}

		#endregion

		#region PartModule

		public override void OnAwake()
		{
			GameEvents.onGamePause.Add(onPause);
			GameEvents.onGameUnpause.Add(onUnPause);
			GameEvents.onVesselStandardModification.Add(OnVesselModified);
		}

		public override void OnStart(StartState state)
		{
			anim = DMUtils.GetAnimation(part, animationName);

			if (!string.IsNullOrEmpty(sampleAnim))
			{
				anim2 = DMUtils.GetAnimation(part, sampleAnim);
				if (experimentLimit != 0)
					secondaryAnimator(sampleAnim, 0f, experimentNumber * (1f / experimentLimit), 1f);
			}
			if (!string.IsNullOrEmpty(indicatorAnim))
			{
				anim2 = DMUtils.GetAnimation(part, indicatorAnim);
				if (experimentLimit != 0)
					secondaryAnimator(indicatorAnim, 0f, experimentNumber * (1f / experimentLimit), 1f);
			}
			if (!string.IsNullOrEmpty(sampleEmptyAnim))
				anim2 = DMUtils.GetAnimation(part, sampleEmptyAnim);
			if (!string.IsNullOrEmpty(looperAnimation))
				anim3 = DMUtils.GetAnimation(part, looperAnimation);
			if (!string.IsNullOrEmpty(bayAnimation))
				anim4 = DMUtils.GetAnimation(part, bayAnimation);
			if (state == StartState.Editor) editorSetup();
			else
			{
				setup();
				if (IsDeployed)
				{
					primaryAnimator(1f, 1f, WrapMode.Default, animationName, anim);
					if (anim4 != null)
						primaryAnimator(1f, 1f, WrapMode.Default, bayAnimation, anim4);
					if (anim3 != null)
						primaryAnimator(2.5f * animSpeed, 0f, WrapMode.Loop, looperAnimation, anim3);
					enableIAnimators();
					Events["deployEvent"].active = false;
					Events["retractEvent"].active = !oneWayAnimation && !oneShot && showEndEvent;
					if (oneShot)
						isLocked = true;
				}
				else
				{
					disableIAnimators();
					Events["deployEvent"].active = showStartEvent;
					Events["retractEvent"].active = false;
					if (oneShot)
						isLocked = false;
				}
			}
		}

		new protected virtual void OnDestroy()
		{
			GameEvents.onGamePause.Remove(onPause);
			GameEvents.onGameUnpause.Remove(onUnPause);
			GameEvents.onVesselStandardModification.Remove(OnVesselModified);
		}

		public override void OnSave(ConfigNode node)
		{
			node.RemoveNodes("ScienceData");
			foreach (ScienceData storedData in storedScienceReports)
			{
				ConfigNode storedDataNode = node.AddNode("ScienceData");
				storedData.Save(storedDataNode);
			}
		}

		public override void OnLoad(ConfigNode node)
		{
			if (node.HasNode("ScienceData"))
			{
				foreach (ConfigNode storedDataNode in node.GetNodes("ScienceData"))
				{
					ScienceData data = new ScienceData(storedDataNode);
					storedScienceReports.Add(data);
				}
			}
		}

		new protected virtual void Update()
		{
			base.Update();

			if (HighLogic.LoadedSceneIsFlight)
			{
				//Durrrr, gameEvents sure are helpful....
				if (Inoperable)
					lastInOperableState = true;
				else if (lastInOperableState)
				{
					lastInOperableState = false;
					onLabReset();
				}
				eventsCheck();
			}
		}

		protected virtual void onLabReset()
		{
			if (experimentLimit != 0)
			{
				if (!string.IsNullOrEmpty(sampleEmptyAnim))
					secondaryAnimator(sampleEmptyAnim, animSpeed, 1f - (experimentNumber * (1f / experimentLimit)), experimentNumber * (anim2[sampleEmptyAnim].length / experimentLimit));
				else if (!string.IsNullOrEmpty(sampleAnim))
					secondaryAnimator(sampleAnim, -1f * animSpeed, experimentNumber * (1f / experimentLimit), experimentNumber * (anim2[sampleAnim].length / experimentLimit));
				if (!string.IsNullOrEmpty(indicatorAnim))
					secondaryAnimator(indicatorAnim, -1f * animSpeed, experimentNumber * (1f / experimentLimit), experimentNumber * (anim2[indicatorAnim].length / experimentLimit));
			}
			experimentNumber = 0;
			experimentsReturned = 0;
			Inoperable = false;
			Deployed = false;

			if (keepDeployedMode == 0) retractEvent();
		}

		protected virtual void FixedUpdate()
		{
			if (HighLogic.LoadedSceneIsFlight)
			{
				if (resourceOn)
				{
					if (PartResourceLibrary.Instance.GetDefinition(resourceExperiment) != null)
					{
						float cost = resourceExpCost * TimeWarp.fixedDeltaTime;
						if (part.RequestResource(resourceExperiment, cost) < cost)
						{
							StopCoroutine("WaitForAnimation");
							resourceOn = false;
							ScreenMessages.PostScreenMessage("Not enough " + resourceExperiment + ", shutting down experiment", 4f, ScreenMessageStyle.UPPER_CENTER);
							if (keepDeployedMode == 0 || keepDeployedMode == 1) retractEvent();
						}
					}
				}
			}
		}

		public override string GetInfo()
		{
			string info = base.GetInfo();
			info += string.Format("\nTransmission: {0:P0}\n", xmitDataScalar);
			info += string.Format("Total Science Available: {0:P0}\n", totalScienceLevel);
			info += string.Format("Asteroid Samples: {0}\n", RUIutils.GetYesNoUIString(asteroidReports));
			if (!rerunnable)
			{
				info += string.Format("Max Samples: {0}\n", experimentLimit);
				info += string.Format("Scientist Level For Reset: {0}\n", resetLevel);
			}
			if (resourceExpCost > 0)
			{
				float time = waitForAnimationTime;
				if (time == -1 && anim != null && !string.IsNullOrEmpty(animationName))
					time = anim[animationName].length;
				info += string.Format("Requires:\n-{0}: {1}/s for {2} s\n", resourceExperiment, resourceExpCost, waitForAnimationTime);
			}
			if (oneShot)
				info += string.Format("OneShot: {0}\n", RUIutils.GetYesNoUIString(oneShot));
			return info;
		}

		private void setup()
		{
			Events["deployEvent"].guiActive = showStartEvent || oneShot;
			Events["retractEvent"].guiActive = showEndEvent && !oneShot;
			Events["toggleEvent"].guiActive = showToggleEvent;
			Events["deployEvent"].guiName = startEventGUIName;
			Events["retractEvent"].guiName = endEventGUIName;
			Events["toggleEvent"].guiName = toggleEventGUIName;
			Events["CollectDataExternalEvent"].guiName = collectActionName;
			Events["ResetExperimentExternal"].guiName = resetActionName;
			Events["ResetExperiment"].guiName = resetActionName;
			Events["DeployExperiment"].guiName = experimentActionName;
			Events["DeployExperiment"].guiActiveUnfocused = externalDeploy;
			Events["DeployExperiment"].externalToEVAOnly = externalDeploy;
			Events["DeployExperiment"].unfocusedRange = interactionRange;
			Actions["DeployAction"].active = useActionGroups;
			Actions["deployAction"].guiName = startEventGUIName;
			Actions["retractAction"].guiName = endEventGUIName;
			Actions["toggleAction"].guiName = toggleEventGUIName;
			Actions["DeployAction"].guiName = experimentActionName;
			if (!primary)
			{
				primaryList = this.part.FindModulesImplementing<DMModuleScienceAnimate>();
				if (primaryList.Count > 0)
				{
					foreach (DMModuleScienceAnimate DMS in primaryList)
						if (DMS.primary) primaryModule = DMS;
				}
			}
			if (USStock)
				enviroList = this.part.FindModulesImplementing<DMEnviroSensor>();
			if (waitForAnimationTime == -1 && animSpeed != 0 && anim != null)
				waitForAnimationTime = anim[animationName].length / animSpeed;
			if (!string.IsNullOrEmpty(experimentID))
			{
				scienceExp = ResearchAndDevelopment.GetExperiment(experimentID);
				if (scienceExp != null)
				{
					sitMask = (int)scienceExp.situationMask;
					bioMask = (int)scienceExp.biomeMask;
				}
			}
			if (FlightGlobals.Bodies.Count >= 17)
				bodyNameFixed = FlightGlobals.Bodies[16].bodyName;
		}

		private void editorSetup()
		{
			Actions["deployAction"].active = showStartEvent;
			Actions["retractAction"].active = showEndEvent;
			Actions["toggleAction"].active = showToggleEvent;
			Actions["deployAction"].guiName = startEventGUIName;
			Actions["retractAction"].guiName = endEventGUIName;
			Actions["toggleAction"].guiName = toggleEventGUIName;
			Actions["ResetAction"].active = experimentLimit <= 1;
			Actions["DeployAction"].guiName = experimentActionName;
			Events["editorDeployEvent"].guiName = startEventGUIName;
			Events["editorRetractEvent"].guiName = endEventGUIName;
			Events["editorDeployEvent"].active = showEditorEvents;
			Events["editorRetractEvent"].active = false;
		}

		private void eventsCheck()
		{
			Events["ResetExperiment"].active = experimentLimit <= 1 && storedScienceReports.Count > 0 && resettable;
			Events["ResetExperimentExternal"].active = storedScienceReports.Count > 0 && resettableOnEVA;
			Events["CollectDataExternalEvent"].active = storedScienceReports.Count > 0 && dataIsCollectable;
			Events["DeployExperiment"].active = !Inoperable;
			Events["DeployExperiment"].guiActiveUnfocused = !Inoperable && externalDeploy;
			Events["ReviewDataEvent"].active = storedScienceReports.Count > 0;
			Events["ReviewInitialData"].active = scienceReports.Count > 0;
			Events["DeployExperimentExternal"].guiActiveUnfocused = false;
			Events["CleanUpExperimentExternal"].active = Inoperable;
			Events["TransferDataEvent"].active = hasContainer && dataIsCollectable && storedScienceReports.Count > 0;
			Actions["ResetAction"].active = useActionGroups && storedScienceReports.Count > 0 && resettable;
		}

		private void onPause()
		{
			if (resultsDialog != null)
				resultsDialog.gameObject.SetActive(false);
		}

		private void onUnPause()
		{
			if (resultsDialog != null)
				resultsDialog.gameObject.SetActive(true);
		}

		#endregion

		#region Animators

		protected void primaryAnimator(float speed, float time, WrapMode wrap, string name, Animation a)
		{
			if (a != null)
			{
				a[name].speed = speed;
				if (!a.IsPlaying(name))
				{
					a[name].wrapMode = wrap;
					a[name].normalizedTime = time;
					a.Blend(name, 1f);
				}
			}
		}

		protected void secondaryAnimator(string whichAnim, float sampleSpeed, float sampleTime, float waitTime)
		{
			if (anim2 != null)
			{
				anim2[whichAnim].speed = sampleSpeed;
				anim2[whichAnim].normalizedTime = sampleTime;
				anim2.Blend(whichAnim, 1f);
				StartCoroutine(WaitForSampleAnimation(whichAnim, waitTime));
			}
		}

		private IEnumerator WaitForSampleAnimation(string whichAnimCo, float waitTimeCo)
		{
			yield return new WaitForSeconds(waitTimeCo);
			anim2[whichAnimCo].enabled = false;
		}

		[KSPEvent(guiActive = true, guiName = "Deploy", active = true)]
		public virtual void deployEvent()
		{
			primaryAnimator(animSpeed * 1f, 0f, WrapMode.Default, animationName, anim);
			IsDeployed = !oneWayAnimation;
			if (USScience)
			{
				if (anim4 != null)
				{
					primaryAnimator(animSpeed * 1f, 0f, WrapMode.Default, bayAnimation, anim4);
				}
				if (anim3 != null)
				{
					primaryAnimator(animSpeed * 2.5f, 0f, WrapMode.Loop, looperAnimation, anim3);
				}
			}
			if (USStock)
			{
				if (enviroList.Count > 0)
				{
					foreach (DMEnviroSensor DMES in enviroList)
					{
						if (!DMES.sensorActive && DMES.primary)
							DMES.toggleSensor();
					}
				}
			}
			enableIAnimators();
			Events["deployEvent"].active = oneWayAnimation;
			Events["retractEvent"].active = showEndEvent && !oneShot;
			isLocked = oneShot;
		}

		private void enableIAnimators()
		{
			if (!HighLogic.LoadedSceneIsFlight)
				return;

			foreach (IAnimatedModule m in part.FindModulesImplementing<IAnimatedModule>())
			{
				if (m.IsSituationValid())
					m.EnableModule();
			}
		}

		private void disableIAnimators()
		{
			if (!HighLogic.LoadedSceneIsFlight)
				return;

			foreach (IAnimatedModule m in part.FindModulesImplementing<IAnimatedModule>())
			{
				m.DisableModule();
			}
		}

		[KSPAction("Deploy")]
		public void deployAction(KSPActionParam param)
		{
			deployEvent();
		}

		[KSPEvent(guiActive = true, guiName = "Retract", active = false)]
		public virtual void retractEvent()
		{
			if (oneWayAnimation) return;
			if (oneShot && !HighLogic.LoadedSceneIsEditor) return;
			isLocked = false;
			primaryAnimator(-1f * animSpeed, 1f, WrapMode.Default, animationName, anim);
			IsDeployed = false;
			if (USScience)
			{
				if (anim4 != null)
				{
					if (anim[animationName].length > anim4[bayAnimation].length && anim4[bayAnimation].length != 0)
						primaryAnimator(-1f * animSpeed, (anim[animationName].length / anim4[bayAnimation].length), WrapMode.Default, bayAnimation, anim4);
					else
						primaryAnimator(-1f * animSpeed, 1f, WrapMode.Default, bayAnimation, anim4);
				}
				if (anim3 != null)
				{
					anim3[looperAnimation].normalizedTime = anim3[looperAnimation].normalizedTime % 1;
					anim3[looperAnimation].wrapMode = WrapMode.Clamp;
				}
			}
			if (USStock)
			{
				if (enviroList.Count > 0)
				{
					foreach (DMEnviroSensor DMES in enviroList)
					{
						if (DMES.sensorActive && DMES.primary) DMES.toggleSensor();
					}
				}
			}
			disableIAnimators();
			Events["deployEvent"].active = showStartEvent;
			Events["retractEvent"].active = false;
		}

		[KSPAction("Retract")]
		public void retractAction(KSPActionParam param)
		{
			retractEvent();
		}

		[KSPEvent(guiActive = true, guiName = "Toggle", active = true)]
		public void toggleEvent()
		{
			if (IsDeployed) retractEvent();
			else deployEvent();
		}

		[KSPAction("Toggle")]
		public void toggleAction(KSPActionParam Param)
		{
			toggleEvent();
		}

		[KSPEvent(guiActiveEditor = true, guiName = "Deploy", active = true)]
		public virtual void editorDeployEvent()
		{
			deployEvent();
			IsDeployed = false;
			Events["editorDeployEvent"].active = oneWayAnimation;
			Events["editorRetractEvent"].active = !oneWayAnimation;
		}

		[KSPEvent(guiActiveEditor = true, guiName = "Retract", active = false)]
		public virtual void editorRetractEvent()
		{
			retractEvent();
			Events["editorDeployEvent"].active = true;
			Events["editorRetractEvent"].active = false;
		}

		#endregion

		#region Science Events

		new public void ResetExperiment()
		{
			if (experimentLimit > 1)
			{
				if(anim2 != null) {
					if (!string.IsNullOrEmpty(sampleEmptyAnim))
						secondaryAnimator(sampleEmptyAnim, animSpeed, 1f - (experimentNumber * (1f / experimentLimit)), experimentNumber * (anim2[sampleEmptyAnim].length / experimentLimit));
					else if (!string.IsNullOrEmpty(sampleAnim))
						secondaryAnimator(sampleAnim, -1f * animSpeed, experimentNumber * (1f / experimentLimit), experimentNumber * (anim2[sampleAnim].length / experimentLimit));
					if (!string.IsNullOrEmpty(indicatorAnim))
						secondaryAnimator(indicatorAnim, -1f * animSpeed, experimentNumber * (1f / experimentLimit), experimentNumber * (anim2[indicatorAnim].length / experimentLimit));
				}

				foreach (ScienceData data in storedScienceReports)
					experimentNumber--;
				storedScienceReports.Clear();
				if (experimentNumber < 0)
					experimentNumber = 0;
				if (keepDeployedMode == 0)
					retractEvent();
			}
			else
			{
				if (keepDeployedMode == 0)
					retractEvent();
				storedScienceReports.Clear();
			}

			Deployed = false;
			Inoperable = false;
			lastInOperableState = false;
		}

		new public void ResetAction(KSPActionParam param)
		{
			ResetExperiment();
		}

		new public void ResetExperimentExternal()
		{
			ResetExperiment();
		}

		new public void CollectDataExternalEvent()
		{
			List<ModuleScienceContainer> EVACont = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceContainer>();

			if (EVACont.Count <= 0)
				return;

			if (storedScienceReports.Count > 0)
			{
				if (EVACont.First().StoreData(new List<IScienceDataContainer> { this }, false))
					DumpAllData(storedScienceReports);
			}
		}

		new public void DeployExperimentExternal()
		{
			DeployExperiment();
		}

		new public void CleanUpExperimentExternal()
		{
			if (!FlightGlobals.ActiveVessel.isEVA)
				return;

			if (!FlightGlobals.ActiveVessel.parts[0].protoModuleCrew[0].HasEffect<ScienceResetSkill>())
			{
				ScreenMessages.PostScreenMessage(string.Format("<b><color=orange>[{0}]: A Kerbal with the Science Reset Skill is needed to reset this experiment.</color></b>", part.partInfo.title), 6f, ScreenMessageStyle.UPPER_LEFT);
				return;
			}

			if (FlightGlobals.ActiveVessel.parts[0].protoModuleCrew[0].experienceLevel < resetLevel)
			{
				ScreenMessages.PostScreenMessage(string.Format("<b><color=orange>[{0}]: A level {1} scientist is required to reset this experiment.</color></b>", part.partInfo.title, resetLevel), 6f, ScreenMessageStyle.UPPER_LEFT);
				return;
			}

			Inoperable = false;
			lastInOperableState = false;
			onLabReset();
			ScreenMessages.PostScreenMessage(string.Format("<b><color=#99ff00ff>[{0}]: Media Restored. Module is operational again.</color></b>", part.partInfo.title), 6f, ScreenMessageStyle.UPPER_LEFT);
		}

		new public void TransferDataEvent()
		{
			if (PartItemTransfer.Instance != null)
			{
				ScreenMessages.PostScreenMessage("<b><color=orange>A transfer is already in progress.</color></b>", 3f, ScreenMessageStyle.UPPER_CENTER);
				return;
			}

			ExperimentTransfer.Create(part, this, new Callback<PartItemTransfer.DismissAction, Part>(transferData));
		}

		private void transferData(PartItemTransfer.DismissAction dismiss, Part p)
		{
			if (dismiss != PartItemTransfer.DismissAction.ItemMoved)
				return;

			if (p == null)
				return;

			if (storedScienceReports.Count <= 0)
			{
				ScreenMessages.PostScreenMessage(string.Format("[{0}]: has no data to transfer.", part.partInfo.title), 6, ScreenMessageStyle.UPPER_CENTER);
				return;
			}

			ModuleScienceContainer container = p.FindModuleImplementing<ModuleScienceContainer>();

			if (container == null)
			{
				ScreenMessages.PostScreenMessage(string.Format("<color=orange>[{0}]: {1} has no data container, canceling transfer.<color>", part.partInfo.title, p.partInfo.title), 6, ScreenMessageStyle.UPPER_CENTER);
				return;
			}

			if ((experimentsReturned >= (experimentLimit - 1)) && !rerunnable)
			{
				List<DialogGUIBase> dialog = new List<DialogGUIBase>();
				dialog.Add(new DialogGUIButton<ModuleScienceContainer>("Remove Data", new Callback<ModuleScienceContainer>(onTransferData), container));
				dialog.Add(new DialogGUIButton("Cancel", null, true));

				PopupDialog.SpawnPopupDialog(
					new Vector2(0.5f, 0.5f),
					new Vector2(0.5f, 0.5f),
					new MultiOptionDialog(
						"TransferWarning",
						collectWarningText,
						part.partInfo.title + "Warning!",
						UISkinManager.defaultSkin,
						dialog.ToArray()
						),
					false,
					UISkinManager.defaultSkin,
					true,
					""
					);
			}
			else
				onTransferData(container);
		}

		private void onTransferData(ModuleScienceContainer target)
		{
			if (target == null)
				return;

			int i = storedScienceReports.Count;

			if (target.StoreData(new List<IScienceDataContainer> { this }, false))
				ScreenMessages.PostScreenMessage(string.Format("[{0}]: {1} Data stored.", target.part.partInfo.title, i), 6, ScreenMessageStyle.UPPER_LEFT);
			else
				ScreenMessages.PostScreenMessage(string.Format("<color=orange>[{0}]: Not all data was stored.</color>", target.part.partInfo.title), 6, ScreenMessageStyle.UPPER_LEFT);
		}

		#endregion

		#region Science Experiment Setup

		new public void DeployExperiment()
		{
			gatherScienceData();
		}

		new public void DeployAction(KSPActionParam param)
		{
			DeployExperiment();
		}

		public virtual void gatherScienceData(bool silent = false)
		{
			if (canConduct())
			{
				if (experimentAnimation)
				{
					if (anim != null && anim.IsPlaying(animationName)) return;
					else
					{
						if (!primary)
						{
							if (!primaryModule.IsDeployed)
								primaryModule.deployEvent();
							IsDeployed = true;
						}
						if (!IsDeployed)
						{
							deployEvent();
							if (!string.IsNullOrEmpty(deployingMessage))
								ScreenMessages.PostScreenMessage(deployingMessage, 5f, ScreenMessageStyle.UPPER_CENTER);
							if (experimentWaitForAnimation)
							{
								if (resourceExpCost > 0)
									resourceOn = true;
								StartCoroutine("WaitForAnimation", silent);
							}
							else
								runExperiment(getSituation(), silent);
						}
						else if (resourceExpCost > 0)
						{
							resourceOn = true;
							StartCoroutine("WaitForAnimation", silent);
						}
						else runExperiment(getSituation(), silent);
					}
				}
				else runExperiment(getSituation(), silent);
			}
			else
				ScreenMessages.PostScreenMessage(failMessage, 5f, ScreenMessageStyle.UPPER_CENTER);
		}

		protected IEnumerator WaitForAnimation(bool s)
		{
			ExperimentSituations vesselSit = getSituation();
			yield return new WaitForSeconds(waitForAnimationTime);
			resourceOn = false;
			runExperiment(vesselSit, s);
		}

		protected virtual void runExperiment(ExperimentSituations sit, bool silent)
		{
			ScienceData data = makeScience(sit);
			if (data == null)
				Debug.LogError("[DMOS] Something Went Wrong Here; Null Science Data Returned; Please Report This On The KSP Forum With Output.log Data");
			else
			{
				GameEvents.OnExperimentDeployed.Fire(data);
				if (experimentLimit <= 1)
				{
					dataIndex = 0;
					storedScienceReports.Add(data);
					Deployed = true;
					if (!silent)
						ReviewData();
				}
				else
				{
					scienceReports.Add(data);
					if (experimentNumber >= experimentLimit - 1)
						Deployed = true;
					if (silent)
						onKeepInitialData(data);
					else
						initialResultsPage();
				}
				if (keepDeployedMode == 1) retractEvent();
			}
		}

		protected virtual float fixSubjectValue(ExperimentSituations s, float f, float boost, CelestialBody body)
		{
			float subV = f;

			switch (s)
			{
				case ExperimentSituations.SrfLanded:
					subV = body.scienceValues.LandedDataValue;
					break;
				case ExperimentSituations.SrfSplashed:
					subV = body.scienceValues.SplashedDataValue;
					break;
				case ExperimentSituations.FlyingLow:
					subV = body.scienceValues.FlyingLowDataValue;
					break;
				case ExperimentSituations.FlyingHigh:
					subV = body.scienceValues.FlyingHighDataValue;
					break;
				case ExperimentSituations.InSpaceLow:
					subV = body.scienceValues.InSpaceLowDataValue;
					break;
				case ExperimentSituations.InSpaceHigh:
					subV = body.scienceValues.InSpaceHighDataValue;
					break;
			}

			return subV * boost;
		}

		public virtual string getBiome(ExperimentSituations s)
		{
			if ((bioMask & (int)s) == 0)
				return "";
			else
			{
				switch (vessel.landedAt)
				{
					case "":
						if (vessel.mainBody.BiomeMap != null)
							return vessel.mainBody.BiomeMap.GetAtt(vessel.latitude * Mathf.Deg2Rad, vessel.longitude * Mathf.Deg2Rad).name;
						else
							return "";
					default:
						return Vessel.GetLandedAtString(vessel.landedAt);
				}
			}
		}

		public virtual ExperimentSituations getSituation()
		{
			if (asteroidReports && DMAsteroidScience.AsteroidGrappled)
				return ExperimentSituations.SrfLanded;
			if (asteroidReports && DMAsteroidScience.AsteroidNear)
				return ExperimentSituations.InSpaceLow;
			switch (vessel.situation)
			{
				case Vessel.Situations.LANDED:
				case Vessel.Situations.PRELAUNCH:
					return ExperimentSituations.SrfLanded;
				case Vessel.Situations.SPLASHED:
					return ExperimentSituations.SrfSplashed;
				default:
					if (vessel.altitude < vessel.mainBody.atmosphereDepth && vessel.mainBody.atmosphere)
					{
						if (vessel.altitude < vessel.mainBody.scienceValues.flyingAltitudeThreshold)
							return ExperimentSituations.FlyingLow;
						else
							return ExperimentSituations.FlyingHigh;
					}
					if (vessel.altitude < vessel.mainBody.scienceValues.spaceAltitudeThreshold)
						return ExperimentSituations.InSpaceLow;
					else
						return ExperimentSituations.InSpaceHigh;
			}
		}

		protected virtual string situationCleanup(ExperimentSituations expSit, string b)
		{
			if (vessel.landedAt != "")
				return " from " + b;
			if (b == "")
			{
				switch (expSit)
				{
					case ExperimentSituations.SrfLanded:
						return " from  " + vessel.mainBody.displayName.LocalizeBodyName() + "'s surface";
					case ExperimentSituations.SrfSplashed:
						return " from " + vessel.mainBody.displayName.LocalizeBodyName() + "'s oceans";
					case ExperimentSituations.FlyingLow:
						return " while flying at " + vessel.mainBody.displayName.LocalizeBodyName();
					case ExperimentSituations.FlyingHigh:
						return " from " + vessel.mainBody.displayName.LocalizeBodyName() + "'s upper atmosphere";
					case ExperimentSituations.InSpaceLow:
						return " while in space near " + vessel.mainBody.displayName.LocalizeBodyName();
					default:
						return " while in space high over " + vessel.mainBody.displayName.LocalizeBodyName();
				}
			}
			else
			{
				switch (expSit)
				{
					case ExperimentSituations.SrfLanded:
						return " from " + vessel.mainBody.displayName.LocalizeBodyName() + "'s " + b;
					case ExperimentSituations.SrfSplashed:
						return " from " + vessel.mainBody.displayName.LocalizeBodyName() + "'s " + b;
					case ExperimentSituations.FlyingLow:
						return " while flying over " + vessel.mainBody.displayName.LocalizeBodyName() + "'s " + b;
					case ExperimentSituations.FlyingHigh:
						return " from the upper atmosphere over " + vessel.mainBody.displayName.LocalizeBodyName() + "'s " + b;
					case ExperimentSituations.InSpaceLow:
						return " from space just above " + vessel.mainBody.displayName.LocalizeBodyName() + "'s " + b;
					default:
						return " while in space high over " + vessel.mainBody.displayName.LocalizeBodyName() + "'s " + b;
				}
			}
		}

		private string astCleanup(ExperimentSituations s, string b)
		{
			string a = "a";
			if (b == "Icy-Organic")
				a = "an";

			string c = " asteroid";
			if (b == "Comet")
				c = "";

			switch (s)
			{
				case ExperimentSituations.SrfLanded:
					return string.Format(" from the surface of {0} {1}{2}", a, b, c);
				case ExperimentSituations.InSpaceLow:
					return string.Format(" while in space near a {0} {1}{2}", a, b, c);
				default:
					return "";
			}
		}

		public virtual bool canConduct()
		{
			failMessage = "";
			if (Inoperable)
			{
				failMessage = "Experiment is no longer functional; must be reset at a science lab or returned to Kerbin";
				return false;
			}
			else if (Deployed)
			{
				failMessage = storageFullMessage;
				return false;
			}
			else if ((experimentNumber >= experimentLimit) && experimentLimit > 1)
			{
				failMessage = storageFullMessage;
				return false;
			}
			else if (storedScienceReports.Count > 0 && experimentLimit <= 1)
			{
				failMessage = storageFullMessage;
				return false;
			}
			else if ((sitMask & (int)getSituation()) == 0)
			{
				MonoBehaviour.print("[XXX 1] sitMask " + sitMask + " situation " + (int)getSituation());
				failMessage = customFailMessage;
				return false;
			}
			else if (scienceExp == null)
			{
				failMessage = "Error: Science Experiment Definition Invalid";
				Debug.LogError("[DMOS] Something Went Wrong Here; Null Experiment Returned; Please Report This On The KSP Forum With Output.log Data");
				return false;
			}
			else if (scienceExp.requireAtmosphere && !vessel.mainBody.atmosphere)
			{
				MonoBehaviour.print("[XXX 2] reqatmo " + scienceExp.requireAtmosphere + " inatmo " + vessel.mainBody.atmosphere);
				failMessage = customFailMessage;
				return false;
			}

			if (FlightGlobals.ActiveVessel.isEVA)
			{
				if (!ScienceUtil.RequiredUsageExternalAvailable(part.vessel, FlightGlobals.ActiveVessel, (ExperimentUsageReqs)usageReqMaskExternal, scienceExp, ref usageReqMessage))
				{
					failMessage = usageReqMessage;
					return false;
				}
			}

			return true;
		}

		private ScienceData makeScience(ExperimentSituations vesselSituation)
		{
			string biome = getBiome(vesselSituation);
			CelestialBody mainBody = vessel.mainBody;
			bool asteroids = false;
			DMAsteroidScience newAsteroid = null;

			//Check for asteroids and alter the biome and celestialbody values as necessary
			if (asteroidReports && (DMAsteroidScience.AsteroidGrappled || DMAsteroidScience.AsteroidNear))
			{
				newAsteroid = new DMAsteroidScience();
				asteroids = true;
				mainBody = newAsteroid.Body;
				biome = newAsteroid.AType + newAsteroid.ASeed.ToString();
			}

			ScienceData data = null;
			ScienceSubject sub = null;

			if (scienceExp == null)
			{
				Debug.LogError("[DMOS] Something Went Wrong Here; Null Experiment Returned; Please Report This On The KSP Forum With Output.log Data");
				return null;
			}

			sub = ResearchAndDevelopment.GetExperimentSubject(scienceExp, vesselSituation, mainBody, biome, "");
			if (sub == null)
			{
				Debug.LogError("[DMOS] Something Went Wrong Here; Null Subject Returned; Please Report This On The KSP Forum With Output.log Data");
				return null;
			}

			if (asteroids)
			{
				DMUtils.OnAsteroidScience.Fire(newAsteroid.AClass, experimentID);
				sub.title = scienceExp.experimentTitle + astCleanup(vesselSituation, newAsteroid.AType);
				registerDMScience(newAsteroid, sub);
				mainBody.bodyName = bodyNameFixed;
			}
			else
			{
				DMUtils.OnAnomalyScience.Fire(mainBody, experimentID, biome);
				sub.title = scienceExp.experimentTitle + situationCleanup(vesselSituation, biome);
				sub.subjectValue *= scienceBoost;
				sub.scienceCap = scienceExp.scienceCap * sub.subjectValue;
			}

			float dat = scienceExp.baseValue * sub.dataScale * totalScienceLevel;

			if (totalScienceLevel < 1)
			{
				float science = (dat * sub.subjectValue) / sub.dataScale;
				float max = sub.scienceCap * totalScienceLevel;
				if (sub.science >= max)
				{
					dat = 0.000001f;
				}
				else
				{
					float sci = Mathf.Max(Mathf.Min(science - sub.science, max), 0.000001f);
					dat = (sci * sub.dataScale) / sub.subjectValue;
					dat /= sub.scientificValue;
				}
			}

			data = new ScienceData(dat, xmitDataScalar, 0, sub.id, sub.title, false, part.flightID);

			return data;
		}

		private void registerDMScience(DMAsteroidScience newAst, ScienceSubject sub)
		{
			if (HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX)
				return;

			DMScienceData DMData = null;

			DMScienceData DMScience = DMScienceScenario.SciScenario.getDMScience(sub.title);
			if (DMScience != null)
			{
				sub.scientificValue *= DMScience.SciVal;
				DMData = DMScience;
			}

			if (DMData == null)
			{
				float astSciCap = scienceExp.scienceCap * 25f;
				DMScienceScenario.SciScenario.RecordNewScience(sub.title, scienceExp.baseValue, 1f, 0f, astSciCap);
				sub.scientificValue = 1f;
			}

			sub.subjectValue = newAst.SciMult;
			sub.scienceCap = scienceExp.scienceCap * sub.subjectValue;
			sub.science = Math.Max(0f, Math.Min(sub.scienceCap, sub.scienceCap - (sub.scienceCap * sub.scientificValue)));
		}

		#endregion

		#region Results Pages

		private void newResultPage(ScienceData data = null)
		{
			if (storedScienceReports.Count > 0)
			{
				if (data == null)
					data = storedScienceReports[dataIndex];
				ExperimentResultDialogPage page = new ExperimentResultDialogPage(part, data, data.baseTransmitValue, data.transmitBonus, (experimentsReturned >= (experimentLimit - 1)) && !rerunnable, transmitWarningText, true, new ScienceLabSearch(vessel, data), new Callback<ScienceData>(onDiscardData), new Callback<ScienceData>(onKeepData), new Callback<ScienceData>(onTransmitData), new Callback<ScienceData>(onSendToLab));
				resultsDialog = ExperimentsResultDialog.DisplayResult(page);
			}
		}

		new public void ReviewData()
		{
			dataIndex = 0;
			foreach (ScienceData data in storedScienceReports)
			{
				newResultPage();
				dataIndex++;
			}
		}

		new public void ReviewDataItem(ScienceData data)
		{
			dataIndex = 0;
			newResultPage(data);
		}

		new public void ReviewDataEvent()
		{
			ReviewData();
		}

		[KSPEvent(guiActive = true, guiName = "Review Initial Data", active = false)]
		public void ReviewInitialData()
		{
			if (scienceReports.Count > 0)
				initialResultsPage();
		}

		private void initialResultsPage()
		{
			if (scienceReports.Count > 0)
			{
				ScienceData data = scienceReports[0];
				ExperimentResultDialogPage page = new ExperimentResultDialogPage(part, data, data.baseTransmitValue, data.transmitBonus, (experimentsReturned >= (experimentLimit - 1)) && !rerunnable, transmitWarningText, true, new ScienceLabSearch(vessel, data), new Callback<ScienceData>(onDiscardInitialData), new Callback<ScienceData>(onKeepInitialData), new Callback<ScienceData>(onTransmitInitialData), new Callback<ScienceData>(onSendInitialToLab));
				resultsDialog = ExperimentsResultDialog.DisplayResult(page);
			}
		}

		private void onDiscardData(ScienceData data)
		{
			resultsDialog = null;

			if (storedScienceReports.Count > 0)
			{
				if (experimentLimit != 0)
				{
					if (!string.IsNullOrEmpty(sampleEmptyAnim) && anim2 != null)
						secondaryAnimator(sampleEmptyAnim, animSpeed, 1f - (experimentNumber * (1f / experimentLimit)), anim2[sampleEmptyAnim].length / experimentLimit);
					else if (!string.IsNullOrEmpty(sampleAnim) && anim2 != null)
						secondaryAnimator(sampleAnim, -1f * animSpeed, experimentNumber * (1f / experimentLimit), anim2[sampleAnim].length / experimentLimit);
					if (!string.IsNullOrEmpty(indicatorAnim) && anim != null)
						secondaryAnimator(indicatorAnim, -1f * animSpeed, experimentNumber * (1f / experimentLimit), anim[indicatorAnim].length / experimentLimit);
				}
				storedScienceReports.Remove(data);
				if (keepDeployedMode == 0) retractEvent();
				experimentNumber--;
				if (experimentNumber < 0)
					experimentNumber = 0;
				Deployed = false;
			}
		}

		private void onKeepData(ScienceData data)
		{
			resultsDialog = null;
		}

		private void onTransmitData(ScienceData data)
		{
			resultsDialog = null;
			IScienceDataTransmitter bestTransmitter = ScienceUtil.GetBestTransmitter(vessel);
			if (bestTransmitter != null)
			{
				bestTransmitter.TransmitData(new List<ScienceData> { data });
				DumpData(data);
			}
			else if (CommNet.CommNetScenario.CommNetEnabled)
				ScreenMessages.PostScreenMessage("No usable, in-range Comms Devices on this vessel. Cannot Transmit Data.", 3f, ScreenMessageStyle.UPPER_CENTER);
			else
				ScreenMessages.PostScreenMessage("No Comms Devices on this vessel. Cannot Transmit Data.", 3f, ScreenMessageStyle.UPPER_CENTER);
		}

		private void onSendToLab(ScienceData data)
		{
			resultsDialog = null;
			ScienceLabSearch labSearch = new ScienceLabSearch(vessel, data);

			if (labSearch.NextLabForDataFound)
			{
				StartCoroutine(labSearch.NextLabForData.ProcessData(data, null));
				DumpData(data);
			}
			else
				labSearch.PostErrorToScreen();
		}

		private void onDiscardInitialData(ScienceData data)
		{
			resultsDialog = null;
			if (scienceReports.Count > 0)
			{
				scienceReports.Remove(data);
				if (keepDeployedMode == 0) retractEvent();
				Deployed = false;
			}
		}

		private void onKeepInitialData(ScienceData data)
		{
			resultsDialog = null;
			if (experimentNumber >= experimentLimit)
			{
				ScreenMessages.PostScreenMessage(storageFullMessage, 5f, ScreenMessageStyle.UPPER_CENTER);
				initialResultsPage();
			}
			else if (scienceReports.Count > 0)
			{
				if (experimentLimit != 0 && anim2 != null)
				{
					if (!string.IsNullOrEmpty(sampleAnim))
						secondaryAnimator(sampleAnim, animSpeed, experimentNumber * (1f / experimentLimit), anim2[sampleAnim].length / experimentLimit);
					if (!string.IsNullOrEmpty(indicatorAnim))
						secondaryAnimator(indicatorAnim, animSpeed, experimentNumber * (1f / experimentLimit), anim2[indicatorAnim].length / experimentLimit);
				}
				storedScienceReports.Add(data);
				scienceReports.Remove(data);
				experimentNumber++;
			}
		}

		private void onTransmitInitialData(ScienceData data)
		{
			resultsDialog = null;
			
			IScienceDataTransmitter bestTransmitter = ScienceUtil.GetBestTransmitter(vessel);
			if (bestTransmitter != null)
			{
				if (experimentLimit != 0 && anim2 != null)
				{
					if (!string.IsNullOrEmpty(sampleAnim))
						secondaryAnimator(sampleAnim, animSpeed, experimentNumber * (1f / experimentLimit), anim2[sampleAnim].length / experimentLimit);
					if (!string.IsNullOrEmpty(indicatorAnim))
						secondaryAnimator(indicatorAnim, animSpeed, experimentNumber * (1f / experimentLimit), anim2[indicatorAnim].length / experimentLimit);
				}

				bestTransmitter.TransmitData(new List<ScienceData> { data });
				DumpInitialData(data);
				experimentNumber++;
			}
			else if (CommNet.CommNetScenario.CommNetEnabled)
				ScreenMessages.PostScreenMessage("No usable, in-range Comms Devices on this vessel. Cannot Transmit Data.", 3f, ScreenMessageStyle.UPPER_CENTER);
			else
				ScreenMessages.PostScreenMessage("No Comms Devices on this vessel. Cannot Transmit Data.", 3f, ScreenMessageStyle.UPPER_CENTER);

		}

		private void onSendInitialToLab(ScienceData data)
		{
			resultsDialog = null;
			ScienceLabSearch labSearch = new ScienceLabSearch(vessel, data);

			if (labSearch.NextLabForDataFound)
			{
				if (experimentLimit != 0 && anim2 != null)
				{
					if (!string.IsNullOrEmpty(sampleAnim))
						secondaryAnimator(sampleAnim, animSpeed, experimentNumber * (1f / experimentLimit), anim2[sampleAnim].length / experimentLimit);
					if (!string.IsNullOrEmpty(indicatorAnim))
						secondaryAnimator(indicatorAnim, animSpeed, experimentNumber * (1f / experimentLimit), anim2[indicatorAnim].length / experimentLimit);
				}

				StartCoroutine(labSearch.NextLabForData.ProcessData(data, null));
				DumpInitialData(data);
				experimentNumber++;
			}
			else
				labSearch.PostErrorToScreen();
		}

		#endregion

		#region IScienceDataContainer

		ScienceData[] IScienceDataContainer.GetData()
		{
			return GetData();
		}

		int IScienceDataContainer.GetScienceCount()
		{
			return GetScienceCount();
		}

		bool IScienceDataContainer.IsRerunnable()
		{
			return IsRerunnable();
		}

		void IScienceDataContainer.ReviewData()
		{
			ReviewData();
		}

		void IScienceDataContainer.ReturnData(ScienceData data)
		{
			ReturnData(data);
		}

		void IScienceDataContainer.ReviewDataItem(ScienceData data)
		{
			ReviewDataItem(data);
		}

		void IScienceDataContainer.DumpData(ScienceData data)
		{
			DumpData(data);
		}

		new public ScienceData[] GetData()
		{
			List<ScienceData> DataList = new List<ScienceData>();

			DataList.AddRange(storedScienceReports);
			DataList.AddRange(scienceReports);

			return DataList.ToArray();
		}

		new public int GetScienceCount()
		{
			return storedScienceReports.Count + scienceReports.Count;
		}

		new public void ReturnData(ScienceData data)
		{
			ReturnDataOverRide(data);
		}

		protected virtual void ReturnDataOverRide(ScienceData data)
		{
			if (data == null)
				return;

			storedScienceReports.Add(data);

			experimentsReturned--;

			if (experimentsReturned < 0)
				experimentsReturned = 0;

			Inoperable = false;
			lastInOperableState = false;

			if (experimentLimit <= 1)
				Deployed = true;
			else
			{
				Deployed = experimentNumber >= experimentLimit;
			}
		}

		protected virtual void DumpAllData(List<ScienceData> data)
		{
			foreach(ScienceData d in data)
				experimentsReturned++;
			Inoperable = !IsRerunnable();
			lastInOperableState = Inoperable;
			Deployed = Inoperable;
			data.Clear();
		}

		new public void DumpData(ScienceData data)
		{
			DumpDataOverride(data);
		}

		protected virtual void DumpDataOverride(ScienceData data)
		{
			if (storedScienceReports.Contains(data))
			{
				experimentsReturned++;
				Inoperable = !IsRerunnable();
				lastInOperableState = Inoperable;
				Deployed = Inoperable;
				storedScienceReports.Remove(data);
			}
			else if (scienceReports.Contains(data))
			{
				if (experimentLimit != 0 && anim2 != null)
				{
					if (!string.IsNullOrEmpty(sampleAnim))
						secondaryAnimator(sampleAnim, animSpeed, experimentNumber * (1f / experimentLimit), anim2[sampleAnim].length / experimentLimit);
					if (!string.IsNullOrEmpty(indicatorAnim))
						secondaryAnimator(indicatorAnim, animSpeed, experimentNumber * (1f / experimentLimit), anim2[indicatorAnim].length / experimentLimit);
				}

				DumpInitialData(data);
				experimentNumber++;
			}
		}

		protected virtual void DumpInitialData(ScienceData data)
		{
			if (scienceReports.Contains(data))
			{
				experimentsReturned++;
				Inoperable = !IsRerunnable();
				lastInOperableState = Inoperable;
				Deployed = Inoperable;
				scienceReports.Remove(data);
			}
		}

		new protected bool IsRerunnable()
		{
			if (rerunnable)
				return true;
			else
				return experimentsReturned < experimentLimit;
		}

		#endregion

	}
}
