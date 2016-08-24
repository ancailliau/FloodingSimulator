using System;
using System.IO;
using System.Linq;

namespace Simulator
{
	public class DotExportProcessor : IMonitorProcessor, IGoalMonitorProcessor
	{
		StreamWriter dotFile;
		string filename;

		public DotExportProcessor (string filename)
		{
			this.filename = filename;
		}

		public void Process(GoalMonitor monitor)
		{
			if (!monitor.Ready)
				return;

			dotFile = new StreamWriter(filename);
			try {
				// Export to dot file
				dotFile.WriteLine("digraph {");
				foreach (var s in monitor.MonitoredStates) {
					var str = string.Join("\n", s.Value.state.Where(kv => monitor.projection.Contains(kv.Key.Name)).Select(kv => kv.Key + ":" + kv.Value));

					if (monitor.monitoredTransitions.ContainsKey(s.Key)) {
						str += "\n(" + monitor.monitoredTransitions[s.Key].Values.Sum() + ")";
					}
					dotFile.WriteLine(s.Key + "[label=\"[{0}]\n{1}\"]", s.Key, str);
				}
				foreach (var t in monitor.monitoredTransitions) {
					var total = t.Value.Values.Sum();
					foreach (var t2 in t.Value) {
						dotFile.WriteLine(t.Key + " -> " + t2.Key + " [label=\"{0:0.##}\"];", ((double)t2.Value) / total);
					}
				}
				dotFile.WriteLine("}");
			} catch (Exception) {
				
			}
			dotFile.Close();
		}

		public void Process (Monitor monitor)
		{
			if (!monitor.Ready)
				return;
			
			dotFile = new StreamWriter (filename);
			// Export to dot file
			dotFile.WriteLine ("digraph {");
			foreach (var s in monitor.MonitoredStates) {
				var str = string.Join ("\n", s.Value.state.Where (kv => monitor.projection.Contains (kv.Key.Name)).Select (kv => kv.Key + ":" + kv.Value));

				if (monitor.monitoredTransitions.ContainsKey (s.Key)) {
					str += "\n(" + monitor.monitoredTransitions [s.Key].Values.Sum () + ")";
				}
				dotFile.WriteLine (s.Key + "[label=\"[{0}]\n{1}\"]", s.Key, str);
			}
			foreach (var t in monitor.monitoredTransitions) {
				var total = t.Value.Values.Sum ();
				foreach (var t2 in t.Value) {
					dotFile.WriteLine (t.Key + " -> " + t2.Key + " [label=\"{0:0.##}\"];", ((double)t2.Value) / total);
				}
			}
			dotFile.WriteLine ("}");
			dotFile.Close ();
		}
	}
}

