using UnityEngine;

namespace DMagic
{
    //Inherit default science experiment module.
    public class RPWSModule : ModuleScienceExperiment
    {

        [KSPField(isPersistant = false)]
        public string animationName;

        //Start in undeployed state.
        [KSPField(isPersistant = false)]
        bool IsEnabled = false;

        protected Animation anim;

        //Get first animation name from part. Force module activate.
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            if (state == StartState.Editor) { return; }
            this.part.force_activate();
            anim = part.FindModelAnimators(animationName)[0];

        }

        //Right click deploy animation. Animation is reversible while playing.
        [KSPEvent(guiActive = true, guiName = "Deploy RPWS Antenna", active = true)]
        public void DeployEvent()
        {
            //Play forward animation
            anim[animationName].speed = 2f;

            //Check if animation is stopped, if not animating and undeployed, start deploy animation. If already deployed do nothing.
            if (!anim.IsPlaying(animationName))
            {
                if (IsEnabled) { return; }
                else
                {
                    anim[animationName].normalizedTime = 0f;
                    anim.Play(animationName);
                }

            }

            //Set part to deployed state.
            IsEnabled = true;

            //print("Deploying RPWS with " + animationName);

            //Trigger retract right click option.
            Events["DeployEvent"].active = false;
            Events["RetractEvent"].active = true;
        }

        //Right click retract animation, same as deploy animation.
        [KSPEvent(guiActive = true, guiName = "Retract RPWS Antenna", active = false)]
        public void RetractEvent()
        {
            //Play animation in reverse.
            anim[animationName].speed = -2f;
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

            //print("Retracting RPWS with " + animationName);

            Events["DeployEvent"].active = true;
            Events["RetractEvent"].active = false;

        }

        //VAB action group deploy, retract and toggle animation options.
        [KSPAction("Deploy RPWS Antenna")]
        public void DeployEventAction(KSPActionParam param)
        {
            DeployEvent();
        }

        [KSPAction("Retract RPWS Antenna")]
        public void RetractEventAction(KSPActionParam param)
        {
            RetractEvent();
        }

        [KSPAction("Toggle RPWS Antenna")]
        public void ToggleEventAction(KSPActionParam param)
        {
            if (IsEnabled)
            {
                RetractEvent();
            }
            else
            {
                DeployEvent();
            }
        }

        //Check craft position and situation.
        public bool VesselSituation()
        {
            //print(vessel.orbit.referenceBody.name + ", " + vessel.situation + ", " + vessel.landedAt + ", " + vessel.altitude + ".");
            if (vessel.landedAt == "KSC" || vessel.situation == Vessel.Situations.PRELAUNCH)
            {
                ScreenMessages.PostScreenMessage("Try again when you're in space!", 2f, ScreenMessageStyle.UPPER_CENTER);
                return false;
            }
            else if (vessel.situation == Vessel.Situations.FLYING)
            {
                ScreenMessages.PostScreenMessage("The RPWS Antenna is not suitable for use during atmospheric flight, try again in space.", 2f, ScreenMessageStyle.UPPER_CENTER);
                return false;
            }
            else
            {

                return true;
            }

        }


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
                        ScreenMessages.PostScreenMessage("You can't expect good results while the antennae are still extending!", 2f, ScreenMessageStyle.UPPER_CENTER);
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
                ScreenMessages.PostScreenMessage("With the antennae retracted the sensers can't read anything, deploying the system now.", 3f, ScreenMessageStyle.UPPER_CENTER);
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
                        ScreenMessages.PostScreenMessage("You can't expect good results while the antennae are still extending!", 2f, ScreenMessageStyle.UPPER_CENTER);
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
                ScreenMessages.PostScreenMessage("With the antennae retracted the sensors can't read anything, deploying the system now.", 3f, ScreenMessageStyle.UPPER_CENTER);
            }
        }



    }
}