#region license
/* DMagic Orbital Science - Seismometer Values
 * An object for storing information about seismic sensors
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
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using DMagic.Part_Modules;
using UnityEngine;

namespace DMagic
{
	public class DMSeismometerValues
	{
		private Vessel vesselRef;
		private ProtoPartSnapshot protoPartRef;
		private ProtoPartModuleSnapshot protoModuleRef;
		private float score;
		private float baseScore;
		private bool armed;
		private bool hammer;
		private bool onAsteroid;
		private uint id;
		private Dictionary<uint, Vector2> nearbySensors = new Dictionary<uint, Vector2>();

		public DMSeismometerValues(Vessel v, ProtoPartSnapshot pp, ProtoPartModuleSnapshot pm, bool h)
		{
			vesselRef = v;
			protoPartRef = pp;
			protoModuleRef = pm;
			id = pp.flightID;

			if (pm.moduleValues.HasValue("IsDeployed"))
				bool.TryParse(pm.moduleValues.GetValue("IsDeployed"), out armed);
			if (pm.moduleValues.HasValue("baseExperimentValue"))
				float.TryParse(pm.moduleValues.GetValue("baseExperimentValue"), out baseScore);

			hammer = h;

			DMUtils.DebugLog("Seismometer Values Loaded\nID: {0}\nArmed: {1}\nBase Value: {2:P1}\nHammer: {3}", id, armed, baseScore, hammer);
		}

		public void addSensor(uint i, Vector2 pos)
		{
			if (!nearbySensors.ContainsKey(i))
				nearbySensors.Add(i, pos);
			else
				nearbySensors[i] = pos;
		}

		public void removeSensor(uint i)
		{
			if (nearbySensors.ContainsKey(i))
				nearbySensors.Remove(i);
		}

		public void removeAllSensors()
		{
			nearbySensors.Clear();
		}

		public DMSeismometerValues getBestHammer()
		{
			if (nearbySensors.Count <= 0)
				return this;
			else
			{
				DMSeismometerValues bestHammer = null;
				float highScore = baseScore;

				for (int i = 0; i < nearbySensors.Count; i++)
				{
					uint id = nearbySensors.ElementAt(i).Key;

					DMSeismometerValues h = DMSeismicHandler.Instance.getSeismicHammer(id);

					if (h == null)
						continue;

					if (bestHammer == null)
						bestHammer = h;

					if (h.score > bestHammer.score)
						bestHammer = h;
				}

				return bestHammer;
			}
		}

		public void updateScore()
		{
			if (hammer)
			{
				if (onAsteroid)
				{
					if (vesselRef.loaded && vesselRef.FindPartModulesImplementing<DMSeismicSensor>().Count > 0)
					{
						score = 1f;
						return;
					}
					else
					{
						score = baseScore;
						return;
					}
				}

				if (nearbySensors.Count <= 0)
				{
					score = baseScore;
					return;
				}

				if (nearbySensors.Count >= 5)
				{
					if (nearbySensors.Where(s => s.Value.x > 10).Count() >= 2)
					{
						score = 1f;
						return;
					}
				}

				float nearbySensorBaseScore = Math.Min(0.2f, 0.1f * nearbySensors.Count);

				float nearSensorScore = 0f;
				float? nearSensorAngle = null;
				float farSensorScore = 0f;
				float? farSensorAngle = null;

				for (int i = 0; i < nearbySensors.Count; i++)
				{
					Vector2 v = nearbySensors.ElementAt(i).Value;

					if (v.x < DMSeismicHandler.nearPodMaxDistance)
					{
						float distanceScore = 0f;
						float angleScore = 0f;

						if (v.x < DMSeismicHandler.nearPodMinDistance)
						{
							distanceScore = 0f;
							angleScore = 0f;
						}
						else
						{
							if (v.x < DMSeismicHandler.nearPodThreshold)
								distanceScore = ((v.x - DMSeismicHandler.nearPodMinDistance) / (DMSeismicHandler.nearPodThreshold - DMSeismicHandler.nearPodMinDistance)) * 0.15f;
							else if (v.x >= DMSeismicHandler.nearPodThreshold)
								distanceScore = 0.15f;

							if (farSensorAngle == null)
								angleScore = 0.05f;
							else
							{
								float angleDelta = angleDifference(v.y, (float)farSensorAngle);

								if (angleDelta < DMSeismicHandler.podMinAngle)
									angleScore = 0f;
								else if (angleDelta < DMSeismicHandler.podAngleThreshold)
									angleScore = ((angleDelta - DMSeismicHandler.podMinAngle) / (DMSeismicHandler.podAngleThreshold - DMSeismicHandler.podMinAngle)) * 0.05f;
								else
									angleScore = 0.05f;
							}
						}

						float totalScore = distanceScore + angleScore;

						if (totalScore > nearSensorScore)
						{
							nearSensorScore = totalScore;
							nearSensorAngle = v.y;
						}
					}
					else
					{
						float distanceScore = 0f;
						float angleScore = 0f;

						if (v.x < DMSeismicHandler.farPodThreshold)
							distanceScore = ((v.x - DMSeismicHandler.farPodMinDistance) / (DMSeismicHandler.farPodThreshold - DMSeismicHandler.farPodMinDistance)) * 0.15f;
						else
							distanceScore = 0.15f;

						if (nearSensorAngle == null)
							angleScore = 0.05f;
						else
						{
							float angleDelta = angleDifference(v.y, (float)nearSensorAngle);

							if (angleDelta < DMSeismicHandler.podMinAngle)
								angleScore = 0f;
							else if (angleDelta < DMSeismicHandler.podAngleThreshold)
								angleScore = ((angleDelta - DMSeismicHandler.podMinAngle) / (DMSeismicHandler.podAngleThreshold - DMSeismicHandler.podMinAngle)) * 0.05f;
							else
								angleScore = 0.05f;
						}

						float totalScore = distanceScore + angleScore;

						if (totalScore > farSensorScore)
						{
							farSensorScore = totalScore;
							farSensorAngle = v.y;
						}
					}
					score = Mathf.Clamp(baseScore + nearbySensorBaseScore + nearSensorScore + farSensorScore, 0f, 1f);
				}
			}
			else
			{
				if (onAsteroid)
				{
					if (vesselRef.loaded && vesselRef.FindPartModulesImplementing<DMSeismicHammer>().Count > 0)
					{
						score = 1f;
						return;
					}
					else
					{
						score = baseScore;
						return;
					}
				}

				if (nearbySensors.Count <= 0)
					score = baseScore;
				else
				{
					float highScore = baseScore;

					for (int i = 0; i < nearbySensors.Count; i++)
					{
						uint id = nearbySensors.ElementAt(i).Key;

						DMSeismometerValues h = DMSeismicHandler.Instance.getSeismicHammer(id);

						if (h == null)
							continue;

						if (h.score > highScore)
							highScore = h.score;
					}
					score = highScore;
				}
			}
		}

		private float angleDifference(float angle1, float angle2)
		{
			float f = angle1 - angle2;
			while (f < -180)
				f += 360;
			while (f > 180)
				f -= 360;

			return f;
		}

		public float Score
		{
			get { return score; }
		}

		public uint ID
		{
			get { return id; }
		}

		public int NearbySensorCount
		{
			get { return nearbySensors.Count; }
		}

		public bool Armed
		{
			get { return armed; }
			set { armed = value; }
		}

		public bool OnAsteroid
		{
			get { return onAsteroid; }
			set { onAsteroid = value; }
		}

		public bool Hammer
		{
			get { return hammer; }
		}

		public ProtoPartSnapshot ProtoPartRef
		{
			get { return protoPartRef; }
		}

		public ProtoPartModuleSnapshot ProtoModuleRef
		{
			get { return protoModuleRef; }
		}

		public Vessel VesselRef
		{
			get { return vesselRef; }
		}
	}
}
