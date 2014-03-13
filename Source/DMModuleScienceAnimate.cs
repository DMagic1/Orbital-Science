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

namespace DMagic
{
    class DMModuleScienceAnimate : ModuleScienceExperiment
    {
        [KSPField]
        public string customFailMessage = null;
        [KSPField]
        public string deployingMessage = null;
        [KSPField(isPersistant = true)]
        public bool IsDeployed;
        [KSPField]
        public string animationName = null;
        [KSPField]
        public bool allowManualControl = false;
        [KSPField(isPersistant = false)]
        public float animSpeed = 1f;
        [KSPField(isPersistant = true)]
        public bool animSwitch = true;
        [KSPField(isPersistant = true)]
        public float animTime = 0f;
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

        [KSPField]
        public bool experimentAnimation = true;
        [KSPField]
        public bool experimentWaitForAnimation = false;
        [KSPField]
        public float waitForAnimationTime = -1;
        [KSPField]
        public bool keepDeployed = false;
        [KSPField]
        public int keepDeployedMode = 0;
        [KSPField]
        public bool oneWayAnimation = false;

        protected Animation anim;
        protected CelestialBody Cbody = null;
        protected ScienceExperiment scienceExp;
        
        List<ScienceData> scienceReportList = new List<ScienceData>();
        List<ModuleScienceLab> labList = new List<ModuleScienceLab>();

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
                eventsCheck();
                setup();
                GetData();
                if (IsDeployed) primaryAnimator(1f, 1f, WrapMode.Default);
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            foreach (ScienceData storedData in scienceReportList)
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
                    scienceReportList.Add(data);
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
            Events["toggleEvent"].guiActive = showToggleEvent;
            //Actions["toggleAction"].active = showToggleEvent;
            Events["deployEvent"].guiName = startEventGUIName;
            //Actions["deployAction"].guiName = startEventGUIName;
            Events["retractEvent"].guiName = endEventGUIName;
            //Actions["retractAction"].guiName = endEventGUIName;
            Events["toggleEvent"].guiName = toggleEventGUIName;
            //Actions["toggleAction"].guiName = toggleEventGUIName;
            //Events["StartExperiment"].guiName = experimentActionName;
            //Actions["StartExperimentAction"].guiName = experimentActionName;
            //Actions["StartExperimentAction"].active = useActionGroups;
            //Events["ResetExperiment"].guiName = resetActionName;
            //Events["ResetExperiment"].active = scienceReportList.Count > 0;
            //Actions["ResetAction"].guiName = resetActionName;
            //Actions["ResetAction"].active = useActionGroups;
            //Events["CollectDataExternalEvent"].active = scienceReportList.Count > 0;
            //Events["CollectDataExternalEvent"].guiActiveUnfocused = dataIsCollectable;
            //Events["CollectDataExternalEvent"].externalToEVAOnly = dataIsCollectable;
            //Events["CollectDataExternalEvent"].guiName = collectActionName;
            //Events["CollectDataExternalEvent"].unfocusedRange = interactionRange;
            //Events["ResetExperimentExternal"].active = scienceReportList.Count > 0;
            //Events["ResetExperimentExternal"].externalToEVAOnly = resettableOnEVA;
            //Events["ResetExperimentExternal"].guiActiveUnfocused = resettableOnEVA;
            //Events["ResetExperimentExternal"].guiName = resetActionName;
            //Events["ResetExperimentExternal"].unfocusedRange = interactionRange;
            //Events["reviewPage"].guiName = reviewActionName;
            //Events["reviewPage"].active = scienceReportList.Count > 0;
            //Actions["ReviewPageAction"].guiName = reviewActionName;
            //Actions["ReviewPageAction"].active = useActionGroups;
            if (waitForAnimationTime == -1) waitForAnimationTime = anim[animationName].length / animSpeed;
            if (experimentID != null) scienceExp = ResearchAndDevelopment.GetExperiment(experimentID);
        }

