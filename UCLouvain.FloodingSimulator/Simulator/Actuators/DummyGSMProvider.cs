using System;
using System.Collections.Generic;
using UCLouvain.FloodingSystem;
using System.Collections.Concurrent;
namespace UCLouvain.FloodingSimulator
{
	public class DummyGSMProvider : IGSMProvider
	{
		class SMS
		{
			readonly string from;
			readonly string to;
			readonly string message;
			readonly DateTime timestamp;

			public SMS (string from, string to, string message)
			{
				this.from = from;
				this.to = to;
				this.message = message;
				timestamp = DateTime.Now;
			}

			public override string ToString ()
			{
				return string.Format ("[SMS: from={0}, to={1}, message={2}, timestamp={3}]", 
				                      from, to, message, timestamp);
			}
		}

		class Call
		{
			readonly string from;
			readonly string to;
			readonly short [] message;
			readonly DateTime timestamp;

			public Call (string from, string to, short [] message)
			{
				this.from = from;
				this.to = to;
				this.message = message;
				timestamp = DateTime.Now;
			}

			public override string ToString ()
			{
				return string.Format ("[Call: from={0}, to={1}, duration={2}, timestamp={3}]", 
				                      from, to, message.Length, timestamp);
			}
		}

		readonly ConcurrentBag<SMS> sms;
		readonly ConcurrentBag<Call> calls;

		readonly Environment environment;
		Random r;

		public DummyGSMProvider (Environment environment)
		{
			sms = new ConcurrentBag<SMS> ();
			calls = new ConcurrentBag<Call> ();
			this.environment = environment;
			r = new Random(Guid.NewGuid().GetHashCode());
		}

		public void PhoneCall (string from, string to, short [] message)
		{
			if (!environment.GSMNetworkDown) {
				if (!environment.VoiceNetworkOveloaded) {
					calls.Add(new Call(from, to, message));
				} else {
					if (r.NextDouble () > .7) 
						calls.Add(new Call(from, to, message));
				}
			}
		}

		public void SendSMS (string from, string to, string message)
		{
			if (!environment.GSMNetworkDown) {
				sms.Add(new SMS(from, to, message));
			}
		}
	}
}

