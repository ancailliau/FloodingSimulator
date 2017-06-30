using System;
using FloodingSystem;
using UCLouvain.EnvironmentSimulator;
using System.Collections.Generic;
using System.Threading;
using NLog;

namespace FloodingSimulator
{
    public class FloodingSimulator
	{
		Environment environment;

		readonly TimeSpan SIMULATION_DELAY = TimeSpan.FromSeconds(1);

		ISensorFactory sensorFactory;
		IActuatorFactory actuatorFactory;
		EstimatorFactory estimatorFactory;
		WarnerFactory warnerFactory;

		ILocalWarner warner;

		FloodingWarningSystem controller;

		DummyRadarDepthSensor depthSensor;

		SimulatedEnvironment system;
		Dictionary<string, Action> actions;

		private static Logger logger = LogManager.GetCurrentClassLogger();

		public FloodingSimulator ()
		{
			environment = new Environment ();
			BuildSimulation();

			sensorFactory = new DummySensorFactory(environment);
			actuatorFactory = new DummyActuatorFactory(environment);
			estimatorFactory = new EstimatorFactory(sensorFactory, actuatorFactory);
			warnerFactory = new WarnerFactory(actuatorFactory);

			var speedEstimator = estimatorFactory.GetUltrasoundSpeedEstimator();
			var depthEstimator = estimatorFactory.GetRadarDepthEstimator();
			warner = warnerFactory.GetSMSWarner();

			controller = new FloodingWarningSystem (speedEstimator, depthEstimator, warner);
		}

		bool camera = false;

		public void DeployCamera()
		{
			if (!controller.CameraActive) {
				controller.Deploy(estimatorFactory.GetCameraSpeedEstimator());
			}
		}

		public void DeployUltrasound()
		{
			if (!controller.UltraSoundActive) {
				controller.Deploy(estimatorFactory.GetUltrasoundSpeedEstimator());
			}
		}


		public void DeployEmail()
		{
			if (!controller.Email) {
				controller.Deploy(warnerFactory.GetEmailWarner());
			}
		}

		public void DeploySMS()
		{
			if (!controller.SMS) {
				controller.Deploy(warnerFactory.GetSMSWarner ());
			}
		}

		public void DeployPhone()
		{
			if (!controller.Phone) {
				controller.Deploy(warnerFactory.GetPhoneWarner());
			}
		}

        internal void Stop()
        {
            system.Stop();
            controller.Stop();
        }

        Dictionary<string, double> probability = new Dictionary<string, double>();
        
        public void SetProbability (string key, double value)
        {
            probability[key] = value;
        }
        
        void SetDefaultProbability ()
        {
            probability["dustAppears"] = .3;
            probability["dustLeaves"] = .45;
            
            probability["noisyImagesAppears"] = 0;
            probability["noisyImagesLeaves"] = 1;
            
            probability["ultrasoundSensorBreak"] = .25;
            probability["ultrasoundSensorRepair"] = .35;
        }

