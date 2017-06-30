using System;
namespace UCLouvain.FloodingSystem
{
	public interface IMailProvider
	{
		void SendEmail (string from, string to, string subject, string message);
	}
}

