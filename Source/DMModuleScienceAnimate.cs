/* DMagic Orbital Science - Module Science Animate
 * Generic module for animated science experiments.
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


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;

namespace DMagicOrbital
{
    public class DMModuleScienceAnimate : ModuleScienceExperiment
    {
        [KSPField]
        new public string collectActionName = "Collect Science Report";
        [KSPField]
        new public string collectWarningText = "This science experiment will be inoperable once this data is collected";
        [KSPField]
        new public bool dataIsCollectable = true;
        [KSPField]
        new public float interactionRange = 1.2f;
        [KSPField]
        new public bool resettableOnEVA = true;
        [KSPField]
        new public string experimentActionName = "Collect Data";
        [KSPField]
        new public string resetActionName = "Reset Experiment";
        [KSPField]
        new public string reviewActionName = "Review Data";
        [KSPField]
        new public string transmitWarningText = "This science experiment will be inoperable after tranmission";
        [KSPField]
        new public string experimentID = null;
        [KSPField]
        new public bool hideUIwhenUnavailable = false;        
        [KSPField]
        new public bool rerunnable = true;
        [KSPField]
        new public bool resettable = true;        
        [KSPField]
        new public float resourceResetCost;
        [KSPField]
        new public string resourceToReset;
        [KSPField(isPersistant = true)]
        new public bool Inoperable;      
        [KSPField]
        new public bool useActionGroups = true;
        [KSPField]
        new public bool useStaging = false;
        [KSPField]
        new public float xmitDataScalar = 0.5f;
        [KSPField]
        public string customFailMessage = null;
        [KSPField]
        public string deployingMessage = null;

        [KSPField(isPersistant = true)]
        public bool IsDeployed;
        [KSPField]
        public bool allowManualControl;
        [KSPField]
        public string animationName;
        [KSPField(isPersistant = true)]
        public float animSpeed;
        [KSPField(isPersistant = true)]
        public bool animSwitch;
        [KSPField(isPersistant = true)]
        public float animTime;
        [KSPField]
        public string endEventGUIName = "Retract";
        [KSPField]
        public bool showEndEvent = true;
        [KSPField]
        public bool isOneShot = false;
        [KSPField]
        public string startEventGUIName = "Deploy";
        [KSPField]
        public bool showStartEvent = true;
        [KSPField]
        public string toggleEventGUIName = "Toggle";
        [KSPField]
        public bool showToggleEvent = false;
        [KSPField]
        public bool showEditorEvents = true;
        [KSPField(guiName = "Status", isPersistant = true, guiActive = true)]
        public string status;

        [KSPField]
        public bool experimentAnimation = false;
        [KSPField]
        public bool experimentWaitForAnimation = false;        
        [KSPField]
        public float waitForAnimationTime = -1;
        [KSPField]
        public bool keepDeployed = false;

        protected Animation anim;
        protected CelestialBody Cbody = null;
        protected ScienceExperiment scienceExp;
        protected IScienceDataTransmitter transmit;
        protected ExperimentResultDialogPage expPage;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            anim = part.FindModelAnimators(animationName)[0];
            if (state == StartState.Editor)
            {
                editorSetup();
            }
            else
            {
                setup();
                if (IsDeployed) primaryAnimator(1f, 1f, WrapMode.Default);
            }
        }
        
        public void setup()
        {
            Events["deployEvent"].active = showStartEvent;
            Actions["deployAction"].active = showStartEvent;
            Events["retractEvent"].active = showEndEvent;
            Actions["retractAction"].active = showEndEvent;
            Events["toggleEvent"].active = showToggleEvent;
            Actions["toggleAction"].active = showToggleEvent;
            Events["deployEvent"].guiName = startEventGUIName;
            Actions["deployAction"].guiName = startEventGUIName;
            Events["retractEvent"].guiName = endEventGUIName;
            Actions["retractEvent"].guiName = endEventGUIName;
            Events["toggleEvent"].guiName = toggleEventGUIName;
            Actions["toggeleAction"].guiName = toggleEventGUIName;
            Events["DeployExperiment"].guiName = experimentActionName;
            Actions["DeployAction"].guiName = experimentActionName;
            Actions["DeployAction"].active = useActionGroups;
            Events["ResetExperiment"].guiName = resetActionName;
            Actions["ResetAction"].guiName = resetActionName;
            Actions["ResetAction"].active = useActionGroups;
            Events["CollectDataExternalEvent"].active = dataIsCollectable;
            Events["CollectDataExternalEvent"].guiActiveUnfocused = dataIsCollectable;
            Events["CollectDataExternalEvent"].externalToEVAOnly = dataIsCollectable;
            Events["CollectDataExternalEvent"].guiName = collectActionName;
            Events["CollectDataExternalEvent"].unfocusedRange = interactionRange;
            Events["ResetExperimentExternal"].active = resettableOnEVA;
            Events["ResetExperimentExternal"].externalToEVAOnly = resettableOnEVA;
            Events["ResetExperimentExternal"].guiActiveUnfocused = resettableOnEVA;
            Events["ResetExperimentExternal"].guiName = resetActionName;
            Events["ResetExperimentExternal"].unfocusedRange = interactionRange;
            Events["ReviewDataEvent"].guiName = reviewActionName;
            Actions["ReviewDataItem"].guiName = reviewActionName;
            Actions["ReviewDataItem"].active = useActionGroups;
            if (waitForAnimationTime == -1) waitForAnimationTime = anim[animationName].length;

            scienceExp = ResearchAndDevelopment.GetExperiment(experimentID);            
        }
        
        public void editorSetup()
        {
            Events["editorDeployEvent"].guiName = startEventGUIName;
            Events["editorRetractEvent"].guiName = endEventGUIName;
            Events["editorDeployEvent"].active = showEditorEvents;
            Events["editorRetractEvent"].active = false;
        }

        public void primaryAnimator(float speed, float time, WrapMode wrap)
        {
            anim[animationName].speed = speed;
            if (!anim.IsPlaying(animationName))
            {
                anim[animationName].wrapMode = wrap;
                anim[animationName].normalizedTime = time;
                anim.Play(animationName);
            }
        }

        [KSPEvent(guiActive = true, guiName = "Deploy", active = true)]
        public void deployEvent()
        {
            primaryAnimator(1f, 0f, WrapMode.Default);
            IsDeployed = true;
            Events["deployEvent"].active = false;
            Events["retractEvent"].active = true;
        }

        [KSPAction("Deploy")]
        public void deployAction(KSPActionParam param)
        {
            deployEvent();
        }

        [KSPEvent(guiActive = true, guiName = "Retract", active = false)]
        public void retractEvent()
        {
            primaryAnimator(-1f, 1f, WrapMode.Default);
            IsDeployed = false;
            Events["deployEvent"].active = true;
            Events["retractEvent"].active = false;
        }

        [KSPAction("Retract")]
        public void retractAction(KSPActionParam param)
        {
            retractEvent();
        }

        [KSPEvent(guiActive = true, guiName = "Toggle", active = true)]
        public void toggleEvent()
        {
            if (IsDeployed) retractEvent();
            else deployEvent();
        }

        [KSPAction("Toggle")]
        public void toggleAction(KSPActionParam Param)
        {
            toggleEvent();
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Deploy", active = true)]
        public void editorDeployEvent()
        {
            deployEvent();
            Events["editorDeployEvent"].active = false;
            Events["editorRetractEvent"].active = true;
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Retract", active = false)]
        public void editorRetractEvent()
        {
            retractEvent();
            Events["editorDeployEvent"].active = true;
            Events["editorRetractEvent"].active = false;
        }

        [KSPEvent(guiActive = true, guiName = "Deploy Experiment", active = true)]
        new public void DeployExperiment()
        {
            if (canConduct())
            {
                if (experimentAnimation)
                {
                    if (!IsDeployed) deployEvent(); 
                    if (deployingMessage != null) ScreenMessages.PostScreenMessage(deployingMessage, 5f, ScreenMessageStyle.UPPER_CENTER);
                    if (experimentWaitForAnimation) StartCoroutine(WaitForAnimation(waitForAnimationTime));
                    else base.DeployExperiment();
                }
                else base.DeployExperiment();
            }
            else
            {
                if (customFailMessage != null) ScreenMessages.PostScreenMessage(customFailMessage, 5f, ScreenMessageStyle.UPPER_CENTER);
                else base.DeployExperiment();
            }
        }

        [KSPAction("Deploy Experiment")]
        new public void DeployAction(KSPActionParam param)
        {
            if (canConduct())
            {
                if (experimentAnimation)
                {
                    if (!IsDeployed) deployEvent();
                    if (deployingMessage != null) ScreenMessages.PostScreenMessage(deployingMessage, 5f, ScreenMessageStyle.UPPER_CENTER);
                    if (experimentWaitForAnimation) StartCoroutine(WaitForAnimationAction(param, waitForAnimationTime));
                    else base.DeployAction(param);
                }
                else base.DeployAction(param);
            }
            else
            {
                if (customFailMessage != null) ScreenMessages.PostScreenMessage(customFailMessage, 5f, ScreenMessageStyle.UPPER_CENTER);
                else base.DeployAction(param);
            }
        }

        public IEnumerator WaitForAnimation(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            base.DeployExperiment();
        }

        public IEnumerator WaitForAnimationAction(KSPActionParam param, float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            base.DeployAction(param);
        }

        [KSPEvent(guiActive = true, guiName = "Reset Experiment", active = false)]
        new public void ResetExperiment()
        {
            if (!keepDeployed) retractEvent();
            base.ResetExperiment();
        }

        [KSPAction("Reset Experiment")]
        new public void ResetAction(KSPActionParam param)
        {
            if (!keepDeployed) retractEvent();
            base.ResetAction(param);
        }

        [KSPEvent(guiActive = true, guiName = "Review Data")]
        new public void ReviewDataEvent()
        {
            base.ReviewDataEvent();
        }

        [KSPAction("Review Data")]
        new public void ReviewDataItem(ScienceData data)
        {
            base.ReviewDataItem(data);
        }

        [KSPEvent(externalToEVAOnly = true, guiActiveUnfocused = true, guiActive = false, guiName = "Collect Data", active = true, unfocusedRange = 1.5f)]
        new public void CollectDataExternalEvent()
        {
            if (!keepDeployed) retractEvent();
            base.CollectDataExternalEvent();
        }

        [KSPEvent(guiName = "Reset", active = true, guiActiveUnfocused = true, externalToEVAOnly = true, guiActive = false, unfocusedRange = 1.5f)]
        new public void ResetExperimentExternal()
        {
            if (!keepDeployed) retractEvent();
            base.ResetExperimentExternal();
        }
        
        public bool canConduct()
        {
            return scienceExp.IsAvailableWhile(getSituation(), vessel.mainBody);
        }

        public ExperimentSituations getSituation()
        {
            switch (vessel.situation)
            {
                case Vessel.Situations.LANDED:
                case Vessel.Situations.PRELAUNCH:
                    return ExperimentSituations.SrfLanded;
                case Vessel.Situations.SPLASHED:
                    return ExperimentSituations.SrfSplashed;
                default:
                    if (vessel.altitude < Cbody.maxAtmosphereAltitude && Cbody.atmosphere)
                    {
                        if (vessel.altitude < Cbody.scienceValues.flyingAltitudeThreshold)
                            return ExperimentSituations.FlyingLow;
                        else
                            return ExperimentSituations.FlyingHigh;
                    }
                    if (vessel.altitude < Cbody.scienceValues.spaceAltitudeThreshold)
                        return ExperimentSituations.InSpaceLow;
                    else
                        return ExperimentSituations.InSpaceHigh;
            }
        }

    }
}
