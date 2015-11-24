using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DMagic.Part_Modules
{
	public class DMBasicScienceModule : PartModule, IScienceDataContainer
	{
		[KSPField]
		public string animationName = null;
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

		private Animation anim;

		public override void OnStart(PartModule.StartState state)
		{
			if (!string.IsNullOrEmpty(animationName))
				anim = part.FindModelAnimators(animationName)[0];

			if (state == StartState.Editor)
				editorSetup();
			else
			{
				setup();
				if (IsDeployed)
					primaryAnimator(1f, 1f, WrapMode.Default, animationName, anim);
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

		protected virtual void EventsCheck()
		{
			Events["ResetExperiment"].active = scienceReports.Count > 0;
			Events["CollectDataExternalEvent"].active = scienceReports.Count > 0 && dataIsCollectable;
			Events["DeployExperiment"].active = scienceReports.Count == 0 && !Deployed && !Inoperable;
			Events["ReviewDataEvent"].active = scienceReports.Count > 0;
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
		public virtual void DeployExperiment()
		{

		}

		[KSPAction("Deploy Experiment")]
		public void DeployAction(KSPActionParam param)
		{
			DeployExperiment();
		}

		#endregion

		#region Results Page

		private void experimentResultsPage(ScienceData data)
		{
			if (scienceReports.Count > 0)
			{
				ExperimentResultDialogPage page = new ExperimentResultDialogPage(part, data, transmitValue, 0f, false, "", true, ModuleScienceLab.IsLabData(vessel, data), new Callback<ScienceData>(onDiscardData), new Callback<ScienceData>(onKeepData), new Callback<ScienceData>(onTransmitData), new Callback<ScienceData>(onSendToLab));
				ExperimentsResultDialog.DisplayResult(page);
			}
		}

		private void onDiscardData(ScienceData data)
		{
			if (scienceReports.Count > 0)
			{
				scienceReports.Clear();
				Deployed = false;
			}
		}

		private void onKeepData(ScienceData data)
		{
		}

		private void onTransmitData(ScienceData data)
		{
			List<IScienceDataTransmitter> tranList = vessel.FindPartModulesImplementing<IScienceDataTransmitter>();
			if (tranList.Count > 0 && scienceReports.Count > 0)
			{
				tranList.OrderBy(ScienceUtil.GetTransmitterScore).First().TransmitData(new List<ScienceData> { data });
				DumpData(data);
			}
			else
				ScreenMessages.PostScreenMessage("No transmitters available on this vessel.", 5f, ScreenMessageStyle.UPPER_LEFT);
		}

		private void onSendToLab(ScienceData data)
		{
			List<ModuleScienceLab> labList = vessel.FindPartModulesImplementing<ModuleScienceLab>();
			if (checkLabOps() && scienceReports.Count > 0)
				labList.OrderBy(ScienceUtil.GetLabScore).First().StartCoroutine(labList.First().ProcessData(data, new Callback<ScienceData>(onComplete)));
			else
				ScreenMessages.PostScreenMessage("No operational lab modules on this vessel. Cannot analyze data.", 5f, ScreenMessageStyle.UPPER_CENTER);
		}

		private void onComplete(ScienceData data)
		{
			ReviewData();
		}

		private bool checkLabOps()
		{
			List<ModuleScienceLab> labList = vessel.FindPartModulesImplementing<ModuleScienceLab>();
			for (int i = 0; i < labList.Count; i++)
			{
				if (labList[i].IsOperational())
					return true;
			}
			return false;
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
