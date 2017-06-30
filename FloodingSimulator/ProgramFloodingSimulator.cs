using System;

namespace UCLouvain.FloodingSimulator
{
    class ProgramFloodingSimulator
	{
		static FloodingSimulator simulator;

		public static void Main (string [] args)
		{
            Console.WriteLine ("*** This is FloodingSimulator. ***");
            Console.WriteLine ("*** For more information on KAOSTools see <https://github.com/ancailliau/FloodingSimulator> ***");
            Console.WriteLine ("*** Please report bugs to <https://github.com/ancailliau/FloodingSimulator/issues> ***");
            Console.WriteLine ();
            Console.WriteLine ("*** Copyright (c) 2017, Université catholique de Louvain ***");
            Console.WriteLine ("");
        
			simulator = new FloodingSimulator();
			simulator.Run();

            var monitoringClient = new MonitoringClient(simulator.GetMonitoredState);
            monitoringClient.Run();

            var optimizationClient = new OptimizationClient(simulator);
            optimizationClient.Run();

            bool stop = false;
            while (!stop) {
                Console.Write("> ");
                var input = Console.ReadLine().Trim();
                if (input.Equals("quit") | input.Equals("exit")) {
                    stop = true;
                    continue;
                }

                var cmdArgs = input.Split(' ');
                if (cmdArgs[0].Equals("set")) {
                    try
                    {
                        simulator.SetProbability(cmdArgs[1], double.Parse(cmdArgs[2]));
                    } catch (Exception e) {
                        Console.WriteLine("Could not set probability ("+e.Message+").");
                    }
                    
                } else {
                    Console.WriteLine("Command not recognized.");
                }
            }

            Console.WriteLine("Exiting...");
            simulator.Stop();
            monitoringClient.Stop();
            optimizationClient.Stop();
		}
	}
}
