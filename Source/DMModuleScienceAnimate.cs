/* DMagic Orbital Science - Module Science Animate
 * Generic module for animated science experiments.
 *
 * Copyright (c) 2014, David Grandy
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

namespace DMagic
{
    public class DMModuleScienceAnimate : ModuleScienceExperiment, IScienceDataContainer
    {
        [KSPField]
        public string customFailMessage = null;
        [KSPField]
        public string deployingMessage = null;
        [KSPField(isPersistant = true)]
        public bool IsDeployed;
        [KSPField]
        public string animationName = null;
        //[KSPField]
        //public bool allowManualControl = false;
        [KSPField(isPersistant = false)]
        public float animSpeed = 1f;
        //[KSPField(isPersistant = true)]
        //public bool animSwitch = true;
        //[KSPField(isPersistant = true)]
        //public float animTime = 0f;
        [KSPField]
        public string endEventGUIName = "Retract";
        [KSPField]
        public bool showEndEvent = true;
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
        public int keepDeployedMode = 0;
        [KSPField]
        public bool oneWayAnimation = false;
        [KSPField]
        public string resourceExperiment = "ElectricCharge";
        [KSPField]
        public float resourceExpCost = 0;
        [KSPField]
        public bool asteroidReports = false;
        [KSPField]
        public bool USStock = false;
        [KSPField]
        public bool primary = true;

        protected Animation anim;
        protected ScienceExperiment scienceExp;
        private bool resourceOn = false;
        private int dataIndex = 0;
        private List<DMEnviroSensor> enviroList = new List<DMEnviroSensor>();
        private List<DMModuleScienceAnimate> primaryList = new List<DMModuleScienceAnimate>();
        private DMModuleScienceAnimate primaryModule = null;

        //Record some default values for Eeloo here to prevent the asteroid science method from screwing them up
        private const string bodyDescription = "There’s been a considerable amount of controversy around the status of Eeloo as being a proper planet or a just “lump of ice going around the Sun”. The debate is still ongoing, since most academic summits held to address the issue have devolved into, on good days, petty name calling, and on worse ones, all-out brawls.";
        private const string bodyName = "Eeloo";
        private const float bodyLandedValue = 15;
        private const float bodySpaceValue = 12;
        
        List<ScienceData> scienceReportList = new List<ScienceData>();

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            this.part.force_activate();
            anim = part.FindModelAnimators(animationName)[0];
            if (state == StartState.Editor) editorSetup();
            else
            {
                setup();
                if (IsDeployed) primaryAnimator(1f, 1f, WrapMode.Default);
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.RemoveNodes("ScienceData");
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

        public override void OnInitialize()
        {
            base.OnInitialize();
            eventsCheck();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (resourceOn)
            {
                if (PartResourceLibrary.Instance.GetDefinition(resourceExperiment) != null)
                {
                    float cost = resourceExpCost * Time.deltaTime;
                    if (part.RequestResource(resourceExperiment, cost) < cost)
                    {
                        StopCoroutine("WaitForAnimation");
                        resourceOn = false;
                        ScreenMessages.PostScreenMessage("Not enough power, shutting down experiment", 4f, ScreenMessageStyle.UPPER_CENTER);
                        if (keepDeployedMode == 0 || keepDeployedMode == 1) retractEvent();
                    }
                }
            }
        }

        public override string GetInfo()
        {
            if (resourceExpCost > 0)
            {
                string info = base.GetInfo();
                info += "Requires:\n-" + resourceExperiment + ": " + resourceExpCost.ToString() + "/s for " + waitForAnimationTime.ToString() + "s\n";
                return info;
            }
            else return base.GetInfo();
        }

        public void setup()
        {
            Events["deployEvent"].guiActive = showStartEvent;
            Events["retractEvent"].guiActive = showEndEvent;
            Events["toggleEvent"].guiActive = showToggleEvent;
            Events["deployEvent"].guiName = startEventGUIName;
            Events["retractEvent"].guiName = endEventGUIName;
            Events["toggleEvent"].guiName = toggleEventGUIName;
            if (!primary)
            {
                primaryList = this.part.FindModulesImplementing<DMModuleScienceAnimate>();
                foreach (DMModuleScienceAnimate DMS in primaryList)
                {
                    if (DMS.primary) primaryModule = DMS;
                }
            }
            if (USStock) enviroList = this.part.FindModulesImplementing<DMEnviroSensor>();
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
            Events["editorDeployEvent"].guiName = startEventGUIName;
            Events["editorRetractEvent"].guiName = endEventGUIName;
            Events["editorDeployEvent"].active = showEditorEvents;
            Events["editorRetractEvent"].active = false;
        }

        #region Animators

        public void primaryAnimator(float speed, float time, WrapMode wrap)
        {
            if (anim != null)
            {
                anim[animationName].speed = speed;
                if (!anim.IsPlaying(animationName))
                {
                    anim[animationName].wrapMode = wrap;
                    anim[animationName].normalizedTime = time;
                    anim.Blend(animationName);
                }
            }
        }

        [KSPEvent(guiActive = true, guiName = "Deploy", active = true)]
        public void deployEvent()
        {
            primaryAnimator(animSpeed * 1f, 0f, WrapMode.Default);
            IsDeployed = !oneWayAnimation;
            if (USStock)
            {
                foreach (DMEnviroSensor DMES in enviroList)
                {
                    if (!DMES.sensorActive)
                    {
                        if (DMES.primary) DMES.toggleSensor();
                    }
                }
            }
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
            if (USStock)
            {
                foreach (DMEnviroSensor DMES in enviroList)
                {
                    if (DMES.sensorActive)
                    {
                        if (DMES.primary) DMES.toggleSensor();
                    }
                }
            }
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

        new public void ResetExperiment()
        {
            if (scienceReportList.Count > 0)
            {
                if (keepDeployedMode == 0) retractEvent();
                scienceReportList.Clear();
            }
            eventsCheck();
        }

        new public void ResetAction(KSPActionParam param)
        {
            ResetExperiment();
        }

        //This ridiculous chunk of code seems to make the EVA data collection work properly
        public class EVAIScienceContainer : IScienceDataContainer
        {
            private bool rerunnable = true;
            List<ScienceData> EVADataList = new List<ScienceData>();
            public EVAIScienceContainer(List<ScienceData> dataList, bool rerun)
            {
                foreach (ScienceData data in dataList)
                {
                    EVADataList.Add(data);
                }
                rerunnable = rerun;
            }
            public bool IsRerunnable()
            {
                return rerunnable;
            }
            public int GetScienceCount()
            {
                return EVADataList.Count;
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

        new public void CollectDataExternalEvent()
        {   
            List<ModuleScienceContainer> EVACont = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceContainer>();
            EVAIScience = new EVAIScienceContainer(scienceReportList, rerunnable);
            if (scienceReportList.Count > 0)
            {
                if (EVACont.First().StoreData(new List<IScienceDataContainer> { EVAIScience }, false)) DumpAllData(scienceReportList);
            }
        }
        
        new public void ResetExperimentExternal()
        {
            ResetExperiment();
        }

        public void eventsCheck()
        {
            Events["ResetExperiment"].active = scienceReportList.Count > 0;
            Events["ResetExperimentExternal"].active = scienceReportList.Count > 0;
            Events["CollectDataExternalEvent"].active = scienceReportList.Count > 0;
            Events["DeployExperiment"].active = !Inoperable;
            Events["ReviewDataEvent"].active = scienceReportList.Count > 0;
        }

        #endregion

        #region Science Experiment Setup

        //Can't use base.DeployExperiment here, we need to create our own science data and control the experiment results page
        new public void DeployExperiment()
        {
            if (Inoperable) ScreenMessages.PostScreenMessage("Experiment is no longer functional; must be reset at a science lab or returned to Kerbin", 6f, ScreenMessageStyle.UPPER_CENTER);
            else if (scienceReportList.Count == 0)
            {
                if (canConduct())
                {
                    if (experimentAnimation)
                    {
                        if (anim.IsPlaying(animationName)) return;
                        else
                        {
                            if (!primary)
                            {
                                if (!primaryModule.IsDeployed) primaryModule.deployEvent();
                                IsDeployed = true;
                            }
                            if (!IsDeployed)
                            {
                                deployEvent();
                                if (!string.IsNullOrEmpty(deployingMessage)) ScreenMessages.PostScreenMessage(deployingMessage, 5f, ScreenMessageStyle.UPPER_CENTER);
                                if (experimentWaitForAnimation)
                                {
                                    if (resourceExpCost > 0) resourceOn = true;
                                    StartCoroutine("WaitForAnimation", waitForAnimationTime);
                                }
                                else runExperiment();
                            }
                            else if (resourceExpCost > 0)
                            {
                                resourceOn = true;
                                StartCoroutine("WaitForAnimation", waitForAnimationTime);
                            }
                            else runExperiment();
                        }
                    }
                    else runExperiment();
                }
                else
                {
                    if (!string.IsNullOrEmpty(customFailMessage)) ScreenMessages.PostScreenMessage(customFailMessage, 5f, ScreenMessageStyle.UPPER_CENTER);
                }
            }
            else ReviewData();
        }

        new public void DeployAction(KSPActionParam param)
        {
            DeployExperiment();
        }

        //In case we need to wait for an animation to finish before running the experiment
        public IEnumerator WaitForAnimation(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            resourceOn = false;
            runExperiment();
        }

        public void runExperiment()
        {
            ScienceData data = makeScience();
            scienceReportList.Add(data);
            dataIndex = scienceReportList.Count - 1;
            ReviewData();
            if (keepDeployedMode == 1) retractEvent();
        }

        //Create the science data
        public ScienceData makeScience()
        {
            ExperimentSituations vesselSituation = getSituation();
            string biome = getBiome(vesselSituation);
            CelestialBody mainBody = vessel.mainBody;
            bool asteroid = false;            
            
            //Check for asteroids and alter the biome and celestialbody values as necessary
            if (asteroidReports && AsteroidScience.asteroidGrappled() || asteroidReports && AsteroidScience.asteroidNear())
            {
                asteroid = true;
                mainBody = AsteroidScience.Asteroid();
                biome = mainBody.bodyDescription;
            }

            ScienceData data = null;
            ScienceExperiment exp = ResearchAndDevelopment.GetExperiment(experimentID);
            ScienceSubject sub = ResearchAndDevelopment.GetExperimentSubject(exp, vesselSituation, mainBody, biome);

            //Replace Eeloo's CelestialBody values with defaults if necessary
            if (asteroid)
            {
                mainBody.bodyDescription = bodyDescription;
                mainBody.bodyName = bodyName;
                mainBody.scienceValues.LandedDataValue = bodyLandedValue;
                mainBody.scienceValues.InSpaceLowDataValue = bodySpaceValue;
                asteroid = false;
            }

            data = new ScienceData(exp.baseValue * sub.dataScale, xmitDataScalar, xmitDataScalar / 2, experimentID, exp.experimentTitle + situationCleanup(vesselSituation, biome));
            data.subjectID = sub.id;
            sub.title = data.title;
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
            //Check for asteroids, return values that should sync with existing parts
            if (asteroidReports && AsteroidScience.asteroidGrappled()) return ExperimentSituations.SrfLanded;
            if (asteroidReports && AsteroidScience.asteroidNear()) return ExperimentSituations.InSpaceLow;
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
            //Add some asteroid specefic results
            if (asteroidReports && AsteroidScience.asteroidGrappled()) return " from the surface of a " + b + " asteroid";
            if (asteroidReports && AsteroidScience.asteroidNear()) return " while in space near a " + b + " asteroid";
            if (vessel.landedAt != "") return " from " + b;
            if (b == "")
            {
                switch (expSit)
                {
                    case ExperimentSituations.SrfLanded:
                        return " from  " + vessel.mainBody.theName + "'s surface";
                    case ExperimentSituations.SrfSplashed:
                        return " from " + vessel.mainBody.theName + "'s oceans";
                    case ExperimentSituations.FlyingLow:
                        return " while flying at " + vessel.mainBody.theName;
                    case ExperimentSituations.FlyingHigh:
                        return " from " + vessel.mainBody.theName + "'s upper atmosphere";
                    case ExperimentSituations.InSpaceLow:
                        return " while in space near " + vessel.mainBody.theName;
                    default:
                        return " while in space high over " + vessel.mainBody.theName;
                }
            }
            else
            {
                switch (expSit)
                {
                    case ExperimentSituations.SrfLanded:
                        return " from " + vessel.mainBody.theName + "'s " + b;
                    case ExperimentSituations.SrfSplashed:
                        return " from " + vessel.mainBody.theName + "'s " + b;
                    case ExperimentSituations.FlyingLow:
                        return " while flying over " + vessel.mainBody.theName + "'s " + b;
                    case ExperimentSituations.FlyingHigh:
                        return " from the upper atmosphere over " + vessel.mainBody.theName + "'s " + b;
                    case ExperimentSituations.InSpaceLow:
                        return " from space just above " + vessel.mainBody.theName + "'s " + b;
                    default:
                        return " while in space high over " + vessel.mainBody.theName + "'s " + b;
                }
            }
        }

        //Custom experiment results dialog page, allows full control over the buttons on that page
        public void newResultPage()
        {
            if (scienceReportList.Count > 0)
            {
                ScienceData data = scienceReportList[dataIndex];
                ExperimentResultDialogPage page = new ExperimentResultDialogPage(part, data, data.transmitValue, xmitDataScalar / 2, !rerunnable, transmitWarningText, true, data.labBoost < 1 && checkLabOps() && xmitDataScalar < 1, new Callback<ScienceData>(onDiscardData), new Callback<ScienceData>(onKeepData), new Callback<ScienceData>(onTransmitData), new Callback<ScienceData>(onSendToLab));
                ExperimentsResultDialog.DisplayResult(page);
            }
            eventsCheck();
        }

        new public void ReviewData()
        {
            dataIndex = 0;
            foreach (ScienceData data in scienceReportList)
            {
                newResultPage();
                dataIndex++;
            }
        }

        new public void ReviewDataEvent()
        {
            ReviewData();
        }

        #endregion   

        #region IScienceDataContainer methods
        
        //Implement these interface methods to make the science lab and transmitters function properly.
        ScienceData[] IScienceDataContainer.GetData()
        {
            return scienceReportList.ToArray();
        }

        int IScienceDataContainer.GetScienceCount()
        {
            return scienceReportList.Count;
        }

        bool IScienceDataContainer.IsRerunnable()
        {
            return base.IsRerunnable();
        }

        void IScienceDataContainer.ReviewData()
        {
            ReviewData();
        }

        void IScienceDataContainer.ReviewDataItem(ScienceData data)
        {
            ReviewData();
        }

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

        //This is called after data is transmitted by right-clicking on the transmitter itself, removes all reports.
        void IScienceDataContainer.DumpData(ScienceData data)
        {
            if (scienceReportList.Count > 0)
            {
                base.DumpData(data);
                if (keepDeployedMode == 0) retractEvent();
                scienceReportList.Clear();
                //print("Dump Data");
            }
            eventsCheck();
        }

        //This one is called after external data collection, removes all science reports.
        public void DumpAllData(List<ScienceData> dataList)
        {
            if (scienceReportList.Count > 0)
            {
                foreach (ScienceData data in dataList)
                {
                    base.DumpData(data);
                }
                scienceReportList.Clear();
                if (keepDeployedMode == 0) retractEvent();
                //print("Dump All Data");
            }
            eventsCheck();
        }

        //This one is called from the results page, removes only one report.
        new public void DumpData(ScienceData data)
        {
            if (scienceReportList.Count > 0)
            {
                base.DumpData(data);
                if (keepDeployedMode == 0) retractEvent();
                scienceReportList.Remove(data);
                //print("Dump Data Local");
            }
            eventsCheck();
        }

        #endregion

        #region Experiment Results Control

        private void onDiscardData(ScienceData data)
        {
            if (scienceReportList.Count > 0)
            {
                scienceReportList.Remove(data);
                if (keepDeployedMode == 0) retractEvent();
            }
            eventsCheck();
            //print("Discard data from page");
        }

        private void onKeepData(ScienceData data)
        {
            //print("Store date from page");
        }
        
        private void onTransmitData(ScienceData data)
        {
            List<IScienceDataTransmitter> tranList = vessel.FindPartModulesImplementing<IScienceDataTransmitter>();
            if (tranList.Count > 0 && scienceReportList.Count > 0)
            {
                tranList.OrderBy(ScienceUtil.GetTransmitterScore).First().TransmitData(new List<ScienceData> {data});
                DumpData(data);
                //print("Transmit data from page");
            }
            else ScreenMessages.PostScreenMessage("No transmitters available on this vessel.", 4f, ScreenMessageStyle.UPPER_LEFT);
        }

        private void onSendToLab(ScienceData data)
        {
            List<ModuleScienceLab> labList = vessel.FindPartModulesImplementing<ModuleScienceLab>();
            if (checkLabOps() && scienceReportList.Count > 0) labList.OrderBy(ScienceUtil.GetLabScore).First().StartCoroutine(labList.First().ProcessData(data, new Callback<ScienceData>(onComplete)));
            else ScreenMessages.PostScreenMessage("No operational lab modules on this vessel. Cannot analyze data.", 4f, ScreenMessageStyle.UPPER_CENTER);
            //print("Send data to lab");
        }

        private void onComplete(ScienceData data)
        {
            ReviewData();
            //print("Data processed in lab");
        }

        //Maybe unnecessary, can be folded into a simpler method???
        public bool checkLabOps()
        {
            List<ModuleScienceLab> labList = vessel.FindPartModulesImplementing<ModuleScienceLab>();
            for (int i = 0; i < labList.Count; i++)
            {
                if (labList[i].IsOperational()) return true;
            }
            return false;
        }

        #endregion

    }
}
