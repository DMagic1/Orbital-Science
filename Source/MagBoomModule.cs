using UnityEngine;

namespace DMagic
{
    //Inherit default science experiment module.
    public class MagBoomModule : ModuleScienceExperiment
    {

        [KSPField(isPersistant = false)]
        public string animationName;

        //Start in undeployed state.
        [KSPField(isPersistant = false)]
        bool IsEnabled = false;

        protected Animation anim;

        //Get first animation name from part. Force module activation.
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            if (state == StartState.Editor) { return; }
            this.part.force_activate();
            anim = part.FindModelAnimators(animationName)[0];

        }

        //Right click deploy animation. Animation is reversible while playing.
        [KSPEvent(guiActive = true, guiName = "Deploy Magnetometer", active = true)]
        public void DeployEvent()
        {
            //Play forward animation at 1.5x speed.
            anim[animationName].speed = 1.5f;

            //Check if animation is stopped, if not animating and undeployed, start deploy animation. If already deployed do nothing.
            if (!anim.IsPlaying(animationName))
            {
                if (IsEnabled) { return; }
                else {
                anim[animationName].normalizedTime = 0f;
                    anim.Play(animationName);
                }
                
            }
                        
            //Set part to deployed state.
            IsEnabled = true;

            //print("Deploying Magnetometer with " + animationName);

            //Trigger retract right click option.
            Events["DeployEvent"].active = false;
            Events["RetractEvent"].active = true;
        }

        //Right click retract animation, same as deploy animation.
        [KSPEvent(guiActive = true, guiName = "Retract Magnetometer", active = false)]
        public void RetractEvent()
        {
            //Play animation in reverse at 1.5x speed.
            anim[animationName].speed = -1.5f;
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

            //print("Retracting Magnetometer with " + animationName);

            Events["DeployEvent"].active = true;
            Events["RetractEvent"].active = false;

        }

        //VAB action group deploy, retract and toggle animation options.
        [KSPAction("Deploy Magnetometer")]
        public void DeployEventAction(KSPActionParam param)
        {
            DeployEvent();
        }

        [KSPAction("Retract Magnetometer")]
        public void RetractEventAction(KSPActionParam param)
        {
            RetractEvent();
        }

        [KSPAction("Toggle Magnetometer")]
        public void ToggleEventAction(KSPActionParam param)
        {
            if (IsEnabled) {
                RetractEvent();
            } else {
                DeployEvent();
            }
        }

        //Check craft position and situation.
        public bool VesselSituation()
        {
            //print(vessel.orbit.referenceBody.name + ", " + vessel.situation + ", " + vessel.landedAt + ", " + vessel.altitude + ".");
            if (vessel.situation == Vessel.Situations.FLYING)
            {
                ScreenMessages.PostScreenMessage("The magnetometer is not suitable for use during atmospheric flight, try again on the ground or in space.", 4f, ScreenMessageStyle.UPPER_CENTER);
                return false;
            } else {

                return true;
            }
            
        }

       // [KSPEvent(guiActive = true, guiName = "Check Position", active = true)]
       // public void CheckPosition()
       // {
       //     VesselSituation();
       // }

       // [KSPAction("Check Position")]
       //  public void PositionCheckAction(KSPActionParam param)
       // { CheckPosition();
       // }

        //Replace default science collection right click function.
        new public void DeployExperiment()
        {
            
            //Only allow data collection if the boom is fully extended. Deploy the boom if it is closed.
            if (IsEnabled)
            {
                if (VesselSituation())
                {
                    if (anim.IsPlaying(animationName))
                    {
                        ScreenMessages.PostScreenMessage("You can't expect good results while the boom is still extending!", 3f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    else
                    {
                        base.DeployExperiment();
                    }
                }
            }                       
            else if (VesselSituation())
            {
                DeployEvent();
                ScreenMessages.PostScreenMessage("Close proximity to the craft scrambles the magnetometer's sensors, deploying the scanner now.", 4f, ScreenMessageStyle.UPPER_CENTER);
            }
        }                   
        //Replace default science collection VAB action group function.
        new public void DeployAction(KSPActionParam p)
        {
            if (IsEnabled)
            {
                if (VesselSituation())
                {
                    if (anim.IsPlaying(animationName))
                    {
                        ScreenMessages.PostScreenMessage("You can't expect good results while the boom is still extending!", 3f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    else
                    {
                        base.DeployAction(p);
                    }
                }
            }
            else if (VesselSituation())
            {
                DeployEvent();
                ScreenMessages.PostScreenMessage("Close proximity to the craft scrambles the magnetometer's sensors, deploying the scanner now.", 4f, ScreenMessageStyle.UPPER_CENTER);
            }
        }

        

    }
}