		void BuildSimulation()
		{
            SetDefaultProbability();
			var startTime = DateTime.Now;
			system = new SimulatedEnvironment();

			system.InitSubenvironment(0);
			system.AddState(0, "depthLow", true);
			system.AddState(0, "depthMedium");
			system.AddState(0, "depthHigh");

			system.AddTransition(0, "depthLow", "depthMedium", new string[] { "depthMedium" }, .8);
			system.AddTransition(0, "depthLow", "depthLow", new string[] { }, .2);

			system.AddTransition(0, "depthMedium", "depthLow", new string[] { "depthLow" }, .3);
			system.AddTransition(0, "depthMedium", "depthHigh", new string[] { "depthHigh" }, .3);
			system.AddTransition(0, "depthMedium", "depthMedium", new string[] { }, .4);

			system.AddTransition(0, "depthHigh", "depthMedium", new string[] { "depthMedium" }, .3);
			system.AddTransition(0, "depthHigh", "depthHigh", new string[] { }, .7);

			system.InitSubenvironment(1);
			system.AddState(1, "speedLow", true);
			system.AddState(1, "speedMedium");
			system.AddState(1, "speedHigh");

			Func<double> lowToMedium = () => (environment.RiverDepth > 4) ? .6 : .4;
			Func<double> mediumToHigh = () => (environment.RiverDepth > 4) ? .7 : .5;
			Func<double> highToMedium = () => (environment.RiverDepth > 4) ? .8 : .2;

			system.AddTransition(1, "speedLow", "speedMedium", new string[] { "speedMedium" }, lowToMedium);
			system.AddTransition(1, "speedLow", "speedLow", new string[] { }, () => 1 - lowToMedium ());

			system.AddTransition(1, "speedMedium", "speedLow", new string[] { "speedLow" }, () => (1 - mediumToHigh()) / 3);
			system.AddTransition(1, "speedMedium", "speedHigh", new string[] { "speedHigh" }, mediumToHigh);
			system.AddTransition(1, "speedMedium", "speedMedium", new string[] { }, () => 2 * (1 - mediumToHigh()) / 3);

			system.AddTransition(1, "speedHigh", "speedMedium", new string[] { "speedMedium" }, highToMedium);
			system.AddTransition(1, "speedHigh", "speedHigh", new string[] { }, () => 1 - highToMedium());

			//

			system.InitSubenvironment(2);
			system.AddState(2, "notDusty", true);
			system.AddState(2, "dusty");

			system.AddTransition(2, "notDusty", "dusty", new string[] { "dustAppears" }, () => probability["dustAppears"]);
			system.AddTransition(2, "notDusty", "notDusty", new string[] { }, () => 1 - probability["dustAppears"]);
			system.AddTransition(2, "dusty", "notDusty", new string[] { "dustLeaves" }, () => probability["dustLeaves"]);
			system.AddTransition(2, "dusty", "dusty", new string[] { },  () => 1 - probability["dustLeaves"]);

			//

			system.InitSubenvironment(3);
			system.AddState(3, "noFalseEcho", true);
			system.AddState(3, "falseEcho");

			system.AddTransition(3, "noFalseEcho", "falseEcho", new string[] { "falseEchoAppears" }, 0.1);
			system.AddTransition(3, "noFalseEcho", "noFalseEcho", new string[] { }, 0.9);
			system.AddTransition(3, "falseEcho", "noFalseEcho", new string[] { "falseEchoLeaves" }, 0.5);
			system.AddTransition(3, "falseEcho", "falseEcho", new string[] { }, 0.5);

			//

			system.InitSubenvironment(4);
			system.AddState(4, "depthSensorWorking", true);
			system.AddState(4, "depthSensorBroken");

			system.AddTransition(4, "depthSensorWorking", "depthSensorBroken", new string[] { "depthSensorBroken" }, 0.15);
			system.AddTransition(4, "depthSensorWorking", "depthSensorWorking", new string[] { }, 0.85);
			system.AddTransition(4, "depthSensorBroken", "depthSensorWorking", new string[] { "depthSensorRepair" }, 0.3);
			system.AddTransition(4, "depthSensorBroken", "depthSensorBroken", new string[] { }, 0.7);

			// UltrasoundSensorBroken

			int id = 5;
			system.InitSubenvironment(id);
			system.AddState(id, "UltrasoundSensorNotBroken", true);
			system.AddState(id, "UltrasoundSensorBroken");
			system.AddTransition(id, "UltrasoundSensorNotBroken", "UltrasoundSensorBroken", new string[] { "ultrasoundSensorBreak" }, () => probability["ultrasoundSensorBreak"]);
			system.AddTransition(id, "UltrasoundSensorNotBroken", "UltrasoundSensorNotBroken", new string[] { }, () => 1 - probability["ultrasoundSensorBreak"]);
			system.AddTransition(id, "UltrasoundSensorBroken", "UltrasoundSensorNotBroken", new string[] { "ultrasoundSensorRepair" }, () => probability["ultrasoundSensorRepair"]);
			system.AddTransition(id, "UltrasoundSensorBroken", "UltrasoundSensorBroken", new string[] { }, () => 1 - probability["ultrasoundSensorRepair"]);

			// UltrasoundDistortion

			id++;
			system.InitSubenvironment(id);
			system.AddState(id, "NoUltrasoundDistortion", true);
			system.AddState(id, "UltrasoundDistortion");
			system.AddTransition(id, "NoUltrasoundDistortion", "UltrasoundDistortion", new string[] { "ultrasoundDistortionAppears" }, 0.25);
			system.AddTransition(id, "NoUltrasoundDistortion", "NoUltrasoundDistortion", new string[] { }, 0.75);
			system.AddTransition(id, "UltrasoundDistortion", "NoUltrasoundDistortion", new string[] { "ultrasoundDistortionLeaves" }, 0.35);
			system.AddTransition(id, "UltrasoundDistortion", "UltrasoundDistortion", new string[] { }, 0.65);

			// NoisyImage

			id++;
			system.InitSubenvironment(id);
			system.AddState(id, "NoiselessImage", true);
			system.AddState(id, "NoisyImage");
			system.AddTransition(id, "NoiselessImage", "NoisyImage", new string[] { "noisyImagesAppears" }, () => probability["noisyImagesAppears"]);
			system.AddTransition(id, "NoiselessImage", "NoiselessImage", new string[] { }, () => 1 - probability["noisyImagesAppears"]);
			system.AddTransition(id, "NoisyImage", "NoiselessImage", new string[] { "noisyImagesLeaves" },  () => probability["noisyImagesLeaves"]);
			system.AddTransition(id, "NoisyImage", "NoisyImage", new string[] { },  () => 1 - probability["noisyImagesLeaves"]);

			// GSMNetworkDown

			id++;
			system.InitSubenvironment(id);
			system.AddState(id, "GSMNetworkUp", true);
			system.AddState(id, "GSMNetworkDown");
			system.AddTransition(id, "GSMNetworkUp", "GSMNetworkDown", new string[] { "gsmDown" }, 0.1);
			system.AddTransition(id, "GSMNetworkUp", "GSMNetworkUp", new string[] { }, 0.9);
			system.AddTransition(id, "GSMNetworkDown", "GSMNetworkUp", new string[] { "gsmUp" }, 0.65);
			system.AddTransition(id, "GSMNetworkDown", "GSMNetworkDown", new string[] { }, 0.35);

			// VoiceNetworkOveloaded

			id++;
			system.InitSubenvironment(id);
			system.AddState(id, "VoiceNetworkWorking", true);
			system.AddState(id, "VoiceNetworkOveloaded");
			system.AddTransition(id, "VoiceNetworkWorking", "VoiceNetworkOveloaded", new string[] { "voiceNetworkOverload" }, 0.1);
			system.AddTransition(id, "VoiceNetworkWorking", "VoiceNetworkWorking", new string[] { }, 0.9);
			system.AddTransition(id, "VoiceNetworkOveloaded", "VoiceNetworkWorking", new string[] { "voiceNetworkDeload" }, 0.58);
			system.AddTransition(id, "VoiceNetworkOveloaded", "VoiceNetworkOveloaded", new string[] { }, 0.42);

			//

			actions = new Dictionary<string, Action>();
			actions.Add("depthLow", () => environment.SetDepth (RiverDepth.LOW));
			actions.Add("depthMedium", () => environment.SetDepth(RiverDepth.MEDIUM));
			actions.Add("depthHigh", () => environment.SetDepth(RiverDepth.HIGH));

			actions.Add("speedLow", () => environment.SetSpeed(Environment.RiverSpeedValues.LOW));
			actions.Add("speedMedium", () => environment.SetSpeed(Environment.RiverSpeedValues.MEDIUM));
			actions.Add("speedHigh", () => environment.SetSpeed(Environment.RiverSpeedValues.HIGH));

			actions.Add("dustAppears", () => environment.Dust = true);
			actions.Add("dustLeaves", () => environment.Dust = false);

			actions.Add("falseEchoAppears", () => environment.FalseEcho = true);
			actions.Add("falseEchoLeaves", () => environment.FalseEcho = false);

			actions.Add("depthSensorBroken", () => environment.DepthSensorBroken = true);
			actions.Add("depthSensorRepair", () => environment.DepthSensorBroken = false);

			actions.Add("ultrasoundSensorBreak", () => environment.UltrasoundSensorBroken = true);
			actions.Add("ultrasoundSensorRepair", () => environment.UltrasoundSensorBroken = false);

			actions.Add("ultrasoundDistortionAppears", () => environment.UltrasoundDistortion = true);
			actions.Add("ultrasoundDistortionLeaves", () => environment.UltrasoundDistortion = false);

			actions.Add("noisyImagesAppears", () => environment.NoisyImage = true);
			actions.Add("noisyImagesLeaves", () => environment.NoisyImage = false);

			actions.Add("gsmDown", () => environment.GSMNetworkDown = true);
			actions.Add("gsmUp", () => environment.GSMNetworkDown = false);

			actions.Add("voiceNetworkOverload", () => environment.VoiceNetworkOveloaded = true);
			actions.Add("voiceNetworkDeload", () => environment.VoiceNetworkOveloaded = false);
		}

