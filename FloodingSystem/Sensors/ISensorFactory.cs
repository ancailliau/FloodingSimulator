using System;
namespace FloodingSystem
{
	public interface ISensorFactory
	{
		ICamera GetCamera  ();
		IDepthSensor GetDepthSensor ();
		IUltrasoundSensor GetUltrasoundSensor ();
	}
}

