using System;
using System.Collections.Generic;

namespace ProbabilisticSimulator
{
	public class SimulatedSubsystem
	{
		public int id;
		public Dictionary<State, TransitionList> transitions;
		public Dictionary<string, State> states;
		public State currentState;

		public SimulatedSubsystem(int id)
		{
			this.id = id;
			transitions = new Dictionary<State, TransitionList>();
			states = new Dictionary<string, State>();
			currentState = null;
		}

		internal void AddState(string id)
		{
			states.Add(id, new State() { id = id, initial = false });
		}

		internal void AddState(string id, bool initial)
		{
			states.Add(id, new State() { id = id, initial = initial });
		}

		TransitionList GetTransitionList(string source)
		{
			var sourceState = states[source];
			TransitionList ltrans;
			if (!transitions.ContainsKey(sourceState))
			{
				ltrans = new TransitionList();
				transitions.Add(states[source], ltrans);
			}
			else {
				ltrans = transitions[sourceState];
			}

			return ltrans;
		}

		internal void AddTransition(string source, string target, string[] actions, double probability)
		{
			TransitionList ltrans = GetTransitionList(source);
			ltrans.Add(new Transition() { target = states[target], actions = actions, probability = () => probability });
		}


		internal void AddTransition(string source, string target, string[] actions, Func<double> probability)
		{
			TransitionList ltrans = GetTransitionList(source);
			ltrans.Add(new Transition() { target = states[target], actions = actions, probability = probability });
		}

	}
}

