using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace DMagic
{
    public class DMScopeModule : ModuleScienceExperiment
    {

        [KSPField(isPersistant = false)]
        public string animationName;

        [KSPField(isPersistant = true)]
        public bool IsEnabled = false;

        protected Animation anim;

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            this.part.force_activate();
            anim = part.FindModelAnimators(animationName)[0];
            if (IsEnabled && anim[animationName] != null)
            {
                anim[animationName].speed = 1f;
                anim[animationName].normalizedTime = 1f;
                anim.Play(animationName);
            }        
        }

        [KSPEvent(guiActive = true, guiName = "Open Shutter", active = true)]
        public void DeployEvent()
        {
            if (anim[animationName] != null)
            {
                anim[animationName].speed = 1f;
                if (!anim.IsPlaying(animationName))
                {
                    if (IsEnabled) { return; }
                    else
                    {
                        anim[animationName].normalizedTime = 0f;
                        anim.Play(animationName);
                    }
                }
                IsEnabled = true;
                Events["DeployEvent"].active = false;
                Events["CloseEvent"].active = true;
            }
        }

        [KSPEvent(guiActive = true, guiName = "Close Shutter", active = false)]
        public void CloseEvent()
        {
            if (anim[animationName] != null)
            {
                anim[animationName].speed = -1f;
                if (!anim.IsPlaying(animationName))
                {
                    if (!IsEnabled) { return; }
                    else
                    {
                        anim[animationName].normalizedTime = 1f;
                        anim.Play(animationName);
                    }
                }
                IsEnabled = false;
                Events["DeployEvent"].active = true;
                Events["CloseEvent"].active = false;
            }
        }

        [KSPAction("Toggle Shutter")]
        public void ToggleEventAction(KSPActionParam param)
        {
            if (IsEnabled)
            {
                CloseEvent();
            }
            else
            {
                DeployEvent();
            }
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Open Shutter", active = true)]
        public void VABOpen()
        {
            anim[animationName].speed = 1f;
            anim[animationName].normalizedTime = 1f;
            anim.Play(animationName);
            Events["VABOpen"].active = false;
            Events["VABClose"].active = true;
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Close Shutter", active = false)]
        public void VABClose()
        {
            anim[animationName].speed = -1f;
            anim[animationName].normalizedTime = 0f;
            anim.Play(animationName);
            Events["VABOpen"].active = true;
            Events["VABClose"].active = false;
        }

        public bool VesselSituation()
        {
            if (vessel.landedAt == "KSC")
            {
                return true;
            }
            else if (vessel.situation == Vessel.Situations.FLYING || vessel.situation == Vessel.Situations.LANDED || vessel.situation == Vessel.Situations.SPLASHED)
            {
                ScreenMessages.PostScreenMessage("This telescope is not suitable for use during atmospheric flight or on the surface, try again in space.", 4f, ScreenMessageStyle.UPPER_CENTER);
                return false;
            } 
            else 
            {
                return true;
            }
        }


        new public void DeployExperiment()
        {
            if (anim[animationName] != null)
            {
                if (IsEnabled)
                {
                    if (VesselSituation())
                    {
                        base.DeployExperiment();
                    }
                }
                else if (VesselSituation())
                {
                    DeployEvent();
                    base.DeployExperiment();
                }
            }
        }

        new public void DeployAction(KSPActionParam p)
        {
            if (anim[animationName] != null)
            {
                if (IsEnabled)
                {
                    if (VesselSituation())
                    {
                        base.DeployAction(p);
                    }
                }
                else if (VesselSituation())
                {
                    DeployEvent();
                    base.DeployAction(p);
                }
            }
        }
        
    }
}
