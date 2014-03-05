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
    public class DMModuleScienceAnimate : PartModule, IScienceDataContainer
    {
        [KSPField]
        public string collectActionName = "Collect Science Report";
        [KSPField]
        public string collectWarningText = "This science experiment will be inoperable once this data is collected";
        [KSPField]
        public bool dataIsCollectable = true;
        [KSPField]
        public float interactionRange = 1.2f;
        [KSPField]
        public bool resettableOnEVA = true;
        [KSPField]
        public string experimentActionName = "Collect Data";
        [KSPField]
        public string resetActionName = "Reset Experiment";
        [KSPField]
        public string reviewActionName = "Review Data";
        [KSPField]
        public string transmitWarningText = "";
        [KSPField]
        public string experimentID = null;
        [KSPField]
        public bool hideUIwhenUnavailable = false;
        [KSPField]
        public bool rerunnable = true;
        [KSPField]
        public bool resettable = true;
        //[KSPField]
        //public float resourceResetCost;
        //[KSPField]
        //public string resourceToReset;
        [KSPField(isPersistant = true)]
        public bool Inoperable;
        [KSPField]
        public bool useActionGroups = true;
        [KSPField]
        public bool useStaging = false;
        [KSPField]
        public float xmitDataScalar = 0.5f;
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
        [KSPField(isPersistant = false)]
        public float animSpeed = 1f;
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
        [KSPField]
        public bool oneWayAnimation = false;

        protected Animation anim;
        protected CelestialBody Cbody = null;
        protected ScienceExperiment scienceExp;
        protected IScienceDataTransmitter transmit;
        protected ModuleScienceLab sciLab;
        protected ExperimentsResultDialog expDialog;
        protected IScienceDataContainer ISciCont;
        protected ExperimentResultDialogPage expPage;
        List<ScienceData> newData = new List<ScienceData>();
        public int labint = 0;
        protected List<ModuleScienceLab> labList = new List<ModuleScienceLab>();    


        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            this.part.force_activate();
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
        
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            foreach (ScienceData storedData in newData)
            {
                ConfigNode storedDataNode = node.AddNode("ScienceData");
                storedData.Save(storedDataNode);
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (node.HasNode("ScienceData"))
            {
                foreach (ConfigNode storedDataNode in node.GetNodes("ScienceData"))
                {
                    ScienceData data = new ScienceData(storedDataNode);
                    newData.Add(data);
                }
            }
        }

        public void setup()
        {
            //Events["deployEvent"].active = showStartEvent;
            Events["deployEvent"].guiActive = showStartEvent;
            //Actions["deployAction"].active = showStartEvent;
            //Events["retractEvent"].active = showEndEvent;
            Events["retractEvent"].guiActive = showEndEvent;
            //Actions["retractAction"].active = showEndEvent;
            Events["toggleEvent"].active = showToggleEvent;
            //Actions["toggleAction"].active = showToggleEvent;
            Events["deployEvent"].guiName = startEventGUIName;
            //Actions["deployAction"].guiName = startEventGUIName;
            Events["retractEvent"].guiName = endEventGUIName;
            //Actions["retractAction"].guiName = endEventGUIName;
            Events["toggleEvent"].guiName = toggleEventGUIName;
            //Actions["toggleAction"].guiName = toggleEventGUIName;
            Events["StartExperiment"].guiName = experimentActionName;
            //Actions["StartExperimentAction"].guiName = experimentActionName;
            //Actions["StartExperimentAction"].active = useActionGroups;
            Events["ResetExperiment"].guiName = resetActionName;
            Events["ResetExperiment"].active = newData.Count > 0;
            //Actions["ResetAction"].guiName = resetActionName;
            //Actions["ResetAction"].active = useActionGroups;
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
            Events["reviewPage"].guiName = reviewActionName;
            Events["reviewPage"].active = newData.Count > 0;
            //Actions["ReviewPageAction"].guiName = reviewActionName;
            //Actions["ReviewPageAction"].active = useActionGroups;
            if (waitForAnimationTime == -1) waitForAnimationTime = anim[animationName].length / animSpeed;

            scienceExp = ResearchAndDevelopment.GetExperiment(experimentID);            
        }
        
        public void editorSetup()
        {
            Actions["deployAction"].active = showStartEvent;
            Actions["retractAction"].active = showEndEvent;
            Actions["toggleAction"].active = showToggleEvent;
            Actions["deployAction"].guiName = startEventGUIName;
            Actions["retractAction"].guiName = endEventGUIName;
            Actions["toggleAction"].guiName = toggleEventGUIName;
            Actions["reviewPageAction"].guiName = reviewActionName;
            Actions["reviewPageAction"].active = useActionGroups;
            Actions["StartExperimentAction"].guiName = experimentActionName;
            Actions["StartExperimentAction"].active = useActionGroups;
            Actions["ResetAction"].guiName = resetActionName;
            Actions["ResetAction"].active = useActionGroups;
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
            primaryAnimator((animSpeed * 1f), 0f, WrapMode.Default);
            IsDeployed = !oneWayAnimation;
            Events["deployEvent"].active = oneWayAnimation;
            Events["retractEvent"].active = showEndEvent;
        }

        [KSPAction("Deploy")]
        public void deployAction(KSPActionParam param)
        {
            deployEvent();
        }

        [KSPEvent(guiActive = true, guiName = "Retract", active = false)]
        public void retractEvent()
        {
            if (oneWayAnimation) return;
            primaryAnimator(-1f * animSpeed, 1f, WrapMode.Default);
            IsDeployed = false;
            Events["deployEvent"].active = showStartEvent;
            Events["retractEvent"].active = !showEndEvent;
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
            Events["editorDeployEvent"].active = oneWayAnimation;
            Events["editorRetractEvent"].active = !oneWayAnimation;
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Retract", active = false)]
        public void editorRetractEvent()
        {
            retractEvent();
            Events["editorDeployEvent"].active = true;
            Events["editorRetractEvent"].active = false;
        }

        [KSPEvent(guiActive = true, guiName = "Start Experiment", active = true)]
        public void StartExperiment()
        {
            if (canConduct())
            {
                if (experimentAnimation)
                {
                    if (anim.IsPlaying(animationName)) return;
                    else
                    {
                        if (!IsDeployed)
                        {
                            deployEvent();
                            if (deployingMessage != null) ScreenMessages.PostScreenMessage(deployingMessage, 5f, ScreenMessageStyle.UPPER_CENTER);
                            if (experimentWaitForAnimation) StartCoroutine(WaitForAnimation(waitForAnimationTime));
                            else runExperiment();
                        }
                        else runExperiment();
                    }
                }
                else runExperiment();
            }
            else
            {
                if (customFailMessage != null) ScreenMessages.PostScreenMessage(customFailMessage, 5f, ScreenMessageStyle.UPPER_CENTER);
                else runExperiment();
            }
        }

        [KSPAction("Start Experiment")]
        public void StartExperimentAction(KSPActionParam param)
        {
            if (canConduct())
            {
                if (experimentAnimation)
                {
                    if (anim.IsPlaying(animationName)) return;
                    else
                    {
                        if (!IsDeployed)
                        {
                            deployEvent();
                            if (deployingMessage != null) ScreenMessages.PostScreenMessage(deployingMessage, 5f, ScreenMessageStyle.UPPER_CENTER);
                            if (experimentWaitForAnimation) StartCoroutine(WaitForAnimationAction(param, waitForAnimationTime));
                            else runExperiment();
                        }
                        else runExperiment();
                    }
                }
                else runExperiment();
            }
            else
            {
                if (customFailMessage != null) ScreenMessages.PostScreenMessage(customFailMessage, 5f, ScreenMessageStyle.UPPER_CENTER);
                else runExperiment();
            }
        }

        public IEnumerator WaitForAnimation(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            runExperiment();
        }

        public IEnumerator WaitForAnimationAction(KSPActionParam param, float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            runExperiment();
        }

        public void runExperiment()
        {
            ScienceData data = makeScience();
            newData.Add(data);
            ReviewData();
            Events["reviewPage"].active = newData.Count > 0;
            Events["ResetExperiment"].active = newData.Count > 0;
            Events["CollectDataExternalEvent"].active = newData.Count > 0;
        }

        public ScienceData makeScience()
        {
            ExperimentSituations vesselSituation = getSituation();
            string biome = getBiome();
            ScienceData data = null;
            ScienceExperiment exp = ResearchAndDevelopment.GetExperiment(experimentID);
            ScienceSubject sub = ResearchAndDevelopment.GetExperimentSubject(exp, vesselSituation, vessel.mainBody, biome);
            data = new ScienceData(exp.baseValue * sub.dataScale, xmitDataScalar, xmitDataScalar/2, experimentID, exp.experimentTitle + " of " + " " + biomeCleanup(biome) + " " + situationCleanup(vesselSituation));
            data.subjectID = sub.id;
            return data;
        }      
  
        
        public string getBiome()
        {
            if (scienceExp.BiomeIsRelevantWhile(getSituation()))
            {
                switch (vessel.landedAt)
                {
                    case "LaunchPad":
                        return vessel.landedAt;
                    case "Runway":
                        return vessel.landedAt;
                    case "KSC":
                        return vessel.landedAt;
                    default:
                        return FlightGlobals.currentMainBody.BiomeMap.GetAtt(vessel.latitude * Mathf.Deg2Rad, vessel.longitude * Mathf.Deg2Rad).name;
                }
            }
            else return "";
        }

        public string situationCleanup(ExperimentSituations expSit)
        {
            switch (expSit)
            {
                case ExperimentSituations.SrfLanded:
                    return "on the surface of " + vessel.mainBody.name + ".";
                case ExperimentSituations.SrfSplashed:
                    return "while splashed down on " + vessel.mainBody.name + ".";
                case ExperimentSituations.FlyingLow:
                    return "while flying above " + vessel.mainBody.name + ".";
                case ExperimentSituations.FlyingHigh:
                    return "while flying in " + vessel.mainBody.name + "'s upper atmosphere.";
                case ExperimentSituations.InSpaceLow:
                    return "while in space near " + vessel.mainBody.name + ".";
                default:
                    return "while high in space above " + vessel.mainBody.name + ".";
            }
        }

        public string biomeCleanup(string biome)
        {
            return "";
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
                    if (vessel.altitude < vessel.mainBody.maxAtmosphereAltitude && vessel.mainBody.atmosphere)
                    {
                        if (vessel.altitude < vessel.mainBody.scienceValues.flyingAltitudeThreshold)
                            return ExperimentSituations.FlyingLow;
                        else
                            return ExperimentSituations.FlyingHigh;
                    }
                        if (vessel.altitude < vessel.mainBody.scienceValues.spaceAltitudeThreshold)
                            return ExperimentSituations.InSpaceLow;
                        else
                            return ExperimentSituations.InSpaceHigh;
            }
        }

        [KSPEvent(guiActive = true, guiName = "Reset", active = false)]
        public void ResetExperiment()
        {
            if (!keepDeployed) retractEvent();
            newData.Clear();
            Events["reviewPage"].active = newData.Count > 0;
            Events["ResetExperiment"].active = newData.Count > 0;
            Events["CollectDataExternalEvent"].active = newData.Count > 0;
        }

        [KSPAction("Reset")]
        public void ResetAction(KSPActionParam param)
        {
            if (!keepDeployed) retractEvent();
            newData.Clear();
            Events["reviewPage"].active = newData.Count > 0;
            Events["ResetExperiment"].active = newData.Count > 0;
            Events["CollectDataExternalEvent"].active = newData.Count > 0;
        }

        public ScienceData[] GetData()
        {
            return newData.ToArray();
        }

        public bool IsRerunnable()
        {
            return rerunnable;
        }

        public int GetScienceCount()
        {
            return newData.Count;
        }

        [KSPEvent(guiActive = true, guiName = "Review Data", active = false)]
        public void reviewPage()
        {
            ReviewData();
        }

        [KSPAction("Review Data")]
        public void reviewPageAction(KSPActionParam param)
        {
            ReviewData();
        }

        public void ReviewData()
        {
            ScienceData data = newData[0];
            ExperimentResultDialogPage page = new ExperimentResultDialogPage(part, data, data.transmitValue, xmitDataScalar/2, transmitWarningText != "", transmitWarningText, true, checkLabOps(), new Callback<ScienceData>(onDiscardData), new Callback<ScienceData>(onKeepData), new Callback<ScienceData>(onTransmitData), new Callback<ScienceData>(onSendToLab));
            ExperimentsResultDialog.DisplayResult(page);
        }

        public void ReviewDataItem(ScienceData data)
        {
            data = newData[0];
            ExperimentResultDialogPage page = new ExperimentResultDialogPage(part, data, data.transmitValue, xmitDataScalar/2, transmitWarningText != "", transmitWarningText, true, checkLabOps(), new Callback<ScienceData>(onDiscardData), new Callback<ScienceData>(onKeepData), new Callback<ScienceData>(onTransmitData), new Callback<ScienceData>(onSendToLab));
            ExperimentsResultDialog.DisplayResult(page);
        }

        [KSPEvent(externalToEVAOnly = true, guiActiveUnfocused = true, guiActive = false, guiName = "CollectEVA", active = true, unfocusedRange = 1.5f)]
        public void CollectDataExternalEvent()
        {
            List<ModuleScienceContainer> EVACont = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceContainer>();
            if (newData.Count > 0)
            {
                if (EVACont.First().StoreData(new List<IScienceDataContainer>() { this }, false))
                {
                    if (!keepDeployed) retractEvent();
                    ScreenMessages.PostScreenMessage("Science report transferred to " + FlightGlobals.ActiveVessel.name, 5f, ScreenMessageStyle.UPPER_CENTER);
                    Events["reviewPage"].active = newData.Count > 0;
                    Events["ResetExperiment"].active = newData.Count > 0;
                    Events["CollectDataExternalEvent"].active = newData.Count > 0;
                }
            }
        }

        [KSPEvent(guiName = "ResetEVA", active = true, guiActiveUnfocused = true, externalToEVAOnly = true, guiActive = false, unfocusedRange = 1.5f)]
        public void ResetExperimentExternal()
        {
            if (!keepDeployed) retractEvent();
            newData.Clear();
            Events["reviewPage"].active = newData.Count > 0;
            Events["ResetExperiment"].active = newData.Count > 0;
            Events["CollectDataExternalEvent"].active = newData.Count > 0;
        }

        public void DumpData(ScienceData data)
        {
            if (!keepDeployed) retractEvent();
            newData.Clear();
            Events["reviewPage"].active = newData.Count > 0;
            Events["ResetExperiment"].active = newData.Count > 0;
            Events["CollectDataExternalEvent"].active = newData.Count > 0;
        }

        private void onDiscardData(ScienceData data)
        {
            ResetExperiment();
        }

        private void onKeepData(ScienceData data)
        {
        }

        private void onTransmitData(ScienceData data)
        {
            bool tranBusy = false;
            List<IScienceDataTransmitter> tranList = vessel.FindPartModulesImplementing<IScienceDataTransmitter>();
            if (tranList.Count > 0 && newData.Contains(data))
            {
                foreach (IScienceDataTransmitter tran in tranList)
                {
                    if (tran.CanTransmit())
                    {
                        if (!tran.IsBusy())         
                        {
                            List<ScienceData> tranData = new List<ScienceData>();
                            tranData.Add(data);
                            tran.TransmitData(tranData);
                            newData.Remove(data);
                            Events["reviewPage"].active = newData.Count > 0;
                            Events["ResetExperiment"].active = newData.Count > 0;
                            if (!keepDeployed) retractEvent();
                            break;
                        }
                        else
                        {
                            tranBusy = true;
                        }
                    }
                }
                if (tranBusy)               
                {
                    List<ScienceData> tranData = new List<ScienceData>();
                    tranData.Add(data);
                    tranList.First().TransmitData(tranData);
                    newData.Remove(data);
                    Events["reviewPage"].active = newData.Count > 0;
                    Events["ResetExperiment"].active = newData.Count > 0;
                    if (!keepDeployed) retractEvent();
                }
            }   
        }

        private void onSendToLab(ScienceData data)
        {
            if (checkLabOps())
            {
                labList[labint].StartCoroutine(labList[labint].ProcessData(data, new Callback<ScienceData>(onComplete)));
            }
            else
            {
                ScreenMessages.PostScreenMessage("No operational lab modules on this vessel. Cannot analyze data.", 4f, ScreenMessageStyle.UPPER_CENTER);
            }  
        }

        private void onComplete(ScienceData data)
        {
            ReviewData();
        }

        public bool checkLabOps()      
        {
            labList = vessel.FindPartModulesImplementing<ModuleScienceLab>();
            bool labOp = false;
            for (labint = 0; labint < labList.Count; labint++)
            {
                if (labList[labint].IsOperational())
                {
                    labOp = true;
                    break;
                }
            }
            return labOp;
        }

    }
}
