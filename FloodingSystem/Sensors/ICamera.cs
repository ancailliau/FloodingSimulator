using System;
using System.Drawing;
namespace FloodingSystem
{
	public interface ICamera
	{
		CustomBitmap [] GetImages (int nb, TimeSpan delay);
	}
}

