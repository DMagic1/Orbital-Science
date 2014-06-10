/* DMagic Orbital Science - Module Science Animate
 * Generic module for animated science experiments.
 *
 * Copyright (c) 2014, David Grandy <david.grandy@gmail.com>
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

using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;

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
        public bool USScience = false;
        [KSPField]
        public bool USStock = false;
        [KSPField]
        public string bayAnimation;
        [KSPField]
        public string looperAnimation;
        [KSPField]
        public bool primary = true;

        protected Animation anim;
        protected Animation anim2;
        protected Animation anim3;
        protected ScienceExperiment scienceExp;
        private bool resourceOn = false;
        private int dataIndex = 0;
        private List<DMEnviroSensor> enviroList = new List<DMEnviroSensor>();
        private List<DMModuleScienceAnimate> primaryList = new List<DMModuleScienceAnimate>();
        private DMModuleScienceAnimate primaryModule = null;
        private CelestialBody mainBody = null;

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
            if (!string.IsNullOrEmpty(looperAnimation))
                anim2 = part.FindModelAnimators(looperAnimation)[0];
            if (!string.IsNullOrEmpty(bayAnimation))
                anim3 = part.FindModelAnimators(bayAnimation)[0];
            if (state == StartState.Editor) editorSetup();
            else
            {
                setup();
                if (FlightGlobals.fetch.bodies[16].bodyName != "Eeloo") //Just to make sure nothing gets permanently screwed up
                {
                    mainBody = FlightGlobals.Bodies[16];
                    mainBody.bodyDescription = bodyDescription;
                    mainBody.bodyName = bodyName;
                    mainBody.scienceValues.LandedDataValue = bodyLandedValue;
                    mainBody.scienceValues.InSpaceLowDataValue = bodySpaceValue;
                }
                if (IsDeployed)
                {
                    primaryAnimator(1f, 1f, WrapMode.Default, animationName, anim);
                    if (anim3 != null) primaryAnimator(1f, 1f, WrapMode.Default, bayAnimation, anim3);
                    if (anim2 != null) primaryAnimator(2.5f * animSpeed, 0f, WrapMode.Loop, looperAnimation, anim2);
                }
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
            eventsCheck();
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

        private void setup()
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
                if (primaryList.Count > 0)
                {
                    foreach (DMModuleScienceAnimate DMS in primaryList)
                    {
                        if (DMS.primary) primaryModule = DMS;
                    }
                }
            }
            if (USStock) enviroList = this.part.FindModulesImplementing<DMEnviroSensor>();
            if (waitForAnimationTime == -1) waitForAnimationTime = anim[animationName].length / animSpeed;
            if (experimentID != null) scienceExp = ResearchAndDevelopment.GetExperiment(experimentID);
        }

        private void editorSetup()
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

        public void primaryAnimator(float speed, float time, WrapMode wrap, string name, Animation a)
        {
            if (a != null)
            {
                a[name].speed = speed;
                if (!a.IsPlaying(name))
                {
                    a[name].wrapMode = wrap;
                    a[name].normalizedTime = time;
                    a.Blend(name, 1f);
                }
            }
        }

        [KSPEvent(guiActive = true, guiName = "Deploy", active = true)]
        public void deployEvent()
        {
            primaryAnimator(animSpeed * 1f, 0f, WrapMode.Default, animationName, anim);
            IsDeployed = !oneWayAnimation;
            if (USScience)
            {
                if (anim3 != null)
                {
                    primaryAnimator(animSpeed * 1f, 0f, WrapMode.Default, bayAnimation, anim3);
                }
                if (anim2 != null)
                {
                    primaryAnimator(animSpeed * 2.5f, 0f, WrapMode.Loop, looperAnimation, anim2);
                }
            }
            if (USStock)
            {
                if (enviroList.Count > 0)
                {
                    foreach (DMEnviroSensor DMES in enviroList)
                    {
                        if (!DMES.sensorActive)
                        {
                            if (DMES.primary) DMES.toggleSensor();
                        }
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
            primaryAnimator(-1f * animSpeed, 1f, WrapMode.Default, animationName, anim);
            IsDeployed = false;
            if (USScience)
            {
                if (anim3 != null)
                {
                    if (anim[animationName].length > anim3[bayAnimation].length)
                        primaryAnimator(-1f * animSpeed, (anim[animationName].length / anim3[bayAnimation].length), WrapMode.Default, bayAnimation, anim3);
                    else 
                        primaryAnimator(-1f * animSpeed, 1f, WrapMode.Default, bayAnimation, anim3);
                }
                if (anim2 != null)
                {
                    anim2[looperAnimation].normalizedTime = anim2[looperAnimation].normalizedTime % 1;
                    anim2[looperAnimation].wrapMode = WrapMode.Clamp;
                }
            }
            if (USStock)
            {
                if (enviroList.Count > 0)
                {
                    foreach (DMEnviroSensor DMES in enviroList)
                    {
                        if (DMES.sensorActive)
                        {
                            if (DMES.primary) DMES.toggleSensor();
                        }
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
                //print("[DM] Reseting Experiment");
            }
            eventsCheck();
        }

        new public void ResetAction(KSPActionParam param)
        {
            ResetExperiment();
        }

        new public void CollectDataExternalEvent()
        {   
            List<ModuleScienceContainer> EVACont = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceContainer>();
            if (scienceReportList.Count > 0)
            {
                if (EVACont.First().StoreData(new List<IScienceDataContainer> { this }, false)) DumpAllData(scienceReportList);
            }
        }
        
        new public void ResetExperimentExternal()
        {
            ResetExperiment();
        }

        private void eventsCheck()
        {
            //print("[DM] Checking Events");
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
        private IEnumerator WaitForAnimation(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            resourceOn = false;
            runExperiment();
        }

        private void runExperiment()
        {
            ScienceData data = makeScience();
            scienceReportList.Add(data);
            dataIndex = scienceReportList.Count - 1;
            ReviewData();
            if (keepDeployedMode == 1) retractEvent();
        }

        //Create the science data
        private ScienceData makeScience()
        {
            ExperimentSituations vesselSituation = getSituation();
            string biome = getBiome(vesselSituation);
            mainBody = vessel.mainBody;
            bool asteroid = false;            
            
            //Check for asteroids and alter the biome and celestialbody values as necessary
            if (asteroidReports && DMAsteroidScience.asteroidGrappled() || asteroidReports && DMAsteroidScience.asteroidNear())
            {
                asteroid = true;
                mainBody = DMAsteroidScience.Asteroid();
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
        
        private string getBiome(ExperimentSituations s)
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

        private bool canConduct()
        {
            return scienceExp.IsAvailableWhile(getSituation(), vessel.mainBody);
        }

        //Get our experimental situation based on the vessel's current flight situation, fix stock bugs with aerobraking and reentry.
        private ExperimentSituations getSituation()
        {
            //Check for asteroids, return values that should sync with existing parts
            if (asteroidReports && DMAsteroidScience.asteroidGrappled()) return ExperimentSituations.SrfLanded;
            if (asteroidReports && DMAsteroidScience.asteroidNear()) return ExperimentSituations.InSpaceLow;
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
        private string situationCleanup(ExperimentSituations expSit, string b)
        {
            //Add some asteroid specefic results
            if (asteroidReports && DMAsteroidScience.asteroidGrappled()) return " from the surface of a " + b + " asteroid";
            if (asteroidReports && DMAsteroidScience.asteroidNear()) return " while in space near a " + b + " asteroid";
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
        private void newResultPage()
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
                //print("[DM] Dump Data");
            }
            eventsCheck();
        }

        //This one is called after external data collection, removes all science reports.
        internal void DumpAllData(List<ScienceData> dataList)
        {
            if (scienceReportList.Count > 0)
            {
                foreach (ScienceData data in dataList)
                {
                    base.DumpData(data);
                }
                scienceReportList.Clear();
                if (keepDeployedMode == 0) retractEvent();
                //print("[DM] Dump All Data");
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
                //print("[DM] Dump Data Local");
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
        private bool checkLabOps()
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
