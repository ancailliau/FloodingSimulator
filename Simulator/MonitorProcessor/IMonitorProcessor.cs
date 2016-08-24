using System;
namespace Simulator
{
	public interface IMonitorProcessor
	{
		void Process (Monitor monitor);
	}
	public interface IGoalMonitorProcessor
	{
		void Process(GoalMonitor monitor);
	}
}

