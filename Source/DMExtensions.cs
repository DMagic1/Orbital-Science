#region license
/* DMagic Orbital Science - DMExtensions
 * Some extensions
 *
 * Copyright (c) 2016, David Grandy <david.grandy@gmail.com>
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

namespace DMagic
{
	public static class DMExtensions
	{

		public static string parse(this ConfigNode node, string name, string original)
		{
			if (node.HasValue(name))
			{
				//DMUtils.DebugLog("Parsing value [{0}] = {1}", name, node.GetValue(name));
				return node.GetValue(name);
			}

			return original;
		}

		public static int parse(this ConfigNode node, string name, int original)
		{
			if (!node.HasValue(name))
				return original;

			int i = original;

			if (int.TryParse(node.GetValue(name), out i))
			{
				//DMUtils.DebugLog("Parsing value [{0}] = {1}", name, i);
				return i;
			}

			return original;
		}

		public static uint parse(this ConfigNode node, string name, uint original)
		{
			if (!node.HasValue(name))
				return original;

			uint i = original;

			if (uint.TryParse(node.GetValue(name), out i))
				return i;

			return original;
		}

		public static float parse(this ConfigNode node, string name, float original)
		{
			if (!node.HasValue(name))
				return original;

			float f = original;

			if (float.TryParse(node.GetValue(name), out f))
			{
				//DMUtils.DebugLog("Parsing value [{0}] = {1}", name, f);
				return f;
			}

			return original;
		}

		public static float? parse(this ConfigNode node, string name, float? original)
		{
			if (!node.HasValue(name))
				return original;

			float f = original == null ? 0 : (float)original;

			if (float.TryParse(node.GetValue(name), out f))
				return (float?)f;

			return original;
		}

		public static double parse(this ConfigNode node, string name, double original)
		{
			if (!node.HasValue(name))
				return original;

			double d = original;

			if (double.TryParse(node.GetValue(name), out d))
			{
				//DMUtils.DebugLog("Parsing value [{0}] = {1}", name, d);
				return d;
			}

			return original;
		}

		public static bool parse(this ConfigNode node, string name, bool original)
		{
			if (!node.HasValue(name))
				return original;

			bool b = original;

			if (bool.TryParse(node.GetValue(name), out b))
			{
				//DMUtils.DebugLog("Parsing value [{0}] = {1}", name, b);
				return b;
			}

			return original;
		}

		public static List<string> parse(this ConfigNode node, string name, char separator, List<string> original)
		{
			if (!node.HasValue(name))
				return original;

			//DMUtils.DebugLog("Parsing value [{0}] = {1}", name, node.GetValue(name));

			return node.GetValue(name).Split(separator).ToList();
		}

		public static Vector2d parse(this ConfigNode node, string name, Vector2d original)
		{
			if (!node.HasValue(name))
				return original;

			Vector2d v = original;

			string[] values = node.GetValue(name).Split('|');

			if (values.Length != 2)
				return v;

			double first = original.x;
			double second = original.y;

			if (!double.TryParse(values[0], out first))
				first = original.x;

			if (!double.TryParse(values[1], out second))
				second = original.y;

			v.x = first;
			v.y = second;

			return v;
		}

		public static Guid parse(this ConfigNode node, string name, Guid original)
		{
			if (!node.HasValue(name))
				return original;

			Guid g = original;

			try
			{
				g = new Guid(node.GetValue(name));
			}
			catch (Exception e)
			{
				Debug.LogError("Error while reading Guid value; creating new value...\n" + e);
			}

			return g;
		}

		public static List<Guid> parse(this ConfigNode node, string name, List<Guid> original)
		{
			if (!node.HasValue(name))
				return original;

			string source = node.GetValue(name);

			if (string.IsNullOrEmpty(source))
				return original;
			else
			{
				List<Guid> ids = new List<Guid>();

				string[] sA = source.Split('|');
				for (int i = 0; i < sA.Length; i++)
				{
					try
					{
						Guid g = new Guid(sA[i]);

						if (g == null)
							continue;

						if (!ids.Contains(g))
							ids.Add(g);
					}
					catch (Exception e)
					{
						DMUtils.Logging("Guid invalid:\n{0}", e);
						continue;
					}
				}

				return ids;
			}
		}

		public static List<Guid> parse(this string name, List<Guid> original)
		{
			if (string.IsNullOrEmpty(name))
				return original;
			else
			{
				List<Guid> ids = new List<Guid>();

				string[] sA = name.Split('|');
				for (int i = 0; i < sA.Length; i++)
				{
					try
					{
						Guid g = new Guid(sA[i]);

						if (g == null)
							continue;

						if (!ids.Contains(g))
							ids.Add(g);
					}
					catch (Exception e)
					{
						DMUtils.Logging("Guid invalid:\n{0}", e);
						continue;
					}
				}

				return ids;
			}
		}

		public static Vessel parse(this ConfigNode node, string name, Vessel original)
		{
			if (!node.HasValue(name))
				return original;

			Vessel v = original;

			string s = node.GetValue(name);

			if (string.IsNullOrEmpty(s))
				return original;
			else
			{
				try
				{
					Guid id = new Guid(s);

					v = FlightGlobals.Vessels.FirstOrDefault(a => a.id == id);

					if (v == null)
						return original;
				}
				catch (Exception e)
				{
					DMUtils.Logging("Vessel invalid:\n{0}", e);
					return original;
				}
			}

			return v;
		}

		public static CelestialBody parse(this ConfigNode node, string name, CelestialBody original)
		{
			if (!node.HasValue(name))
				return original;

			CelestialBody c = original;

			string s = node.GetValue(name);

			int body;

			if (!int.TryParse(s, out body))
				return c;

			if (FlightGlobals.Bodies.Count > body)
				c = FlightGlobals.Bodies[body];
			else
			{
				//DMUtils.DebugLog("Parsing value [{0}] = {1}", name, c.displayName);
				return original;
			}

			return c;
		}

		public static ScienceSubject parse(this ConfigNode node, string name, ScienceSubject original)
		{
			if (!node.HasValue(name))
				return original;

			ScienceSubject subject = original;

			string id = node.GetValue(name);

			subject = ResearchAndDevelopment.GetSubjectByID(id);

			if (subject != null)
				return subject;

			return original;
		}

		public static string vector2ToString(this Vector2d v)
		{
			return v.x.ToString("F6") + "|" + v.y.ToString("F6");
		}
	}
}
