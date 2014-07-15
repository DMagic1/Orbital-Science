using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DMagic
{
	class DMSolarCollector: DMModuleScienceAnimate
	{

		[KSPField]
		public string loopingAnim = null;

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);
			if (string.IsNullOrEmpty(loopingAnim))
				anim = part.FindModelAnimators(loopingAnim)[0];
			if (IsDeployed)
				primaryAnimator(1f, 0f, WrapMode.Loop, loopingAnim, anim);
		}

		public override void deployEvent()
		{
			primaryAnimator(1f, 0f, WrapMode.Loop, loopingAnim, anim);
			base.deployEvent();
		}

		public override void retractEvent()
		{
			if (anim != null && !string.IsNullOrEmpty(loopingAnim)) {
					anim[loopingAnim].normalizedTime = anim[loopingAnim].normalizedTime % 1;
					anim[loopingAnim].wrapMode = WrapMode.Clamp;
				}
			base.retractEvent();
		}

		protected override void onComplete(ScienceData data)
		{
			data.transmitValue = 0.7f;
			data.labBoost = 2.5f;
			base.onComplete(data);
		}

		protected override void onInitialComplete(ScienceData data)
		{
			data.transmitValue = 0.7f;
			data.labBoost = 2.5f;
			base.onInitialComplete(data);
		}

	}
}
