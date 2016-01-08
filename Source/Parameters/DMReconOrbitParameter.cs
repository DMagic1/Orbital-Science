using System;
using System.Collections;
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
		private OrbitType type;
		private double inc, ecc, sma, lan, aop, mae, epo, deviation;
		private OrbitDriver orbitDriver;
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
			return childOrbitParameter.BaseNotes + "\nThe target orbit will not disappear until the full contract has been completed.";
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
			if (this.Root.ContractState != Contract.State.Active)
				return;

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
			body = node.parse("Body", (CelestialBody)null);
			if (body == null)
			{
				loadFail("Failed To Load Target Body; DMRecon Parameter Removed");
				return;
			}

			int oType = node.parse("OrbitType", (int)1000);
			if (oType == 1000)
			{
				loadFail("Failed To Orbit Type; DMRecon Parameter Removed");
				return;
			}
			type = (OrbitType)oType;

			inc = node.parse("Inclination", (double)63.5);
			ecc = node.parse("Eccentricity", (double)0);
			sma = node.parse("SemiMajorAxis", (double)0);
			aop = node.parse("ArgOfPeriapsis", (double)0);
			mae = node.parse("MeanAnomalyAtEpoch", (double)0);
			epo = node.parse("Epoch", (double)0);
			lan = node.parse("LAN", (double)0);
			deviation = node.parse("Deviation", (double)10);

			try
			{
				root = (DMLongOrbitParameter)Parent;
			}
			catch (Exception e)
			{
				loadFail("Could not find root long orbit parameter; removing DMReconOrbit Parameter\n" + e.ToString());
				return;
			}

			ContractSystem.Instance.StartCoroutine(loadChildParameter());

			disableOnStateChange = false;

			setupOrbit();
		}

		private IEnumerator loadChildParameter()
		{
			int timer = 0;
			while (this.ParameterCount < 1 && timer < 200)
			{
				timer++;
				yield return null;
			}

			if (timer >= 200)
			{
				loadFail("Could not find child specific orbit parameter; timed out; removing DMReconOrbit Parameter");
				yield break;
			}

			try
			{
				childOrbitParameter = this.GetParameter<DMSpecificOrbitParameterExtended>();
			}
			catch (Exception e)
			{
				loadFail("Could not find child specific orbit parameter; removing DMReconOrbit Parameter\n" + e.ToString());
				yield break;
			}

			if (childOrbitParameter == null)
				loadFail("Could not find child specific orbit parameter; removing DMReconOrbit Parameter");
		}

		protected override void OnSave(ConfigNode node)
		{
			node.AddValue("Body", body.flightGlobalsIndex);
			node.AddValue("OrbitalType", (int)type);
			node.AddValue("Inclination", inc.ToString("N5"));
			node.AddValue("Eccentricity", ecc.ToString("N5"));
			node.AddValue("SemiMajorAxis", sma.ToString("N5"));
			node.AddValue("ArgOfPeriapsis", aop.ToString("N5"));
			node.AddValue("LAN", lan.ToString("N5"));
			node.AddValue("MeanAnomalyAtEpoch", mae.ToString("N5"));
			node.AddValue("Epoch", epo.ToString("N5"));
			node.AddValue("Deviation", deviation.ToString("N5"));
		}

		private void loadFail(string message)
		{
			this.Unregister();
			this.Parent.RemoveParameter(this);
			DMUtils.Logging(message);
		}

	}
}
