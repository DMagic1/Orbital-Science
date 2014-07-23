using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DMagic
{
	static class DMUtils
	{
		internal static System.Random rand;
		internal static Dictionary<string, DMcontractScience> availableScience = new Dictionary<string, DMcontractScience>();
		internal static float science, reward, forward, penalty;

		internal static void Logging(string s, params object[] stringObjects)
		{
			s = string.Format(s, stringObjects);
			string finalLog = string.Format("[DM] {0}", s);
			Debug.Log(finalLog);
		}

		#region Debug Logging
#if DEBUG

		internal static void DebugLog(string s, params object[] stringObjects)
		{
			s = string.Format(s, stringObjects);
			string finalLog = string.Format("[DM] {0}", s);
			Debug.Log(finalLog);
		}

#endif
		#endregion


	}
}
