using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Contracts;
using Contracts.Parameters;
using Contracts.Agents;

namespace DMagic
{
	class DMOrbitalSurveyContract: Contract
	{
		internal DMCollectScience[] newParams = new DMCollectScience[5];
		private CelestialBody body;
		private int i = 0;
		private System.Random rand = DMUtils.rand;
		
		protected override bool Generate()
		{
			if (!GetBodies_Reached(true, true).Contains(FlightGlobals.Bodies[1]))
				return false;
			if (ContractSystem.Instance.GetCurrentContracts<DMOrbitalSurveyContract>().Count() > 1)
				return false;
			//No trivial contracts here
			this.prestige = (ContractPrestige)rand.Next(1, 3);

			//Generates the science experiment, returns null if experiment fails any check
			if ((newParams[0] = DMOrbitalSurveyGenerator.fetchOrbitalScience(this.Prestige, GetBodies_Reached(false, true), GetBodies_NextUnreached(4, null))) == null)
				return false;

			body = newParams[0].Body;

			//Generate several more experiments using the target body returned from the first
			if ((newParams[1] = DMOrbitalSurveyGenerator.fetchOrbitalScience(body)) == null)
				return false;
			if ((newParams[2] = DMOrbitalSurveyGenerator.fetchOrbitalScience(body)) == null)
				return false;
			newParams[3] = DMOrbitalSurveyGenerator.fetchOrbitalScience(body);
			newParams[4] = DMOrbitalSurveyGenerator.fetchOrbitalScience(body);

			//Add in all acceptable paramaters to the contract
			foreach(DMCollectScience DMC in newParams)
			{
				if (DMC != null)
				{
					this.AddParameter(newParams[i], null);
					DMUtils.DebugLog("Parameter Added");
				}
				i++;
			}

			this.agent = Contracts.Agents.AgentList.Instance.GetAgent("DMagic");
			base.SetExpiry(10, Math.Max(15, 15) * (float)(this.prestige + 1));
			base.SetScience(newParams.Length * body.scienceValues.InSpaceLowDataValue * 2, body);
			base.SetDeadlineDays(20f * (float)(this.prestige + 1), body);
			base.SetReputation(newParams.Length * body.scienceValues.InSpaceLowDataValue * 0.5f, body);
			base.SetFunds(3000 * newParams.Length * body.scienceValues.InSpaceLowDataValue, 3000 * newParams.Length, 1000 * newParams.Length * body.scienceValues.InSpaceLowDataValue, body);
			return true;
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
			return body.name;
		}

		protected override string GetTitle()
		{
			return "Stupid Code Is Stupid";
		}

		protected override string GetDescription()
		{
			string story = DMUtils.storyList[rand.Next(0, DMUtils.storyList.Count)];
			return "Do Something!";
		}

		protected override string GetSynopsys()
		{
			DMUtils.DebugLog("Generating Synopsis From Target Body: [{0}]", body.theName);
			return string.Format("Conduct an orbital survey of {0} by collecting multiple science observations.", body.theName);
		}

		protected override string MessageCompleted()
		{
			return string.Format("You completed a survey of {0}, well done.", body.theName);
		}

		protected override void OnLoad(ConfigNode node)
		{
			DMUtils.DebugLog("Loading Contract");
			int target;
			target = int.Parse(node.GetValue("Orbital_Survey_Target"));
			body = FlightGlobals.Bodies[target];
		}

		protected override void OnSave(ConfigNode node)
		{
			DMUtils.DebugLog("Saving Contract");
			node.AddValue("Orbital_Survey_Target", body.flightGlobalsIndex);
		}

		public override bool MeetRequirements()
		{
			return true;
		}


	}
}
