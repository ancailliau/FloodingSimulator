using System;
using System.Collections.Generic;
using System.Linq;

namespace UCLouvain.EnvironmentSimulator
{
	public class TransitionList
	{
		public List<Transition> transitions;

		public TransitionList()
		{
			transitions = new List<Transition>();
		}

		public void Add(Transition transition)
		{
			transitions.Add(transition);
		}

		public Transition PickTransition(Random r)
		{
			var random = r.NextDouble();

			var transitionProbabilities = transitions.Select(x => x.probability());
			double sum = transitionProbabilities.Sum();
			var normalizedTransitions = transitionProbabilities.Select(x => x / sum).ToArray();

			//Console.WriteLine("Normalized transitions: " + string.Join(",", normalizedTransitions));
			//Console.WriteLine("Order : " + string.Join(",", transitions.Select(x => "{" + string.Join(",", x.actions) + "}")));

			sum = 0;
			for (int i = 0; i < normalizedTransitions.Length; i++)
			{
				sum += normalizedTransitions[i];
				if (random <= sum)
					return transitions[i];
			}
			return null;
		}

		public override string ToString()
		{
			return string.Format("{{{0}}}", string.Join (",", transitions.Select (x => "[" + string.Join (",", x.actions) + "]")));
		}
	}
}

