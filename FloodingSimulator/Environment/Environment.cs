using System;
using NLog;

namespace FloodingSimulator
{
	public static class RiverDepth
	{
		public const double VERY_LOW = 0;
		public const double LOW = 2;
		public const double MEDIUM = 4;
		public const double HIGH = 6;
		public const double VERY_HIGH = 8;
	}

	public class Environment
	{
		static Logger logger = LogManager.GetCurrentClassLogger();

		public enum RiverSpeedValues
		{
			LOW, MEDIUM, HIGH
		};

		/// <summary>
		/// Gets or sets the speed of the river, in meters per second.
		/// </summary>
		/// <value>The speed.</value>
		public double RiverSpeed { get; protected set; }

		/// <summary>
		/// Gets or sets the depth of the river, in meters.
		/// </summary>
		/// <value>The depth.</value>
		public double RiverDepth { get; protected set; }


		public bool Dust { get; set; }
		public bool FalseEcho { get; set; }
		public bool DepthSensorBroken { get; set; }

		public bool UltrasoundSensorBroken { get; set; }
		public bool UltrasoundDistortion { get; set; }
		public bool NoisyImage { get; set; }
		public bool GSMNetworkDown { get; set; }
		public bool VoiceNetworkOveloaded { get; set; }

		public Environment ()
		{
			Dust = false;
			FalseEcho = false;
			DepthSensorBroken = false;
			UltrasoundSensorBroken = false;
			UltrasoundDistortion = false;
			NoisyImage = false;
			GSMNetworkDown = false;
			VoiceNetworkOveloaded = false;
		}

		/// <summary>
		/// Sets the speed.
		/// </summary>
		/// <returns>The speed.</returns>
		/// <param name="speed">Speed.</param>
		/// <exception cref="T:System.NotImplementedException"></exception>
		public void SetSpeed (RiverSpeedValues speed)
		{
			// logger.Info("SetSpeed to {0}", Enum.GetName(typeof(RiverSpeedValues), speed));

			if (speed == RiverSpeedValues.LOW) {
				RiverSpeed = 2;
			} else if (speed == RiverSpeedValues.MEDIUM) {
				RiverSpeed = 4;
			} else if (speed == RiverSpeedValues.HIGH) {
				RiverSpeed = 6;
			} else {
				throw new NotImplementedException(
					string.Format("{0} is not recognized as a valid river speed",
								   Enum.GetName(typeof(RiverSpeedValues), speed))
				);
			}
		}

		/// <summary>
		/// Sets the depth of the river.
		/// </summary>
		/// <param name="depth">Depth.</param>
		/// <exception cref="T:System.NotImplementedException"></exception>
		public void SetDepth (double depth)
		{
			//logger.Info("SetDepth to {0}", depth);
			RiverDepth = depth;
		}
	}
}

