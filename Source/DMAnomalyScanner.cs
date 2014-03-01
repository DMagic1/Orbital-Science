/* DMagic Orbital Science - Anomaly Scanner
 * Anomaly detection and science data setup.
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

namespace DMagic
{
    class DMAnomalyScanner : PartModule , IScienceDataContainer
    {

        [KSPField]
        public string animationName = null;
        [KSPField]
        public string dishAnimate = null;
        [KSPField]
        public string camAnimate = null;
        [KSPField]
        public string foundAnimate = null;

        [KSPField]
        public string experimentID = null;
        [KSPField]
        public float xmitDataValue = 0f;

        [KSPField(isPersistant = true)]
        public bool IsDeployed = false;
        [KSPField(isPersistant = true)]
        public bool IsEnabled = false; 
         
        public string closestAnom = null;
        public bool anomCloseRange = false;
        public bool anomInRange = false;               
        public bool camDeployed = false;
        public bool closeRange = false;

        protected Animation anim;
        protected Animation animSecondary;        
        protected CelestialBody CBody = null;
        protected Transform cam = null;

        public int labint = 0;
        protected List<ModuleScienceLab> labList = new List<ModuleScienceLab>();        

        List<PQSCity> anomList = new List<PQSCity>(); //Master PQSCity list for all anomalies on current planet.
        
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            anim = part.FindModelAnimators(animationName)[0];
            animSecondary = part.FindModelAnimators(dishAnimate)[0];
            animSecondary = part.FindModelAnimators(camAnimate)[0];
            animSecondary = part.FindModelAnimators(foundAnimate)[0];
            if (state == StartState.Editor)
            {
                Events["deployDishEditor"].active = true;
                Events["retractDishEditor"].active = false;
            }
            else
            {
                cam = this.part.transform.FindChild("model").FindChild("anom scanner v4").FindChild("base").FindChild("camBaseArm0").FindChild("camBaseArm1").FindChild("camBaseArm2").FindChild("camBase");
                anomList.Clear();
                GameEvents.onVesselSOIChanged.Add(ScanOnSOIChange);
                pqsBuild();
                if (IsDeployed)
                {
                    deployDish(1f, 0f);
                }
            }
        }

        public void OnDestroy()
        {
            GameEvents.onVesselSOIChanged.Remove(ScanOnSOIChange);
        }
        
        public void ScanOnSOIChange(GameEvents.HostedFromToAction<Vessel, CelestialBody> VB)
        {
            anomList.Clear(); //Reset PQSCity list when SOI changes
            pqsBuild();
        }

        public override void OnUpdate()
        {
            if (!HighLogic.LoadedSceneIsEditor)
            {
                if (IsDeployed)                 //Don't waste OnUpdate unless the part is deployed.
                {
                    inRange();
                }
            }
        }



        # region Animators

        public void deployAnimation(float animSpeed, float animTime, float waitTime)
        {
            if (anim != null)
            {
                anim[animationName].speed = animSpeed;
                if (!anim.IsPlaying(animationName))
                {
                    anim[animationName].normalizedTime = animTime;
                    anim.Blend(animationName, 1f);
                }
                else
                {
                    StopCoroutine("WaitForAnim");
                }
                if (!IsEnabled)
                {
                    StartCoroutine("WaitForAnim", (waitTime - (anim[animationName].normalizedTime * anim[animationName].length)));
                }
            }
        }

        IEnumerator WaitForAnim(float coWaitTime)
        {
            yield return new WaitForSeconds(coWaitTime);
            animSecondary[dishAnimate].enabled = false;
            secondaryAnimator(dishAnimate, 1f, 0f, WrapMode.Loop);
            IsDeployed = true;
        }

        public void secondaryAnimator(string animName, float dishSpeed, float dishTime, WrapMode wrap)
        {
            if (animSecondary != null)
            {
                animSecondary[animName].speed = dishSpeed;
                animSecondary[animName].normalizedTime = dishTime;
                animSecondary[animName].wrapMode = wrap;
                anim.Blend(animName, 1f);
            }
        }
        
        public void deployDish(float deployTime, float waitTime)
        {
            deployAnimation(1f, deployTime, waitTime);
            IsEnabled = true;
        }

        public void retractDish()
        {
            if (IsDeployed)
            {                
                animSecondary[dishAnimate].normalizedTime = animSecondary[dishAnimate].normalizedTime % 1;
                animSecondary[dishAnimate].wrapMode = WrapMode.Default;
                if (camDeployed)
                {
                    animSecondary[foundAnimate].wrapMode = WrapMode.Default;
                    cam.localRotation = Quaternion.Slerp(cam.localRotation, new Quaternion(0, 0, 0, 1), 1f);
                    secondaryAnimator(camAnimate, -1f, 1f, WrapMode.Default);
                    camDeployed = false;
                }
            }
            deployAnimation(-1f, 1f, 0f);
            IsEnabled = false;
            IsDeployed = false;
        }

        [KSPEvent(guiActive = true, guiName = "Toggle Scanner", active = true)]
        public void toggleDish()
        {
            if (!IsEnabled)
            {
                deployDish(0f, anim[animationName].length);
            }
            else
            {
                retractDish();
            }
        }

        [KSPAction("Toggle Scanner")]
        public void toggleDishAction(KSPActionParam param)
        {
            toggleDish();
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Deploy Dish", active = true)]
        public void deployDishEditor()
        {
            deployDish(1f, 0f);
            secondaryAnimator(camAnimate, 1f, 1f, WrapMode.Default);
            secondaryAnimator(foundAnimate, 1f, 0f, WrapMode.PingPong);
            Events["deployDishEditor"].active = false;
            Events["retractDishEditor"].active = true;
        }

        [KSPEvent(guiActiveEditor = true, guiName = "Retract Dish", active = false)]
        public void retractDishEditor()
        {

            deployAnimation(-1f, 0f, 0f);
            secondaryAnimator(dishAnimate, 0f, 0f, WrapMode.Default);
            secondaryAnimator(camAnimate, -1f, 0f, WrapMode.Default);
            animSecondary[foundAnimate].wrapMode = WrapMode.Default;
            Events["deployDishEditor"].active = true;
            Events["retractDishEditor"].active = false;
        }

        //Rotate camera on its y-axis to look at the anomaly.
        public void camRotate(Vector3 anom)
        {
            Vector3 localAnom = transform.InverseTransformPoint(anom);
            Vector3 toTarget = localAnom - part.transform.position;
            toTarget.y = 0;
            Quaternion lookToTarget = Quaternion.LookRotation(localAnom);
            lookToTarget.x = 0;
            lookToTarget.z = 0;
            cam.localRotation = Quaternion.Slerp(cam.localRotation, lookToTarget, Time.deltaTime * 2f);
        }

        //Control camera and light states based on distance to anomaly.
        public void inRange()
        {
            bool anomInRange = false;
            foreach (PQSCity anom in anomList)
            {
                Vector3 vV = vessel.transform.position;
                Vector3 aV = anom.transform.position;
                double vdiff = (aV - vV).magnitude;
                double valt = vessel.mainBody.GetAltitude(vV);
                double anomAlt = vessel.mainBody.GetAltitude(aV);
                double vheight = anomAlt - valt;
                double hDist = Math.Sqrt((vdiff * vdiff) - (vheight * vheight));

                if (hDist < (11000 * (1 - (Math.Abs(vheight)) / 6000)))
                {
                    anomInRange = true;
                    if (hDist < (10000 * (1 - (Math.Abs(vheight)) / 5000)))
                    {
                        if (!camDeployed)
                        {
                            secondaryAnimator(camAnimate, 1f, 0f, WrapMode.Default);
                            camDeployed = true;
                            if (vdiff < 250)
                            {
                                secondaryAnimator(foundAnimate, 1f, 0f, WrapMode.PingPong);
                                closeRange = true;
                            }
                            else
                            {
                                closeRange = false;
                            }
                        }
                        if (camDeployed)
                        {
                            camRotate(aV);
                            if (vdiff < 250 && closeRange == false)
                            {
                                secondaryAnimator(foundAnimate, 1f, 0f, WrapMode.PingPong);
                                closeRange = true;
                                print("In range");
                            }
                            if (vdiff >= 275 && closeRange == true)
                            {
                                animSecondary[foundAnimate].wrapMode = WrapMode.Default;
                                closeRange = false;
                                print("Moved out of range");
                            }
                        }
                    }
                }
            }
            if (!anomInRange && camDeployed)
            {
                animSecondary[foundAnimate].wrapMode = WrapMode.Default;
                cam.localRotation = Quaternion.Slerp(cam.localRotation, new Quaternion(0, 0, 0, 1), 1f);
                secondaryAnimator(camAnimate, -1f, 1f, WrapMode.Default);
                camDeployed = false;
                print("No more anom in range");
            }
        }

        # endregion



        #region Anomaly detection and setup
        
        public void pqsBuild() //Build PQSCity list for all anomalies on the current planet.
        {
            PQSCity[] Cities = FindObjectsOfType(typeof(PQSCity)) as PQSCity[];
            foreach (PQSCity anomalyObject in Cities)
            {
                if (anomalyObject.transform.parent.name == vessel.mainBody.name)
                {
                    anomList.Add(anomalyObject);
                }
            }
        }

        public void getAnomValues()
        {
            closestAnom = null;             //Reset all of the public values.
            anomCloseRange = false;
            anomInRange = false;
            if (anomList.Count == 0)
            {
                pqsBuild();                 //rebuild PQSCity list if it hasn't already been.
            }
            foreach (PQSCity anom in anomList)                      //Check for any anomalies within scanning range of the vessel.
            {
                Vector3 vV3 = vessel.transform.position;
                Vector3 aV3 = anom.transform.position;
                double vdiffd = (aV3 - vV3).magnitude;
                double valt = vessel.mainBody.GetAltitude(vV3);
                double anomAlt = vessel.mainBody.GetAltitude(aV3);
                double vheight = anomAlt - valt;
                double hDist = Math.Sqrt((vdiffd * vdiffd)-(vheight * vheight));      //Use right triangle to calculate the horizontal distance to the anomaly.

                if (hDist < (30000 * (1 - (Math.Abs(vheight))/15000)))                //Determine cutoff distance on sliding scale based on altitude above the anomaly.
                {
                    double aBearing = bearing(aV3);                                   //Calculate the bearing to the anomaly from the current vessel position.
                    string anomDirection = direction(aBearing);                       //Get cardinal directions based on the bearing.
                    anomInRange = true;
                    print("Anomaly: " + anom.name + " is at bearing: " + Math.Round(aBearing, 2).ToString() + " deg at a distance of " + Math.Round(vdiffd, 1).ToString() + "m.");
                    if (vdiffd < 250)           //Scanning range distance for science experiment.
                    {
                        closestAnom = anom.name;
                        anomCloseRange = true;
                    }
                    else if (Math.Abs(vheight) > 10000)             //Use alternate message when more than 10km above the anomaly.
                    {
                        
                        ScreenMessages.PostScreenMessage("Anomalous signal detected approximately " + Math.Round(vdiffd / 1000 + RandomDouble((2 * (vdiffd / 1000) / 30), (4 * (vdiffd / 1000) / 30)), 1).ToString() + "km below current position, get closer for a better signal", 4f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage("Anomalous signal detected approximately " + Math.Round(vdiffd / 1000 + RandomDouble((2 * (vdiffd / 1000) / 30), (4 * (vdiffd / 1000) / 30)), 1).ToString() + "km away to the " + anomDirection + ", get closer for a better signal.", 4f, ScreenMessageStyle.UPPER_CENTER);
                    }
                }
            }
        }          
        
        //Random number to fudge the distance estimate in the screen messages above; extent of fudging based on total distance to the anomaly, +- 2km at 30km away.
        double RandomDouble(double min, double max) 
        {
            System.Random randomd = new System.Random();
            double random = randomd.NextDouble();
            return (random * max) - min;
        }

        //Calculate great circle bearing using the power of the internet.
        public double bearing(Vector3 anomPos) 
        {
            double alat = vessel.mainBody.GetLatitude(anomPos);
            double alon = vessel.mainBody.GetLongitude(anomPos);
            double vlat = vessel.latitude;
            double vlon = vessel.longitude;
            double longdiff = (alon - vlon) * Mathf.Deg2Rad;

            double y = Math.Sin(longdiff) * Math.Cos(Mathf.Deg2Rad * alat);
            double x = Math.Cos(Mathf.Deg2Rad * vlat) * Math.Sin(Mathf.Deg2Rad * alat) - Math.Sin(Mathf.Deg2Rad * vlat) * Math.Cos(Mathf.Deg2Rad * alat) * Math.Cos(longdiff);
            double aBearing = (Math.Atan2(y, x) * Mathf.Rad2Deg + 360) % 360;
            return aBearing;
        }

        public string direction(double bearing) 
        {
            if (bearing >= 0 && bearing < 22.5)
            {
                return "North";
            }
            else if (bearing >= 22.5 && bearing < 67.5)
            {
                return "NorthEast";
            }
            else if (bearing >= 67.5 && bearing < 112.5)
            {
                return "East";
            }
            else if (bearing >= 112.5 && bearing < 157.5)
            {
                return "SouthEast";
            }
            else if (bearing >= 157.5 && bearing < 202.5)
            {
                return "South";
            }
            else if (bearing >= 202.5 && bearing < 247.5)
            {
                return "SouthWest";
            }
            else if (bearing >= 247.5 && bearing < 292.5)
            {
                return "West";
            }
            else if (bearing >= 292.5 && bearing < 337.5)
            {
                return "NorthWest";
            }
            else if (bearing >= 337.5 && bearing < 360)
            {
                return "North";
            }
            else
            {
                return "???";
            }
        }

        # endregion



        #region Science data and experiment setup

        public ExperimentSituations currentExpSit()
        {
            if (vessel.situation == Vessel.Situations.LANDED || vessel.situation == Vessel.Situations.PRELAUNCH || vessel.situation == Vessel.Situations.SPLASHED)
            {
                return ExperimentSituations.SrfLanded;
            }
            else if (vessel.situation == Vessel.Situations.FLYING || vessel.situation == Vessel.Situations.SUB_ORBITAL || vessel.situation == Vessel.Situations.ORBITING || vessel.situation == Vessel.Situations.ESCAPING)
            {
                return ExperimentSituations.FlyingLow; //Set all other options to flying low.
            }
            else
            {
                return ExperimentSituations.InSpaceHigh; //Hopefully never gets this far.
            }
        }

        //Spoiler protection.
        public string anomalyBiomeName(string anomName)
        {
            switch(anomName) 
            {
                case "KSC":
                    return "01";
                case "IslandAirfield":
                    return "02";
                case "KSC2":
                    return "03";
                case "Monolith00":
                    return "04";
                case "Monolith01":
                    return "05";
                case "UFO":
                    return "06";
                case "Cave":
                    return "07";
                case "Face":
                    return "08";
                case "MSL":
                    return "09";
                case "Pyramid":
                    return "10";
                case "Monolith02":
                    return "11";
                case "Pyramids":
                    return "12";
                case "RockArch01":
                    return "13";
                case "ArmstrongMemorial":
                    return "14";
                case "Icehenge":
                    return "15";
                case "RockArch00":
                    return "16";
                case "DeadKraken":
                    return "17";
                case "RockArch02":
                    return "18";
                default :
                    return "???";
            }
        }  
      
        //Clean up anomaly names for experiment results page.
        public string biomeResultName(string anomName) 
        {
            switch (anomName)
            {
                case "KSC":
                    return anomName;
                case "IslandAirfield":
                    return "Island Airfield";
                case "KSC2":
                    return "KSC 2";
                case "Monolith00":
                    return "Monolith";
                case "Monolith01":
                    return "Monolith";
                case "UFO":
                    return anomName;
                case "Cave":
                    return anomName;
                case "Face":
                    return anomName;
                case "MSL":
                    return "Mast Camera";
                case "Pyramid":
                    return anomName;
                case "Monolith02":
                    return "Monolith";
                case "Pyramids":
                    return anomName;
                case "RockArch01":
                    return "Rock Arch";
                case "ArmstrongMemorial":
                    return "Armstrong Memorial";
                case "Icehenge":
                    return anomName;
                case "RockArch00":
                    return "Rock Arch";
                case "DeadKraken":
                    return "Dead Kraken";
                case "RockArch02":
                    return "Rock Arch";
                default:
                    return "???";
            }
        }

        [KSPEvent(guiActive = true, guiName = "Collect Data", active = true)]
        public void collectScience()
        {
            getAnomValues();
            if (anomInRange)                //Give location and direction info only for anomalies within 30km.
            {
                if (anomCloseRange)         //Only collect science for close range anomalies.
                {
                    startExperiment();
                }
            }
            else                        //Put this message here to simplify the getAnomValues() method.
            {
                ScreenMessages.PostScreenMessage("No anomalous signals detected.", 4f, ScreenMessageStyle.UPPER_CENTER);
            }
        }
        
        [KSPAction("Collect Data")]
        public void collectScienceAction(KSPActionParam param)
        {
            collectScience();
        }

        List<ScienceData> anomalyData = new List<ScienceData>();

        public ScienceData makeNewScience()
        {
            ScienceData data = null;
            ScienceExperiment anomExp = ResearchAndDevelopment.GetExperiment(experimentID);
            ScienceSubject anomSub = ResearchAndDevelopment.GetExperimentSubject(anomExp, currentExpSit(), vessel.mainBody, anomalyBiomeName(closestAnom)); //Use anomaly index number in place of biome name.
            data = new ScienceData(anomExp.baseValue * anomSub.dataScale, xmitDataValue, 0.25f, experimentID, "Scan of the " + biomeResultName(closestAnom) + " on " + vessel.mainBody.theName + ".");
            data.subjectID = anomSub.id;
            return data;
        }

        public void startExperiment()
        {
            ScienceData data = makeNewScience();
            anomalyData.Add(data);
            ReviewData();
        }

        #endregion



        # region IScienceDataContainer Stuff

        public bool checkLabOps()       //Make sure any science labs present are operational.
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

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            foreach (ScienceData storedData in anomalyData)
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
                    anomalyData.Add(data);
                }
            }
        }

        public ScienceData[] GetData()     
        {
            return anomalyData.ToArray();
        }

        public int GetScienceCount()
        {
            return anomalyData.Count;
        }

        public bool IsRerunnable()
        {
            return true;
        }

        public void DumpData(ScienceData data)
        {
            anomalyData.Remove(data);
        }

        private void onPageDiscard(ScienceData data)
        {
            DumpData(data);
        }

        private void onKeepData(ScienceData data)
        {
        }

        private void onTransmitData(ScienceData data)   //Surely there's a better way of doing this...
        {
            bool tranBusy = false;
            List<IScienceDataTransmitter> tranList = vessel.FindPartModulesImplementing<IScienceDataTransmitter>();
            if (tranList.Count > 0 && anomalyData.Contains(data))
            {
                foreach (IScienceDataTransmitter tran in tranList)
                {
                    if (tran.CanTransmit())
                    {
                        if (!tran.IsBusy())         //Check for non-busy transmitters to use.
                        {
                            List<ScienceData> tranData = new List<ScienceData>();
                            tranData.Add(data);
                            tran.TransmitData(tranData);
                            anomalyData.Remove(data);
                            print("Transmitting now.");
                            break;
                        }
                        else
                        {
                            tranBusy = true;
                        }
                    }
                }
                if (tranBusy)               //If all transmitters are busy add data to queue for first transmitter.
                {
                    List<ScienceData> tranData = new List<ScienceData>();
                    tranData.Add(data);
                    tranList.First().TransmitData(tranData);
                    anomalyData.Remove(data);
                    print("Transmitting now from queue.");
                }
            }            
        }
        
        private void onSendToLab(ScienceData data)
        {
            bool labOperating = checkLabOps();
            if (labOperating)           
            {
                labList[labint].StartCoroutine(labList[labint].ProcessData(data, new Callback<ScienceData>(onComplete)));
            }
            else
            {
                ScreenMessages.PostScreenMessage("No operational lab modules on this vessel. Cannot analyze data.", 3f, ScreenMessageStyle.UPPER_CENTER);
            }   
        }

        private void onComplete(ScienceData data)
        {
            ReviewData();
        }

        public void ReviewData()
        {
            checkLabOps();
            ScienceData storedExp = anomalyData[0];
            ExperimentResultDialogPage page = new ExperimentResultDialogPage(part, storedExp, storedExp.transmitValue, 0.25f, false, "", true, labList.Count > 0, new Callback<ScienceData>(onPageDiscard), new Callback<ScienceData>(onKeepData), new Callback<ScienceData>(onTransmitData), new Callback<ScienceData>(onSendToLab));
            ExperimentsResultDialog.DisplayResult(page);
        }

        public void ReviewDataItem(ScienceData data)
        {
            ReviewData();
        }

# endregion 


    }
}
