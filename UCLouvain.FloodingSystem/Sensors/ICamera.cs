using System;
using System.Drawing;
namespace UCLouvain.FloodingSystem
{
	public interface ICamera
	{
		CustomBitmap [] GetImages (int nb, TimeSpan delay);
	}
}

