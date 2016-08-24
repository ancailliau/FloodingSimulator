using System;

namespace FloodingSystem
{
	public interface ILocalWarner
	{
		void Warn ();
		DateTime? GetLastWarn();
	}
}