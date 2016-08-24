using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using LtlSharp.ModelCheckers;
using LtlSharp;
using LtlSharp.Models;
using LtlSharp.Automata.Nodes.Factories;
using LtlSharp.Automata.Utils;
using LtlSharp.Automata;
using System.Linq;

namespace Simulator
{
	public class MainClass
	{
		const int sleepTime = 150;

		private class BreakSimulator
		{
			//Simulator s;
			//int count = 0;
			//public BreakSimulator (Simulator s)
			//{
			//	this.s = s;
			//}
			//public void SimulateBreak ()
			//{
			//	Random r = new Random ();
			//	while (true) {
			//		if (!s.motor.Broken & r.NextDouble () > .9d) {
			//			Console.WriteLine ("breakpump");
			//			s.motor.Break ();
			//			count = 0;

			//		} else {
			//			if (s.motor.Broken) {
			//				count++;
			//				if (count == 5) {
			//					Console.WriteLine ("repairpump");
			//					s.motor.Repair ();
			//				}
			//			}
			//		}

			//		Thread.Sleep (50);
			//	}
			//}
		}

		public static int Main1 (string [] args)
		{

			var phi = new Implication (new Proposition ("HighWater"), // new Conjunction (new Proposition ("HighWater"), new Negation (new Proposition ("Methane"))),
			                           // new Next (new Proposition ("PumpOn")));
			                           new BoundedFinally (new Proposition ("PumpOn"), 4)).Normalize ();
			var mc = new MarkovChain<AutomatonNode, double> (new AutomatonNodeFactory (), new DoubleProbabilityFactory ());

			ILiteral pumpOff = new Negation (new Proposition ("PumpOn"));
			ILiteral pumpOn = new Proposition ("PumpOn");

			ILiteral methane = new Proposition ("Methane");
			ILiteral nomethane = new Proposition ("Methane");

			ILiteral highwater = new Proposition ("HighWater");
			ILiteral lowwater = new Proposition ("LowWater");
			ILiteral nohighwater = new Negation (new Proposition ("HighWater"));
			ILiteral nolowwater = new Negation (new Proposition ("LowWater"));

			ILiteral broken = new Proposition ("Broken");
			ILiteral notbroken = new Negation (new Proposition ("Broken"));

			var node0 = new AutomatonNode (0, "0", new ILiteral [] { pumpOff, nomethane, lowwater, nohighwater, notbroken });
			var node529 = new AutomatonNode (529, "529", new ILiteral [] { pumpOff, nomethane, nolowwater, nohighwater, notbroken });
			var node1058 = new AutomatonNode (1058, "1058", new ILiteral [] { pumpOff, nomethane, nolowwater, highwater, broken });
			var node1075 = new AutomatonNode (1075, "1075", new ILiteral [] { pumpOn, nomethane, nolowwater, highwater, notbroken });
			var node546 = new AutomatonNode (546, "546", new ILiteral [] { pumpOn, nomethane, nolowwater, nohighwater, notbroken });
			var node17 = new AutomatonNode (17, "17", new ILiteral [] { pumpOn, nomethane, lowwater, nohighwater, notbroken });

			mc.AddNode (node0);
			mc.AddNode (node529);
			mc.AddNode (node1058);
			mc.AddNode (node1075);
			mc.AddNode (node546);
			mc.AddNode (node17);

			mc.AddTransition (node0, 0.76, node0);
			mc.AddTransition (node0, 0.24, node529);

			mc.AddTransition (node529, 0.61, node529);
			mc.AddTransition (node529, 0.24, node1058);
			mc.AddTransition (node529, 0.05, node1075);

			mc.AddTransition (node1058, 0.73, node1058);
			mc.AddTransition (node1058, 0.14, node1075);
			mc.AddTransition (node1058, 0.12, node529);

			mc.AddTransition (node1075, 0.77, node1075);
			mc.AddTransition (node1075, 0.05, node529);
			mc.AddTransition (node1075, 0.13, node546);

			mc.AddTransition (node546, 0.66, node546);
			mc.AddTransition (node546, 0.05, node17);
			mc.AddTransition (node546, 0.05, node529);
			mc.AddTransition (node546, 0.05, node1058);
			mc.AddTransition (node546, 0.19, node1075);

			mc.AddTransition (node17, 1, node0);

			var calculator = new DoubleModelCheckerCalculator<AutomatonNode> (0);
			var modelChecker = new PCTLModelChecker<AutomatonNode, double> (mc, phi, calculator);

			var trans = new [] { node1075, node1058 }.SelectMany (x=> mc.GetInTransitions (x)).GroupBy (x => x.Source);
			foreach (var t in trans) {
				Console.WriteLine (t.Sum (x=>x.Decoration.Probability) + " * " + t.Key + " " + string.Join (",", t.Select (x => x.Target)));
			}
			// Console.WriteLine (trans.Sum (x => x.Decoration.Probability));

			Console.WriteLine ("Checking : " + phi);

			calculator.Init ();
			var dict = DoubleModelCheckerCalculator<AutomatonNode>.QuantitativeLinearProperty (mc, phi);

			//var dict = modelChecker.Check ();

			foreach (var kv in dict) {
				Console.WriteLine (kv.Key + " => " + kv.Value);
			}

			return 0;
		}

