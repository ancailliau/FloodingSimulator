using System;
namespace UCLouvain.FloodingSystem
{
	public interface ISensorFactory
	{
		ICamera GetCamera  ();
		IDepthSensor GetDepthSensor ();
		IUltrasoundSensor GetUltrasoundSensor ();
	}
}

