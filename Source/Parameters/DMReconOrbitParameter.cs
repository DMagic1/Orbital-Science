using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DMagic.Contracts;
using Contracts;
using FinePrint.Utilities;
using FinePrint.Contracts.Parameters;

namespace DMagic.Parameters
{
	public class DMReconOrbitParameter : ContractParameter
	{
		private DMSpecificOrbitParameterExtended childOrbitParameter;
		private CelestialBody body;
		OrbitType type;
		double inc;
		double ecc;
		double sma;
		double lan;
		double aop;
		double mae;
		double epo;
		double deviation;
		OrbitDriver orbitDriver;
		private DMLongOrbitParameter root;

		public DMReconOrbitParameter() { }

		public DMReconOrbitParameter(OrbitType orbitType, double inclination, double eccentricity, double semi, double la, double argumentOfPeriapsis, double meanAnomalyAtEpoch, double epoch, CelestialBody targetBody, double deviationWindow, DMLongOrbitParameter r)
		{
			type = orbitType;
			body = targetBody;
			inc = inclination;
			ecc = eccentricity;
			sma = semi;
			lan = la;
			aop = argumentOfPeriapsis;
			mae = meanAnomalyAtEpoch;
			epo = epoch;
			deviation = deviationWindow;
			root = r;
			disableOnStateChange = false;
			setupOrbit();
		}

		protected override string GetTitle()
		{
			return childOrbitParameter.BaseTitle;
		}

		protected override string GetNotes()
		{
			return childOrbitParameter.BaseNotes + "\nNote that the target orbit will not disappear until the full contract has been completed.";
		}

		private void setupOrbit()
		{
			orbitDriver = new OrbitDriver();
			orbitDriver.orbit = new Orbit();
			orbitDriver.orbit.referenceBody = body;
			orbitDriver.orbit.inclination = inc;
			orbitDriver.orbit.eccentricity = ecc;
			orbitDriver.orbit.argumentOfPeriapsis = aop;
			orbitDriver.orbit.semiMajorAxis = sma;
			orbitDriver.orbit.LAN = lan;
			orbitDriver.orbit.meanAnomalyAtEpoch = mae;
			orbitDriver.orbit.epoch = epo;
			orbitDriver.orbit.Init();
		}

		protected override void OnUpdate()
		{
			if (HighLogic.LoadedSceneIsEditor)
				return;

			if (orbitDriver == null)
			{
				this.SetIncomplete();
				return;
			}

			if (orbitDriver.orbit == null)
			{
				this.SetIncomplete();
				return;
			}

			if (root == null)
			{
				this.SetIncomplete();
				return;
			}

			for (int i = 0; i < root.VesselCount; i++)
			{
				Vessel v = root.GetVessel(i);

				if (v == null)
					continue;

				if (VesselUtilities.VesselAtOrbit(orbitDriver.orbit, deviation, v))
				{
					this.SetComplete();
					return;
				}
			}

			this.SetIncomplete();
		}

		protected override void OnLoad(ConfigNode node)
		{
			string[] values = node.GetValue("Recon_Values").Split('|');

			if (values.Length != 10)
			{
				loadFail("Error while loading DMReconOrbitParameter; Removing Now...");
				return;
			}

			int target;
			if (int.TryParse(values[0], out target))
			{
				if (FlightGlobals.Bodies.Count >= target)
					body = FlightGlobals.Bodies[target];
				else
				{
					loadFail(string.Format("No Celestial Body Found For Index: [{0}] For Recon Orbit Parameter; Removing Now...", target));
					return;
				}
			}
			else
			{
				loadFail("");
				return;
			}

			int t = DMUtils.parseValue(values[1], (int)2);
			type = (OrbitType)t;

			inc = DMUtils.parseValue(values[2], (double)75);
			ecc = DMUtils.parseValue(values[3], (double)0);
			sma = DMUtils.parseValue(values[4], (double)0);
			aop = DMUtils.parseValue(values[5], (double)0);
			mae = DMUtils.parseValue(values[6], (double)0);
			epo = DMUtils.parseValue(values[7], (double)0);
			lan = DMUtils.parseValue(values[8], (double)0);
			deviation = DMUtils.parseValue(values[9], (double)10);

			if (this.ParameterCount <= 0)
			{
				loadFail("");
				return;
			}

			try
			{
				childOrbitParameter = (DMSpecificOrbitParameterExtended)this.GetParameter(0);
			}
			catch (Exception e)
			{
				loadFail("Could not find child specific orbit parameter; removing DMReconOrbit Parameter\n" + e.ToString());
				return;
			}

			try
			{
				root = (DMLongOrbitParameter)Parent;
			}
			catch (Exception e)
			{
				loadFail("Could not find root long orbit parameter; removing DMReconOrbit Parameter\n" + e.ToString());
				return;
			}

			disableOnStateChange = false;

			setupOrbit();
		}

		protected override void OnSave(ConfigNode node)
		{
			node.AddValue("Recon_Values", string.Format("{0}|{1}|{2:N5}|{3:N5}|{4:N5}|{5:N5}|{6:N5}|{7:N5}|{8:N5}|{9:N5}", body.flightGlobalsIndex, (int)type, inc, ecc, sma, aop, mae, epo, lan, deviation));
		}

		private void loadFail(string message)
		{
			this.Unregister();
			this.Parent.RemoveParameter(this);
			DMUtils.Logging(message);
		}

	}
}
