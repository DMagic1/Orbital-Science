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

namespace DMagic
{
	class DMAnomalyContract: Contract
	{
		private DMCollectScience newParam;
		private DMScienceContainer DMScience;
		private List<DMScienceContainer> sciList = new List<DMScienceContainer>();
		private DMAnomalyParameter[] anomParams = new DMAnomalyParameter[4];
		private CelestialBody body;
		private PQSCity targetAnomaly;
		private double lat, lon;
		private double fudgedLat, fudgedLon;
		private string cardNS, cardEW;
		private string hash;
		private int i = 0;
		private List<PQSCity> aList = new List<PQSCity>();
		private System.Random rand = DMUtils.rand;

		protected override bool Generate()
		{
			if (!GetBodies_Reached(true, true).Contains(FlightGlobals.Bodies[1]))
				return false;
			int total = ContractSystem.Instance.GetCurrentContracts<DMAnomalyContract>().Count();
			if (total > 0)
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
				if (rand.Next(0, 2) == 0)
					body = FlightGlobals.Bodies[3];
				else
					body = FlightGlobals.Bodies[6];
			}
			//Vall, Tylo, and Bop are last
			else if (this.Prestige == ContractPrestige.Exceptional)
			{
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

			//Build a list of anomalies for the target planet
			PQSCity[] Cities = UnityEngine.Object.FindObjectsOfType(typeof(PQSCity)) as PQSCity[];
			foreach (PQSCity city in Cities)
			{
				if (city.transform.parent.name == body.name)
					aList.Add(city);
			}

			//Select random anomaly
			targetAnomaly = aList[rand.Next(0, aList.Count)];
			hash = targetAnomaly.name;
			lat = clampLat(body.GetLatitude(targetAnomaly.transform.position));
			lon = clampLon(body.GetLongitude(targetAnomaly.transform.position));
			fudgedLat = fudgeLat(lat);
			fudgedLon = fudgeLon(lon);
			cardNS = NSDirection(lat);
			cardEW = EWDirection(lon);
			DMUtils.DebugLog("Anomaly [{0}] Selected On {1} at Latitude: {2:N1} and Longitude: {3:N1}", targetAnomaly.name, body.theName, lat, lon);

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

			this.AddParameter(newParam, null);
			DMUtils.DebugLog("Added Primary Anomaly Parameter");

			foreach (DMAnomalyParameter aP in anomParams)
			{
				if (aP != null)
				{
					this.AddParameter(aP);
					DMUtils.DebugLog("Added Secondary Anomaly Parameter");
				}
			}

			if (this.ParameterCount == 0)
				return false;

			base.SetExpiry(10, 20 * (float)(this.prestige + 1));
			base.SetDeadlineDays(30f * (float)(this.prestige + 1), body);
			base.SetReputation(5f * (float)(this.prestige + 1) * DMUtils.reward, 10f * (float)(this.prestige + 1) * DMUtils.penalty, body);
			base.SetFunds(3000f * DMUtils.forward, 2000f * DMUtils.reward, 2500f * DMUtils.penalty, body);
			return true;
		}

		private double clampLat(double Lat)
		{
			return (Lat + 180 +90) % 180 - 90;
		}

		private double clampLon(double Lon)
		{
			return (Lon + 360 + 180) % 360 -180;
		}

		private double fudgeLat(double Lat)
		{
			double f = Math.Round(((double)rand.Next(-5, 5) + Lat) / 10) * 10;
			return f;
		}

		private double fudgeLon(double Lon)
		{
			double f = Math.Round(((double)rand.Next(-5, 5) + Lon) / 10) * 10;
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
			return string.Format("Study the anomalous readings coming from {0}", body.theName);
			
		}

		protected override string GetDescription()
		{
			string story = DMUtils.backStory["anomaly"][rand.Next(0, DMUtils.backStory["anomaly"].Count)];
			return string.Format(story, this.agent.Name, body.theName);
		}

		protected override string GetSynopsys()
		{
			DMUtils.DebugLog("Generating Synopsis From Anomaly [{0}]", hash);
			return string.Format("Locate and study the source of the anomalous readings coming from {0}'s surface at around {1:N0} degrees {2} and {3:N0} degrees {4}", body.theName, fudgedLat, cardNS, fudgedLon, cardEW);
		}

		protected override string MessageCompleted()
		{
			return string.Format("You successfully returned data from the {0} on {1}, well done.", hash, body.theName);
		}

		protected override void OnLoad(ConfigNode node)
		{
			DMUtils.DebugLog("Loading Anomaly Contract");
			int targetBodyID;
			string[] anomalyString = node.GetValue("Target_Anomaly").Split('|');
			hash = anomalyString[0];
			if (int.TryParse(anomalyString[1], out targetBodyID))
				body = FlightGlobals.Bodies[targetBodyID];
			else
			{
				DMUtils.Logging("Failed To Load Anomaly Contract");
				this.Cancel();
			}
			if (!double.TryParse(anomalyString[2], out lat))
			{
				DMUtils.Logging("Failed To Load Anomaly Contract");
				this.Cancel();
			}
			if (!double.TryParse(anomalyString[3], out lon))
			{
				DMUtils.Logging("Failed To Load Anomaly Contract");
				this.Cancel();
			}
			fudgedLat = fudgeLat(lat);
			fudgedLon = fudgeLon(lon);
			cardNS = NSDirection(lat);
			cardEW = EWDirection(lon);
			if (HighLogic.LoadedScene == GameScenes.FLIGHT)
				try
				{
					targetAnomaly = (UnityEngine.Object.FindObjectsOfType(typeof(PQSCity)) as PQSCity[]).FirstOrDefault(c => c.name == hash);
				}
				catch
				{
					DMUtils.Logging("Failed To Load Anomaly Contract");
					this.Cancel();
				}
			if (this.ParameterCount == 0)
				this.Cancel();
		}

		protected override void OnSave(ConfigNode node)
		{
			DMUtils.DebugLog("Saving Anomaly Contract");
			node.AddValue("Target_Anomaly", string.Format("{0}|{1}|{2:N2}|{3:N2}", hash, body.flightGlobalsIndex, lat, lon));
		}

		public override bool MeetRequirements()
		{
			return true;
		}

	}
}
