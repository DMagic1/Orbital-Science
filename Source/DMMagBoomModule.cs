/* DMagic Orbital Science - Magnetometer
 * Magnetometer animation and science collection.
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
using System;
using System.Collections;
using System.Collections.Generic;

namespace DMagic
{
    //Inherit default science experiment module.
    public class DMMagBoomModule : ModuleScienceExperiment
    {

        [KSPField(isPersistant = false)]
        public string animationName;

        [KSPField(isPersistant = true)]
        bool IsEnabled = false;

        [KSPField(isPersistant = false)]
        bool IsExperimenting = false;
        [KSPField(guiActive = false, guiName = "Bt", isPersistant = true)]
        public string Bt;
        [KSPField(guiActive = false, guiName = "Inc")]
        public string inc;
        [KSPField(guiActive = false, guiName = "Dec")]
        public string dec;
        [KSPField(guiActive = false, guiName = "nDay")]
        public string nDays;
        [KSPField(guiActive = false, guiName = "Lat")]
        public string lats;
        [KSPField(guiActive = false, guiName = "Lon")]
        public string lons;
        [KSPField(guiActive = false, guiName = "Sun")]
        public string suns;

        protected Animation anim;
        private static double sDay = 21650.81276574;

        //Get first animation name from part. Force module activation and make deployed animation stick.
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
            Fields["Bt"].guiActive = IsEnabled;
            Fields["inc"].guiActive = IsEnabled;
            Fields["dec"].guiActive = IsEnabled;
            Fields["nDays"].guiActive = IsEnabled;
            Fields["lats"].guiActive = IsEnabled;
            Fields["lons"].guiActive = IsEnabled;
            Fields["suns"].guiActive = IsEnabled;
        }

        //Right click deploy animation. Animation is reversible while playing.
        [KSPEvent(guiActive = true, guiName = "Deploy Magnetometer", active = true)]
        public void DeployEvent()
        {
            if (anim[animationName] != null)
            {
                anim[animationName].speed = 1.5f;
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
                Events["DeployEvent"].active = false;
                Events["RetractEvent"].active = true;
                double pTime = Planetarium.GetUniversalTime();
                double uDay = (pTime % sDay) / sDay;
                Fields["Bt"].guiActive = IsEnabled;
                Fields["inc"].guiActive = IsEnabled;
                Fields["dec"].guiActive = IsEnabled;
                Fields["nDays"].guiActive = IsEnabled;
                Fields["lats"].guiActive = IsEnabled;
                Fields["lons"].guiActive = IsEnabled;
                Fields["suns"].guiActive = IsEnabled;
                print("Day: " + Math.Round(uDay, 2) + " Planetarium time: " + Math.Round(pTime, 2));
            }
        }

        //Right click retract animation, same as deploy animation.
        [KSPEvent(guiActive = true, guiName = "Retract Magnetometer", active = false)]
        public void RetractEvent()
        {
            if (anim[animationName] != null)
            {
                anim[animationName].speed = -1.5f;
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
                Fields["Bt"].guiActive = IsEnabled;
                Fields["inc"].guiActive = IsEnabled;
                Fields["dec"].guiActive = IsEnabled;
                Fields["nDays"].guiActive = IsEnabled;
                Fields["lats"].guiActive = IsEnabled;
                Fields["lons"].guiActive = IsEnabled;
                Fields["suns"].guiActive = IsEnabled;
                Events["DeployEvent"].active = true;
                Events["RetractEvent"].active = false;
            }
        }

        MagVar magValues = new MagVar();

        public double[] getMag(double lat, double lon, double alt, long date, int i, double[] field)
        {
            return magValues.SGMagVar(lat, lon, alt, date, i, field);
        }

        //[KSPEvent(guiActive = true, guiName = "Mag Values", active = true)]
        //public void mags()
        //{
            
        //    //print("Mag Int: " + Math.Round(magComp[0], 2).ToString() + " / Inlination: " + Math.Round(magComp[1], 2).ToString() + " / Declination: " + Math.Round(magComp[2], 2).ToString());
        //}

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (IsEnabled)
            {
                double lat = vessel.latitude * Mathf.Deg2Rad;
                double lon = vessel.longitude * Mathf.Deg2Rad;
                double latDeg = (vessel.latitude + 90 + 180) % 180 - 90;
                double lonDeg = (vessel.longitude + 180 + 360) % 360 - 180;
                double alt = vessel.altitude / 1000;
                double uTime = Planetarium.GetUniversalTime();
                double uDay = uTime / sDay;
                double nDay = uDay % 1;
                alt *= 1.5 + Math.Sin(((1 + 2*nDay) % 2) * Math.PI + lon);
                int localDay = Convert.ToInt32(uDay);
                long date = 2455197 + localDay;
                int i = 10;
                double[] field = new double[6];
                double[] magComp = getMag(lat, lon, alt, date, i, field);
                
                //Magnetic field components
                double Bx = magComp[3];
                double By = magComp[4];
                double Bz = magComp[5];
                double Bh = Math.Sqrt((Bx * Bx) + (By * By));
                double Bti = Math.Sqrt((Bh * Bh) + (Bz * Bz));
                double dip = Math.Atan2(Bz, Bh);
                double decD;
                if (Bx != 0.0 || By != 0.0) decD = Math.Atan2(By, Bx);
                else decD = 0.0;

                dip *= Mathf.Rad2Deg;
                decD *= Mathf.Rad2Deg;

                //Convert doubles to floats for better display
                float Btf = (float)Bti;
                float incf = (float)dip;
                float decf = (float)decD;
                float altf = (float)alt;

                Vector3 sunP = FlightGlobals.fetch.bodies[0].position;
                Vector3 sunD = transform.InverseTransformPoint(sunP) - part.transform.localPosition;

                //Display in right-click menu
                Bt = Btf.ToString("F2") + " nT / Alt: " + altf.ToString("F4");
                //Bt = uTime.ToString();
                inc = incf.ToString("F2") + " Deg";
                dec = decf.ToString("F2") + " Deg";
                float nDayf = (float)nDay;
                nDays = nDayf.ToString("F4");
                float latf = (float)latDeg;
                float lonf = (float)lonDeg;
                lats = latf.ToString("F3") + " Deg";
                lons = lonf.ToString("F3") + " Deg";

                //field[0] = B_r;
                //field[1] = B_theta;
                //field[2] = B_phi;
                //field[3] = X;
                //field[4] = Y;
                //field[5] = Z;
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

        //VAB action group toggle animation.
        [KSPAction("Toggle Magnetometer")]
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

        //Check craft position and situation.
        public bool VesselSituation()
        {
            if (vessel.situation == Vessel.Situations.FLYING)
            {
                ScreenMessages.PostScreenMessage("The magnetometer is not suitable for use during atmospheric flight, try again on the ground or in space.", 4f, ScreenMessageStyle.UPPER_CENTER);
                return false;
            } else 
            {
                return true;
            }
        }

       //Replace default science collection mechanism
        //If the part is stowed call the deploy animation and collect science when it completes
        //If the part is in deploy animation, wait until finished and collect science
        //If the part is in the retract animation, reverse the animation and wait until finished to collect science
        //Check that the vessel is in the proper situation for science collection
        //Only allow one data collection operation at a time
        
       IEnumerator WaitForDeploy(float animTime)
       {
        if (anim[animationName] != null)
        {
            yield return new WaitForSeconds(animTime);
            base.DeployExperiment();
            IsExperimenting = false;
        }
       }

        new public void DeployExperiment()
        {
            if (IsExperimenting) { return; }
            else if (IsEnabled)
                {
                    if (VesselSituation())
                    {
                        if (anim.IsPlaying(animationName))
                        {
                            ScreenMessages.PostScreenMessage("You can't expect good results while the boom is still extending!", 3f, ScreenMessageStyle.UPPER_CENTER);
                            StartCoroutine(WaitForDeploy((anim[animationName].length / 1.5f) - (anim[animationName].normalizedTime * (anim[animationName].length / 1.5f))));
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
                    ScreenMessages.PostScreenMessage("Close proximity to the craft scrambles the magnetometer's sensors, deploying the scanner now.", 4f, ScreenMessageStyle.UPPER_CENTER);
                    StartCoroutine(WaitForDeploy(anim[animationName].length / 1.5f));
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
                            ScreenMessages.PostScreenMessage("You can't expect good results while the boom is still extending!", 3f, ScreenMessageStyle.UPPER_CENTER);
                            StartCoroutine(WaitForDeploy((anim[animationName].length / 1.5f) - (anim[animationName].normalizedTime * (anim[animationName].length / 1.5f))));
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
                    ScreenMessages.PostScreenMessage("Close proximity to the craft scrambles the magnetometer's sensors, deploying the scanner now.", 4f, ScreenMessageStyle.UPPER_CENTER);
                    StartCoroutine(WaitForDeploy(anim[animationName].length / 1.5f));
                    IsExperimenting = true;
                }
        }
        
    }
}
