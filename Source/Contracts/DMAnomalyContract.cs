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
using DMagic.Scenario;

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
		private double lat, lon;
		private string cardNS, cardEW, hash;
		private int i = 0;
		private System.Random rand = DMUtils.rand;
		private System.Random r;
		private int latRand, lonRand;

		protected override bool Generate()
		{
			DMAnomalyContract[] anomContracts = ContractSystem.Instance.GetCurrentContracts<DMAnomalyContract>();
			int offers = 0;
			int active = 0;
			int maxOffers = DMUtils.maxAnomalyOffered;
			int maxActive = DMUtils.maxAnomalyActive;

			for (int i = 0; i < anomContracts.Length; i++ )
			{
				DMAnomalyContract a = anomContracts[i];
				if (a.ContractState == State.Offered)
					offers++;
				else if (a.ContractState == State.Active)
					active++;
			}

			if (offers >= maxOffers)
				return false;
			if (active >= maxActive)
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

			r = new System.Random(this.MissionSeed);
			latRand = r.Next(-5, 5);
			lonRand = r.Next(-5, 5);

			//Select random anomaly
			targetAnomaly = new DMAnomalyObject(cities[rand.Next(0, cities.Count)]);
			hash = targetAnomaly.Name;
			lon = targetAnomaly.Lon;
			lat = targetAnomaly.Lat;
			cardNS = NSDirection(lat);
			cardEW = EWDirection(lon);

			//Assign primary anomaly contract parameter
			if ((newParam = DMAnomalyGenerator.fetchAnomalyParameter(body, targetAnomaly)) == null)
				return false;

			sciList.AddRange(DMUtils.availableScience[DMScienceType.Anomaly.ToString()].Values);

			for (i = 0; i < 3; i++)
			{
				if (sciList.Count > 0)
				{
					DMScience = sciList[rand.Next(0, sciList.Count)];
					anomParams[i] = (DMAnomalyGenerator.fetchAnomalyParameter(body, DMScience));
					sciList.Remove(DMScience);
				}
				else
					anomParams[i] = null;
			}

			this.AddParameter(newParam, "AnomalyScience");

			float primaryLocationMod = GameVariables.Instance.ScoreSituation(DMUtils.convertSit(newParam.Situation), newParam.Body) * ((float)rand.Next(85, 116) / 100f);
			newParam.SetFunds(12000f * DMUtils.reward * primaryLocationMod, body);
			newParam.SetScience(6f * DMUtils.science * DMUtils.fixSubjectVal(newParam.Situation, 1f, body), null);

			//Add the science collection parent parameter
			DMCompleteParameter DMcp = new DMCompleteParameter(3, 1);
			this.AddParameter(DMcp);

			foreach (DMAnomalyParameter aP in anomParams)
			{
				if (aP != null)
				{
					DMcp.addToSubParams(aP, "CollectAnomalyScience");
					float locationMod = GameVariables.Instance.ScoreSituation(DMUtils.convertSit(aP.Situation), body) * ((float)rand.Next(85, 116) / 100f);
					aP.SetFunds(7000f * DMUtils.reward * locationMod, body);
					aP.SetScience(aP.Container.Exp.baseValue * 0.25f * DMUtils.science * DMUtils.fixSubjectVal(aP.Situation, 1f, body), null);
				}
			}

			if (DMcp.ParameterCount < 2)
				return false;

			this.agent = AgentList.Instance.GetAgent("DMagic");
			base.SetExpiry(10 * DMUtils.deadline, 20 * DMUtils.deadline);
			base.SetDeadlineYears(1.5f * ((float)rand.Next(80, 121)) / 100f * DMUtils.deadline, body);
			base.SetReputation(8f * DMUtils.reward * primaryLocationMod, 9f * DMUtils.penalty * primaryLocationMod, null);
			base.SetFunds(20000f * DMUtils.forward * primaryLocationMod, 24000f * DMUtils.reward * primaryLocationMod, 20000f * DMUtils.penalty * primaryLocationMod, body);
			return true;
		}

		private double FudgedLat
		{
			get
			{
				if (HighLogic.LoadedSceneIsFlight)
					lat = targetAnomaly.Lat;
				return Math.Round(((double)latRand + lat) / 10d) * 10d;
			}
		}

		private double FudgedLon
		{
			get
			{
				if (HighLogic.LoadedSceneIsFlight)
					lon = targetAnomaly.Lon;
				return Math.Round(((double)lonRand + lon) / 10d) * 10d;
			}
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
			return string.Format("Locate the anomalous signal coming from roughly {0}° {1} and {2}° {3}.", Math.Abs(FudgedLat), cardNS, Math.Abs(FudgedLon), cardEW);
		}

		protected override string GetDescription()
		{
			string story = DMUtils.backStory["anomaly"][rand.Next(0, DMUtils.backStory["anomaly"].Count)];
			return string.Format(story, this.agent.Name, body.theName);
		}

		protected override string GetSynopsys()
		{
			return string.Format("We would like you to travel to a specific location on {0}. Once there attempt to locate and study the source of the anomalous signal.", body.theName);
		}

		protected override string MessageCompleted()
		{
			return string.Format("You successfully returned data from the {0} on {1}, well done.", hash, body.theName);
		}

		protected override void OnLoad(ConfigNode node)
		{
			r = new System.Random(this.MissionSeed);
			latRand = r.Next(-5, 5);
			lonRand = r.Next(-5, 5);

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
				if (DMScienceScenario.SciScenario != null)
				{
					if (DMScienceScenario.SciScenario.anomalyList != null)
						targetAnomaly = DMScienceScenario.SciScenario.anomalyList.getAnomalyObject(body.name, hash);
				}
				if (targetAnomaly != null)
				{
					lat = targetAnomaly.Lat;
					lon = targetAnomaly.Lon;
				}
				else
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
					lat = 0.000d;
				if (!double.TryParse(anomalyString[3], out lon))
					lon = 0.000d;
			}
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

		/// <summary>
		/// Used externally to return the target Celestial Body
		/// </summary>
		/// <param name="cP">Instance of the requested Contract</param>
		/// <returns>Celestial Body object</returns>
		public static CelestialBody TargetBody(Contract c)
		{
			if (c == null || c.GetType() != typeof(DMAnomalyContract))
				return null;

			DMAnomalyContract Instance = (DMAnomalyContract)c;
			return Instance.body;
		}

		public CelestialBody Body
		{
			get { return body; }
		}

		public DMAnomalyObject TargetAnomaly
		{
			get { return targetAnomaly; }
		}

	}
}
