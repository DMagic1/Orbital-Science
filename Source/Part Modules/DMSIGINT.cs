using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DMagic.Part_Modules
{
	public class DMSIGINT : DMBreakablePart, IDMSurvey
	{
		private readonly string[] dishTransformNames = new string[5] { "dish000", "dish001", "dish002", "dish003", "focalColumn" };

		private List<Transform> dishTransforms = new List<Transform>();

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);

			assignTransforms();

			parseTransfrom(part.transform);
		}

		private void assignTransforms()
		{
			for (int i = 0; i < 5; i++)
			{
				string s = dishTransformNames[i];

				if (string.IsNullOrEmpty(s))
					continue;

				Transform t = part.FindModelTransform(s);

				if (t == null)
					continue;

				dishTransforms.Add(t);
			}
		}

		public void scanPlanet(CelestialBody b)
		{
			DMAnomalyStorage anom = DMAnomalyList.getAnomalyStorage(b.name);

			if (anom == null)
				anom = new DMAnomalyStorage(b, false);

			if (anom.Scanned)
				return;

			if (anom.scanBody())
				DMAnomalyList.addAnomalyStorage(b.name, anom);
		}

		protected override void getGameObjects()
		{
			if (!breakable)
				return;

			for (int i = 0; i < dishTransforms.Count; i++)
			{
				Transform t = dishTransforms[i];

				if (t == null)
					continue;

				GameObject obj = t.gameObject;

				if (obj == null)
					continue;

				if (obj.GetComponent<SkinnedMeshRenderer>() == null && obj.GetComponent<MeshRenderer>() == null)
					continue;

				breakableObjects.Add(obj);
			}			
		}

		protected override void setTransformState(bool on)
		{
			if (!breakable)
				return;

			for (int i = 0; i < dishTransforms.Count; i++)
			{
				Transform t = dishTransforms[i];

				if (t == null)
					continue;

				if (t.gameObject == null)
					continue;

				t.gameObject.SetActive(on);
			}
		}

		private void parseTransfrom(Transform t)
		{
			DMUtils.Logging("Parsing Transfrom [{0}]", t.name);

			for (int i = 0; i < t.childCount; i++)
			{
				Transform tChild = t.GetChild(i);

				if (tChild == null)
					continue;

				DMUtils.Logging("Transfrom [{0}] Child [{1}] Of Parent [{2}]", tChild.name, i, t.name);

				parseTransfrom(tChild);
			}
		}
	}
}
