using System.Threading;
using System;
using NLog;

namespace FloodingSystem
{
	/// <summary>
	/// Represents a flooding warning system based on measured depth and speed. 
	/// 
	/// Once the two factors reach their corresponding critical levels, a warning
	/// is issed for the locals.
	/// </summary>
	public class FloodingWarningSystem
	{
		/// <summary>
		/// The speed estimator.
		/// </summary>
		ISpeedEstimator speedEstimator;

		/// <summary>
		/// The depth estimator.
		/// </summary>
		IDepthEstimator depthEstimator;

		/// <summary>
		/// The local warner module.
		/// </summary>
		ILocalWarner localWarner;

		/// <summary>
		/// The critical depth in m.
		/// </summary>
		public const double CRITICAL_DEPTH = 5;

		/// <summary>
		/// The critical speed in pixels per ms.
		/// </summary>
		public const double CRITICAL_SPEED = 5;

		private static Logger logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Initializes a new instance of the flooding warning system.
		/// </summary>
		/// <param name="speedEstimator">Speed estimator.</param>
		/// <param name="depthEstimator">Depth estimator.</param>
		/// <param name="localWarner">Local warner.</param>
		public FloodingWarningSystem (ISpeedEstimator speedEstimator, 
		                              IDepthEstimator depthEstimator,
		                              ILocalWarner localWarner)
		{
			Deploy(speedEstimator);
			Deploy(depthEstimator);
			Deploy(localWarner);
			InitializeMonitoringHooks();
		}

		/// <summary>
		/// Deploy the specified speedEstimator as the new estimator for speed.
		/// </summary>
		/// <param name="speedEstimator">Speed estimator.</param>
		public void Deploy (ISpeedEstimator speedEstimator)
		{
			this.speedEstimator = speedEstimator;
		}

		/// <summary>
		/// Deploy the specified depthEstimator as the new estimator for depth.
		/// </summary>
		/// <param name="depthEstimator">Depth estimator.</param>
		public void Deploy (IDepthEstimator depthEstimator)
		{
			this.depthEstimator = depthEstimator;
		}

		/// <summary>
		/// Deploy the specified localWarner as the new warner module.
		/// </summary>
		/// <param name="localWarner">Local warner.</param>
		public void Deploy (ILocalWarner localWarner)
		{
			this.localWarner = localWarner;
		}

		bool LocalWarned = false;

		#region Monitoring hooks

		public bool DepthAcquired {
			get;
			private set;
		}

		public bool SpeedAcquired {
			get;
			private set;
		}

		public double MeasuredDepth {
			get;
			private set;
		}

		public double MeasuredSpeed {
			get;
			private set;
		}

		public bool UltraSoundActive { 
			get {
				return speedEstimator is UltrasoundSpeedEstimator;
			}
		}
		public bool CameraActive {
			get {
				return speedEstimator is CameraSpeedEstimator;
			}
		}

		public bool Phone {
			get {
				return localWarner is PhoneWarner;
			}
		}

		public bool SMS {
			get {
				return localWarner is SMSWarner;
			}
		}

		public bool Email {
			get {
				return localWarner is EmailWarner;
			}
		}

		void InitializeMonitoringHooks ()
		{
			DepthAcquired = false;
			SpeedAcquired = false;
		}

		#endregion

		/// <summary>
		/// Run the controller.
		/// </summary>
		public void Run ()
		{
			while (true) {
				
				MeasuredDepth = AcquireDepth ();
				MeasuredSpeed = AcquireSpeed ();

				if (DepthAcquired & SpeedAcquired) {
					logger.Info("Speed ({0}) and Depth ({1}) Acquired", MeasuredSpeed, MeasuredDepth);

					if (MeasuredDepth > CRITICAL_DEPTH && MeasuredSpeed > CRITICAL_SPEED && !LocalWarned) {
						WarnLocals();
					}

					if (MeasuredDepth <= CRITICAL_DEPTH && MeasuredSpeed <= CRITICAL_SPEED) {
						LocalWarned = false;
					}
				} else {
					if (DepthAcquired)
						logger.Info("Speed not acquired");
					else if (SpeedAcquired)
						logger.Info("Depth not acquired");
					else
						logger.Info("Speed and depth not acquired");
				}

				Thread.Sleep (TimeSpan.FromSeconds (1));

				// Clean old information
				DepthAcquired = false;
				SpeedAcquired = false;
			}
		}

		/// <summary>
		/// Acquires the speed.
		/// </summary>
		/// <returns>The speed.</returns>
		double AcquireSpeed ()
		{
			var value = speedEstimator.GetSpeed();
			if (value >= 0)
				SpeedAcquired = true;
			return value;
		}

		/// <summary>
		/// Acquires the depth.
		/// </summary>
		/// <returns>The depth.</returns>
		double AcquireDepth ()
		{
			var value = depthEstimator.GetDepth();
			if (value >= 0) 
				DepthAcquired = true;
			return value;
		}

		/// <summary>
		/// Warns the locals.
		/// </summary>
		/// <returns>The locals.</returns>
		void WarnLocals ()
		{
			logger.Info ("Warning Locals");
			localWarner.Warn ();
			LocalWarned = true;
		}
	}
}