        public void editorSetup()
        {
            Actions["deployAction"].active = showStartEvent;
            Actions["retractAction"].active = showEndEvent;
            Actions["toggleAction"].active = showToggleEvent;
            Actions["deployAction"].guiName = startEventGUIName;
            Actions["retractAction"].guiName = endEventGUIName;
            Actions["toggleAction"].guiName = toggleEventGUIName;
            //Actions["reviewPageAction"].guiName = reviewActionName;
            //Actions["reviewPageAction"].active = useActionGroups;
            //Actions["StartExperimentAction"].guiName = experimentActionName;
            //Actions["StartExperimentAction"].active = useActionGroups;
            //Actions["ResetAction"].guiName = resetActionName;
            //Actions["ResetAction"].active = useActionGroups;
            Events["editorDeployEvent"].guiName = startEventGUIName;
            Events["editorRetractEvent"].guiName = endEventGUIName;
            Events["editorDeployEvent"].active = showEditorEvents;
            Events["editorRetractEvent"].active = false;
        }

        #region Animators

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
            primaryAnimator(animSpeed * 1f, 0f, WrapMode.Default);
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
            IsDeployed = false;
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

        #endregion

        #region Science Events and Actions

        //[KSPEvent(guiActive = true, guiName = "Review Data", active = false)]
        //public void reviewPage()
        //{
        //    if (scienceReportList.Count > 0) ReviewData();
        //}

        //Maybe remove this altogether
        //[KSPAction("Review Data")]
        //public void reviewPageAction(KSPActionParam param)
        //{
        //    if (scienceReportList.Count > 0) ReviewData();
        //}        

        //[KSPEvent(guiActive = true, guiName = "Reset", active = false)]
        new public void ResetExperiment()
        {
            if (scienceReportList.Count > 0)
            {
                if (keepDeployedMode == 0) retractEvent();
                scienceReportList.Clear();
                eventsCheck();
            }
        }

        //[KSPAction("Reset")]
        new public void ResetAction(KSPActionParam param)
        {
            ResetExperiment();
        }

        //This ridiculous chunk of code seems to make the EVA data collection work properly
        public class EVAIScienceContainer: IScienceDataContainer
        {
            List<ScienceData> EVADataList = new List<ScienceData>();
            public EVAIScienceContainer(ScienceData data)
            {
                EVADataList.Add(data);
            }
            public bool IsRerunnable()
            {
                return true;
            }
            public int GetScienceCount()
            {
                return 1;
            }
            public void ReviewData()
            {
            }
            public void ReviewDataItem(ScienceData data)
            {
            }
            public void DumpData(ScienceData data)
            {
            }
            public ScienceData[] GetData()
            {
                return EVADataList.ToArray();
            }
        }

        EVAIScienceContainer EVAIScience;

        //[KSPEvent(externalToEVAOnly = true, guiActiveUnfocused = true, guiActive = false, guiName = "CollectEVA", active = true, unfocusedRange = 1.5f)]
        new public void CollectDataExternalEvent()
        {
            List<ModuleScienceContainer> EVACont = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceContainer>();
            //List<ScienceData> EVAdata = new List<ScienceData>();
            //EVAdata.Add(scienceReportList[0]);
            EVAIScience = new EVAIScienceContainer(scienceReportList[0]);
            if (scienceReportList.Count > 0)
            {
                if (EVACont.First().StoreData(new List<IScienceDataContainer> { EVAIScience }, false))    //scienceReportList[0])}, false))
                {                    
                    scienceReportList.Clear();
                    ScreenMessages.PostScreenMessage("Sample data transferred to " + FlightGlobals.ActiveVessel.name, 3f, ScreenMessageStyle.UPPER_CENTER);
                    if (keepDeployedMode == 0) retractEvent();
                    eventsCheck();
                }
            }
        }

