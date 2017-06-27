using System;
using NLog;

namespace FloodingSystem
{
	public class UltrasoundSpeedEstimator : ISpeedEstimator 
	{
		IUltrasoundSensor sensor;

		static Logger logger = LogManager.GetCurrentClassLogger();

		public UltrasoundSpeedEstimator (IUltrasoundSensor sensor)
		{
			this.sensor = sensor;
		}

		public double GetSpeed ()
		{
			var v = sensor.GetUltrasoundSpeedData();
			logger.Info ("Speed estimated by ultrasound: {0} m/s", v);
			return v;
		}
	}
}

