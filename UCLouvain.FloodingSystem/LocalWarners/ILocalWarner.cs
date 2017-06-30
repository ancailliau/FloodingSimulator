using System;

namespace UCLouvain.FloodingSystem
{
	public interface ILocalWarner
	{
		void Warn ();
		DateTime? GetLastWarn();
	}
}