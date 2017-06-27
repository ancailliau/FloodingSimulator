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
			logger.Info("Depth measured by radar: {0}", value);
			return value;
		}
	}
}

