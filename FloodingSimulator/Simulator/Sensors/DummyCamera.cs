using System;
using System.Drawing;
using NLog;
using UCLouvain.FloodingSystem;

namespace UCLouvain.FloodingSimulator
{
	public class DummyCamera : ICamera
	{
		int width;
		int height;
		Environment river;
		double ppm;

		const int square_size = 5;

		Random r;
		private static Logger logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Initializes a new instance of the <see cref="T:FloodingSimulator.DummyCamera"/> class.
		/// </summary>
		/// <param name="width">Width.</param>
		/// <param name="height">Height.</param>
		/// <param name="ppm">The resolution of the camera (Pixel per meter).</param>
		/// <param name="river">River.</param>
		public DummyCamera (int width, int height, double ppm, Environment river)
		{
			this.width = width;
			this.height = height;
			this.ppm = ppm;
			this.river = river;
			r = new Random (0);
		}

		public CustomBitmap [] GetImages (int nb, TimeSpan delay)
		{
			// logger.Info("Acquiring images... " + nb);
			var images = new CustomBitmap [nb];
			// logger.Info("array built ");

			var startX = 1; //r.Next() % (width / 2);
			var startY = r.Next() % (height / 2);
			// logger.Info("Start {0},{1}", startX, startY);

			for (int i = 0; i < nb; i++) {
				// logger.Info("will generate " + i + "  ");

				var image = new CustomBitmap (width, height);
				//logger.Info("Image " + i + " bitmap built ");
				//logger.Info("River speed: " + river.RiverSpeed);
				//logger.Info("Pixels per meter: " + ppm);
				//logger.Info("Delay: " + delay.TotalSeconds);

				// Move the start according the speed of the river
				startX = (int)(startX + river.RiverSpeed * ppm * delay.TotalSeconds);
				//startY = (int) (startY + river.RiverSpeed * ppm * delay.TotalSeconds);
				//logger.Info("Next {0},{1}", startX, startY);

				for (int x = 0; x < square_size; x++) {
					for (int y = 0; y < square_size; y++) {

						image.SetPixel(Math.Min (startX + x, width-1), Math.Min (startY + y, height-1), 1);
					}
				}
				//logger.Info("square drawn ");

				if (river.NoisyImage) {
					for (int j = 0; j < 5; j++) {
						image.SetPixel(r.Next(0, width), r.Next(0, height), 1);
					}
				}

				images [i] = image;
				//logger.Info("Image " + i + " generated ");

				//logger.Trace(image.ToString ());

			}

			return images;
		}
	}
}

