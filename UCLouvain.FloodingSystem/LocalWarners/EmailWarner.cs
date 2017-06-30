using System;
namespace UCLouvain.FloodingSystem
{
	public class EmailWarner : ILocalWarner
	{
		IMailProvider provider;
		DateTime? lastWarn;

		public EmailWarner (IMailProvider provider)
		{
			this.provider = provider;
			lastWarn = null;
		}

		public DateTime? GetLastWarn()
		{
			return lastWarn;
		}

		public void Warn ()
		{
			provider.SendEmail ("floodwarning@example.com", 
			                    "users.floodwarning@example.com", 
			                    "Flood iminent", 
			                    "Bla");
		}
	}
}

