using System;
using FloodingSystem;
namespace FloodingSimulator
{
	public class DummyUltrasoundSensor : IUltrasoundSensor
	{
		readonly Environment river;
		Random r;

		public DummyUltrasoundSensor (Environment river)
		{
			this.river = river;
			r = new Random(Guid.NewGuid().GetHashCode ());
		}

		public double GetUltrasoundSpeedData ()
		{
			if (river.UltrasoundSensorBroken) {
				return -1;
			}

			if (river.UltrasoundDistortion) {
				return Math.Max (0, river.RiverSpeed + (r.NextDouble () - 0.5) * 2);
			}

			return river.RiverSpeed;
		}
	}
}

