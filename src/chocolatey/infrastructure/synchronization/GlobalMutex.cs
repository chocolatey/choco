// Copyright © 2017 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// 
// You may obtain a copy of the License at
// 
// 	http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace chocolatey.infrastructure.synchronization
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading;
	using System.Runtime.InteropServices;
	using System.Reflection;
	using System.Security.AccessControl;
	using System.Security.Principal;

	/// <summary>
  ///   global mutex used for synchronizing multiple processes based on appguid
  /// </summary>
  /// <remarks>
	///   Based on http://stackoverflow.com/a/7810107/2279385
  /// </remarks>
	public class GlobalMutex : IDisposable
	{
		public bool hasHandle = false;
		private Mutex mutex;

		private void InitMutex()
		{
			string appGuid = ((GuidAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(GuidAttribute), false).GetValue(0)).Value.ToString();
			string mutexId = string.Format("Global\\{{{0}}}", appGuid);
			mutex = new Mutex(false, mutexId);

			var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
			var securitySettings = new MutexSecurity();
			securitySettings.AddAccessRule(allowEveryoneRule);
			mutex.SetAccessControl(securitySettings);
		}

		public GlobalMutex(int timeOut)
		{
			InitMutex();
			try
			{
				if (timeOut < 0)
				{
					hasHandle = mutex.WaitOne(Timeout.Infinite, false);
				}
				else
				{
					hasHandle = mutex.WaitOne(timeOut, false);
				}

				if (hasHandle == false)
				{
					throw new TimeoutException("Timeout waiting for exclusive access on SingleInstance");
				}
			}
			catch (AbandonedMutexException)
			{
				hasHandle = true;
			}
		}


		public void Dispose()
		{
			if (mutex != null)
			{
				if (hasHandle)
				{
					mutex.ReleaseMutex();
				}
				mutex.Dispose();
			}
		}
	}


}
