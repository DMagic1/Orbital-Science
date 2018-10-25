using System;
using System.Collections;
using UnityEngine;

namespace DMagic.Part_Modules
{
    public class DMUniversalStorageScience : DMModuleScienceAnimate
    {
        [KSPField]
        public string RaySourceTransform = String.Empty;

        private Transform _raySource;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (!string.IsNullOrEmpty(RaySourceTransform))
                _raySource = part.FindModelTransform(RaySourceTransform);
        }

        public override void retractEvent()
        {
            base.retractEvent();
        }

        public override void deployEvent()
        {
            IScalarModule scalar = null;
            float deployLimit = 0;

            RaycastHit hit;
            
            if (_raySource != null && Physics.Raycast(_raySource.position, _raySource.forward, out hit, 1f, LayerUtil.DefaultEquivalent))
            {
                if (hit.collider != null)
                {
                    bool primary = false;
                    bool secondary = false;

                    if (hit.collider.gameObject.name == "PrimaryDoorCollider")
                        primary = true;
                    else if (hit.collider.gameObject.name == "SecondaryDoorCollider")
                        secondary = true;

                    if (primary || secondary)
                    {
                        //DMUtils.Logging("Door detected");

                        Part p = Part.GetComponentUpwards<Part>(hit.collider.gameObject);

                        if (p != null)
                        {
                           // DMUtils.Logging("Part from GameObject: {0}", p.partInfo.title);

                            PartModule USAnimate = null;

                            for (int i = p.Modules.Count - 1; i >= 0; i--)
                            {
                                if (p.Modules[i].moduleName == "USAnimateGeneric")
                                {
                                    USAnimate = p.Modules[i];

                                    if (USAnimate is IScalarModule)
                                        scalar = USAnimate as IScalarModule;

                                    break;
                                }
                            }

                            if (USAnimate != null)
                            {
                               // DMUtils.Logging("US Animate Module Detected");

                                BaseEvent doorEvent = null;
                                BaseField doorLimit = null;

                                if (primary)
                                {
                                    doorEvent = USAnimate.Events["toggleEventPrimary"];

                                    doorLimit = USAnimate.Fields["primaryDeployLimit"];
                                }
                                else if (secondary)
                                {
                                    doorEvent = USAnimate.Events["toggleEventSecondary"];

                                    doorLimit = USAnimate.Fields["secondaryDeployLimit"];
                                }

                                if (doorLimit != null)
                                    deployLimit = doorLimit.GetValue<float>(USAnimate) * 0.01f;
                                else
                                    deployLimit = 1;

                                if (doorEvent != null)
                                {
                                    //DMUtils.Logging("Door Event Found");

                                    if (doorEvent.active && doorEvent.guiActive)
                                    {
                                        doorEvent.Invoke();

                                        //DMUtils.Logging("Door Invoked");

                                        StartCoroutine(WaitForBayDoors(scalar, deployLimit));
                                    }
                                    else
                                    {
                                        base.deployEvent();

                                        DeploySucceed();
                                    }
                                }
                                else
                                {
                                    base.deployEvent();

                                    DeploySucceed();
                                }
                            }
                            else
                            {
                                base.deployEvent();

                                DeploySucceed();
                            }
                        }
                        else
                        {
                            base.deployEvent();

                            DeploySucceed();
                        }
                    }
                    else
                    {
                        var ownColliders = part.GetComponentsInChildren<Collider>();

                        bool flag = false;

                        for (int i = ownColliders.Length - 1; i >= 0; i--)
                        {
                            if (hit.collider == ownColliders[i])
                            {
                                flag = true;

                                break;
                            }
                        }

                        if (flag)
                        {
                            base.deployEvent();

                            DeploySucceed();
                        }
                        else
                        {
                            ScreenMessages.PostScreenMessage(
                                string.Format(
                                "<b><color=orange>Obstruction detected preventing {0} from being deployed.</color></b>"
                                , part.partInfo.title)
                                , 5f, ScreenMessageStyle.UPPER_CENTER);
                        }
                    }
                }
            }
            else
            {
                base.deployEvent();

                DeploySucceed();
            }
        }

        private IEnumerator WaitForBayDoors(IScalarModule scalar, float limit)
        {
            if (scalar == null)
                yield break;

            while (scalar.GetScalar < limit)
                yield return null;

            base.deployEvent();

            DeploySucceed();
        }

        protected virtual void DeploySucceed()
        {

        }
    }
}
