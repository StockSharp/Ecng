namespace Ecng.Common
{
	using System;

	/// <summary>
	/// Represents a smart pointer that uses reference counting to manage resource lifetimes.
	/// </summary>
	public interface ISmartPointer : IDisposable
	{
		/// <summary>
		/// Gets the current reference counter.
		/// </summary>
		int Counter { get; }

		/// <summary>
		/// Increments the reference counter.
		/// </summary>
		void IncRef();

		/// <summary>
		/// Decrements the reference counter.
		/// </summary>
		void DecRef();
	}
}