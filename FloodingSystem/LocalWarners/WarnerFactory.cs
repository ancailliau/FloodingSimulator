using System;
namespace UCLouvain.FloodingSystem
{
	public class WarnerFactory
	{
		readonly IActuatorFactory actuatorFactory;

		public WarnerFactory(IActuatorFactory actuatorFactory)
		{
			this.actuatorFactory = actuatorFactory;
		}

		public EmailWarner GetEmailWarner()
		{
			return new EmailWarner(actuatorFactory.GetMailProvider());
		}

		public PhoneWarner GetPhoneWarner()
		{
			return new PhoneWarner(actuatorFactory.GetGSMProvider());
		}

		public SMSWarner GetSMSWarner()
		{
			return new SMSWarner(actuatorFactory.GetGSMProvider());
		}
	}
}

