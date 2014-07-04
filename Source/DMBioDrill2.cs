using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DMagic
{
	class DMBioDrill2: DMScienceMultiple
	{

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);
			newSetup();
		}

		private void newSetup()
		{
			base.experimentLimit = 3;
			base.sitMask = 1;
			base.bioMask = 1;
		}

		new public void DeployExperiment()
		{
			if (vessel.mainBody.name == "Eve" || vessel.mainBody.name == "Kerbin" || vessel.mainBody.name == "Duna" || vessel.mainBody.name == "Laythe" || vessel.mainBody.name == "Bop" || vessel.mainBody.name == "Vall" || vessel.mainBody.atmosphere) {
				if (vessel.mainBody.name == "Eve")
					base.scienceBoost = 2f;
				else 
					base.scienceBoost = 1f;
				base.DeployExperiment();
			}
			else
				ScreenMessages.PostScreenMessage(customFailMessage, 5f, ScreenMessageStyle.UPPER_CENTER);
		}

	}
}
