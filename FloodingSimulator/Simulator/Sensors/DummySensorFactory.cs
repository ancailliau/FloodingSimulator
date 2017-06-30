using System;
using UCLouvain.FloodingSystem;
namespace UCLouvain.FloodingSimulator
{
	public class DummySensorFactory : ISensorFactory
	{
		Environment river;

		public DummySensorFactory(Environment river)
		{
			this.river = river;
		}

		public ICamera GetCamera()
		{
			return new DummyCamera(20, 20, 1, river);
		}

		public IDepthSensor GetDepthSensor()
		{
			return new DummyRadarDepthSensor(river);
		}

		public IUltrasoundSensor GetUltrasoundSensor()
		{
			return new DummyUltrasoundSensor(river);
		}
	}
}

