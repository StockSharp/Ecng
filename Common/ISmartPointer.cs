namespace Ecng.Common
{
	using System;

	public interface ISmartPointer : IDisposable
	{
		int Counter { get; }

		void IncRef();

		void DecRef();
	}
}