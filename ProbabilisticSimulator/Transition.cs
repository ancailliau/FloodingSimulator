using System;
namespace ProbabilisticSimulator
{
	public class Transition
	{
		public Func<double> probability;
		public string[] actions;
		public State target;
	}

}

