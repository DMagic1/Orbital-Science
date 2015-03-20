#region license
/* DMagic Orbital Science - DMAnomalyContract
 * Class for generating anomaly science experiment contracts
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
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Contracts;
using Contracts.Parameters;
using Contracts.Agents;
using DMagic.Parameters;

namespace DMagic.Contracts
{
	public class DMAnomalyContract: Contract
	{
		private DMCollectScience newParam;
		private DMScienceContainer DMScience;
		private List<DMScienceContainer> sciList = new List<DMScienceContainer>();
		private DMAnomalyParameter[] anomParams = new DMAnomalyParameter[4];
		private CelestialBody body;
		private List<PQSCity> cities = new List<PQSCity>();
		private DMAnomalyObject targetAnomaly;
		private double lat, lon, fudgedLat, fudgedLon;
		private string cardNS, cardEW, hash;
		private int i = 0;
		private System.Random rand = DMUtils.rand;

		protected override bool Generate()
		{
			int total = ContractSystem.Instance.GetCurrentContracts<DMAnomalyContract>().Count();
			if (total >= DMUtils.maxAnomaly)
				return false;

			//Make sure that the anomaly scanner is available
			AvailablePart aPart = PartLoader.getPartInfoByName("dmAnomScanner");
			if (aPart == null)
				return false;
			if (!ResearchAndDevelopment.PartModelPurchased(aPart))
				return false;

			//Kerbin or Mun Anomalies for trivial contracts
			if (this.Prestige == ContractPrestige.Trivial)
			{
				if (rand.Next(0, 3) == 0)
					body = FlightGlobals.Bodies[1];
				else
					body = FlightGlobals.Bodies[2];
			}
			//Minmus and Duna are next
			else if (this.Prestige == ContractPrestige.Significant)
			{
				if (!ProgressTracking.Instance.NodeComplete(new string[] { "Kerbin", "Escape" }))
					return false;
				if (rand.Next(0, 2) == 0)
					body = FlightGlobals.Bodies[3];
				else
					body = FlightGlobals.Bodies[6];
			}
			//Vall, Tylo, and Bop are last; only if we've been to Jool first
			else if (this.Prestige == ContractPrestige.Exceptional)
			{
				if (!ProgressTracking.Instance.NodeComplete(new string[] { "Jool", "Flyby" }))
					return false;
				int i = rand.Next(0, 3);
				if (i == 0)
					body = FlightGlobals.Bodies[10];
				else if (i == 1)
					body = FlightGlobals.Bodies[11];
				else if (i == 2)
					body = FlightGlobals.Bodies[12];
				else
					return false;
			}
			else
				return false;

			PQSCity[] Cities = UnityEngine.Object.FindObjectsOfType(typeof(PQSCity)) as PQSCity[];
			foreach (PQSCity city in Cities)
			{
				if (city.transform.parent.name == body.name)
					cities.Add(city);
			}

			//Select random anomaly
			targetAnomaly = new DMAnomalyObject(cities[rand.Next(0, cities.Count)]);
			hash = targetAnomaly.Name;
			lon = targetAnomaly.Lon;
			lat = targetAnomaly.Lat;
			fudgedLat = fudgeLat(lat);
			fudgedLon = fudgeLon(lon);
			cardNS = NSDirection(lat);
			cardEW = EWDirection(lon);
			DMUtils.DebugLog("Anomaly [{0}] Selected On {1} at Latitude: {2:N1} and Longitude: {3:N1}", targetAnomaly.Name, body.theName, lat, lon);

			//Assign primary anomaly contract parameter
			if ((newParam = DMAnomalyGenerator.fetchAnomalyParameter(body, targetAnomaly)) == null)
				return false;

			sciList.AddRange(DMUtils.availableScience[DMScienceType.Anomaly.ToString()].Values);

			for (i = 0; i < 3; i++)
			{
				if (sciList.Count > 0)
				{
					DMScience = sciList[rand.Next(0, sciList.Count)];
					anomParams[i] = (DMAnomalyGenerator.fetchAnomalyParameter(body, targetAnomaly, DMScience));
					sciList.Remove(DMScience);
				}
				else
					anomParams[i] = null;
			}

			this.AddParameter(newParam);
			DMUtils.DebugLog("Added Primary Anomaly Parameter");
			float primaryLocationMod = GameVariables.Instance.ScoreSituation(DMUtils.convertSit(newParam.Situation), newParam.Body) * ((float)rand.Next(85, 116) / 100f);
			newParam.SetFunds(20000f * DMUtils.reward * primaryLocationMod, 10000f * DMUtils.penalty * primaryLocationMod, body);
			newParam.SetReputation(80f * DMUtils.reward * primaryLocationMod, 10f * DMUtils.penalty * primaryLocationMod, body);
			newParam.SetScience(35f * DMUtils.science * DMUtils.fixSubjectVal(newParam.Situation, 1f, body), null);

			foreach (DMAnomalyParameter aP in anomParams)
			{
				if (aP != null)
				{
					this.AddParameter(aP, "collectDMAnomaly");
					DMUtils.DebugLog("Added Secondary Anomaly Parameter");
					float locationMod = GameVariables.Instance.ScoreSituation(DMUtils.convertSit(aP.Situation), aP.Body) * ((float)rand.Next(85, 116) / 100f);
					aP.SetFunds(9000f * DMUtils.reward * locationMod, 6000f * DMUtils.penalty * locationMod, body);
					aP.SetReputation(20f * DMUtils.reward * locationMod, 5f * DMUtils.penalty * locationMod, body);
					aP.SetScience(aP.Container.Exp.baseValue * 2f * DMUtils.science * DMUtils.fixSubjectVal(aP.Situation, 1f, body), null);
				}
			}

			if (this.ParameterCount == 0)
				return false;

			this.agent = AgentList.Instance.GetAgent("DMagic");
			base.SetExpiry(10 * DMUtils.deadline, 20 * DMUtils.deadline);
			base.SetDeadlineYears(1.8f * ((float)rand.Next(80, 121)) / 100f * DMUtils.deadline, body);
			base.SetReputation(20f * DMUtils.reward * primaryLocationMod, 10f * DMUtils.penalty * primaryLocationMod, body);
			base.SetFunds(25000f * DMUtils.forward * primaryLocationMod, 22000f * DMUtils.reward * primaryLocationMod, 16000f * DMUtils.penalty * primaryLocationMod, body);
			return true;
		}

		private double fudgeLat(double Lat)
		{
			double f = Math.Round(((double)rand.Next(-5, 5) + Lat) / 10d) * 10d;
			return f;
		}

		private double fudgeLon(double Lon)
		{
			double f = Math.Round(((double)rand.Next(-5, 5) + Lon) / 10d) * 10d;
			return f;
		}

		private string NSDirection(double Lat)
		{
			if (Lat >= -90 && Lat < 0)
				return "South";
			if (Lat >= 0 && Lat <= 90)
				return "North";
			return "";
		}

		private string EWDirection(double Lon)
		{
			if (Lon >= -180 && Lon < 0)
				return "West";
			if (Lon >= 0 && Lon < 180)
				return "East";
			return "";
		}

		public override bool CanBeCancelled()
		{
			return true;
		}

		public override bool CanBeDeclined()
		{
			return true;
		}

		protected override string GetHashString()
		{
			return hash;
		}

		protected override string GetTitle()
		{
			return string.Format("Study the source of the anomalous readings coming from {0}'s surface", body.theName);
		}

		protected override string GetNotes()
		{
			return string.Format("Locate the anomalous signal coming from roughly {0}° {1} and {2}° {3}. An on-screen message will indicate successful collection of results; data must be transmitted or returned to complete each parameter.\n", Math.Abs(fudgedLat), cardNS, Math.Abs(fudgedLon), cardEW);
		}

		protected override string GetDescription()
		{
			string story = DMUtils.backStory["anomaly"][rand.Next(0, DMUtils.backStory["anomaly"].Count)];
			return string.Format(story, this.agent.Name, body.theName);
		}

		protected override string GetSynopsys()
		{
			return string.Format("We would like you to travel to a specific location on {0}. Once there attempt to locate and study the source of the anomalous signal detected from that region.", body.theName);
		}

		protected override string MessageCompleted()
		{
			return string.Format("You successfully returned data from the {0} on {1}, well done.", hash, body.theName);
		}

		protected override void OnLoad(ConfigNode node)
		{
			int targetBodyID;
			string[] anomalyString = node.GetValue("Target_Anomaly").Split('|');
			hash = anomalyString[0];
			if (int.TryParse(anomalyString[1], out targetBodyID))
				body = FlightGlobals.Bodies[targetBodyID];
			else
			{
				DMUtils.Logging("Failed To Load Anomaly Contract Target Body");
				this.Unregister();
				ContractSystem.Instance.Contracts.Remove(this);
				return;
			}
			if (HighLogic.LoadedSceneIsFlight)
			{
				try
				{
					targetAnomaly = new DMAnomalyObject((UnityEngine.Object.FindObjectsOfType(typeof(PQSCity)) as PQSCity[]).FirstOrDefault(c => c.name == hash && c.transform.parent.name == body.name));
					lat = targetAnomaly.Lat;
					lon = targetAnomaly.Lon;
				}
				catch
				{
					DMUtils.Logging("Failed To Load Anomaly Contract Object");
					this.Unregister();
					ContractSystem.Instance.Contracts.Remove(this);
					return;
				}
			}
			else
			{
				if (!double.TryParse(anomalyString[2], out lat))
				{
					DMUtils.Logging("Failed To Load Anomaly Lat Values");
					lat = 0.000d;
				}
				if (!double.TryParse(anomalyString[3], out lon))
				{
					DMUtils.Logging("Failed To Load Anomaly Long Values");
					lon = 0.000d;
				}
			}
			fudgedLat = fudgeLat(lat);
			fudgedLon = fudgeLon(lon);
			cardNS = NSDirection(lat);
			cardEW = EWDirection(lon);
			if (this.ParameterCount == 0)
			{
				DMUtils.Logging("No Parameters Loaded For This Anomaly Contract; Removing Now...");
				this.Unregister();
				ContractSystem.Instance.Contracts.Remove(this);
				return;
			}
		}

		protected override void OnSave(ConfigNode node)
		{
			node.AddValue("Target_Anomaly", string.Format("{0}|{1}|{2:N2}|{3:N2}", hash, body.flightGlobalsIndex, lat, lon));
		}

		public override bool MeetRequirements()
		{
			return ProgressTracking.Instance.NodeComplete(new string[] { "Kerbin", "Orbit" });
		}

	}
}
