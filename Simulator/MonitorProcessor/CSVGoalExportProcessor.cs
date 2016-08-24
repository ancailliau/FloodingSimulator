using System;
using System.IO;
using System.Text;
using KAOSTools.MetaModel;
using System.Linq;
using MoreLinq;
using System.Collections.Generic;

namespace Simulator
{
	public class CSVGoalExportProcessor : IGoalMonitorProcessor
	{
		string filename_goal;
		string filename_obstacle;

		bool headerDisplayedObstacle;
		bool headerDisplayedGoal;

		public CSVGoalExportProcessor (string filename_goal, string filename_obstacle)
		{
			this.filename_goal = filename_goal;

			if (File.Exists (filename_goal)) {
				File.Delete (filename_goal);
			}

			this.filename_obstacle = filename_obstacle;

			if (File.Exists(filename_obstacle)) {
				File.Delete(filename_obstacle);
			}

			headerDisplayedObstacle = false;
			headerDisplayedGoal = false;

		}

		public void Process (GoalMonitor monitor)
		{
			if (!monitor.Ready)
				return;
			
			ExportGoals(monitor);
			ExportObstacles(monitor);
		}

		public void Process(GoalMonitor monitor, IEnumerable<Goal> goals, IEnumerable<Obstacle> obstacles)
		{
			if (!monitor.Ready)
				return;

			ExportGoals(monitor, goals);
			ExportObstacles(monitor, obstacles);
		}

		void ExportGoals(GoalMonitor monitor)
		{
			ExportGoals(monitor, monitor.model.Goals());
		}

		void ExportGoals(GoalMonitor monitor, IEnumerable<Goal> goals)
		{
			var ngoals = goals.Count();
			var nmonitoredgoals = monitor.kaosElementMonitor.Keys.Count(x => x is Goal);
			var ncolumns = (ngoals - nmonitoredgoals) * 1 + nmonitoredgoals * 4 + 1;

			string[] columns = new string[ncolumns];

			if (!headerDisplayedGoal) {
				headerDisplayedGoal = true;

				int j = 1;
				foreach (var r in goals) {
					columns[j] = r.FriendlyName;
					if (monitor.kaosElementMonitor.ContainsKey(r)) {
						j = j + 3;
					}
					j++;
				}
				File.AppendAllText(filename_goal, string.Join(",", columns) + "\n");
				columns = new string[ncolumns];

				columns[0] = "timestamp";
				j = 1;
				foreach (var r in goals) {
					columns[j] = "cps";
					if (monitor.kaosElementMonitor.ContainsKey(r)) {
						columns[j] = "mean";
						columns[j + 1] = "lower";
						columns[j + 2] = "upper";
						columns[j + 3] = "cps";
						j = j + 3;
					}
					j++;
				}
				File.AppendAllText(filename_goal, string.Join(",", columns) + "\n");
				columns = new string[ncolumns];

			}


			columns[0] = DateTime.Now.ToString("HH:mm:ss.ffff");
			int i = 1;
			foreach (var r in goals) {
				if (monitor.kaosElementMonitor.ContainsKey(r)) {
					var element = r;
					var m = monitor.kaosElementMonitor[r];

					var min = m.Min;
					if (min != null) {
						columns[i] = string.Format("{0:0.0000}", min.Mean);
						columns[i + 1] = string.Format("{0:0.0000}", Math.Max(0, Math.Min(1, min.Mean + 1.64f * min.StdDev)));
						columns[i + 2] = string.Format("{0:0.0000}", Math.Max(0, Math.Min(1, min.Mean - 1.64f * min.StdDev)));
						columns[i + 3] = string.Format("{0:0.0000}", Math.Max(0, Math.Min(1, ((Goal)element).CPS)));
						i = i + 4;
					}
					
				} else {
					columns[i] = string.Format("{0:0.0000}", Math.Max(0, Math.Min(1, r.CPS)));
					i++;
				}
			}

			File.AppendAllText(filename_goal, string.Join(",", columns) + "\n");
		}

		void ExportObstacles(GoalMonitor monitor)
		{
			var obstacles = monitor.model.Obstacles().ToList();
		}

		void ExportObstacles(GoalMonitor monitor, IEnumerable<Obstacle> obstacles)
		{
			var nobstacles = obstacles.Count();
			var nmonitoredobstacles = monitor.kaosElementMonitor.Keys.Count(x => x is Obstacle);
			var ncolumns = (nobstacles - nmonitoredobstacles) * 1 + nmonitoredobstacles * 4 + 1;

			string[] columns = new string[ncolumns];

			if (!headerDisplayedObstacle) {
				headerDisplayedObstacle = true;

				int j = 1;
				foreach (var r in obstacles) {
					columns[j] = r.FriendlyName;
					if (monitor.kaosElementMonitor.ContainsKey(r)) {
						j = j + 3;
					}
					j++;
				}
				File.AppendAllText(filename_obstacle, string.Join(",", columns) + "\n");
				columns = new string[ncolumns];

				columns[0] = "timestamp";
				j = 1;
				foreach (var r in obstacles) {
					columns[j] = "cps";
					if (monitor.kaosElementMonitor.ContainsKey(r)) {
						columns[j] = "mean";
						columns[j + 1] = "lower";
						columns[j + 2] = "upper";
						columns[j + 3] = "cps";
						j = j + 3;
					}
					j++;
				}
				File.AppendAllText(filename_obstacle, string.Join(",", columns) + "\n");
				columns = new string[ncolumns];

			}


			columns[0] = DateTime.Now.ToString("HH:mm:ss.ffff");
			int i = 1;
			foreach (var r in obstacles) {
				if (monitor.kaosElementMonitor.ContainsKey(r)) {
					var element = r;
					var m = monitor.kaosElementMonitor[r];

					var max = m.Max;
					if (max != null) {
						columns[i] = string.Format("{0:0.0000}", max.Mean);
						columns[i + 1] = string.Format("{0:0.0000}", Math.Max(0, Math.Min(1, max.Mean + 1.64f * max.StdDev)));
						columns[i + 2] = string.Format("{0:0.0000}", Math.Max(0, Math.Min(1, max.Mean - 1.64f * max.StdDev)));
						columns[i + 3] = string.Format("{0:0.0000}", Math.Max(0, Math.Min(1, ((Obstacle)element).CPS)));
						i = i + 4;
					}

				} else {
					columns[i] = string.Format("{0:0.0000}", Math.Max(0, Math.Min(1, r.CPS)));
					i++;
				}
			}

			File.AppendAllText(filename_obstacle, string.Join(",", columns) + "\n");
		}
	}
}

