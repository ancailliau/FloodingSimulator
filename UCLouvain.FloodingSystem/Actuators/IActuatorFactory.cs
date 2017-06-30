using System;
namespace UCLouvain.FloodingSystem
{
	public interface IActuatorFactory
	{
		IGSMProvider GetGSMProvider ();
		IMailProvider GetMailProvider ();
	}
}

