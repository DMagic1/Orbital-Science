
using System.Collections.Generic;
using UnityEngine;

namespace DMagic
{
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	internal class DMConfigLoader: MonoBehaviour
	{
		internal static Dictionary<string, DMcontractScience> availableScience = new Dictionary<string, DMcontractScience>();
		internal static float science, reward, forward, penalty;

		private void Start()
		{
			configLoad();
		}

		private void configLoad()
		{
			ConfigNode setNode = GameDatabase.Instance.GetConfigNode("DM_CONTRACT_SETTINGS");
			if (setNode != null)
			{
				science = float.Parse(setNode.GetValue("Global_Science_Return"));
				reward = float.Parse(setNode.GetValue("Global_Fund_Reward"));
				forward = float.Parse(setNode.GetValue("Global_Fund_Forward"));
				penalty = float.Parse(setNode.GetValue("Global_Fund_Penalty"));
				Debug.Log(string.Format("[DM] Contract Variables Set; Science Reward: {0} ; Completion Reward: {1} ; Forward Amount: {2} ; Penalty Amount: {3}", science.ToString(), reward.ToString(), forward.ToString(), penalty.ToString())
					);
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
					availableScience.Add(name, new DMcontractScience(expID, exp, sitMask, bioMask, part, techNode, agent));
					Debug.Log(string.Format("[DM] New Experiment: [{0}] Available For Contracts", exp.experimentTitle));
				}
			}
			Debug.Log(string.Format("[DM] Successfully Added {0} New Experiments To Contract List", availableScience.Count));
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
