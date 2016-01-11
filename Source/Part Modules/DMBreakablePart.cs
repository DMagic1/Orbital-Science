using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DMagic.Part_Modules
{
	class DMBreakablePart : DMModuleScienceAnimate
	{
		private List<GameObject> breakableObjects = new List<GameObject>();
		private Transform baseTransform;

		[KSPField(isPersistant = true)]
		public bool broken;
		[KSPField]
		public string baseTransfromName = "";

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);

			if (!string.IsNullOrEmpty(baseTransfromName))
				baseTransform = part.FindModelTransform(baseTransfromName);

			if (broken)
			{
				setTransformState(false);
			}
		}

		public override void OnFixedUpdate()
		{
			base.OnFixedUpdate();
		}

		private void setTransformState(bool on)
		{
			if (baseTransform == null)
				return;

			baseTransform.gameObject.SetActive(on);
		}

		private void breakObjects()
		{
			if (broken)
				return;

			getChildren(baseTransform);

			for (int i = 0; i < breakableObjects.Count; i++)
			{
				GameObject o = breakableObjects[i];

				if (o == null)
					continue;

				Rigidbody r = o.AddComponent<Rigidbody>();

				if (r == null)
					continue;

				r.angularVelocity = part.rigidbody.angularVelocity;
				r.velocity = part.rigidbody.velocity;
				r.mass = 0.001f;
				r.useGravity = false;
				o.transform.parent = null;
			}
		}

		private void getChildren(Transform t)
		{
			if (t == null)
				return;

			var tEnum = t.GetEnumerator();

			while (tEnum.MoveNext())
			{
				Transform tChild = (Transform)tEnum.Current;

				if (tChild == null)
					continue;

				GameObject obj = tChild.gameObject;

				if (obj == null)
					continue;

				if (obj.GetComponent<MeshRenderer>() == null)
					continue;

				breakableObjects.Add(obj);

				getChildren(tChild);
			}
		}

		protected void onBreak()
		{
			breakObjects();

			broken = true;
		}

		protected void onFix()
		{
			broken = false;

			setTransformState(true);
		}
	}
}
