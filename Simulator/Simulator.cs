using System;
using System.Threading;
using LtlSharp;
using LtlSharp.Monitoring;
using System.Collections.Generic;

namespace Simulator
{
	public class Simulator
	{
		public PumpMotor motor { get; private set; }
		public MethaneDetector methaneDetector { get; private set; }
		public WaterLevelSensor waterLevelSensor { get; private set; }

		Thread thread;
		public List<Monitor> monitor;

		public Simulator ()
		{
			motor = new PumpMotor ();
			methaneDetector = new MethaneDetector ();
			waterLevelSensor = new WaterLevelSensor ();
			monitor = new List<Monitor> ();
		}

		public void Run ()
		{
			Console.WriteLine ("Running PumpController");
			var pctl = new PumpController (motor, methaneDetector, waterLevelSensor);
			var threadStart = new ThreadStart (pctl.Run);
			thread = new Thread (threadStart);
			thread.Start ();

			var hw = new Proposition ("HighWater");
			var lw = new Proposition ("LowWater");
			var po = new Proposition ("PumpOn");
			var methane = new Proposition ("Methane");
			var broken = new Proposition ("Broken");

			var goalFormalSpec = new Implication (
				// new Conjunction (hw, new Negation (methane)),
				//new Negation (methane),
				hw,
				new Next (
					new Until (
						po,
						//methane
						new Negation (hw)
						// new Conjunction (hw, new Negation (methane)).Negate ()
					)
				)
			);

			var obstacleFormalSpec = new Conjunction (
				// new Conjunction (hw, new Negation (methane)), 
				// new Negation (methane),
				hw,
				new Next (
					new Release (
						broken,
						hw
						//new Negation (methane)
						//new Conjunction (hw, new Negation (methane))
					)
				)
			);

			var domPropFormalSpec = new Implication (broken, new Negation (po)).Normalize ();

			var d = new Dictionary<Proposition, Func<bool>> ();
			d.Add (hw, () => waterLevelSensor.Level >= 2);
			d.Add (lw, () => waterLevelSensor.Level <= 0); 
			d.Add (po, () => motor.On);
			d.Add (methane, () => methaneDetector.IsMethane);
			d.Add (broken, () => motor.Broken);

			var goal = new MonitoredFormula () {
				Name = "Achieve [Pump On When High Water And No Methane]",
				formula = goalFormalSpec,
				expectedPositives = 10, expectedNegatives = 10
			};

			var obstacle = new MonitoredFormula () {
				Name = "Pump Broken Next When High Water And Methane",
				formula = obstacleFormalSpec,
				expectedPositives = 100, expectedNegatives = 1
			};

			var domainProperty = new MonitoredFormula () {
				Name = "Pump Not On Next When Pump Broken",
				formula = domPropFormalSpec,
				expectedPositives = 100, expectedNegatives = 1
			};


			var mon1 = new Monitor (new [] { goal, obstacle, domainProperty }, 
			                       new HashSet<string> (new string [] { "PumpOn", "HighWater", "LowWater", "Broken", "Methane" }),
			                        () => new TimedStateInformationStorage (TimeSpan.FromSeconds (60), TimeSpan.FromMinutes (1)));
			monitor.Add (mon1);

			var mon2 = new Monitor (new [] { goal, obstacle, domainProperty },
								   new HashSet<string> (new string [] { "PumpOn", "HighWater", "LowWater", "Broken", "Methane" }),
			                        () => new FiniteStateInformationStorage (50));
			monitor.Add (mon2);

			var mon3 = new Monitor (new [] { goal, obstacle, domainProperty },
								   new HashSet<string> (new string [] { "PumpOn", "HighWater", "LowWater", "Broken", "Methane" }),
			                        () => new InfiniteStateInformationStorage ());
			monitor.Add (mon3);

			// var processor = new RegularProcessor (mon1);

			//var summary = new SummaryProcessor ("./monitoring-2.log");
			//processor.AddProcessor (summary, TimeSpan.FromSeconds (1));

			//var dotExport = new DotExportProcessor ("./temp.dot");
			//processor.AddProcessor (dotExport, TimeSpan.FromSeconds (5));

			var csvExport = new CSVExportProcessor ("./probabilities.csv");
			new RegularProcessor (mon1).AddProcessor (csvExport, TimeSpan.FromSeconds (1));

			csvExport = new CSVExportProcessor ("./probabilities-experiment2.csv");
			new RegularProcessor (mon2).AddProcessor (csvExport, TimeSpan.FromSeconds (1));

			csvExport = new CSVExportProcessor ("./probabilities-experiment3.csv");
			new RegularProcessor (mon3).AddProcessor (csvExport, TimeSpan.FromSeconds (1));

			foreach (var mon in monitor) {
				mon.Run (true);
			}

		}

		internal void MonitorStep (MonitoredState ms, DateTime now)
		{
			foreach (var mon in monitor) {
				mon.MonitorStep (ms, now);
			}
		}

		public void Stop ()
		{
			foreach (var mon in monitor) {
				mon.Stop ();
			}
		}

		internal MonitoredState GetMonitoredState ()
		{
			var ms = new MonitoredState ();
			ms.Set (new Proposition ("PumpOn"), motor.On);
			ms.Set (new Proposition ("Methane"), methaneDetector.IsMethane);
			ms.Set (new Proposition ("HighWater"), waterLevelSensor.Level >= 2);
			ms.Set (new Proposition ("LowWater"), waterLevelSensor.Level <= 0);
			ms.Set (new Proposition ("Broken"), motor.Broken);

			return ms;
		}

}
}
