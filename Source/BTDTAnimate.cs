using UnityEngine;

namespace DMagic
{
    class BTDTAnimate : PartModule
    {
        [KSPField(isPersistant = false)]
        public string animationName;

        [KSPField(isPersistant = false)]
        public string dishAnimate;

        [KSPField(isPersistant = false)]
        public string redLight;

        [KSPField(isPersistant = false)]
        public string greenLight;

        //Start in undeployed state.
        [KSPField(isPersistant = false)]
        bool IsEnabled = false;

        //[KSPField(isPersistant = false)]
        //bool IsSpin = false;

        [KSPField(isPersistant = false)]
        bool IsSearch = false;

        [KSPField(isPersistant = false)]
        bool IsFound = false;

        protected Animation anim;
        protected Animation anim2;
        protected Animation anim3;
        protected Animation anim4;


        //Get animation names from part. Force module activation.
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);

            this.part.force_activate();
            anim = part.FindModelAnimators(animationName)[0];
            anim2 = part.FindModelAnimators(dishAnimate)[0];
            anim3 = part.FindModelAnimators(redLight)[0];
            anim4 = part.FindModelAnimators(greenLight)[0];
            print(animationName + ", " + dishAnimate + "," + redLight + ", " + greenLight + ".");
        }


        //Deploy animation right click.
        [KSPEvent(guiActive = true, guiName = "Deploy BTDT", active = true)]
        public void DeployEvent()
        {
            //Check if part is already deployed.
            if (!IsEnabled)
            {                
                //Play forward animation, queue radar spinning animation after deploy animation finishes
                anim[animationName].speed = 1f;
                anim[animationName].normalizedTime = 0f;
                anim.PlayQueued(animationName, QueueMode.PlayNow);
                anim2.PlayQueued(dishAnimate, QueueMode.CompleteOthers);
                anim2[dishAnimate].wrapMode = WrapMode.Loop;
                
            }

            //Set part to deployed state.
            IsEnabled = true;

            //print("Deploying with " + animationName + " then " + dishAnimate + " and setting " + IsEnabled.ToString());

            //Trigger retract right click option.
            Events["DeployEvent"].active = false;
            Events["RetractEvent"].active = true;
        }

      

        //Right click retract animation. Reset light animations and states if they are playing.
        [KSPEvent(guiActive = true, guiName = "Retract BTDT", active = false)]
        public void RetractEvent()
        {
            if (IsEnabled)
            {          
                //Stop orange light if it's playing
                if (anim3.IsPlaying(redLight))
                {
                    anim3[redLight].normalizedTime = (anim3[redLight].normalizedTime % 1);
                    anim3[redLight].wrapMode = WrapMode.Clamp;
                    IsSearch = false;
                    Events["SearchEvent"].active = true;
                    Events["StopSearchEvent"].active = false;
                }
                
                //Stop green light if it's playing
                if (anim4.IsPlaying(greenLight))
                {
                    anim4[greenLight].normalizedTime = (anim4[greenLight].normalizedTime % 1);
                    anim4[greenLight].wrapMode = WrapMode.Clamp;
                    IsFound = false;
                    Events["FoundEvent"].active = true;
                    Events["LostEvent"].active = false;
                }

                anim[animationName].speed = -1f;
                anim[animationName].normalizedTime = 1f;
                anim.Play(animationName);
            }

            IsEnabled = false;

            //print("Retracting with " + dishAnimate + " then " + animationName + " and setting " + IsEnabled.ToString());

            Events["DeployEvent"].active = true;
            Events["RetractEvent"].active = false;

        }

        //Orange light animation
        [KSPEvent(guiActive = true, guiName = "Search Light", active = true)]
        public void SearchEvent()
        {
            //Orange emission animation in loop mode. Use anim3.blend to prevent dispruption of radar spin animation.
            anim3[redLight].speed = 1f;
            anim3[redLight].normalizedTime = 0f;
            anim3.Blend(redLight, 1f);
            anim3[redLight].wrapMode = WrapMode.Loop;

            //Stop green light if it's playing
            if (anim4.IsPlaying(greenLight))
            {
                anim4[greenLight].normalizedTime = (anim4[greenLight].normalizedTime % 1);
                anim4[greenLight].wrapMode = WrapMode.Clamp;
                IsFound = false;
                Events["FoundEvent"].active = true;
                Events["LostEvent"].active = false;
            }
            
            IsSearch = true;

            //print("Starting search light with " + redLight);

            //Trigger stop orange light right click option.
            Events["SearchEvent"].active = false;
            Events["StopSearchEvent"].active = true;
        }

        //Orange light right click stop
        [KSPEvent(guiActive = true, guiName = "Stop Search Light", active = false)]
        public void StopSearchEvent()
        {
            //if animation has played through more than one loop, return normalized time to value between 0 and 1
            anim3[redLight].normalizedTime = (anim3[redLight].normalizedTime % 1);
            
            //stop animation after completing the next loop
            //print(anim2[dishAnimate].normalizedTime.ToString());
            anim3[redLight].wrapMode = WrapMode.Clamp;
            
            IsSearch = false;
            
            //print("Stopping search light with " + redLight);

            Events["SearchEvent"].active = true;
            Events["StopSearchEvent"].active = false;
            }

        //Green light animation
        [KSPEvent(guiActive = true, guiName = "Anomaly Light", active = true)]
        public void FoundEvent()
        {
            //Play green emission animation in loop mode; same as orange light.
            anim4[greenLight].speed = 1f;
            anim4[greenLight].normalizedTime = 0f;
            anim4.Blend(greenLight, 1f);
            anim4[greenLight].wrapMode = WrapMode.Loop;
            
            IsFound = true;

            //Stop orange light if it's playing
            if (anim3.IsPlaying(redLight))
            {
                anim3[redLight].normalizedTime = (anim3[redLight].normalizedTime % 1);
                anim3[redLight].wrapMode = WrapMode.Clamp;
                IsSearch = false;
                Events["SearchEvent"].active = true;
                Events["StopSearchEvent"].active = false;
            }

            //print("Starting anomaly light with " + greenLight);

            //Trigger stop green light right click option.
            Events["FoundEvent"].active = false;
            Events["LostEvent"].active = true;
        }

        //Green light stop animation
        [KSPEvent(guiActive = true, guiName = "Stop Anomaly Light", active = false)]
        public void LostEvent()
        {
            //if animation has played through more than one loop, return normalized time to value between 0 and 1
            anim4[greenLight].normalizedTime = (anim4[greenLight].normalizedTime % 1);
            
            //stop animation after completing the next loop
            //print(anim2[dishAnimate].normalizedTime.ToString());
            anim4[greenLight].wrapMode = WrapMode.Clamp;
            
            IsFound = false;
            
            //print("Stopping anomaly light with " + greenLight);

            Events["FoundEvent"].active = true;
            Events["LostEvent"].active = false;
        }


        //VAB Action group buttons.       
        [KSPAction("Toggle BTDT")]
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
        
        [KSPAction("Toggle Search Light")]
        public void ToggleSearchEventAction(KSPActionParam param)
        {
            if (IsSearch)
            {
                StopSearchEvent();
            }
            else
            {
                SearchEvent();
            }
        }
        
        [KSPAction("Toggle Anomaly Light")]
        public void ToggleFoundEventAction(KSPActionParam param)
        {
            if (IsFound)
            {
                LostEvent();
            }
            else
            {
                FoundEvent();
            }
        }
        
        //Code for individual deploy and radar dish spin animations......

        //Right click deploy animation. Animation is reversible while playing.
        //[KSPEvent(guiActive = true, guiName = "Deploy BTDT", active = true)]
        //public void DeployEvent()
        //{
        //    //Play forward animation.
        //    anim[animationName].speed = 1f;

        //    //Check if animation is stopped, if not animating and undeployed, start deploy animation. If already deployed do nothing.
        //    if (!anim.IsPlaying(animationName))
        //    {
        //        if (IsEnabled) { return; }
        //        else
        //        {
        //            anim[animationName].normalizedTime = 0f;
        //            anim.Play(animationName);
        //        }

        //    }

        //    //Set part to deployed state.
        //    IsEnabled = true;

        //    //print("Deploying BTDT with " + animationName);

        //    //Trigger retract right click option.
        //    Events["DeployEvent"].active = false;
        //    Events["RetractEvent"].active = true;
        //}



        ////Right click retract animation, same as deploy animation.
        //[KSPEvent(guiActive = true, guiName = "Retract BTDT", active = false)]
        //public void RetractEvent()
        //{
        //    //Play animation in reverse.
        //    anim[animationName].speed = -1f;
        //    if (!anim.IsPlaying(animationName))
        //    {
        //        if (!IsEnabled) { return; }
        //        else
        //        {
        //            anim[animationName].normalizedTime = 1f;
        //            anim.Play(animationName);
        //        }

        //    }

        //    IsEnabled = false;

        //    //print("Retracting BTDT with " + animationName);

        //    Events["DeployEvent"].active = true;
        //    Events["RetractEvent"].active = false;

        //}


        ////Right click radar dish spin animation
        //[KSPEvent(guiActive = true, guiName = "Spin Radar", active = true)]
        //public void DeployEvent2()
        //{
        //    //Play radar dish, spin animation in loop mode
        //    anim2[dishAnimate].speed = 1f;
        //    anim2[dishAnimate].normalizedTime = 0f;
        //    anim2.Blend(dishAnimate, 1f);
        //    anim2[dishAnimate].wrapMode = WrapMode.Loop;


        //    IsSpin = true;

        //    //print("Starting radar dish with " + dishAnimate);

        //    //Trigger stop spin right click option.
        //    Events["DeployEvent2"].active = false;
        //    Events["RetractEvent2"].active = true;
        //}

        ////Right click radar stop animation
        //[KSPEvent(guiActive = true, guiName = "Stop Radar Spin", active = false)]
        //public void RetractEvent2()
        //{
        //    //check if normalized time is greater than 1( animation has played through at least one loop).
        //    if (anim2[dishAnimate].normalizedTime > 1)
        //    {
        //        //if animation has played through more than one loop, return normalized time to value between 0 and 1
        //        anim2[dishAnimate].normalizedTime = (anim2[dishAnimate].normalizedTime % 1);
        //    }

        //    //stop animation after completing the next loop
        //    //print(anim2[dishAnimate].normalizedTime.ToString());
        //    anim2[dishAnimate].wrapMode = WrapMode.Clamp;

        //    IsSpin = false;

        //    //print("Stopping radar with " + dishAnimate);

        //    Events["DeployEvent2"].active = true;
        //    Events["RetractEvent2"].active = false;

        //}

        //[KSPAction("Toggle Spin")]
        //public void ToggleSpinEventAction(KSPActionParam param)
        //{
        //    if (IsSpin)
        //    {
        //        RetractEvent2();
        //    }
        //    else
        //    {
        //        DeployEvent2();
        //    }
        //}

    }
}