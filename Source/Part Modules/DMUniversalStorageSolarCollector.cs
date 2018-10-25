
using UnityEngine;

namespace DMagic.Part_Modules
{
    public class DMUniversalStorageSolarCollector : DMUniversalStorageScience
    {
        [KSPField]
        public string loopingAnim = "";

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);

            if (string.IsNullOrEmpty(loopingAnim))
                anim = part.FindModelAnimators(loopingAnim)[0];

            if (IsDeployed)
                primaryAnimator(1f, 0f, WrapMode.Loop, loopingAnim, anim);
        }

        protected override void DeploySucceed()
        {
            primaryAnimator(1f, 0f, WrapMode.Loop, loopingAnim, anim);
        }
        
        public override void retractEvent()
        {
            if (anim != null && !string.IsNullOrEmpty(loopingAnim))
            {
                anim[loopingAnim].normalizedTime = anim[loopingAnim].normalizedTime % 1;
                anim[loopingAnim].wrapMode = WrapMode.Clamp;
            }

            base.retractEvent();
        }

    }
}
