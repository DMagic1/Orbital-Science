using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DMagic.Part_Modules
{
	public class DMBasicScienceModule : PartModule, IScienceDataContainer
	{

		[KSPField]
		public string experimentID = "";
		[KSPField]
		public string experimentResource = "ElectricCharge";
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

		#region Events

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
		public void DeployExperiment()
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
