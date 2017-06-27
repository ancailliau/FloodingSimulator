using System;

namespace FloodingSimulator
{
    class MainClass
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
		}
	}
}
