using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Contracts;
using FinePrint.Utilities;
using FinePrint.Contracts.Parameters;

namespace DMagic.Parameters
{
	public class DMSpecificOrbitParameterExtended : SpecificOrbitParameter
	{
		private string baseTitle = "";
		private string baseNotes = "";

		public DMSpecificOrbitParameterExtended() { }

		public DMSpecificOrbitParameterExtended(OrbitType orbitType, double inclination, double eccentricity, double sma, double lan, double argumentOfPeriapsis, double meanAnomalyAtEpoch, double epoch, CelestialBody targetBody, double deviationWindow) : base(orbitType, inclination, eccentricity, sma, lan, argumentOfPeriapsis, meanAnomalyAtEpoch, epoch, targetBody, deviationWindow)
		{
			
		}

		protected override void OnUpdate()
		{
			if (this.Root.ContractState != Contract.State.Active)
				return;

			if (HighLogic.LoadedSceneIsEditor)
				return;

			if (this.state != ParameterState.Incomplete)
				this.SetIncomplete();

			return;
		}

		protected override string GetNotes()
		{
			baseNotes = base.GetNotes();

			return "";
		}

		protected override string GetTitle()
		{
			baseTitle = base.GetTitle();

			return "";
		}

		public string BaseTitle
		{
			get { return baseTitle; }
		}

		public string BaseNotes
		{
			get { return baseNotes; }
		}
	}
}
