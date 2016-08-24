using System;
namespace FloodingSystem
{
	public interface IActuatorFactory
	{
		IGSMProvider GetGSMProvider ();
		IMailProvider GetMailProvider ();
	}
}

