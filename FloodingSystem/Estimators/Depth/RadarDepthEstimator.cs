using System;
using NLog;

namespace FloodingSystem
{
	public class RadarDepthEstimator : IDepthEstimator 
	{
		IDepthSensor sensor;
		static Logger logger = LogManager.GetCurrentClassLogger();

		public RadarDepthEstimator (IDepthSensor sensor)
		{
			this.sensor = sensor;
		}

		public double GetDepth ()
		{
			var value = sensor.GetDepth();
			logger.Info("Measured depth: {0}", value);
			return value;
		}
	}
}

