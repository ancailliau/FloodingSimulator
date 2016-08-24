using System;
using System.Collections.Generic;
using System.Linq;

namespace Simulator
{
	public class SimulationMC
	{
		class State
		{
			public string id;
			public bool initial;
		}

		class TransitionList
		{
			public List<Transition> transitions;
			private Random r;

			public TransitionList ()
			{
				transitions = new List<Transition> ();
				r = new Random ();
			}

			public void Add (Transition transition)
			{
				transitions.Add (transition);
			}

			public Transition PickTransition ()
			{
				var random = r.NextDouble ();

				var transitionProbabilities = transitions.Select (x => x.probability ());
				double sum = transitionProbabilities.Sum ();
				var normalizedTransitions = transitionProbabilities.Select (x => x / sum).ToArray ();

				Console.WriteLine ("Normalized transitions: " + string.Join (",", normalizedTransitions));
				Console.WriteLine ("Order : " + string.Join (",", transitions.Select (x => "{" + string.Join (",", x.actions) + "}")));

				sum = 0;
				for (int i = 0; i < normalizedTransitions.Length; i++) {
					sum += normalizedTransitions [i];
					if (random <= sum)
						return transitions[i];
				}
				return null;
			}
		}

		class Transition
		{
			public Func<double> probability;
			public string[] actions;
			public State target;
		}

		class SubMC
		{
			int id;
			public Dictionary<State, TransitionList> transitions;
			public Dictionary<string, State> states;
			public State currentState;

			public SubMC (int id)
			{
				this.id = id;
				transitions = new Dictionary<State, TransitionList> ();
				states = new Dictionary<string, State> ();
				currentState = null;
			}

			internal void AddState (string id)
			{
				states.Add (id, new State () { id = id, initial = false });
			}

			internal void AddState (string id, bool initial)
			{
				states.Add (id, new State () { id = id, initial = initial });
			}

			TransitionList GetTransitionList (string source)
			{
				var sourceState = states [source];
				TransitionList ltrans;
				if (!transitions.ContainsKey (sourceState)) {
					ltrans = new TransitionList ();
					transitions.Add (states [source], ltrans);
				} else {
					ltrans = transitions [sourceState];
				}

				return ltrans;
			}

			internal void AddTransition (string source, string target, string [] actions, double probability)
			{
				TransitionList ltrans = GetTransitionList (source);
				ltrans.Add (new Transition () { target = states [target], actions = actions, probability = () => probability });
			}


			internal void AddTransition (string source, string target, string [] actions, Func<double> probability)
			{
				TransitionList ltrans = GetTransitionList (source);
				ltrans.Add (new Transition () { target = states [target], actions = actions, probability = probability });
			}

		}

		Dictionary<int, SubMC> markovChains;

		public SimulationMC ()
		{
			markovChains = new Dictionary<int, SubMC> ();
		}

		internal void AddMC (int mc)
		{
			markovChains.Add (mc, new SubMC (mc));
		}

		internal void AddState (int mc, string id)
		{
			markovChains [mc].AddState (id);
		}

		internal void AddState (int mc, string id, bool initial)
		{
			markovChains [mc].AddState (id, initial);
		}

		internal void AddTransition (int mc, string source, string target, string [] actions, double probability)
		{
			markovChains [mc].AddTransition (source, target, actions, probability);
		}

		internal void AddTransition (int mc, string source, string target, string [] actions, Func<double> probability)
		{
			markovChains [mc].AddTransition (source, target, actions, probability);
		}

		public void Start ()
		{
			foreach (var markovChain in markovChains.Values) {
				markovChain.currentState = markovChain.states.Values.Single (x => x.initial == true);
			}
		}

		public void Step (Dictionary<string, Action> actions)
		{
			// First, pick all the transition that will be fired
			var transitionsToFire = new Dictionary<SubMC, Transition> ();
			foreach (var markovChain in markovChains.Values) {
				var outgoing = markovChain.transitions [markovChain.currentState];
				var transition = outgoing.PickTransition ();
				transitionsToFire.Add (markovChain, transition);
			}

			// Second, fire the transitions
			foreach (var kv in transitionsToFire) {
				var markovChain = kv.Key;
				var transition = kv.Value;
				foreach (var a in transition.actions) {
					var now = DateTime.Now;
					Console.WriteLine ("*** [" + now.ToString ("hh:mm:ss.fff") + "] " + a);
					Console.Out.Flush ();
					actions [a] ();
				}
				markovChain.currentState = transition.target;
			}
		}

	}
}