		public static int Main2 (string [] args)
		{
			ILiteral pumpOff = new Negation (new Proposition ("PumpOn"));
			ILiteral pumpOn = new Proposition ("PumpOn");

			ILiteral methane = new Proposition ("Methane");
			ILiteral nomethane = new Negation (new Proposition ("Methane"));

			ILiteral highwater = new Proposition ("HighWater");
			ILiteral lowwater = new Proposition ("LowWater");
			ILiteral nohighwater = new Negation (new Proposition ("HighWater"));
			ILiteral nolowwater = new Negation (new Proposition ("LowWater"));

			ILiteral broken = new Proposition ("Broken");
			ILiteral notbroken = new Negation (new Proposition ("Broken"));
			
			var phi = new Implication (
				new Conjunction (highwater, new Negation (methane)),
				new Next (
					new Until (
						pumpOn,
						new Conjunction (highwater, new Negation (methane)).Negate ()
					)
				)
			);

			var mc = new MarkovChain<AutomatonNode, double> (new AutomatonNodeFactory (), new DoubleProbabilityFactory ());

			var node0 = new AutomatonNode (0, "897", new ILiteral [] { pumpOff, nomethane, nolowwater, nohighwater, notbroken });
			var node1 = new AutomatonNode (1, "087", new ILiteral [] { pumpOn,  nomethane, nolowwater, highwater,   notbroken });
			var node2 = new AutomatonNode (2, "593", new ILiteral [] { pumpOff, methane,   nolowwater, highwater,   notbroken });
			var node3 = new AutomatonNode (3, "936", new ILiteral [] { pumpOff, methane,   nolowwater, highwater,   broken });
			var node4 = new AutomatonNode (4, "407", new ILiteral [] { pumpOff, nomethane, nolowwater, highwater,   broken });

			mc.AddNode (node0);
			mc.AddNode (node1);
			mc.AddNode (node2);
			mc.AddNode (node3);
			mc.AddNode (node4);

			mc.AddTransition (node0, 1, node1);

			mc.AddTransition (node1, 0.46, node1);
			mc.AddTransition (node1, 0.43, node2);
			mc.AddTransition (node1, 0.07, node3);
			mc.AddTransition (node1, 0.04, node4);

			mc.AddTransition (node2, 0.09, node4);
			mc.AddTransition (node2, 0.91, node1);

			mc.AddTransition (node3, 1, node1);

			mc.AddTransition (node4, 0.36, node2);
			mc.AddTransition (node4, 0.64, node1);


			var calculator = new DoubleModelCheckerCalculator<AutomatonNode> (0);
			var modelChecker = new PCTLModelChecker<AutomatonNode, double> (mc, phi, calculator);

			Console.WriteLine ("Checking : " + phi);

			calculator.Init ();
			var dict = DoubleModelCheckerCalculator<AutomatonNode>.QuantitativeLinearProperty (mc, phi);

			//var dict = modelChecker.Check ();

			foreach (var kv in dict) {
				Console.WriteLine (kv.Key + " => " + kv.Value);
			}

			return 0;
		}

