using System;
namespace Simulator
{
	public class PumpMotor : IPumpMotor
	{
		public bool broken;

		public bool Broken { 
			get {
				lock (@lock) {
					return broken;
				}
			}
		}

		object @lock;

		public PumpMotor ()
		{
			broken = false;
			@lock = new object ();
		}

		public bool On {
			get;
			private set;
		}

		public void Break ()
		{
			lock (@lock) {
				broken = true;
				On = false;
				Console.WriteLine (DateTime.Now + " [Break]");
			}
		}

		public void Repair ()
		{
			lock (@lock) {
				broken = false;
				On = false;
				Console.WriteLine (DateTime.Now + " [Repair]");
			}
		}

		public void TurnOn ()
		{
			lock (@lock) {
				if (!broken) {
					On = true;
					Console.WriteLine (DateTime.Now + " [TurnOn]");
				}
			}
		}

		public void TurnOff ()
		{
			lock (@lock) {
				if (!broken) {
					On = false;
					Console.WriteLine (DateTime.Now + " [TurnOff]");
				}
			}
		}
	}
}

