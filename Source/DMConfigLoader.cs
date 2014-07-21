
using System.Collections.Generic;
using UnityEngine;

namespace DMagic
{
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	internal class DMConfigLoader: MonoBehaviour
	{
		internal static Dictionary<string, DMcontractScience> availableScience = new Dictionary<string, DMcontractScience>();

		private void Start()
		{
			configLoad();
		}

		private void configLoad()
		{
			foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("CONTRACT_EXPERIMENT"))
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
