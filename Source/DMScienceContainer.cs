#region license
/* DMagic Orbital Science - DMScienceContainer
 * Object to store experiment properties
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

namespace DMagic
{
	public class DMScienceContainer
	{
		private int sitMask, bioMask;
		private float transmit;
		private DMScienceType type;
		private ScienceExperiment exp;
		private string sciPart, agent;

		internal DMScienceContainer(ScienceExperiment sciExp, int sciSitMask, int sciBioMask, DMScienceType Type,  string sciPartID, string agentName, float Transmit)
		{
			sitMask = sciSitMask;
			bioMask = sciBioMask;
			exp = sciExp;
			sciPart = sciPartID;
			agent = agentName;
			type = Type;
			transmit = Transmit;
		}

		public int SitMask
		{
			get { return sitMask; }
		}

		public int BioMask
		{
			get { return bioMask; }
		}

		public float Transmit
		{
			get { return transmit; }
		}

		public DMScienceType DMType
		{
			get { return type; }
		}

		public ScienceExperiment Exp
		{
			get { return exp; }
		}

		public string SciPart
		{
			get { return sciPart; }
		}

		public string Agent
		{
			get { return agent; }
		}
	}

	public enum DMScienceType
	{
		All = 0,
		Surface = 1,
		Aerial = 2,
		Space = 4,
		Biological = 8,
		Asteroid = 16,
		Anomaly = 32,
	}
}
