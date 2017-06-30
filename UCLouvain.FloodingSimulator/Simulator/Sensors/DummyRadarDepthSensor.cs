using System;
using MathNet.Numerics.Distributions;
using UCLouvain.FloodingSystem;

namespace UCLouvain.FloodingSimulator
{
	public class DummyRadarDepthSensor : IDepthSensor
	{
		readonly Environment river;
		readonly Random r;

		const double EPSILON = .0001;

		public DummyRadarDepthSensor (Environment river)
		{
			this.river = river;
			this.r = new Random();
		}

		public double GetDepth ()
		{
			// River too high
			//if (Math.Abs(river.Depth - RiverDepth.VERY_HIGH) < EPSILON) {
			//	// Return a random number between VERY_LOW and VERY_HIGH
			//	return RiverDepth.VERY_LOW + r.NextDouble() * RiverDepth.VERY_HIGH;
			//}

			if (river.DepthSensorBroken) {
				return -1;
			}

			if (river.FalseEcho) {
				return Math.Max (0, river.RiverDepth + (Normal.Sample (0, .05) * RiverDepth.VERY_HIGH));
			}

			if (river.Dust) {
				return river.RiverDepth + 1;
			}

			return river.RiverDepth; // + (Normal.Sample (0, .5));
		}
	}
}

