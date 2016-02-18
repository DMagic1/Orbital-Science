#region license
/* DMagic Orbital Science - DMCompleteParameter
 * Parameter To Hold Science Collection Sub-Parameters
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Contracts;
using Contracts.Parameters;
using DMagic.Contracts;

namespace DMagic.Parameters
{
	public class DMCompleteParameter : ContractParameter
	{
		private int type;
		private int minus;
		private int subParamCountToComplete;

		public DMCompleteParameter()
		{
		}

		public DMCompleteParameter(int Type, int i)
		{
			type = Type;
			minus = i;
		}

		public void addToSubParams(ContractParameter cp)
		{
			AddParameter(cp);
			subParamCountToComplete = ParameterCount - minus;
		}

		protected override string GetTitle()
		{
			if (ParameterCount == subParamCountToComplete)
				return "Return or transmit data from the following experiments:";
			else
				return string.Format("Return or transmit data from at least {0} of the following experiments:", subParamCountToComplete);
		}

		protected override string GetNotes()
		{
			switch(type)
			{
				case 0:
					return "The following experiments must be returned or transmitted with some science value; if no science value is remaining the results must be returned home.";
				case 1:
					return "The following experiments can be conducted at any time during the contract.";
				case 2:
					return "An on-screen message will indicate successful collection of asteroid data from the following experiments; return or transmit the results to complete each objective.";
				case 3:
					return "An on-screen message will indicate successful collection of data while near to, or above, the target; return or transmit the results to complete each objective.";
			}

			return "";
		}

		protected override void OnRegister()
		{
			GameEvents.Contract.onParameterChange.Add(onParamChange);
		}

		protected override void OnUnregister()
		{
			GameEvents.Contract.onParameterChange.Remove(onParamChange);
		}

		protected override void OnSave(ConfigNode node)
		{
			node.AddValue("Type", type);
			node.AddValue("Count_To_Complete", subParamCountToComplete);
		}

		protected override void OnLoad(ConfigNode node)
		{
			type = node.parse("Type", (int)1000);
			if (type == 1000)
			{
				DMUtils.Logging("Failed To Load Parent Parameter Variables; Parent Parameter Removed");
				this.Unregister();
				this.Parent.RemoveParameter(this);
				return;
			}

			subParamCountToComplete = node.parse("Count_To_Complete", (int)2);
		}

		private int SubParamCompleted()
		{
			return AllParameters.Where(p => p.State == ParameterState.Complete).Count();
		}

		private void onParamChange(Contract c, ContractParameter p)
		{
			if (this.Root.ContractState != Contract.State.Active)
				return;

			if (c == null)
				return;

			if (c != this.Root)
				return;
	
			if (SubParamCompleted() >= subParamCountToComplete)
				this.SetComplete();
		}

	}
}
