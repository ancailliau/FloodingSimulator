using System;
namespace FloodingSystem
{
	public class PhoneWarner : ILocalWarner
	{
		IGSMProvider provider;
		DateTime? lastWarn;

		public PhoneWarner (IGSMProvider provider)
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
			provider.PhoneCall ("0131 496 0000", "03069 990001", new short [] { });
		}
	}
}