		public static void Main (string [] args)
		{
			//Console.WriteLine ("Hello World!");

			//Console.WriteLine ("Initializing simulated components");

			//var simulator = new Simulator ();
			//simulator.Run ();

			//var mc = new SimulationMC ();
			//mc.AddMC (0);

			//mc.AddState (0, "s0", true);
			//mc.AddState (0, "s1");
			//mc.AddState (0, "s2");

			//mc.AddTransition (0, "s0", "s1", new [] { "aboveLow" }, 1); // .5
			//mc.AddTransition (0, "s0", "s0", new string [] { }, 0); // .5

			//mc.AddTransition (0, "s1", "s2", new [] { "aboveHigh" }, 1); // .5
			//mc.AddTransition (0, "s1", "s0", new [] { "belowLow" }, 0); // .25
			//mc.AddTransition (0, "s1", "s1", new string [] { }, 0); // .25

			//Func<double> lowerWaterLevel = () => {
			//	if (simulator.motor.On) {
			//		return .7;
			//	}

			//	return .05;
			//};

			//mc.AddTransition (0, "s2", "s1", new [] { "aboveLow" }, lowerWaterLevel); // .5
			//mc.AddTransition (0, "s2", "s2", new string [] { }, () => 1 - lowerWaterLevel()); // .5

			//var now = DateTime.Now;
			//var delay = TimeSpan.FromSeconds (90);
			//var current = .9;

			//Func<double> probabilityToNotBreak = () => {
			//	if (DateTime.Now - now >= delay) {
			//		current = 1 - current;
			//		now = DateTime.Now;
			//	}

			//	return current;
			//};

			//mc.AddMC (1);
			//mc.AddState (1, "s0", true);
			//mc.AddState (1, "s1");
			//mc.AddTransition (1, "s0", "s0", new string [] { }, probabilityToNotBreak);
			//mc.AddTransition (1, "s0", "s1", new string [] { "break" }, () => {
			//	return 1 - probabilityToNotBreak();
			//});
			//mc.AddTransition (1, "s1", "s1", new string [] { }, () => 1 - probabilityToNotBreak());
			//mc.AddTransition (1, "s1", "s0", new string [] { "repair" }, () => probabilityToNotBreak ());

			//mc.AddMC (2);
			//mc.AddState (2, "s0", true);
			//mc.AddState (2, "s1");

			//mc.AddTransition (2, "s0", "s0", new string [] { }, 1);
			//mc.AddTransition (2, "s0", "s1", new string [] { "methaneAppears" }, 0);
			//mc.AddTransition (2, "s1", "s1", new string [] { }, 0);
			//mc.AddTransition (2, "s1", "s0", new string [] { "methaneLeaves" }, 1);

			//var d = new Dictionary<string, Action> ();
			//d.Add ("aboveLow", () => simulator.waterLevelSensor.AboveLow ());
			//d.Add ("aboveHigh", () => simulator.waterLevelSensor.AboveHigh ());
			//d.Add ("belowLow", () => simulator.waterLevelSensor.BelowLow ());
			//d.Add ("break", () => simulator.motor.Break ());
			//d.Add ("repair", () => simulator.motor.Repair ());
			//d.Add ("methaneAppears", () => simulator.methaneDetector.MethaneAppears ());
			//d.Add ("methaneLeaves", () => simulator.methaneDetector.MethaneLeaves ());

			//mc.Start ();
			//// for (int i = 0; i < 250; i++) {
			//while (true) {
			//	mc.Step (d);
			//	Thread.Sleep (50);
			//	var ms = simulator.GetMonitoredState ();
			//	simulator.MonitorStep (ms, DateTime.Now);
			//}

			//simulator.Stop ();

			////simulator.Stop ();
		}
	}
}

