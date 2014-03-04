/* DMagic Orbital Science - Laser Scanner 
 * Laser scanner animation and science collection.
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
        
        public class DMLaserModule : ModuleScienceExperiment
        {

            [KSPField(isPersistant = false)]
            public string animationName;

            protected Animation anim;
            
            public override void OnStart(PartModule.StartState state)
            {
                base.OnStart(state);
                this.part.force_activate();
                anim = part.FindModelAnimators(animationName)[0];
            }

            public void DeployEvent()
            {
                if (anim[animationName] != null)
                {
                    anim[animationName].speed = 1f;
                    if (anim.IsPlaying(animationName)) { return; }
                else
                {
                    anim[animationName].normalizedTime = 0f;
                    anim.Play(animationName);
                }
                }
            }
                           
            IEnumerator WaitForAnimation(float animTime)
            {                
                {
                    yield return new WaitForSeconds(animTime);
                    base.DeployExperiment();
                }
            }

            IEnumerator ActionDeploy(float animTime, KSPActionParam param)
            {
                {
                    yield return new WaitForSeconds(animTime);
                    base.DeployAction(param);
                }
            }

            public bool vesselSituation()
            {
                if (vessel.situation == Vessel.Situations.LANDED || vessel.situation == Vessel.Situations.SPLASHED || vessel.situation == Vessel.Situations.PRELAUNCH)
                {
                    return true;
                }
                else
                {
                    ScreenMessages.PostScreenMessage("The laser is only suitable for surface based observations.", 4f, ScreenMessageStyle.UPPER_CENTER);
                    return false;
                }
            }
            
            //Override default science collection code. Play animation and wait four seconds to collect data.
            new public void DeployExperiment()
            {
                if (vesselSituation())
                {
                    if (anim[animationName] != null)
                    {
                        anim[animationName].speed = 1f;
                        if (anim.IsPlaying(animationName)) { return; }
                        else
                        {
                            anim[animationName].normalizedTime = 0f;
                            anim.Play(animationName);
                            StartCoroutine(WaitForAnimation(4f));
                        }
                    }
                }
            }

            new public void DeployAction(KSPActionParam p)
            {
                if (vesselSituation())
                {
                    if (anim[animationName] != null)
                    {
                        anim[animationName].speed = 1f;
                        if (anim.IsPlaying(animationName)) { return; }
                        else
                        {
                            anim[animationName].normalizedTime = 0f;
                            anim.Play(animationName);
                            StartCoroutine(ActionDeploy(4f, p));
                        }
                    }
                }
            }
            
            //VAB/SPH tweakable deploy/retract toggle.
            [KSPEvent(guiActiveEditor = true, guiName = "Deploy", active = true)]
            public void VABDeploy()
            {
                if (anim[animationName] != null)
                {
                    Events["VABDeploy"].active = false;
                    Events["VABRetract"].active = true;
                    anim[animationName].speed = 0f;
                    anim[animationName].normalizedTime = 0.5f;
                    anim.Play(animationName);
                }
            }

            [KSPEvent(guiActiveEditor = true, guiName = "Retract", active = false)]
            public void VABRetract()
            {
                if (anim[animationName] != null)
                {
                    Events["VABDeploy"].active = true;
                    Events["VABRetract"].active = false;
                    anim[animationName].speed = -1f;
                    anim[animationName].normalizedTime = 0f;
                    anim.Play(animationName);
                }
            }
            
            //VAB action group deploy animation option.
            [KSPAction("Deploy Laser")]
            public void DeployEventAction(KSPActionParam param)
            {
                DeployEvent();
            }

        }
    }

