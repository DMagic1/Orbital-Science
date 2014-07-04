using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DMagic
{
	class DMScienceMultiple: ModuleScienceExperiment, IScienceDataContainer
	{
		#region Fields

		[KSPField]
		public string customFailMessage = null;
		[KSPField]
		public string deployingMessage = null;
		[KSPField(isPersistant = true)]
		public bool IsDeployed;
		[KSPField]
		public string animationName = null;
		[KSPField]
		public string sampleAnim = null;
		[KSPField]
		public string indicatorAnim = null;
		[KSPField]
		public string sampleEmptyAnim = null;
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
		public string resourceExperiment = "ElectricCharge";
		[KSPField]
		public float resourceExpCost = 0;
		[KSPField]
		public bool asteroidReports = false;
		[KSPField]
		public bool asteroidTypeDependent = false;
		[KSPField(isPersistant = true)]
		public int experimentsCollected = 0;
		[KSPField]
		public string storageFullMessage = "No more samples can be collected";

		private Animation anim;
		private Animation anim2;
		private ScienceExperiment scienceExp;
		private bool resourceOn = false;
		private int dataIndex = 0;
		protected List<ScienceData> scienceReports = new List<ScienceData>();
		protected List<ScienceData> storedScienceReports = new List<ScienceData>(); 
		private const string bodyNameFixed = "Eeloo";
		protected int experimentLimit = 2;
		protected int sitMask = 0;
		protected int bioMask = 0;
		private bool lastInOperableState = false;
		protected float scienceBoost = 1f;

		#endregion

		#region PartModule

		public override void OnStart(StartState state)
		{
			this.part.force_activate();
			if (!string.IsNullOrEmpty(animationName))
				anim = part.FindModelAnimators(animationName)[0];
			if (!string.IsNullOrEmpty(sampleAnim)) {
				anim2 = part.FindModelAnimators(sampleAnim)[0];
				secondaryAnimator(sampleAnim, 0f, experimentsCollected * (1f / experimentLimit), 1f);
			}
			if (!string.IsNullOrEmpty(indicatorAnim)) {
				anim2 = part.FindModelAnimators(indicatorAnim)[0];
				secondaryAnimator(indicatorAnim, 0f, experimentsCollected * (1f / experimentLimit), 1f);
			}
			if (!string.IsNullOrEmpty(sampleEmptyAnim))
				anim2 = part.FindModelAnimators(sampleEmptyAnim)[0];
			if (state == StartState.Editor) editorSetup();
			else {
				setup();
				if (IsDeployed)
					primaryAnimator(1f, 1f, WrapMode.Default, animationName);
			}
		}

		public override void OnSave(ConfigNode node)
		{
			node.RemoveNodes("ScienceData");
			foreach (ScienceData storedData in storedScienceReports) {
				ConfigNode storedDataNode = node.AddNode("ScienceData");
				storedData.Save(storedDataNode);
			}
		}

		public override void OnLoad(ConfigNode node)
		{
			if (node.HasNode("ScienceData")) {
				foreach (ConfigNode storedDataNode in node.GetNodes("ScienceData")) {
					ScienceData data = new ScienceData(storedDataNode);
					storedScienceReports.Add(data);
				}
			}
		}

		public override void OnUpdate()
		{
			if (resourceOn) {
				if (PartResourceLibrary.Instance.GetDefinition(resourceExperiment) != null) {
					float cost = resourceExpCost * Time.deltaTime;
					if (part.RequestResource(resourceExperiment, cost) < cost) {
						StopCoroutine("WaitForAnimation");
						resourceOn = false;
						ScreenMessages.PostScreenMessage("Not enough " + resourceExperiment + ", shutting down experiment", 4f, ScreenMessageStyle.UPPER_CENTER);
						if (keepDeployedMode == 0 || keepDeployedMode == 1) retractEvent();
					}
				}
			}
			//Durrrr, gameEvents sure are helpful....
			if (Inoperable)
				lastInOperableState = true;
			else if (lastInOperableState) {
				lastInOperableState = false;
				if (!string.IsNullOrEmpty(sampleEmptyAnim))
					secondaryAnimator(sampleEmptyAnim, animSpeed, 1f - (experimentsCollected * (1f / experimentLimit)), experimentsCollected * (anim2[sampleEmptyAnim].length / experimentLimit));
				else if (!string.IsNullOrEmpty(sampleAnim))
					secondaryAnimator(sampleAnim, -1f * animSpeed, experimentsCollected * (1f / experimentLimit), experimentsCollected * (anim2[sampleAnim].length / experimentLimit));
				if (!string.IsNullOrEmpty(indicatorAnim))
					secondaryAnimator(indicatorAnim, -1f * animSpeed, experimentsCollected * (1f / experimentLimit), experimentsCollected * (anim2[indicatorAnim].length / experimentLimit));
				experimentsCollected = 0;
			}
			eventsCheck();
		}

		public override string GetInfo()
		{
			if (resourceExpCost > 0) {
				string info = base.GetInfo();
				info += "Requires:\n-" + resourceExperiment + ": " + resourceExpCost.ToString() + "/s for " + waitForAnimationTime.ToString() + "s\n";
				return info;
			}
			else return base.GetInfo();
		}

		private void setup()
		{
			Events["deployEvent"].guiActive = showStartEvent;
			Events["retractEvent"].guiActive = showEndEvent;
			Events["toggleEvent"].guiActive = showToggleEvent;
			Events["deployEvent"].guiName = startEventGUIName;
			Events["retractEvent"].guiName = endEventGUIName;
			Events["toggleEvent"].guiName = toggleEventGUIName;
			if (waitForAnimationTime == -1)
				waitForAnimationTime = anim[animationName].length / animSpeed;
			if (experimentID != null)
				scienceExp = ResearchAndDevelopment.GetExperiment(experimentID);
			if (FlightGlobals.Bodies[16].bodyName != "Eeloo")
				FlightGlobals.Bodies[16].bodyName = bodyNameFixed;
		}

		private void editorSetup()
		{
			Actions["deployAction"].active = showStartEvent;
			Actions["retractAction"].active = showEndEvent;
			Actions["toggleAction"].active = showToggleEvent;
			Actions["deployAction"].guiName = startEventGUIName;
			Actions["retractAction"].guiName = endEventGUIName;
			Actions["toggleAction"].guiName = toggleEventGUIName;
			Actions["ResetAction"].active = false;
			Events["editorDeployEvent"].guiName = startEventGUIName;
			Events["editorRetractEvent"].guiName = endEventGUIName;
			Events["editorDeployEvent"].active = showEditorEvents;
			Events["editorRetractEvent"].active = false;
			}

		private void eventsCheck()
		{
			Events["ResetExperiment"].active = false;
			Events["ResetExperimentExternal"].active = storedScienceReports.Count > 0;
			Events["CollectDataExternalEvent"].active = storedScienceReports.Count > 0;
			Events["DeployExperiment"].active = !Inoperable;
			Events["ReviewDataEvent"].active = storedScienceReports.Count > 0;
			Events["ReviewInitialData"].active = scienceReports.Count > 0;
		}

		#endregion

		#region Animators

		private void primaryAnimator(float speed, float time, WrapMode wrap, string name)
		{
			if (anim != null) {
				anim[name].speed = speed;
				if (!anim.IsPlaying(name)) {
					anim[name].wrapMode = wrap;
					anim[name].normalizedTime = time;
					anim.Blend(name, 1f);
				}
			}
		}

		private void secondaryAnimator(string whichAnim, float sampleSpeed, float sampleTime, float waitTime)
		{
			if (anim2 != null) {
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
		public void deployEvent()
		{
			primaryAnimator(animSpeed * 1f, 0f, WrapMode.Default, animationName);
			IsDeployed = !oneWayAnimation;
			Events["deployEvent"].active = oneWayAnimation;
			Events["retractEvent"].active = showEndEvent;
		}

		[KSPAction("Deploy")]
		public void deployAction(KSPActionParam param)
		{
			deployEvent();
		}

		[KSPEvent(guiActive = true, guiName = "Retract", active = false)]
		public void retractEvent()
		{
			if (oneWayAnimation) return;
			primaryAnimator(-1f * animSpeed, 1f, WrapMode.Default, animationName);
			IsDeployed = false;
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
		public void editorDeployEvent()
		{
			deployEvent();
			IsDeployed = false;
			Events["editorDeployEvent"].active = oneWayAnimation;
			Events["editorRetractEvent"].active = !oneWayAnimation;
		}

		[KSPEvent(guiActiveEditor = true, guiName = "Retract", active = false)]
		public void editorRetractEvent()
		{
			retractEvent();
			Events["editorDeployEvent"].active = true;
			Events["editorRetractEvent"].active = false;
		}

		#endregion

		#region Science Events

		new public void ResetExperiment()
		{
		}

		new public void ResetAction(KSPActionParam param)
		{
			ResetExperiment();
		}

		new public void ResetExperimentExternal()
		{
			if (storedScienceReports.Count > 0) {
				if (!string.IsNullOrEmpty(sampleEmptyAnim))
					secondaryAnimator(sampleEmptyAnim, animSpeed, 1f - (experimentsCollected * (1f / experimentLimit)), experimentsCollected * (anim2[sampleEmptyAnim].length / experimentLimit));
				else if (!string.IsNullOrEmpty(sampleAnim))
					secondaryAnimator(sampleAnim, -1f * animSpeed, experimentsCollected * (1f / experimentLimit), experimentsCollected * (anim2[sampleAnim].length / experimentLimit));
				if (!string.IsNullOrEmpty(indicatorAnim))
					secondaryAnimator(indicatorAnim, -1f * animSpeed, experimentsCollected * (1f / experimentLimit), experimentsCollected * (anim2[indicatorAnim].length / experimentLimit));
				foreach (ScienceData data in storedScienceReports) {
					storedScienceReports.Remove(data);
					experimentsCollected--;
				}
				if (experimentsCollected < 0)
					experimentsCollected = 0;
			}
		}

		new public void CollectDataExternalEvent()
		{
			List<ModuleScienceContainer> EVACont = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceContainer>();
			if (storedScienceReports.Count > 0) {
				if (EVACont.First().StoreData(new List<IScienceDataContainer> { this }, false)) DumpAllData(storedScienceReports);
				storedScienceReports.Clear();
			}
		}

		#endregion

		#region Science Experiment Setup

		new protected void DeployExperiment()
		{
			if (Inoperable)
				ScreenMessages.PostScreenMessage("Experiment is no longer functional; must be reset at a science lab or returned to Kerbin", 5f, ScreenMessageStyle.UPPER_CENTER);
			else if (experimentsCollected >= experimentLimit)
				ScreenMessages.PostScreenMessage(storageFullMessage, 5f, ScreenMessageStyle.UPPER_CENTER);
			else if (DMScienceUtils.canConduct(experimentsCollected, experimentLimit, sitMask, asteroidReports, vessel)) {
				if (experimentAnimation) {
					if (anim.IsPlaying(animationName)) return;
					else if (!IsDeployed) {
						deployEvent();
						if (!string.IsNullOrEmpty(deployingMessage))
							ScreenMessages.PostScreenMessage(deployingMessage, 5f, ScreenMessageStyle.UPPER_CENTER);
						if (experimentWaitForAnimation) {
							if (resourceExpCost > 0)
								resourceOn = true;
							StartCoroutine("WaitForAnimation", waitForAnimationTime);
						}
						else
							runExperiment();
					}
					else if (resourceExpCost > 0) {
						resourceOn = true;
						StartCoroutine("WaitForAnimation", waitForAnimationTime);
					}
					else runExperiment();
				}
				else runExperiment();
			}
			else if (!string.IsNullOrEmpty(customFailMessage))
				ScreenMessages.PostScreenMessage(customFailMessage, 5f, ScreenMessageStyle.UPPER_CENTER);
		}

		new public void DeployAction(KSPActionParam param)
		{
			DeployExperiment();
		}

		private IEnumerator WaitForAnimation(float waitTime)
		{
			yield return new WaitForSeconds(waitTime);
			resourceOn = false;
			runExperiment();
		}

		private void runExperiment()
		{
			ScienceData data = DMScienceUtils.makeScience(asteroidReports, asteroidTypeDependent, vessel, bioMask, experimentID, xmitDataScalar, scienceBoost);
			scienceReports.Add(data);
			initialResultsPage();
			if (keepDeployedMode == 1) retractEvent();
		}

		#endregion

		#region Results Pages

		private void newResultPage()
		{
			if (storedScienceReports.Count > 0) {
				ScienceData data = storedScienceReports[dataIndex];
				ExperimentResultDialogPage page = new ExperimentResultDialogPage(part, data, data.transmitValue, data.labBoost, !rerunnable, transmitWarningText, true, data.labBoost < 1 && checkLabOps() && xmitDataScalar < 1, new Callback<ScienceData>(onDiscardData), new Callback<ScienceData>(onKeepData), new Callback<ScienceData>(onTransmitData), new Callback<ScienceData>(onSendToLab));
				ExperimentsResultDialog.DisplayResult(page);
			}
		}

		new public void ReviewData()
		{
			dataIndex = 0;
			foreach (ScienceData data in storedScienceReports) {
				newResultPage();
				dataIndex++;
			}
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
			if (scienceReports.Count > 0) {
				ScienceData data = scienceReports[0];
				ExperimentResultDialogPage page = new ExperimentResultDialogPage(part, data, data.transmitValue, data.labBoost, !rerunnable, transmitWarningText, true, data.labBoost < 1 && checkLabOps() && xmitDataScalar < 1, new Callback<ScienceData>(onDiscardInitialData), new Callback<ScienceData>(onKeepInitialData), new Callback<ScienceData>(onTransmitInitialData), new Callback<ScienceData>(onSendInitialToLab));
				ExperimentsResultDialog.DisplayResult(page);
			}
		}

		private void onDiscardData(ScienceData data)
		{
			if (storedScienceReports.Count > 0) {
				if (!string.IsNullOrEmpty(sampleEmptyAnim))
					secondaryAnimator(sampleEmptyAnim, animSpeed, 1f - (experimentsCollected * (1f / experimentLimit)), anim2[sampleEmptyAnim].length / experimentLimit);
				else if (!string.IsNullOrEmpty(sampleAnim))
					secondaryAnimator(sampleAnim, -1f * animSpeed, experimentsCollected * (1f / experimentLimit), anim2[sampleAnim].length / experimentLimit);
				if (!string.IsNullOrEmpty(indicatorAnim))
					secondaryAnimator(indicatorAnim, -1f * animSpeed, experimentsCollected * (1f / experimentLimit), anim[indicatorAnim].length / experimentLimit);
				storedScienceReports.Remove(data);
				if (keepDeployedMode == 0) retractEvent();
				experimentsCollected--;
				if (experimentsCollected < 0)
					experimentsCollected = 0;
			}
		}

		private void onKeepData(ScienceData data)
		{
		}

		private void onTransmitData(ScienceData data)
		{
			List<IScienceDataTransmitter> tranList = vessel.FindPartModulesImplementing<IScienceDataTransmitter>();
			if (tranList.Count > 0 && storedScienceReports.Count > 0) {
				tranList.OrderBy(ScienceUtil.GetTransmitterScore).First().TransmitData(new List<ScienceData> { data });
				if (!string.IsNullOrEmpty(sampleEmptyAnim))
					secondaryAnimator(sampleEmptyAnim, animSpeed, 1f - (experimentsCollected * (1f / experimentLimit)), anim2[sampleEmptyAnim].length / experimentLimit);
				DumpData(data);
			}
			else ScreenMessages.PostScreenMessage("No transmitters available on this vessel.", 4f, ScreenMessageStyle.UPPER_LEFT);
		}

		private void onSendToLab(ScienceData data)
		{
			List<ModuleScienceLab> labList = vessel.FindPartModulesImplementing<ModuleScienceLab>();
			if (checkLabOps() && storedScienceReports.Count > 0)
				labList.OrderBy(ScienceUtil.GetLabScore).First().StartCoroutine(labList.First().ProcessData(data, new Callback<ScienceData>(onComplete)));
			else
				ScreenMessages.PostScreenMessage("No operational lab modules on this vessel. Cannot analyze data.", 4f, ScreenMessageStyle.UPPER_CENTER);
		}

		private void onComplete(ScienceData data)
		{
			ReviewData();
		}

		private bool checkLabOps()
		{
			List<ModuleScienceLab> labList = vessel.FindPartModulesImplementing<ModuleScienceLab>();
			for (int i = 0; i < labList.Count; i++) {
				if (labList[i].IsOperational())
					return true;
			}
			return false;
		}

		private void onDiscardInitialData(ScienceData data)
		{
			if (scienceReports.Count > 0) {
				scienceReports.Remove(data);
				if (keepDeployedMode == 0) retractEvent();
			}
		}

		private void onKeepInitialData(ScienceData data)
		{
			if (experimentsCollected >= experimentLimit) {
				ScreenMessages.PostScreenMessage(storageFullMessage, 5f, ScreenMessageStyle.UPPER_CENTER);
				initialResultsPage();
			}
			else if (scienceReports.Count > 0) {
				if (!string.IsNullOrEmpty(sampleAnim))
					secondaryAnimator(sampleAnim, animSpeed, experimentsCollected * (1f / experimentLimit), anim2[sampleAnim].length / experimentLimit);
				if (!string.IsNullOrEmpty(indicatorAnim))
					secondaryAnimator(indicatorAnim, animSpeed, experimentsCollected * (1f / experimentLimit), anim2[indicatorAnim].length / experimentLimit);
				storedScienceReports.Add(data);
				scienceReports.Remove(data);
				experimentsCollected++;
				print(string.Format("[DM] Experiments Collected: {0}", experimentsCollected));
			}
		}

		private void onTransmitInitialData(ScienceData data)
		{
			List<IScienceDataTransmitter> tranList = vessel.FindPartModulesImplementing<IScienceDataTransmitter>();
			if (tranList.Count > 0 && scienceReports.Count > 0) {
				if (!string.IsNullOrEmpty(sampleAnim))
					secondaryAnimator(sampleAnim, animSpeed, experimentsCollected * (1f / experimentLimit), anim2[sampleAnim].length / experimentLimit);
				if (!string.IsNullOrEmpty(indicatorAnim))
					secondaryAnimator(indicatorAnim, animSpeed, experimentsCollected * (1f / experimentLimit), anim2[indicatorAnim].length / experimentLimit);
				tranList.OrderBy(ScienceUtil.GetTransmitterScore).First().TransmitData(new List<ScienceData> { data });
				DumpInitialData(data);
				experimentsCollected++;
			}
			else
				ScreenMessages.PostScreenMessage("No transmitters available on this vessel.", 4f, ScreenMessageStyle.UPPER_LEFT);
		}

		private void onSendInitialToLab(ScienceData data)
		{
			List<ModuleScienceLab> labList = vessel.FindPartModulesImplementing<ModuleScienceLab>();
			if (checkLabOps() && scienceReports.Count > 0)
				labList.OrderBy(ScienceUtil.GetLabScore).First().StartCoroutine(labList.First().ProcessData(data, new Callback<ScienceData>(onInitialComplete)));
			else
				ScreenMessages.PostScreenMessage("No operational lab modules on this vessel. Cannot analyze data.", 4f, ScreenMessageStyle.UPPER_CENTER);
		}

		private void onInitialComplete(ScienceData data)
		{
			initialResultsPage();
		}

		#endregion

		#region IScienceDataContainer

		ScienceData[] IScienceDataContainer.GetData()
		{
			return storedScienceReports.ToArray();
		}

		int IScienceDataContainer.GetScienceCount()
		{
			return storedScienceReports.Count;
		}

		bool IScienceDataContainer.IsRerunnable()
		{
			return experimentsCollected < experimentLimit;
		}

		void IScienceDataContainer.ReviewData()
		{
			ReviewData();
		}

		void IScienceDataContainer.ReviewDataItem(ScienceData data)
		{
			ReviewData();
		}

		void IScienceDataContainer.DumpData(ScienceData data)
		{
			DumpData(data);
		}

		protected void DumpAllData(List<ScienceData> dataList)
		{
			if (storedScienceReports.Count > 0) {
				storedScienceReports.Clear();
				if (keepDeployedMode == 0) retractEvent();
			}
		}

		new public void DumpData(ScienceData data)
		{
			if (storedScienceReports.Count > 0) {
				if (keepDeployedMode == 0) retractEvent();
				storedScienceReports.Remove(data);
			}
		}

		protected void DumpInitialData(ScienceData data)
		{
			if (scienceReports.Count > 0) {
				if (keepDeployedMode ==0) retractEvent();
				scienceReports.Remove(data);
			}
		}

		#endregion

	}
}
