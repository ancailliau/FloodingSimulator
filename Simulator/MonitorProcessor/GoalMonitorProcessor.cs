using System;
using System.Collections.Generic;
using System.Threading;

namespace Simulator
{
	public class GoalMonitorProcessor
	{
		GoalMonitor monitor;

		Dictionary<IGoalMonitorProcessor, Timer> processors;

		public GoalMonitorProcessor (GoalMonitor monitor)
		{
			this.monitor = monitor;

			processors = new Dictionary<IGoalMonitorProcessor, Timer> ();
		}

		public void AddProcessor (IGoalMonitorProcessor dotExport, TimeSpan timeSpan)
		{
			var tc = new TimerCallback (_ => { dotExport.Process (monitor); });
			var t = new Timer (tc, null, TimeSpan.Zero, timeSpan);
			processors.Add (dotExport, t);
		}

	}
}