        //[KSPEvent(guiName = "ResetEVA", active = true, guiActiveUnfocused = true, externalToEVAOnly = true, guiActive = false, unfocusedRange = 1.5f)]
        new public void ResetExperimentExternal()
        {
            ResetExperiment();
        }

        public void eventsCheck()
        {
            //Events["reviewPage"].active = scienceReportList.Count > 0;
            
            //Unnecessary???
            Events["ResetExperiment"].active = scienceReportList.Count > 0;
            Events["ResetExperimentExternal"].active = scienceReportList.Count > 0;
            Events["CollectDataExternalEvent"].active = scienceReportList.Count > 0;
            Events["DeployExperiment"].active = !Inoperable;
            //Events["DeployExperiment"].active = scienceReportList.Count == 0;
            Events["ReviewDataEvent"].active = scienceReportList.Count > 0;
        }

        #endregion

        #region Science Experiment Setup

        //Can't use base.DeployExperiment here, we need to create our own science data and control the experiment results page
        new public void DeployExperiment()
        {
            if (Inoperable) ScreenMessages.PostScreenMessage("Experiment is no longer functional; must be reset at a science lab or returned to Kerbin", 6f, ScreenMessageStyle.UPPER_CENTER);
            else
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
                }
            }
        }

        //[KSPAction("Start Experiment")]
        new public void DeployAction(KSPActionParam param)
        {
            DeployExperiment();
        }

        //In case we need to wait for an animation to finish before running the experiment
        public IEnumerator WaitForAnimation(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            runExperiment();
        }

        public void runExperiment()
        {
            ScienceData data = makeScience();
            scienceReportList.Add(data);
            ReviewData();
            eventsCheck();
            if (keepDeployedMode == 1) retractEvent();
        }

        //Create the science data
        public ScienceData makeScience()
        {
            ExperimentSituations vesselSituation = getSituation();
            string biome = getBiome(vesselSituation);
            ScienceData data = null;
            ScienceExperiment exp = ResearchAndDevelopment.GetExperiment(experimentID);
            ScienceSubject sub = ResearchAndDevelopment.GetExperimentSubject(exp, vesselSituation, vessel.mainBody, biome);
            data = new ScienceData(exp.baseValue * sub.dataScale, xmitDataScalar, xmitDataScalar / 2, experimentID, exp.experimentTitle + situationCleanup(vesselSituation, biome));
            data.subjectID = sub.id;
            return data;
        }

        public string getBiome(ExperimentSituations s)
        {
            if (scienceExp.BiomeIsRelevantWhile(s))
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

        public bool canConduct()
        {
            return scienceExp.IsAvailableWhile(getSituation(), vessel.mainBody);
        }

        //Get our experimental situation based on the vessel's current flight situation, fix stock bugs with aerobraking and reentry.
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

        //This is for the title bar of the experiment results page
        public string situationCleanup(ExperimentSituations expSit, string b)
        {
            if (b == "")
            {
                switch (expSit)
                {
                    case ExperimentSituations.SrfLanded:
                        return " from  " + vessel.mainBody.name + "'s surface";
                    case ExperimentSituations.SrfSplashed:
                        return " from " + vessel.mainBody.name + "'s oceans";
                    case ExperimentSituations.FlyingLow:
                        return " while flying at " + vessel.mainBody.name;
                    case ExperimentSituations.FlyingHigh:
                        return " from " + vessel.mainBody.name + "'s upper atmosphere";
                    case ExperimentSituations.InSpaceLow:
                        return " while in space near " + vessel.mainBody.name;
                    default:
                        return " while in space high over " + vessel.mainBody.name;
                }
            }
            else
            {
                switch (expSit)
                {
                    case ExperimentSituations.SrfLanded:
                        return " from " + vessel.mainBody.name + "'s " + b;
                    case ExperimentSituations.SrfSplashed:
                        return " from " + vessel.mainBody.name + "'s " + b;
                    case ExperimentSituations.FlyingLow:
                        return " while flying over " + vessel.mainBody.name + "'s " + b;
                    case ExperimentSituations.FlyingHigh:
                        return " from the upper atmosphere over " + vessel.mainBody.name + "'s " + b;
                    case ExperimentSituations.InSpaceLow:
                        return " from space just above " + vessel.mainBody.name + "'s " + b;
                    default:
                        return " while in space high over " + vessel.mainBody.name + "'s " + b;
                }
            }
        }

        //Custom experiment results dialog page, allows full control over the buttons on that page
        new public void ReviewData()
        {
            if (scienceReportList.Count > 0)
            {
                GetData();
                ScienceData data = scienceReportList[0];
                ExperimentResultDialogPage page = new ExperimentResultDialogPage(part, data, data.transmitValue, xmitDataScalar / 2, !rerunnable, transmitWarningText, true, data.labBoost < 1 && checkLabOps() && xmitDataScalar < 1, new Callback<ScienceData>(onDiscardData), new Callback<ScienceData>(onKeepData), new Callback<ScienceData>(onTransmitData), new Callback<ScienceData>(onSendToLab));
                ExperimentsResultDialog.DisplayResult(page);
            }
        }

        new public void ReviewDataEvent()
        {
            ReviewData();
        }

        #endregion   

        #region Experiment Results Control

        //Still not quite sure what exactly this is doing
        new public ScienceData[] GetData()
        {
            return scienceReportList.ToArray();
        }

        new public bool IsRerunnable()
        {
            return base.IsRerunnable();
        }

        new public int GetScienceCount()
        {
            return scienceReportList.Count;
        }

        //This is called after data is transmitted by right-clicking on the transmitter itself
        new public void DumpData(ScienceData data)
        {
            if (scienceReportList.Count > 0)
            {
                base.DumpData(data);
                if (keepDeployedMode == 0) retractEvent();
                scienceReportList.Clear();
                Events["DeployExperiment"].active = !Inoperable;
                print("Dump Data");
            }
        }

        private void onDiscardData(ScienceData data)
        {            
            ResetExperiment();
            print("Discard data from page");
        }

        private void onKeepData(ScienceData data)
        {
            print("Store date from page");
        }

        private void onTransmitData(ScienceData data)
        {
            List<IScienceDataTransmitter> tranList = vessel.FindPartModulesImplementing<IScienceDataTransmitter>();
            if (tranList.Count > 0 && scienceReportList.Count > 0)
            {
                tranList.OrderBy(ScienceUtil.GetTransmitterScore).First().TransmitData(new List<ScienceData> {data});
                DumpData(data);
                print("Transmit data from page");
            }
            else ScreenMessages.PostScreenMessage("No transmitters available on this vessel.", 4f, ScreenMessageStyle.UPPER_LEFT);
        }

        private void onSendToLab(ScienceData data)
        {
            if (checkLabOps() && scienceReportList.Count > 0) labList.OrderBy(ScienceUtil.GetLabScore).First().StartCoroutine(labList.First().ProcessData(data, new Callback<ScienceData>(onComplete)));
            else ScreenMessages.PostScreenMessage("No operational lab modules on this vessel. Cannot analyze data.", 4f, ScreenMessageStyle.UPPER_CENTER);
            print("Send data to lab");
        }

        private void onComplete(ScienceData data)
        {
            ReviewData();
            print("Data processed in lab");
        }

        //Maybe unnecessary, can be folded into a simpler method???
        public bool checkLabOps()
        {
            labList = vessel.FindPartModulesImplementing<ModuleScienceLab>();
            bool labOp = false;
            for (int i = 0; i < labList.Count; i++)
            {
                if (labList[i].IsOperational())
                {
                    labOp = true;
                    break;
                }
            }
            return labOp;
        }

        #endregion

    }
}
