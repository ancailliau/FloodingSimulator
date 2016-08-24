using System;
using System.IO;
using System.Linq;

namespace Simulator
{
	public class SummaryProcessor : IMonitorProcessor
	{
		StreamWriter summaryWriter;

		public SummaryProcessor (string summaryFilename)
		{
			summaryWriter = new StreamWriter (summaryFilename);
		}

		public void Process (Monitor monitor)
		{
			if (!monitor.Ready)
				return;
			
			try {
				foreach (var m in monitor.ltlMonitors) {
					var min = m.Min;
					var max = m.Max;

					summaryWriter.WriteLine ("{0} ({1})", "", m.Formula);
					summaryWriter.WriteLine ("    Min: [{0:0.##}, {1:0.##}] (Avg: {2:0.##}, Id: {3}, Monitors: {4}/{5}/{6})",
									 Math.Max (0, min.Mean - 1.64 * min.StdDev),
									 Math.Min (1, min.Mean + 1.64 * min.StdDev),
									 min.Mean,
									 min.Hash,
									 min.Positive, min.Negative, m.currents.Count (x => x.stateHash == min.Hash)
									);
					summaryWriter.WriteLine ("    Max: [{0:0.##}, {1:0.##}] (Avg: {2:0.##}, Id: {3}, Monitors: {4}/{5}/{6})",
									 Math.Max (0, max.Mean - 1.64 * max.StdDev),
									 Math.Min (1, max.Mean + 1.64 * max.StdDev),
									 max.Mean,
									 max.Hash,
					                 max.Positive, max.Negative, m.currents.Count (x => x.stateHash == max.Hash)
									);

					//summaryWriter.WriteLine ("-");
					//summaryWriter.WriteLine (m.storage.PrintDebug ());
					//summaryWriter.WriteLine ("-");
				}

			} catch (Exception e) {
				summaryWriter.WriteLine (e);
			}

			summaryWriter.WriteLine ("---");
			summaryWriter.Flush ();
		}
	}
}

