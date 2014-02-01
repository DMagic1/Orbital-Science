using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace DMagic
{
        
        public class DMLaserModule : ModuleScienceExperiment
        {

            [KSPField(isPersistant = false)]
            public string animationName;
            
            //[KSPField(isPersistant = false)]
            //bool IsEnabled = false;

            protected Animation anim;
            private KSPActionParam actParams;
            
            //Get first animation name from part. Force module activation.
            public override void OnStart(PartModule.StartState state)
            {
                base.OnStart(state);
                this.part.force_activate();
                anim = part.FindModelAnimators(animationName)[0];
                
            }

            //Right click deploy animation.
            //[KSPEvent(guiActive = true, guiName = "Deploy Laser", active = true)]
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

                ////Set part to deployed state.
                //IsEnabled = true;

                }
            }
                           
            IEnumerator WaitForAnimation(float animTime)
            {                
                {
                    yield return new WaitForSeconds(animTime);
                    base.DeployExperiment();
                }
            }

            IEnumerator ActionDeploy(float animTime)
            {
                {
                    yield return new WaitForSeconds(animTime);
                    base.DeployAction(actParams);
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
                            StartCoroutine(ActionDeploy(4f));
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

