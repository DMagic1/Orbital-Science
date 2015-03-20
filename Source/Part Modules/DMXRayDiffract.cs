#region license
/* DMagic Orbital Science - X Ray Diffraction
 * Some CheMin-Specific Code On Top Of DMModuleScienceAnimate
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
 *  
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DMagic.Part_Modules
{
	class DMXRayDiffract : DMModuleScienceAnimate
	{
		private const string drillTransform = "SampleDrill";
		private const string potato = "PotatoRoid";

		protected override bool canConduct()
		{
			if (base.canConduct())
			{
				if (drillImpact(asteroidReports && DMAsteroidScience.AsteroidGrappled))
					return true;
				failMessage = "The drill cannot impact the surface from this distance";
			}
			return false;
		}

		private bool drillImpact(bool b)
		{
			RaycastHit hit = new RaycastHit();
			Transform t = part.FindModelTransform(drillTransform);
			Vector3 p = t.position;
			Ray r = new Ray(p, -1f * t.forward);
			float scale = part.rescaleFactor;
			if (part.Modules.Contains("TweakScale"))
			{
				PartModule pM = part.Modules["TweakScale"];
				if (pM.Fields.GetValue("currentScale") != null)
				{
					float tweakedScale = 1f;
					try
					{
						tweakedScale = pM.Fields.GetValue<float>("currentScale");
						DMUtils.Logging("TweakScale Value Detected On XRay Instrument; Drill Length Set To 8.8m * {0}", tweakedScale);
					}
					catch
					{
						DMUtils.Logging("Error in TweakScale PartModule Field; Resetting TweakScale Factor to 1");
					}
					scale *= tweakedScale;
				}
			}
			float drillLength = 4f * scale;
			Physics.Raycast(r, out hit, drillLength);
			if (hit.collider != null)
			{
				if (b)
				{
					DMUtils.DebugLog("Checking Asteroid Surface");
					string obj = hit.collider.attachedRigidbody.gameObject.name;
					return (obj.StartsWith(potato));
				}
				else
				{
					Transform hitT = hit.collider.transform;
					int i = 0; //Just to prevent this from getting stuck in a loop
					while (hitT != null && i < 200)
					{
						DMUtils.DebugLog("Transform: {0} Hit", hitT.name);
						if (hitT.name.Contains(vessel.mainBody.name))
							return true;
						hitT = hitT.parent;
						i++;
					}
				}
			}
			return false;
		}
	}
}