		public void Run ()
		{
			var startController = new ThreadStart (controller.Run);
			var startSimulation = new ThreadStart(() => system.Run(actions, SIMULATION_DELAY));

			var threadController = new Thread(startController);
			var threadSimulation = new Thread(startSimulation);

			threadController.Start();
			threadSimulation.Start();
		}

		internal Dictionary<string, bool> GetMonitoredState()
		{
			var ms = new Dictionary<string, bool>();
			ms.Add("radarDepthAcquired", controller.DepthAcquired);
			ms.Add("speedAcquiredByUltraSound", controller.UltraSoundActive & controller.SpeedAcquired);
			ms.Add("speedAcquiredByCamera", controller.CameraActive & controller.SpeedAcquired);

			
			var lastWarn = warner.GetLastWarn();
			if (lastWarn == null) {
				ms.Add("localsWarnedByPhone", false);
			} else {
                ms.Add("localsWarnedByPhone", controller.Phone & (DateTime.Now - ((DateTime)lastWarn)).TotalSeconds < 5);
			}

			if (lastWarn == null) {
				ms.Add("localsWarnedBySMS", false);
			} else {
				ms.Add("localsWarnedBySMS", controller.SMS & (DateTime.Now - ((DateTime)lastWarn)).TotalSeconds < 5);
			}

            if (lastWarn == null) {
                ms.Add("localsWarnedByEmail", false);
            } else {
                ms.Add("localsWarnedByEmail", controller.Email & (DateTime.Now - ((DateTime)lastWarn)).TotalSeconds < 5);
            }

			ms.Add("depthAccurate", Math.Abs (environment.RiverDepth - controller.MeasuredDepth) < 1);
			ms.Add("speedAccurate", Math.Abs(environment.RiverSpeed - controller.MeasuredSpeed) < 1);

			ms.Add("acquiredDepthCritical", controller.MeasuredDepth >= 5);
			ms.Add("acquiredSpeedCritical", controller.MeasuredSpeed >= 5);

			ms.Add("dustyEnvironment", environment.Dust);
			ms.Add("falseEcho", environment.FalseEcho);
			ms.Add("radarDepthSensorBroken", environment.DepthSensorBroken);

			ms.Add("ultrasoundSensorBroken", environment.UltrasoundSensorBroken);
			ms.Add("ultrasoundDistortion", environment.UltrasoundDistortion);
			ms.Add("noisyImage", environment.NoisyImage);
			ms.Add("gsmNetworkDown", environment.GSMNetworkDown);
			ms.Add("voiceNetworkOverloaded", environment.VoiceNetworkOveloaded);

			return ms;
		}
	}
}

