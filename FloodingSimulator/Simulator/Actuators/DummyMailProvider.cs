using System;
using UCLouvain.FloodingSystem;
using System.Collections.Concurrent;
namespace UCLouvain.FloodingSimulator
{
	public class DummyMailProvider : IMailProvider
	{
		class Email
		{
			string from;
			string to;
			string subject;
			string body;
			DateTime timestamp;

			public Email (string from, string to, string subject, string body)
			{
				this.from = from;
				this.to = to;
				this.subject = subject;
				this.body = body;
				this.timestamp = DateTime.Now;
			}

			public override string ToString ()
			{
				return string.Format ("[Email: from={0}, to={1}, subject={2}, length={3}, timestamp={4}]", 
				                      from, to, subject, body.Length, timestamp);
			}
		}

		readonly ConcurrentBag<Email> emails;
		readonly Environment environment;

		public DummyMailProvider (Environment environment)
		{
			emails = new ConcurrentBag<Email> ();
			this.environment = environment;
		}

		public void SendEmail (string from, string to, string subject, string message)
		{
			emails.Add (new Email (from, to, subject, message));
		}
	}
}

