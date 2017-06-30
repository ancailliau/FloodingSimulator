using System;
namespace UCLouvain.FloodingSystem
{
	public interface IGSMProvider
	{
		void SendSMS (string from, string to, string message);
		void PhoneCall (string from, string to, short [] message);
	}
}

