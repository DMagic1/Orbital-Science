/* DMagic Orbital Science - Radio and Plasma Wave Science
 * RPWS animation and science collection.
 *
 * Copyright (c) 2014, DMagic
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without modification, 
 * are permitted provided that the following conditions are met:
 * 
 * 1. Redistributions of source code must retain the above copyright notice, 
 * this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright notice, 
 * this list of conditions and the following disclaimer in the documentation and/or other materials 
 * provided with the distribution.
 * 
 * 3. Neither the name of the copyright holder nor the names of its contributors may be used 
 * to endorse or promote products derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE 
 * GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF 
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT 
 * OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *  
 */


using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace DMagic
{
    //Inherit default science experiment module.
    public class DMRPWSModule : ModuleScienceExperiment
    {

        [KSPField(isPersistant = false)]
        public string animationName; 

        [KSPField(isPersistant = true)]
        bool IsEnabled = false;

        [KSPField(isPersistant = false)]
        bool IsExperimenting = false;

        protected Animation anim;

        //Get first animation name from part. Force module activate.
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

        //Right click deploy animation. Animation is reversible while playing.
        [KSPEvent(guiActive = true, guiName = "Deploy RPWS Antenna", active = true)]
        public void DeployEvent()
        {
            if (anim[animationName] != null)
            {
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

                IsEnabled = true;
                
                //Trigger retract right click option.
                Events["DeployEvent"].active = false;
                Events["RetractEvent"].active = true;
            }
        }

        //Right click retract animation, same as deploy animation.
        [KSPEvent(guiActive = true, guiName = "Retract RPWS Antenna", active = false)]
        public void RetractEvent()
        {
            if (anim[animationName] != null)
            {
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

                Events["DeployEvent"].active = true;
                Events["RetractEvent"].active = false;
            }
        }

        //VAB action group toggle animation option.
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

        //VAB/SPH tweakable deploy/retract toggle.
        [KSPEvent(guiActiveEditor = true, guiName = "Deploy", active = true)]
        public void VABDeploy()
        {
            Events["VABDeploy"].active = false;
            Events["VABRetract"].active = true;
            anim[animationName].speed = 1f;
            anim[animationName].normalizedTime = 1f;
            anim.Play(animationName);
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Retract", active = false)]
        public void VABRetract()
        {
            Events["VABDeploy"].active = true;
            Events["VABRetract"].active = false;
            anim[animationName].speed = -1f;
            anim[animationName].normalizedTime = 0f;
            anim.Play(animationName);
        }

        //Check craft position and situation.
        public bool VesselSituation()
        {
            //print(vessel.orbit.referenceBody.name + ", " + vessel.situation + ", " + vessel.landedAt + ", " + vessel.altitude + ".");
            if (vessel.situation == Vessel.Situations.PRELAUNCH || vessel.situation == Vessel.Situations.LANDED || vessel.situation == Vessel.Situations.SPLASHED)
            {
                ScreenMessages.PostScreenMessage("Try again when you're in space!", 2f, ScreenMessageStyle.UPPER_CENTER);
                return false;
            }
            else if (vessel.situation == Vessel.Situations.FLYING || vessel.altitude <= vessel.mainBody.maxAtmosphereAltitude)
            {
                ScreenMessages.PostScreenMessage("The RPWS Antenna is not suitable for use during atmospheric flight, try again in space.", 2f, ScreenMessageStyle.UPPER_CENTER);
                return false;
            }
            else
            {
                return true;
            }
        }

        IEnumerator WaitForDeploy(float animTime)
        {
            if (anim[animationName] != null)
            {
                yield return new WaitForSeconds(animTime);
                base.DeployExperiment();
                IsExperimenting = false;
            }
        }

        //Replace default science collection right click function. Same as magnetometer code.
        new public void DeployExperiment()
        {
            if (IsExperimenting) { return; }
            else if (IsEnabled)
                {
                    if (VesselSituation())
                    {
                        if (anim.IsPlaying(animationName))
                        {
                            ScreenMessages.PostScreenMessage("You can't expect good results while the antennae are still extending!", 3f, ScreenMessageStyle.UPPER_CENTER);
                            StartCoroutine(WaitForDeploy((anim[animationName].length / 2f) - (anim[animationName].normalizedTime * (anim[animationName].length / 2f))));
                            IsExperimenting = true;
                        }
                        else
                        {
                            base.DeployExperiment();
                            IsExperimenting = false;
                        }
                    }
                }
                else if (VesselSituation() && anim.IsPlaying(animationName) == true)
                {
                    DeployEvent();
                    DeployExperiment();
                }
                else if (VesselSituation())
                {
                    DeployEvent();
                    ScreenMessages.PostScreenMessage("With the antennae retracted the sensors can't read anything, deploying the system now.", 4f, ScreenMessageStyle.UPPER_CENTER);
                    StartCoroutine(WaitForDeploy(anim[animationName].length / 2f));
                    IsExperimenting = true;
                }
            }
                           

        //Replace default science collection VAB action group function.
        new public void DeployAction(KSPActionParam p)
        {
            if (IsExperimenting) { return; }
            else if (IsEnabled)
                {
                    if (VesselSituation())
                    {
                        if (anim.IsPlaying(animationName))
                        {
                            ScreenMessages.PostScreenMessage("You can't expect good results while the antennae are still extending!", 3f, ScreenMessageStyle.UPPER_CENTER);
                            StartCoroutine(WaitForDeploy((anim[animationName].length / 2f) - (anim[animationName].normalizedTime * (anim[animationName].length / 2f))));
                            IsExperimenting = true;
                        }
                        else
                        {
                            base.DeployAction(p);
                            IsExperimenting = false;
                        }
                    }
                }
                else if (VesselSituation() && anim.IsPlaying(animationName) == true)
                {
                    DeployEvent();
                    DeployAction(p);
                }
                else if (VesselSituation())
                {
                    DeployEvent();
                    ScreenMessages.PostScreenMessage("With the antennae retracted the sensors can't read anything, deploying the system now.", 4f, ScreenMessageStyle.UPPER_CENTER);
                    StartCoroutine(WaitForDeploy(anim[animationName].length /2f));
                    IsExperimenting = true;
                }
        }
          
    }
}