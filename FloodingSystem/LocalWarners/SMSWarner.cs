using System;
namespace UCLouvain.FloodingSystem
{
	public class SMSWarner : ILocalWarner
	{
		IGSMProvider provider;
		DateTime? lastWarn;

		public SMSWarner (IGSMProvider provider)
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
			lastWarn = DateTime.Now;
			provider.SendSMS ("0131 496 0000", "03069 990001", "Iminent flooding");
		}
	}
}

