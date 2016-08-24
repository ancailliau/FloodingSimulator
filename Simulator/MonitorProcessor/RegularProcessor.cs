using System;
using System.Collections.Generic;
using System.Threading;

namespace Simulator
{
	public class RegularProcessor
	{
		Monitor monitor;

		Dictionary<IMonitorProcessor, Timer> processors;

		public RegularProcessor (Monitor monitor)
		{
			this.monitor = monitor;

			processors = new Dictionary<IMonitorProcessor, Timer> ();
		}

		public void AddProcessor (IMonitorProcessor dotExport, TimeSpan timeSpan)
		{
			var tc = new TimerCallback (_ => { dotExport.Process (monitor); });
			var t = new Timer (tc, null, TimeSpan.Zero, timeSpan);
			processors.Add (dotExport, t);
		}

	}
}

