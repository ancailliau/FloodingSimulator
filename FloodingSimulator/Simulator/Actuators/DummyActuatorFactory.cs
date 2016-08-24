using System;
using FloodingSystem;
namespace FloodingSimulator
{
	public class DummyActuatorFactory : IActuatorFactory
	{
		Environment environment;

		public DummyActuatorFactory(Environment environment)
		{
			this.environment = environment;
		}

		public IGSMProvider GetGSMProvider()
		{
			return new DummyGSMProvider(environment);
		}

		public IMailProvider GetMailProvider()
		{
			return new DummyMailProvider(environment);
		}
	}
}

