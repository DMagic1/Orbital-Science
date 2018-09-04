﻿#region license
/* DMagic Orbital Science - DMBasicScienceModule
 * Part Module to handle basic aspects of science reports
 *
 * Copyright (c) 2015, David Grandy <david.grandy@gmail.com>
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
using KSP.UI.Screens.Flight.Dialogs;

namespace DMagic.Part_Modules
{
	public class DMBasicScienceModule : PartModule, IScienceDataContainer
	{
		[KSPField]
		public string animationName = "";
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
		public bool deployExternal = true;
		[KSPField]
		public string collectActionName;
		[KSPField]
		public string experimentActionName;
		[KSPField]
		public float interactionRange;
		[KSPField]
		public string resetActionName;
		[KSPField]
		public bool resettable;
		[KSPField]
		public bool resettableOnEVA;
		[KSPField]
		public string reviewActionName;
		[KSPField]
		public string experimentID = "";
		[KSPField]
		public string experimentResource = "ElectricCharge";
		[KSPField]
		public string customFailMessage = "";
		[KSPField]
		public string experimentFullMessage = "No more data can be stored";
		[KSPField]
		public bool rerunnable = true;
		[KSPField]
		public bool dataIsCollectable = true;
		[KSPField]
		public float resourceCost = 0f;
		[KSPField]
		public float transmitValue = 1f;
		[KSPField(isPersistant = true)]
		public bool IsDeployed = false;
		[KSPField(isPersistant = true)]
		public bool Inoperable = false;
		[KSPField(isPersistant = true)]
		public bool Deployed = false;
		[KSPField]
		public int usageReqMaskExternal = -1;
		[KSPField]
		public string usageReqMessage = "";

		protected ScienceExperiment exp = null;
		protected List<ScienceData> scienceReports = new List<ScienceData>();
		private ExperimentsResultDialog resultsDialog;
		private bool hasContainer;

		private Animation anim;

		/// <summary>
		/// For external use to determine if a module can conduct science
		/// </summary>
		/// <param name="MSE">The base PartModule instance</param>
		/// <returns>True if the experiment can be conducted under current conditions</returns>
		public static bool conduct(PartModule MSE)
		{
			if (MSE.GetType() != typeof(DMBasicScienceModule))
				return false;

			DMBasicScienceModule DMBasicMod = (DMBasicScienceModule)MSE;
			try
			{
				return DMBasicMod.canConduct();
			}
			catch (Exception e)
			{
				Debug.LogWarning("[DM] Error in casting PartModule to DMBasicScienceModule; Invalid Part Module... : " + e);
				return false;
			}
		}

		public override void OnAwake()
		{
			GameEvents.onGamePause.Add(onPause);
			GameEvents.onGameUnpause.Add(onUnPause);
			GameEvents.onVesselStandardModification.Add(onVesselModified);
		}

		public override void OnStart(PartModule.StartState state)
		{
			anim = DMUtils.GetAnimation(part, animationName);

			if (state == StartState.Editor)
				editorSetup();
			else
			{
				setup();
				if (IsDeployed)
					primaryAnimator(1f, 1f, WrapMode.Default, animationName, anim);
                
			    findContainers();
			}

		}

		public override void OnSave(ConfigNode node)
		{
			node.RemoveNodes("ScienceData");
			foreach (ScienceData storedData in scienceReports)
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
					scienceReports.Add(data);
				}
			}
		}

		protected virtual void OnDestroy()
		{
			GameEvents.onGamePause.Remove(onPause);
			GameEvents.onGameUnpause.Remove(onUnPause);
			GameEvents.onVesselStandardModification.Remove(onVesselModified);
		}

		public override string GetInfo()
		{
			string info = base.GetInfo();

			info += string.Format("Transmission: {0:P0}\n", transmitValue);

			return info;
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

		private void onVesselModified(Vessel v)
		{
			if (v == vessel && HighLogic.LoadedSceneIsFlight)
				findContainers();
		}

		private void findContainers()
		{
			for (int i = vessel.Parts.Count - 1; i >= 0; i--)
			{
				Part p = vessel.Parts[i];

				if (p == null)
					continue;

				if (p.State == PartStates.DEAD)
					continue;

				ModuleScienceContainer container = p.FindModuleImplementing<ModuleScienceContainer>();

				if (container == null)
					continue;

				hasContainer = container.canBeTransferredToInVessel;
				break;
			}
		}

		protected virtual void EventsCheck()
		{
			Events["ResetExperiment"].active = scienceReports.Count > 0;
			Events["CollectDataExternalEvent"].active = scienceReports.Count > 0 && dataIsCollectable;
			Events["DeployExperiment"].active = scienceReports.Count == 0 && !Deployed && !Inoperable;
			Events["ReviewDataEvent"].active = scienceReports.Count > 0;
			Events["TransferDataEvent"].active = hasContainer && dataIsCollectable && scienceReports.Count > 0;
		}

		protected virtual void setup()
		{
			Events["deployEvent"].guiActive = showStartEvent;
			Events["retractEvent"].guiActive = showEndEvent;
			Events["toggleEvent"].guiActive = showToggleEvent;
			Events["deployEvent"].guiActiveUnfocused = Events["deployEvent"].externalToEVAOnly  = deployExternal;
			Events["retractEvent"].guiActiveUnfocused = Events["retractEvent"].externalToEVAOnly = deployExternal;
			Events["toggleEvent"].guiActiveUnfocused = Events["toggleEvent"].externalToEVAOnly = deployExternal;
			Events["deployEvent"].guiName = startEventGUIName;
			Events["retractEvent"].guiName = endEventGUIName;
			Events["toggleEvent"].guiName = toggleEventGUIName;
			Events["CollectDataExternalEvent"].guiName = collectActionName;
			Events["ResetExperiment"].guiName = resetActionName;
			Events["DeployExperiment"].guiName = experimentActionName;
			Events["DeployExperiment"].unfocusedRange = interactionRange;
			Actions["deployAction"].guiName = startEventGUIName;
			Actions["retractAction"].guiName = endEventGUIName;
			Actions["toggleAction"].guiName = toggleEventGUIName;
			Actions["DeployAction"].guiName = experimentActionName;
			if (!string.IsNullOrEmpty(experimentID))
				exp = ResearchAndDevelopment.GetExperiment(experimentID);
		}

		protected virtual void editorSetup()
		{
			Events["deployEvent"].guiActive = showEditorEvents && showStartEvent;
			Events["retractEvent"].guiActive = showEditorEvents && showEndEvent;
			Events["toggleEvent"].guiActive = showEditorEvents && showToggleEvent;
			Events["deployEvent"].guiName = startEventGUIName;
			Events["retractEvent"].guiName = endEventGUIName;
			Events["toggleEvent"].guiName = toggleEventGUIName;
			Actions["deployAction"].active = showStartEvent;
			Actions["retractAction"].active = showEndEvent;
			Actions["toggleAction"].active = showToggleEvent;
			Actions["deployAction"].guiName = startEventGUIName;
			Actions["retractAction"].guiName = endEventGUIName;
			Actions["toggleAction"].guiName = toggleEventGUIName;
			Actions["ResetAction"].active = true;
			Actions["DeployAction"].guiName = experimentActionName;
		}

		#region Animator

		protected virtual void primaryAnimator(float speed, float time, WrapMode wrap, string name, Animation a)
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

		#endregion

		#region Events

		[KSPEvent(guiActive = true, guiName = "Deploy", active = true)]
		public virtual void deployEvent()
		{
			primaryAnimator(animSpeed * 1f, 0f, WrapMode.Default, animationName, anim);
			IsDeployed = true;
			Events["deployEvent"].active = false;
			Events["retractEvent"].active = true;
		}

		[KSPAction("Deploy")]
		public void deployAction(KSPActionParam param)
		{
			deployEvent();
		}

		[KSPEvent(guiActive = true, guiName = "Retract", active = false)]
		public virtual void retractEvent()
		{
			primaryAnimator(-1f * animSpeed, 1f, WrapMode.Default, animationName, anim);
			IsDeployed = false;
			Events["deployEvent"].active = true;
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
			if (IsDeployed)
				retractEvent();
			else
				deployEvent();
		}

		[KSPAction("Toggle")]
		public void toggleAction(KSPActionParam Param)
		{
			toggleEvent();
		}

		[KSPEvent(guiActive = true, guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Reset Experiment", active = false)]
		public void ResetExperiment()
		{
			if (scienceReports.Count > 0)
			{
				scienceReports.Clear();
				Deployed = false;
			}
		}

		[KSPAction("Reset Experiment")]
		public void ResetAction(KSPActionParam param)
		{
			ResetExperiment();
		}

		[KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Collect Data", active = false)]
		public void CollectDataExternalEvent()
		{
			List<ModuleScienceContainer> EVACont = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceContainer>();
			if (scienceReports.Count > 0)
			{
				if (EVACont.First().StoreData(new List<IScienceDataContainer> { this }, false))
					DumpData(scienceReports[0]);
			}
		}

		[KSPEvent(guiActive = true, guiName = "Transfer Data", active = false)]
		public void TransferDataEvent()
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

			if (scienceReports.Count <= 0)
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

			if (!rerunnable)
			{
				List<DialogGUIBase> dialog = new List<DialogGUIBase>();
				dialog.Add(new DialogGUIButton<ModuleScienceContainer>("Remove Data", new Callback<ModuleScienceContainer>(onTransferData), container));
				dialog.Add(new DialogGUIButton("Cancel", null, true));

				PopupDialog.SpawnPopupDialog(
					new Vector2(0.5f, 0.5f),
					new Vector2(0.5f, 0.5f),
					new MultiOptionDialog(
						"TransferWarning",
						"Removing the experiment data will render this module inoperable.\n\nRestoring functionality will require a Scientist.",
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

			int i = scienceReports.Count;

			if (target.StoreData(new List<IScienceDataContainer> { this }, false))
				ScreenMessages.PostScreenMessage(string.Format("[{0}]: {1} Data stored.", target.part.partInfo.title, i), 6, ScreenMessageStyle.UPPER_LEFT);
			else
				ScreenMessages.PostScreenMessage(string.Format("<color=orange>[{0}]: Not all data was stored.</color>", target.part.partInfo.title), 6, ScreenMessageStyle.UPPER_LEFT);
		}

		[KSPEvent(guiActive = true, guiName = "Review Data", active = false)]
		public void ReviewDataEvent()
		{
			ReviewData();
		}

		[KSPAction("Review Data")]
		public void ReviewDataAction(KSPActionParam param)
		{
			ReviewData();
		}

		[KSPEvent(guiActive = true, guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Deploy Experiment", active = false)]
		public void DeployExperiment()
		{
			gatherScienceData();
		}

		public virtual void gatherScienceData(bool silent = false)
		{

		}

		[KSPAction("Deploy Experiment")]
		public void DeployAction(KSPActionParam param)
		{
			DeployExperiment();
		}

		#endregion

		public virtual bool canConduct()
		{
			return false;
		}

		#region Results Page

		private void experimentResultsPage(ScienceData data)
		{
			if (scienceReports.Count > 0)
			{
				ExperimentResultDialogPage page = new ExperimentResultDialogPage(part, data, data.baseTransmitValue, data.transmitBonus, false, "", true, new ScienceLabSearch(vessel, data), new Callback<ScienceData>(onDiscardData), new Callback<ScienceData>(onKeepData), new Callback<ScienceData>(onTransmitData), new Callback<ScienceData>(onSendToLab));
				resultsDialog = ExperimentsResultDialog.DisplayResult(page);
			}
		}

		private void onDiscardData(ScienceData data)
		{
			resultsDialog = null;
			if (scienceReports.Count > 0)
			{
				scienceReports.Clear();
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

		#endregion

		#region IScienceDataContainer

		public void ReviewData()
		{
			if (scienceReports.Count > 0)
				experimentResultsPage(scienceReports[0]);
		}

		public void ReviewDataItem(ScienceData data)
		{
			ReviewData();
		}

		public void ReturnData(ScienceData data)
		{
			if (data == null)
				return;

			scienceReports.Add(data);
			Inoperable = false;
			Deployed = true;
		}

		public bool IsRerunnable()
		{
			return rerunnable;
		}

		public int GetScienceCount()
		{
			return scienceReports.Count;
		}

		public ScienceData[] GetData()
		{
			return scienceReports.ToArray();
		}

		public void DumpData(ScienceData data)
		{
			if (scienceReports.Count > 0)
			{
				Inoperable = !IsRerunnable();
				Deployed = Inoperable;
				scienceReports.Remove(data);
			}
		}

		#endregion
	}
}
