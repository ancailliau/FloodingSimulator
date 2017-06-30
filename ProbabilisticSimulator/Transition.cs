using System;
namespace UCLouvain.EnvironmentSimulator
{
	public class Transition
	{
		public Func<double> probability;
		public string[] actions;
		public State target;
	}

}

