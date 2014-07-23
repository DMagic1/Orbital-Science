
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DMagic
{
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	internal class DMConfigLoader: MonoBehaviour
	{

		private void Start()
		{
			DMUtils.rand = new System.Random();
			DMUtils.DebugLog("Generating Global Random Number Generator");
			configLoad();
		}

		private void configLoad()
		{
			foreach (ConfigNode setNode in GameDatabase.Instance.GetConfigNodes("DM_CONTRACT_SETTINGS"))
				if (setNode.GetValue("name") == "Contract Settings")
				{
					DMUtils.science = float.Parse(setNode.GetValue("Global_Science_Return"));
					DMUtils.reward = float.Parse(setNode.GetValue("Global_Fund_Reward"));
					DMUtils.forward = float.Parse(setNode.GetValue("Global_Fund_Forward"));
					DMUtils.penalty = float.Parse(setNode.GetValue("Global_Fund_Penalty"));
					DMUtils.Logging("Contract Variables Set; Science Reward: {0} ; Completion Reward: {1} ; Forward Amount: {2} ; Penalty Amount: {3}",
						DMUtils.science, DMUtils.reward, DMUtils.forward, DMUtils.penalty);
					break;
				}
			foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("DM_CONTRACT_EXPERIMENT"))
			{
				string name, part, techNode, agent, expID = "";
				int sitMask, bioMask = 0;
				expID = node.GetValue("experimentID");
				ScienceExperiment exp = ResearchAndDevelopment.GetExperiment(expID);
				if (exp != null)
				{
					name = node.GetValue("name");
					sitMask = int.Parse(node.GetValue("sitMask"));
					bioMask = int.Parse(node.GetValue("bioMask"));
					part = node.GetValue("part");
					if (node.HasValue("techNode"))
						techNode = node.GetValue("techNode");
					else
						techNode = "None";
					if (node.HasValue("agent"))
						agent = node.GetValue("agent");
					else
						agent = "Any";
					DMUtils.availableScience.Add(name, new DMcontractScience(expID, exp, sitMask, bioMask, part, techNode, agent));
					DMUtils.Logging("New Experiment: [{0}] Available For Contracts", exp.experimentTitle);
				}
			}
			DMUtils.Logging("Successfully Added {0} New Experiments To Contract List", DMUtils.availableScience.Count);
		}

		private void OnDestroy()
		{
		}

	}

	internal class DMcontractScience
	{
		internal string name;
		internal int sitMask, bioMask;
		internal ScienceExperiment exp;
		internal string sciPart;
		internal string sciNode;
		internal string agent;

		internal DMcontractScience(string expName, ScienceExperiment sciExp, int sciSitMask, int sciBioMask, string sciPartID, string sciTechNode, string agentName)
		{
			name = expName;
			sitMask = sciSitMask;
			bioMask = sciBioMask;
			exp = sciExp;
			sciPart = sciPartID;
			sciNode = sciTechNode;
			agent = agentName;
		}
	}

}
