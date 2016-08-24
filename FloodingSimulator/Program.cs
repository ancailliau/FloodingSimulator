using System;
using Simulator;
using KAOSTools.MetaModel;
using System.Collections.Generic;
using LtlSharp;
using System.Threading;
using NLog;
using System.Text;
using System.Linq;
using MoreLinq;
using System.IO;

using UncertaintySimulation;

namespace FloodingSimulator
{
	class MainClass
	{
		static FloodingSimulator simulator;
		static GoalMonitor monitor;

		static KAOSModel model;

		static Optimizer optimizer;
		static IEnumerable<Resolution> ActiveResolutions;

		static readonly Logger logger = LogManager.GetCurrentClassLogger();

		static Dictionary<Goal, ObstructionSuperset> obstructions;

		static string adaptation_filename = "experiment-adaptation.csv";

		public static void Main (string [] args)
		{
			Console.WriteLine("Hello World!");

			if (File.Exists(adaptation_filename)) {
				File.Delete(adaptation_filename);
			}
			File.AppendAllText(adaptation_filename, "timestamp,ultrasound,camera,phone,sms,email\n");

			logger.Info("Building KAOS model.");
			// Open and load the KAOS Model.
			var filename = "./Model.kaos";
			var parser = new KAOSTools.Parsing.ModelBuilder();
			model = parser.Parse(File.ReadAllText(filename), filename);
			var model2 = parser.Parse(File.ReadAllText(filename), filename);
			ActiveResolutions = Enumerable.Empty<Resolution>();

			var declarations = parser.Declarations;
			logger.Info("(done)");

			logger.Info("Building the simulator.");
			// Create the simulator.
			simulator = new FloodingSimulator();
			logger.Info("(done)");

			logger.Info("Configuring monitors.");
			// Configure all the monitors (for all obstacles and domain properties).
			KAOSMetaModelElement[] goals = model.Goals().ToArray();
			KAOSMetaModelElement[] obstacles = model.LeafObstacles().ToArray();
			var projection = new HashSet<string>(GetAllPredicates(goals));
			monitor = new GoalMonitor(model, goals.Union(obstacles), projection, GetStorage,
									  TimeSpan.FromSeconds(1));
			//monitor = new GoalMonitor(model, goals.Union(obstacles), projection, () =>
			//                          new InfiniteStateInformationStorage(),
			//                          TimeSpan.FromSeconds (1));
			logger.Info("(done)");

			logger.Info("Launching monitors");
			monitor.Run(false);

			logger.Info("Launching processors");
			new Timer((state) => UpdateCPS(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
			new Timer((state) => MonitorStep(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
			new Timer((state) => LogStatistic(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

			var goalMonitorProcessor = new GoalMonitorProcessor(monitor);

			//var dotExport = new DotExportProcessor("relation.dot");
			//goalMonitorProcessor.AddProcessor(dotExport, TimeSpan.FromSeconds (1));

			var csvExport = new CSVGoalExportProcessor("experiment-1.csv", "experiment-obstacle.csv");
			goalMonitorProcessor.AddProcessor(csvExport, TimeSpan.FromSeconds(1));

			// Initialize obstruction sets
			obstructionLock = new object();
			ComputeObstructionSets();

			// Configure optimization process.
			optimizer = new Optimizer(monitor, model2);
			new Timer((state) => Optimize(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(120));

			// Run the simulator.
			logger.Info("Running simulator");
			simulator.Run();
		}

		public static IStateInformationStorage GetStorage(KAOSMetaModelElement element)
		{
			if (element.Identifier == "ultrasound_broken")
				return new TimedStateInformationStorage(TimeSpan.FromMinutes(30),
														TimeSpan.FromHours(1));
			else
				return new InfiniteStateInformationStorage();
		}

		static object obstructionLock;

		static void ComputeObstructionSets()
		{
			lock (obstructionLock) {
				logger.Info("Obstruction sets ----");
				obstructions = new Dictionary<Goal, ObstructionSuperset>();
				// Get the obstruction set
				var goals = model.Goals();
				foreach (var goal in goals) {
					logger.Info("Obstruction set for " + goal.FriendlyName);
					ObstructionSuperset obstructionSuperset;
					if (goal.Replacements().Count() > 0) {
						obstructionSuperset = goal.GetObstructionSuperset(false);
					} else {
						obstructionSuperset = goal.GetObstructionSuperset(true);
					}
					obstructions.Add(goal, obstructionSuperset);
					foreach (var kv in obstructionSuperset.mapping) {
						logger.Info("{0} => {1}", kv.Key.FriendlyName, kv.Value);
					}
				}
				logger.Info("---- obstruction sets");
			}
		}

		static HashSet<string> GetAllPredicates(KAOSMetaModelElement[] goals)
		{
			var predicates = goals.SelectMany(x => {
				if (x is Goal && ((Goal)x).FormalSpec != null) {
					return ((Goal)x).FormalSpec.PredicateReferences;

				} else if (x is Obstacle && ((Obstacle)x).FormalSpec != null) {
					return ((Obstacle)x).FormalSpec.PredicateReferences;

				} else {
					return Enumerable.Empty<PredicateReference>();
				}
			});
				
			return predicates.Select (x => x.Predicate.Name).ToHashSet ();
		}

		static void UpdateCPS()
		{
			lock (obstructionLock) {
				foreach (var kv in obstructions) {
					var sampleVector = new Dictionary<int, double>();
					foreach (var o in kv.Value.mapping) {
						var candidate = monitor.kaosElementMonitor.Where(a => a.Key.Identifier == o.Key.Identifier);
						if (candidate.Count() == 1) {
							var lmon = candidate.First().Value;
							if (lmon.Max != null) {
								sampleVector.Add(o.Value, lmon.Max.Mean);
							} else {
								sampleVector.Add(o.Value, 1); // no data = 1
								logger.Warn("No data available for " + kv.Key.FriendlyName);
							}
						} else {
							logger.Error("No key {0} in monitors", o.Key.FriendlyName);
							sampleVector.Add(o.Value, ((Obstacle)o.Key).EPS);
						}
					}

					var p = 1 - kv.Value.GetProbability(sampleVector);
					kv.Key.CPS = p;
				}
			}
		}

		static void Optimize()
		{
			var rootGoal = model.Goal(x => x.Identifier == "locals_warned_when_risk_imminent");
			if (rootGoal.CPS >= rootGoal.RDS) {
				return;
			}

			var countermeasures = optimizer.Optimize();

			Type simType = simulator.GetType();

			// Adapt the software
			foreach (var cm in ActiveResolutions.Except(countermeasures)) {
				var method = cm.ResolvingGoal().CustomData["onwithold"];
				logger.Info("onwithold: " + method);
				var mi = simType.GetMethod(method);
				if (mi != null) {
					mi.Invoke(simulator, new object[] { });
				} else {
					logger.Warn("Cannot find the method " + method);
				}
			}
			foreach (var cm in countermeasures.Except(ActiveResolutions)) {
				var method = cm.ResolvingGoal().CustomData["ondeploy"];
				logger.Info("ondeploy: " + method);
				var mi = simType.GetMethod(method);
				if (mi != null) {
					mi.Invoke(simulator, new object[] { });
				} else {
					logger.Warn("Cannot find the method " + method);
				}
			}

			// Update the requirement model
			logger.Info("Countermeasures to withold:");
			foreach (var cm in ActiveResolutions.Except(countermeasures)) {
				logger.Info("- {0}", cm.ResolvingGoal().FriendlyName);

				var resolutionToWithold = model.Elements.OfType<Resolution>()
				                              .Single(x => x.ResolvingGoalIdentifier == cm.ResolvingGoalIdentifier
				                                      & x.ObstacleIdentifier == cm.ObstacleIdentifier);

				ResolutionIntegrationHelper.Desintegrate(resolutionToWithold);
				
			}
			logger.Info("Countermeasures to deploy: ");
			foreach (var cm in countermeasures.Except (ActiveResolutions)) {
				logger.Info("- {0}", cm.ResolvingGoal ().FriendlyName);

				var resolutionToDeploy = model.Elements.OfType<Resolution>()
											  .Single(x => x.ResolvingGoalIdentifier == cm.ResolvingGoalIdentifier
													  & x.ObstacleIdentifier == cm.ObstacleIdentifier);

				ResolutionIntegrationHelper.Integrate (resolutionToDeploy);
			}

			ActiveResolutions = countermeasures;

			// Update the obstruction sets
			ComputeObstructionSets();
			UpdateCPS();
			logger.Info("New configuration deployed");

			// Export the adaptation for drawing beautiful graphs!
			var sb = new StringBuilder();
			var ultrasound = ActiveResolutions.All (x => x.ResolvingGoalIdentifier != "speed_acquired_by_camera");
			var camera = !ultrasound;
			var phone = ActiveResolutions.All(x => x.ResolvingGoalIdentifier != "locals_warned_by_sms" 
			                                 & x.ResolvingGoalIdentifier != "locals_warned_by_email");
			var sms = ActiveResolutions.Any(x => x.ResolvingGoalIdentifier == "locals_warned_by_sms");
			var email = ActiveResolutions.Any(x => x.ResolvingGoalIdentifier == "locals_warned_by_email");
			sb.AppendFormat("{5},{0},{1},{2},{3},{4}\n", 
			                ultrasound ? 1 : 0, camera ? 1 : 0, phone ? 1 : 0, sms ? 1 : 0, email ? 1 : 0,
			                DateTime.Now.ToString("hh:mm:ss.ffff"));

			File.AppendAllText(adaptation_filename, sb.ToString ());
		}

		static void MonitorStep()
		{
			var sb = new StringBuilder();
			var ms = simulator.GetMonitoredState();
			var maxLen = ms.state.Max(x => x.Key.Name.Length);
			sb.AppendLine("Monitor Step:");
			sb.AppendFormat(" +-{0}-+-------+\n", new string ('-', maxLen));
			foreach (var kv in ms.state) {
				sb.AppendFormat(" | {0,-"+maxLen+"} | {1,-5} |\n", kv.Key.Name, kv.Value);
			}
			sb.AppendFormat(" +-{0}-+-------+", new string('-', maxLen));
			monitor.MonitorStep(ms, DateTime.Now);
			logger.Info(sb.ToString());
		}

		static void LogStatistic()
		{
			var maxLen = monitor.kaosElementMonitor.Max(x => x.Key.FriendlyName.Length);
			HashSet<int> hashes = new HashSet<int>();

			var sb = new StringBuilder();
			sb.AppendLine("Goal statisfaction statistics:");
			sb.AppendFormat("+-{0}-+----------------------------------------+----------------------------------------+\n", new string('-', maxLen));
			sb.AppendFormat("| {0} | min                                    + max                                    +\n", new string(' ', maxLen));
			sb.AppendFormat("| {0} +------+-------------+-------------+-----+------+-------------+-------------+-----+\n", "Name".PadRight(maxLen));
			sb.AppendFormat("| {0} | mean | conf. int   | id          | #   | mean | conf. int   | id          | #   |\n", new string(' ', maxLen));
			sb.AppendFormat("+-{0}-+------+-------------+-------------+-----+------+-------------+-------------+-----+\n", new string('-', maxLen));
			foreach (var m in monitor.kaosElementMonitor) {
				if (m.Value.Max != null & m.Value.Min != null) {
					sb.AppendFormat("| {0,-"+maxLen+"} " +
					                "| {1:0.00} | [{2:0.00},{3:0.00}] | {4,-11} | {5,3} "  +
					                "| {6:0.00} | [{7:0.00},{8:0.00}] | {9,-11} | {10,3} |\n", 
					                m.Key.FriendlyName, 
					                m.Value.Min.Mean, 
					                Math.Max (0, m.Value.Min.Mean - 1.64 * m.Value.Min.StdDev),
					                Math.Min (1, m.Value.Min.Mean + 1.64 * m.Value.Min.StdDev),
					                m.Value.Min.Hash,
					                m.Value.Min.Negative + m.Value.Min.Positive,
					                m.Value.Max.Mean,
					                Math.Max(0, m.Value.Max.Mean - 1.64 * m.Value.Max.StdDev),
					                Math.Min(1, m.Value.Max.Mean + 1.64 * m.Value.Max.StdDev),
					                m.Value.Max.Hash,
									m.Value.Max.Negative + m.Value.Max.Positive);
					hashes.Add(m.Value.Min.Hash);
					hashes.Add(m.Value.Max.Hash);
				} else {
					sb.AppendFormat("| {0,-" + maxLen + "} | {1,-67} |\n",
									m.Key.FriendlyName,
									"N.A.");
				}
			}
			sb.AppendFormat("+-{0}-+------+-------------+-------------+-----+------+-------------+-------------+-----+\n", new string('-', maxLen));

			//if (monitor.MonitoredStates.Count > 0) {
			//	var mLen = monitor.MonitoredStates.Max(x => x.Value.state.Max(y => y.Key.Name.Length));
			//	sb.AppendFormat("+-------------+-{0}-+\n", new string('-', mLen + 12));
			//	foreach (var hash in hashes) {
			//		var subArray = new List<string>();
			//		subArray.Add(string.Format("+-{0}-+-------+", new string('-', mLen)));
			//		foreach (var prop in monitor.MonitoredStates[hash].state) {
			//			subArray.Add(string.Format("| {0,-" + mLen + "} | {1,-5} |", prop.Key.Name, prop.Value));
			//		}
			//		subArray.Add(string.Format("+-{0}-+-------+", new string('-', mLen)));

			//		sb.AppendFormat("| {0} | {1} |\n", hash, subArray[0]);
			//		for (int i = 1; i < subArray.Count; i++) {
			//			sb.AppendFormat("|             | {0} |\n", subArray[i]);
			//		}
			//	}
			//	sb.AppendFormat("+-------------+-{0}-+\n", new string('-', mLen + 12));
			//}

			logger.Info(sb.ToString ());
		}

		static void SaveTransitionRelation()
		{
			
		}
	}
}
