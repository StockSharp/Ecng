namespace Ecng.Serialization
{
	using System;

	public interface IBatchContext : IDisposable
	{
		void Commit();
	}
}