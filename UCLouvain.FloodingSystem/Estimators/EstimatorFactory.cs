using System;
namespace UCLouvain.FloodingSystem
{
	public class EstimatorFactory
	{
		ISensorFactory sensorFactory;
		IActuatorFactory actuatorFactory;

		public EstimatorFactory (ISensorFactory sensorFactory, 
		                         IActuatorFactory actuatorFactory)
		{
			this.sensorFactory = sensorFactory;
			this.actuatorFactory = actuatorFactory;
		}

		public RadarDepthEstimator GetRadarDepthEstimator()
		{
			return new RadarDepthEstimator(sensorFactory.GetDepthSensor());
		}

		public CameraSpeedEstimator GetCameraSpeedEstimator()
		{
			return new CameraSpeedEstimator(sensorFactory.GetCamera(), 3, TimeSpan.FromSeconds(1));
		}

		public UltrasoundSpeedEstimator GetUltrasoundSpeedEstimator()
		{
			return new UltrasoundSpeedEstimator(sensorFactory.GetUltrasoundSensor());
		}
	}
}

