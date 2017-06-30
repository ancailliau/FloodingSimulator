using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NLog;

namespace UCLouvain.FloodingSystem
{
	public class CameraSpeedEstimator : ISpeedEstimator
	{
		static Logger logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// No square was found in the provided image.
		/// </summary>
		class NoSquareFound : Exception
		{
			CustomBitmap image;

			public NoSquareFound (CustomBitmap image) : base ("No square found in image.")
			{
				this.image = image;
			}
		}

		ICamera camera;
		int samplingNb;
		TimeSpan delay;

		public CameraSpeedEstimator (ICamera camera, int samplingNb, TimeSpan delay)
		{
			this.camera = camera;
			this.samplingNb = samplingNb;
			this.delay = delay;
		}

		Point GetSquareCoordinate (CustomBitmap i)
		{
			for (int x = 0; x < i.Width; x++) {
				for (int y = 0; y < i.Height; y++) {
					var color = i.GetPixel (x, y);
					if (color == 1) {
						return new Point (x, y);
					}
				}
			}

			throw new NoSquareFound (i);
		}

		/// <summary>
		/// Gets the speed in pixels per seconds given the two specified points 
		/// were observed by the specified delay.
		/// </summary>
		/// <returns>The speed.</returns>
		/// <param name="a">The start point.</param>
		/// <param name="b">The end point.</param>
		/// <param name="delay">Delay between the two observations.</param>
		double GetSpeed (Point a, Point b, TimeSpan c)
		{
			var xspeed = (double) (b.X - a.X);
			var yspeed = (double) (b.Y - a.Y);
			//logger.Info("Delta x : " + (xspeed / c.TotalSeconds));
			//logger.Info("Delta y : " + (yspeed / c.TotalSeconds));
			double speed = Math.Sqrt(xspeed * xspeed + yspeed * yspeed) / c.TotalSeconds;
            //logger.Info("Speed acquired by camera: " + speed);
            return speed;
		}

		double ComputePrediction ()
		{
			//logger.Trace("Get camera images");
			var images = this.camera.GetImages (samplingNb, delay);
			//logger.Trace("Got "+images.Count()+" camera images");
			var speeds = new List<double> ();

			Point lastCoordinates = GetSquareCoordinate (images.First ());
			//logger.Info("Coordinates: {0},{1}", lastCoordinates.X, lastCoordinates.Y);
			foreach (var i in images.Skip (1)) {
				try {
					var point = GetSquareCoordinate(i);
					//logger.Info("Coordinates: {0},{1}", point.X, point.Y);
					var v = GetSpeed(lastCoordinates, point, delay);
					speeds.Add(v);
					//logger.Trace("Computed speed: " + v);

					lastCoordinates = point;

				} catch (NoSquareFound) {
					//logger.Trace("No Square Found");
					continue; 
				}

			}

			logger.Trace("Speed estimated by camera: " + speeds.Average ());

			return speeds.Average ();
		}

		public double GetSpeed ()
		{
			return ComputePrediction ();
		}
	}
}

