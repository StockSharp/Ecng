namespace Ecng.Common;

#region Using Directives

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

#endregion

#if NET10_0
using SyncObject = System.Threading.Lock;
#endif

/// <summary>
/// Interface for disposable objects that can be disposed with a reason.
/// </summary>
public interface IReasonDisposable : IDisposable
{
	/// <summary>
	/// The reason for disposal.
	/// </summary>
	string Reason { get; }

	/// <summary>
	/// Disposes the object with a reason.
	/// </summary>
	/// <param name="reason">The reason for disposal.</param>
	/// <returns><c>true</c> if the object was successfully disposed; otherwise, <c>false</c>.</returns>
	bool Dispose(string reason);
}

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize

/// <summary>
/// Provides a base class for implementing the dispose pattern. 
/// This class helps manage the disposal of managed and native resources.
/// </summary>
[Serializable]
public abstract class Disposable : IReasonDisposable
{
	private readonly SyncObject _lock = new();

	enum DisposeState : byte
	{
		None,
		Disposing,
		Disposed
	}

	#region IsDisposed

	private DisposeState _state = DisposeState.None;

	/// <summary>
	/// Gets a value indicating whether this instance is disposed.
	/// </summary>
	/// <value>
	///   <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
	/// </value>
	[XmlIgnore]
	[Browsable(false)]
	public bool IsDisposed => _state == DisposeState.Disposed;

	/// <summary>
	/// Gets a value indicating whether the dispose process has been started.
	/// </summary>
	/// <value>
	///   <c>true</c> if the dispose process has been initiated; otherwise, <c>false</c>.
	/// </value>
	[XmlIgnore]
	[Browsable(false)]
	public bool IsDisposeStarted => _state > DisposeState.None;

	#endregion

	/// <summary>
	/// Occurs when the object has been disposed.
	/// </summary>
	public event Action Disposed;

	#region IReasonDisposable Members

	private string _reason;
	string IReasonDisposable.Reason => _reason;

	bool IReasonDisposable.Dispose(string reason)
	{
		lock (_lock)
		{
			if (IsDisposeStarted)
				return false;

			_state = DisposeState.Disposing;
		}

		_reason = reason;

		try
		{
			DisposeManaged();
			DisposeNative();
		}
		finally
		{
			_state = DisposeState.Disposed;
			Disposed?.Invoke();
			GC.SuppressFinalize(this);
		}

		return true;
	}

	#endregion

	#region IDisposable Members

	/// <summary>
	/// Performs tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	public virtual void Dispose()
		=> ((IReasonDisposable)this).Dispose(nameof(IDisposable.Dispose));

	#endregion

	/// <summary>
	/// Disposes the managed resources.
	/// Override this method to add custom clean up of managed resources.
	/// </summary>
	protected virtual void DisposeManaged()
	{
	}

	/// <summary>
	/// Disposes the native resources.
	/// Override this method to add custom clean up of native resources.
	/// </summary>
	protected virtual void DisposeNative()
	{
	}

	/// <summary>
	/// Throws an exception if the dispose process has already been started.
	/// </summary>
	/// <exception cref="ObjectDisposedException">Thrown if the dispose process has already been initiated.</exception>
	protected void ThrowIfDisposeStarted()
	{
		ThrowIfDisposed();

		if (IsDisposeStarted)
			throw new ObjectDisposedException(GetType().Name + " has started dispose process");
	}

	/// <summary>
	/// Throws an exception if the object is already disposed.
	/// </summary>
	/// <exception cref="ObjectDisposedException">Thrown if the object is already disposed.</exception>
	protected void ThrowIfDisposed()
	{
		if (IsDisposed)
			throw new ObjectDisposedException(GetType().Name);
	}

	#region Finalize

	/// <summary>
	/// Releases unmanaged resources and performs other cleanup operations before the
	/// object is reclaimed by garbage collection.
	/// </summary>
	~Disposable()
	{
		// http://stackoverflow.com/a/9903121
		try
		{
			DisposeNative();
		}
		catch (Exception ex)
		{
			Trace.WriteLine(ex);
		}
	}

	#endregion
}
