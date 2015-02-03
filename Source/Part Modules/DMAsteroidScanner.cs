#region license
/* DMagic Orbital Science - Asteroid Scanner
 * Science Module For Asteroid Scanning Experiment
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
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DMagic.Part_Modules
{
	class DMAsteroidScanner : PartModule, IScienceDataContainer
	{

		#region Fields

		[KSPField]
		public string animationName;
		[KSPField]
		public string experimentID;
		[KSPField]
		public string greenLight;
		[KSPField]
		public string yellowLight;
		[KSPField]
		public string redLight;
		[KSPField]
		public string experimentResource;
		[KSPField]
		public bool rerunnable;
		[KSPField]
		public bool dataIsCollectable;
		[KSPField]
		public float resourceCost = 1f;
		[KSPField]
		public float transmitValue = 1f;
		[KSPField(isPersistant=true)]
		public bool IsDeployed = false;
		[KSPField(isPersistant=true)]
		public bool Inoperable = false;
		[KSPField(isPersistant = true)]
		public bool Deployed = false;
		[KSPField(guiActive = false, guiName = "Status")]
		public string status;

		private const string asteroidBodyNameFixed = "Eeloo";
		private const string transformName = "DishTransform";
		private const string transformRotatorName = "DishBaseTransform";
		private const string potato = "PotatoRoid";
		private Animation Anim;
		private Animation IndicatorAnim1;
		private Animation IndicatorAnim2;
		private Animation IndicatorAnim3;
		private ScienceExperiment exp = null;
		private List<ScienceData> scienceReports = new List<ScienceData>();
		private bool validTarget = false;
		private bool targetInRange = false;
		private bool targetInSite = false;
		private bool rotating = false;
		private DMAsteroidScanner targetModule = null;
		private Transform t;
		private Transform tR;
		private float targetDistance = 0f;
		private float[] astWidth = new float[6] { 4, 8, 12, 16, 20, 30 };

		#endregion

		#region PartModule

		public override void OnStart(PartModule.StartState state)
		{
			if (!string.IsNullOrEmpty(animationName))
				Anim = part.FindModelAnimators(animationName)[0];
			if (!string.IsNullOrEmpty(greenLight))
				IndicatorAnim1 = part.FindModelAnimators(greenLight)[0];
			if (!string.IsNullOrEmpty(yellowLight))
				IndicatorAnim2 = part.FindModelAnimators(yellowLight)[0];
			if (!string.IsNullOrEmpty(redLight))
				IndicatorAnim3 = part.FindModelAnimators(redLight)[0];
			if (!string.IsNullOrEmpty(experimentID))
				exp = ResearchAndDevelopment.GetExperiment(experimentID);
			if (IsDeployed)
				animator(0f, 1f);
			if (FlightGlobals.Bodies[16].bodyName != "Eeloo")
				FlightGlobals.Bodies[16].bodyName = asteroidBodyNameFixed;
			t = part.FindModelTransform(transformName);
			tR = part.FindModelTransform(transformRotatorName);
		}

		public override void OnSave(ConfigNode node)
		{
			node.RemoveNodes("ScienceData");
			foreach (ScienceData storedData in scienceReports)
			{
				ConfigNode storedDataNode = node.AddNode("ScienceData");
				storedData.Save(storedDataNode);
			}
		}

		public override void OnLoad(ConfigNode node)
		{
			if (node.HasNode("ScienceData"))
			{
				foreach (ConfigNode storedDataNode in node.GetNodes("ScienceData"))
				{
					ScienceData data = new ScienceData(storedDataNode);
					scienceReports.Add(data);
				}
			}
		}

		private void Update()
		{
			if (HighLogic.LoadedSceneIsFlight)
			{
				EventsCheck();
				if (vessel == FlightGlobals.ActiveVessel)
				{

					if (IsDeployed)
					{
						Vessel target = vessel.targetObject as Vessel;
						string s = "Searching...";

						if (target != null)
						{
							if (target != vessel)
							{
								targetModule = target.FindPartModulesImplementing<DMAsteroidScanner>().FirstOrDefault();
								if (targetModule != null)
								{
									s = "Receiver Located";
									validTarget = true;
									targetDistance = (targetModule.part.transform.position - t.position).magnitude;
									if (targetDistance < 5000)
									{
										s = "Receiver In Range";
										targetInRange = true;
										if (simpleRayHit())
										{
											s = "Experiment Ready";
											targetInSite = true;
											lightAnimationController(IndicatorAnim1, IndicatorAnim2, IndicatorAnim3, greenLight, yellowLight, redLight);
										}
										else
										{
											targetInSite = false;
											lightAnimationController(IndicatorAnim2, IndicatorAnim1, IndicatorAnim3, yellowLight, greenLight, redLight);
										}
									}
									else
									{
										targetInRange = targetInSite = false;
										targetDistance = 0f;
										lightAnimationController(IndicatorAnim3, IndicatorAnim1, IndicatorAnim2, redLight, greenLight, yellowLight);
									}
								}
								else
								{
									validTarget = targetInRange = targetInSite = false;
									targetDistance = 0f;
									lightAnimationController(IndicatorAnim3, IndicatorAnim1, IndicatorAnim2, redLight, greenLight, yellowLight);
								}
							}
							else
							{
								targetDistance = 0f;
								targetModule = null;
								validTarget = targetInRange = targetInSite = false;
								lightAnimationController(IndicatorAnim3, IndicatorAnim1, IndicatorAnim2, redLight, greenLight, yellowLight);
							}
						}
						else
						{
							targetDistance = 0f;
							targetModule = null;
							validTarget = targetInRange = targetInSite = false;
							lightAnimationController(IndicatorAnim3, IndicatorAnim1, IndicatorAnim2, redLight, greenLight, yellowLight);
						}
						status = s;
					}
				}
			}
		}

		private void EventsCheck()
		{
			Events["ResetExperiment"].active = scienceReports.Count > 0;
			Events["CollectDataExternalEvent"].active = scienceReports.Count > 0 && dataIsCollectable;
			Events["DeployExperiment"].active = scienceReports.Count == 0 && !Deployed && !Inoperable;
			Events["ReviewDataEvent"].active = scienceReports.Count > 0;
			Fields["status"].guiActive = IsDeployed;
		}

		private void FixedUpdate()
		{
			if (HighLogic.LoadedSceneIsFlight)
			{
				if (IsDeployed)
				{
					if (validTarget && targetInRange && targetModule != null)
					{
						//Point dish at the target;
						Vector3 localTarget = transform.InverseTransformPoint(targetModule.transform.position);
						Quaternion lookToTarget = Quaternion.LookRotation(localTarget);
						Quaternion lookToTargetFlat = lookToTarget;
						lookToTargetFlat.x = 0;
						lookToTargetFlat.y = 0;
						tR.localRotation = Quaternion.Slerp(tR.localRotation, lookToTargetFlat, Time.deltaTime * 5f);
						lookToTarget.y = 0;
						lookToTarget.z = 0;
						t.localRotation = Quaternion.Slerp(t.localRotation, lookToTarget, Time.deltaTime * 5f);
					}
					else
					{
						//Slowly rotate dish
						tR.Rotate(Vector3.forward * Time.deltaTime * 60f);
						if (t.localEulerAngles.x < 42)
							t.Rotate(Vector3.right * Time.deltaTime * 20f);
						else if (t.localEulerAngles.x > 47)
							t.Rotate(Vector3.left * Time.deltaTime * 20f);
					}
					rotating = true;
				}
				else if (rotating)
				{
					if (tR.localEulerAngles.z > 1 || t.localEulerAngles.x > 1)
					{
						if (tR.localEulerAngles.z > 1)
							tR.Rotate(Vector3.forward * Time.deltaTime * 60f);
						if (t.localEulerAngles.x > 1)
							t.Rotate(Vector3.left * Time.deltaTime * 20f);
					}
					else
						rotating = false;
				}
			}
		}

		private bool simpleRayHit()
		{
			Vector3 tPos = t.position;
			Ray r = new Ray(tPos, t.forward);
			RaycastHit hit = new RaycastHit();
			Physics.Raycast(r, out hit, 5000);
			if (hit.collider != null)
			{
				string obj = hit.collider.attachedRigidbody.gameObject.name;
				if (obj.StartsWith(potato))
					return true;
			}
			return false;
		}

		public override string GetInfo()
		{
			string info = base.GetInfo();
			if (resourceCost > 0f)
			{
				info += string.Format("Requires:\n-{0}: {1}/s\n", experimentResource, resourceCost);
			}
			return info;
		}

		#endregion

		#region Animator

		//Controls the main, door-opening animation
		private void animator(float speed, float time)
		{
			if (Anim != null)
			{
				Anim[animationName].speed = speed;
				if (!Anim.IsPlaying(animationName))
				{
					Anim[animationName].normalizedTime = time;
					Anim.Blend(animationName, 1f);
				}
			}
		}

		private void lightAnimator(Animation a, string name, bool stop)
		{
			if (a != null)
			{
				if (!a.IsPlaying(name) && !stop)
				{
					a[name].speed = 1f;
					a[name].normalizedTime = 0f;
					a[name].wrapMode = WrapMode.Loop;
					a.Blend(name);
				}
				else if (stop)
				{
					a[name].normalizedTime = a[name].normalizedTime % 1;
					a[name].wrapMode = WrapMode.Clamp;
				}
			}
		}

		//Cludgy method for controlling the indicator lights...
		private void lightAnimationController(Animation a, Animation stop1, Animation stop2, string name1, string stopName1, string stopName2)
		{
			if (a != null)
			{
				if (!a.IsPlaying(name1))
					lightAnimator(a, name1, false);
			}
			if (stop1 != null)
			{
				if (stop1.IsPlaying(stopName1))
					lightAnimator(stop1, stopName1, true);
			}
			if (stop2 != null)
			{
				if (stop2.IsPlaying(stopName2))
					lightAnimator(stop2, stopName2, true);
			}
		}

		private void deployEvent()
		{
			animator(1f, 0f);
			IsDeployed = true;
		}

		private void retractEvent()
		{
			animator(-1f, 1f);
			IsDeployed = false;
		}

		#endregion

		#region Events and Actions

		[KSPEvent(guiActive = true, guiName = "Toggle Asteroid Scanner", active = true)]
		public void toggleEvent()
		{
			if (IsDeployed) retractEvent();
			else deployEvent();
		}

		[KSPAction("Toggle Asteroid Scanner")]
		public void toggleAction(KSPActionParam param)
		{
			toggleEvent();
		}

		[KSPEvent(guiActiveEditor = true, guiName = "Deploy Asteroid Scanner", active = true)]
		public void editorDeployEvent()
		{
			deployEvent();
			IsDeployed = false;
			Events["editorDeployEvent"].active = false;
			Events["editorRetractEvent"].active = true;
		}

		[KSPEvent(guiActiveEditor = true, guiName = "Retract Asteroid Scanner", active = false)]
		public void editorRetractEvent()
		{
			retractEvent();
			Events["editorDeployEvent"].active = true;
			Events["editorRetractEvent"].active = false;
		}

		[KSPEvent(guiActive = true, guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Reset Asteroid Scanner", active = false)]
		public void ResetExperiment()
		{
			if (scienceReports.Count > 0)
			{
				scienceReports.Clear();
				Deployed = false;
			}
		}

		[KSPAction("Reset Asteroid Scanner")]
		public void ResetAction(KSPActionParam param)
		{
			ResetExperiment();
		}

		[KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Collect Asteroid Data", active = false)]
		public void CollectDataExternalEvent()
		{
			List<ModuleScienceContainer> EVACont = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceContainer>();
			if (scienceReports.Count > 0)
			{
				if (EVACont.First().StoreData(new List<IScienceDataContainer> { this }, false))
					DumpData(scienceReports[0]);
			}
		}

		[KSPEvent(guiActive = true, guiName = "Review Data", active = false)]
		public void ReviewDataEvent()
		{
			ReviewData();
		}

		[KSPAction("Review Data")]
		public void ReviewDataAction(KSPActionParam param)
		{
			ReviewData();
		}

		[KSPEvent(guiActive = true, guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Scan Asteroid Interior", active = false)]
		public void DeployExperiment()
		{
			if (validTarget && targetInRange && targetInSite)
			{
				ModuleAsteroid modAst = null;
				float distance = asteroidScanLength(out modAst);
				runExperiment(distance, modAst);
			}
			else
				ScreenMessages.PostScreenMessage("No valid targets within scaning range", 5f, ScreenMessageStyle.UPPER_CENTER);
		}

		[KSPAction("Scan Asteroid")]
		public void DeployAction(KSPActionParam param)
		{
			DeployExperiment();
		}

		#endregion

		#region Science Setup

		//Determine if the signal passes through the asteroid, and what distance it travels if so
		private float asteroidScanLength(out ModuleAsteroid m)
		{
			m = null;
			float dist = 0f;
			if (t != null && targetModule != null)
			{
				Vector3 tPos = t.position;
				Vector3 targetPos = targetModule.part.transform.position;
				Vector3 direction = targetPos - tPos;
				Ray r = new Ray(tPos, direction);
				RaycastHit hit = new RaycastHit();
				Physics.Raycast(r, out hit, targetDistance);

				//The first ray determines whether or not the asteroid was hit and the distance from the dish
				//to that first encounter
				if (hit.collider != null)
				{
					string obj = hit.collider.attachedRigidbody.gameObject.name;
					if (obj.StartsWith(potato))
					{
						float firstDist = hit.distance;
						Vector3 reverseDirection = tPos - targetPos;
						Ray targetRay = new Ray(targetPos, reverseDirection);
						RaycastHit targetHit = new RaycastHit();
						Physics.Raycast(targetRay, out targetHit, targetDistance);

						//The second ray determines the distance from the target vessel to the asteroid
						if (targetHit.collider != null)
						{
							string targetObj = targetHit.collider.attachedRigidbody.gameObject.name;
							if (targetObj.StartsWith(potato))
							{
								float secondDist = hit.distance;

								Part p = Part.FromGO(hit.transform.gameObject) ?? hit.transform.gameObject.GetComponentInParent<Part>();

								if (p != null)
								{
									if (p.Modules.Contains("ModuleAsteroid"))
										m = p.FindModuleImplementing<ModuleAsteroid>();
								}

								//The two distances are subtracted from the total distance between vessels to 
								//give the distance the signal travels while inside the asteroid
								dist = targetDistance - secondDist - firstDist;
							}
						}
					}
				}
			}

			return dist;
		}

		private void runExperiment(float distance, ModuleAsteroid m)
		{
			ScienceData data = makeScience(distance, m);
			if (data == null)
				Debug.LogError("[DM] Something Went Wrong Here; Null Asteroid Science Data Returned; Please Report This On The KSP Forum With Output.log Data");
			else
			{
				scienceReports.Add(data);
				Deployed = true;
				ReviewData();
			}
		}

		private int aClassInt(string s)
		{
			switch (s)
			{
				case "A":
					return 0;
				case "B":
					return 1;
				case "C":
					return 2;
				case "D":
					return 3;
				case "E":
					return 4;
				default:
					return 5;
			}
		}

		private ScienceData makeScience(float dist, ModuleAsteroid m)
		{
			if (dist <= 0 || m == null)
			{
				DMUtils.Logging("Asteroid Not Scanned...");
				return null;
			}
			ScienceData data = null;
			ScienceSubject sub = null;
			CelestialBody body = null;
			DMAsteroidScience ast = null;
			string biome = "";
			float multiplier = 1f;

			ast = new DMAsteroidScience(m);
			body = ast.body;
			biome = ast.aType + ast.aSeed;
			multiplier = Math.Max(1f, dist / astWidth[aClassInt(ast.aClass)]);

			if (exp == null)
			{
				Debug.LogError("[DM] Something Went Wrong Here; Null Asteroid Experiment Returned; Please Report This On The KSP Forum With Output.log Data");
				return null;
			}

			sub = ResearchAndDevelopment.GetExperimentSubject(exp, ExperimentSituations.InSpaceLow, body, biome);

			if (sub == null)
			{
				Debug.LogError("[DM] Something Went Wrong Here; Null Asteroid Subject Returned; Please Report This On The KSP Forum With Output.log Data");
				return null;
			}

			DMUtils.OnAsteroidScience.Fire(ast.aClass, exp.id);
			sub.title = string.Format("{0} of a {1} asteroid", exp.experimentTitle, ast.aType);
			registerDMScience(ast, sub);
			body.bodyName = asteroidBodyNameFixed;

			data = new ScienceData(multiplier * exp.baseValue * sub.dataScale, transmitValue, 0f, sub.id, sub.title);

			return data;
		}

		private void registerDMScience(DMAsteroidScience newAst, ScienceSubject sub)
		{
			DMScienceScenario.DMScienceData DMData = null;
			foreach (DMScienceScenario.DMScienceData DMScience in DMScienceScenario.SciScenario.recoveredScienceList)
			{
				if (DMScience.title == sub.title)
				{
					sub.scientificValue *= DMScience.scival;
					DMData = DMScience;
					break;
				}
			}
			if (DMData == null)
			{
				float astSciCap = exp.scienceCap * 40f;
				DMScienceScenario.SciScenario.RecordNewScience(sub.title, exp.baseValue, 1f, 0f, astSciCap);
				sub.scientificValue = 1f;
			}
			sub.subjectValue = newAst.sciMult;
			sub.scienceCap = exp.scienceCap * sub.subjectValue;
			sub.science = sub.scienceCap - (sub.scienceCap * sub.scientificValue);
		}

		#endregion

		#region Results Page

		private void experimentResultsPage(ScienceData data)
		{
			if (scienceReports.Count > 0)
			{
				ExperimentResultDialogPage page = new ExperimentResultDialogPage(part, data, transmitValue, 0f, false, "", true, data.labBoost < 1 && checkLabOps() && transmitValue < 1f, new Callback<ScienceData>(onDiscardData), new Callback<ScienceData>(onKeepData), new Callback<ScienceData>(onTransmitData), new Callback<ScienceData>(onSendToLab));
				ExperimentsResultDialog.DisplayResult(page);
			}
		}

		private void onDiscardData(ScienceData data)
		{
			if (scienceReports.Count > 0)
			{
				scienceReports.Clear();
				Deployed = false;
			}
		}

		private void onKeepData(ScienceData data)
		{
		}

		private void onTransmitData(ScienceData data)
		{
			List<IScienceDataTransmitter> tranList = vessel.FindPartModulesImplementing<IScienceDataTransmitter>();
			if (tranList.Count > 0 && scienceReports.Count > 0)
			{
				tranList.OrderBy(ScienceUtil.GetTransmitterScore).First().TransmitData(new List<ScienceData> { data });
				DumpData(data);
			}
			else
				ScreenMessages.PostScreenMessage("No transmitters available on this vessel.", 5f, ScreenMessageStyle.UPPER_LEFT);
		}

		private void onSendToLab(ScienceData data)
		{
			List<ModuleScienceLab> labList = vessel.FindPartModulesImplementing<ModuleScienceLab>();
			if (checkLabOps() && scienceReports.Count > 0)
				labList.OrderBy(ScienceUtil.GetLabScore).First().StartCoroutine(labList.First().ProcessData(data, new Callback<ScienceData>(onComplete)));
			else
				ScreenMessages.PostScreenMessage("No operational lab modules on this vessel. Cannot analyze data.", 5f, ScreenMessageStyle.UPPER_CENTER);
		}

		private void onComplete(ScienceData data)
		{
			ReviewData();
		}

		private bool checkLabOps()
		{
			List<ModuleScienceLab> labList = vessel.FindPartModulesImplementing<ModuleScienceLab>();
			for (int i = 0; i < labList.Count; i++)
			{
				if (labList[i].IsOperational())
					return true;
			}
			return false;
		}

		#endregion

		#region IScienceDataContainer

		public void ReviewData()
		{
			if (scienceReports.Count > 0)
				experimentResultsPage(scienceReports[0]);
		}

		public void ReviewDataItem(ScienceData data)
		{
			ReviewData();
		}

		public bool IsRerunnable()
		{
			return rerunnable;
		}

		public int GetScienceCount()
		{
			return scienceReports.Count;
		}

		public ScienceData[] GetData()
		{
			return scienceReports.ToArray();
		}

		public void DumpData(ScienceData data)
		{
			if (scienceReports.Count > 0)
			{
				Inoperable = !IsRerunnable();
				Deployed = !Inoperable;
				scienceReports.Remove(data);
			}
		}

		#endregion

	}
}
