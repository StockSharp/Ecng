namespace Ecng.Common;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

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

/// <summary>
/// Interface for async disposable objects that can be disposed with a reason.
/// </summary>
public interface IAsyncReasonDisposable : IAsyncDisposable
{
	/// <summary>
	/// The reason for disposal.
	/// </summary>
	string Reason { get; }

	/// <summary>
	/// Disposes the object asynchronously with a reason.
	/// </summary>
	/// <param name="reason">The reason for disposal.</param>
	/// <returns><c>true</c> if the object was successfully disposed; otherwise, <c>false</c>.</returns>
	ValueTask<bool> DisposeAsync(string reason);
}

/// <summary>
/// Base class with common dispose state management for both sync and async dispose patterns.
/// </summary>
public abstract class DisposableBase
{
	private readonly Lock _lock = new();

	/// <summary>
	/// Dispose state enumeration.
	/// </summary>
	protected enum DisposeState : byte
	{
		/// <summary>Not disposed.</summary>
		None,
		/// <summary>Dispose in progress.</summary>
		Disposing,
		/// <summary>Disposed.</summary>
		Disposed
	}

	private DisposeState _state = DisposeState.None;

	/// <summary>
	/// Gets a value indicating whether this instance is disposed.
	/// </summary>
	[XmlIgnore]
	[Browsable(false)]
	public bool IsDisposed => _state == DisposeState.Disposed;

	/// <summary>
	/// Gets a value indicating whether the dispose process has been started.
	/// </summary>
	[XmlIgnore]
	[Browsable(false)]
	public bool IsDisposeStarted => _state > DisposeState.None;

	/// <summary>
	/// Occurs when the object has been disposed.
	/// </summary>
	public event Action Disposed;

	/// <summary>
	/// Tries to begin the dispose process.
	/// </summary>
	/// <returns><c>true</c> if dispose can proceed; <c>false</c> if already disposing/disposed.</returns>
	protected bool TryBeginDispose()
	{
		using (_lock.EnterScope())
		{
			if (IsDisposeStarted)
				return false;

			_state = DisposeState.Disposing;
			return true;
		}
	}

	/// <summary>
	/// Marks the dispose process as complete.
	/// </summary>
	protected void EndDispose()
	{
		_state = DisposeState.Disposed;
		Disposed?.Invoke();
		GC.SuppressFinalize(this);
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

	/// <summary>
	/// Releases unmanaged resources in the finalizer.
	/// </summary>
	~DisposableBase()
	{
		try
		{
			DisposeNative();
		}
		catch (Exception ex)
		{
			Trace.WriteLine(ex);
		}
	}
}
