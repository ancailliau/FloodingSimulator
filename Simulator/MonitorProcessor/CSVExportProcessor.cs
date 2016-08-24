using System;
using System.IO;

namespace Simulator
{
	public class CSVExportProcessor : IMonitorProcessor
	{
		string filename;

		public CSVExportProcessor (string filename)
		{
			this.filename = filename;

			if (File.Exists (filename)) {
				File.Delete (filename);
			}
		}

		public void Process (Monitor monitor)
		{
			if (!monitor.Ready)
				return;
			
			string [] columns = new string [monitor.ltlMonitors.Count * 3 + 1];

			columns [0] = DateTime.Now.ToString ("hh:mm:ss.ffff");
			int i = 1;
			foreach (var m in monitor.ltlMonitors) {
				var min = m.Min;
				columns [i] = string.Format ("{0:0.0000}", min.Mean);
				columns [i + 1] = string.Format ("{0:0.0000}", Math.Max (0, Math.Min (1, min.Mean + 1.64f * min.StdDev)));
				columns [i + 2] = string.Format ("{0:0.0000}", Math.Max (0, Math.Min (1, min.Mean - 1.64f * min.StdDev)));
				i = i + 3;
			}

			File.AppendAllText (filename, string.Join (",", columns) + "\n");
		}
	}
}

