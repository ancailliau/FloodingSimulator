using System;
namespace Simulator
{
	public class WaterLevelSensor : IWaterLevelSensor
	{
		public WaterLevelSensor ()
		{
			Level = 0;
		}

		public int Level {
			get;
			private set;
		}

		public void AboveHigh ()
		{
			Console.WriteLine (DateTime.Now + " [AboveHigh]");
			Level = 2;
		}

		public void BelowLow ()
		{
			Console.WriteLine (DateTime.Now + " [BelowLow]");
			Level = 0;
		}

		public void AboveLow ()
		{
			Console.WriteLine (DateTime.Now + " [AboveLow]");
			Level = 1;
		}
	}
}

