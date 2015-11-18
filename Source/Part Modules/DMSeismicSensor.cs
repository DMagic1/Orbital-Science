using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DMagic.Part_Modules
{
	public class DMSeismicSensor : DMBasicScienceModule, IDMSeismometer
	{
		[KSPField]
		public string animationName = "";

		private Dictionary<uint, DMSeismicHammer> nearbyHammers = new Dictionary<uint, DMSeismicHammer>();

		private Animation Anim;

		public override void OnStart(PartModule.StartState state)
		{
			if (!string.IsNullOrEmpty(animationName))
				Anim = part.FindModelAnimators(animationName)[0];
			if (!string.IsNullOrEmpty(experimentID))
				exp = ResearchAndDevelopment.GetExperiment(experimentID);
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);
		}

		public override void OnSave(ConfigNode node)
		{
			base.OnSave(node);
		}

		private void Update()
		{
			base.EventsCheck();
		}

		#region Animator

		//Controls the main, door-opening animation
		private void animator(float speed, float time, Animation a, string name)
		{
			if (a != null)
			{
				a[name].speed = speed;
				if (!a.IsPlaying(name))
				{
					a[name].normalizedTime = time;
					a.Blend(name, 1f);
				}
			}
		}


		#endregion

		public void updatePosition()
		{

		}

		public void addSeismometer(uint id, IDMSeismometer sensor, float distance)
		{
			if (nearbyHammers.ContainsKey(id))
				return;

			nearbyHammers.Add(id, (DMSeismicHammer)sensor);
		}

	}
}
