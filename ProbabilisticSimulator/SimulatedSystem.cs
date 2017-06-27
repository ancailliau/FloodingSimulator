using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog;

namespace ProbabilisticSimulator
{
	public class SimulatedSystem
	{
		Dictionary<int, SimulatedSubsystem> markovChains;
		static Logger logger = LogManager.GetCurrentClassLogger();

		public bool Running
		{
			get;
			set;
		}

		Random r;

		public SimulatedSystem ()
		{
			markovChains = new Dictionary<int, SimulatedSubsystem> ();
			r = new Random();
		}

		public void InitSubsystem (int mc)
		{
			markovChains.Add (mc, new SimulatedSubsystem (mc));
		}

		public void AddState (int mc, string id)
		{
			markovChains [mc].AddState (id);
		}

		public void AddState (int mc, string id, bool initial)
		{
			markovChains [mc].AddState (id, initial);
		}

		public void AddTransition (int mc, string source, string target, string [] actions, double probability)
		{
			markovChains [mc].AddTransition (source, target, actions, probability);
		}

		public void AddTransition (int mc, string source, string target, string [] actions, Func<double> probability)
		{
			markovChains [mc].AddTransition (source, target, actions, probability);
		}

		void Start ()
		{
			foreach (var markovChain in markovChains.Values) {
				markovChain.currentState = markovChain.states.Values.Single (x => x.initial == true);
			}
		}

		public void Stop()
		{
			Running = false;
		}

		public void Run (Dictionary<string, Action> actions, TimeSpan delay)
		{
			Start();
			Running = true;

			logger.Info("Running Simulated System");
			while (Running)
			{
				Step(actions);
				Thread.Sleep(delay);
			}
			logger.Info("Simulated System Stopped");
		}

		public void Step (Dictionary<string, Action> actions)
		{
			// First, pick all the transition that will be fired
			var transitionsToFire = new Dictionary<SimulatedSubsystem, Transition> ();
			foreach (var markovChain in markovChains.Values) {
				var outgoing = markovChain.transitions [markovChain.currentState];
				var transition = outgoing.PickTransition (r);
				transitionsToFire.Add (markovChain, transition);
			}

			logger.Trace ("Simulation Step: {0}", 
			              string.Join (",", 
			                           transitionsToFire.Select (x => string.Format("{0} {{{1}}}", 
			                                                                        x.Key.id, 
			                                                                        string.Join(",", x.Value.actions)))
			                          )
			             );

			// Second, fire the transitions
			foreach (var kv in transitionsToFire) {
				var markovChain = kv.Key;
				var transition = kv.Value;
				foreach (var a in transition.actions) {
					var now = DateTime.Now;
					//logger.Info ("Action {0} executed by simulated system", a);
					actions [a] ();
				}
				markovChain.currentState = transition.target;
			}
		}

	}
}

