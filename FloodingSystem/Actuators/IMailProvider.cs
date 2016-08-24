using System;
namespace FloodingSystem
{
	public interface IMailProvider
	{
		void SendEmail (string from, string to, string subject, string message);
	}
}

