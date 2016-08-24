using System;
namespace Simulator
{
	public class MethaneDetector : IMethaneDetector
	{
		public MethaneDetector ()
		{
			IsMethane = false;
		}

		public bool IsMethane {
			get;
			private set;
		}

		public void MethaneAppears ()
		{
			Console.WriteLine (DateTime.Now + " [MethaneAppears]");
			IsMethane = true;
		}

		public void MethaneLeaves ()
		{
			Console.WriteLine (DateTime.Now + " [MethaneLeaves]");
			IsMethane = false;
		}
	}
}

