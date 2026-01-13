namespace Ecng.Common;

using System;

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
public abstract class Disposable : DisposableBase, IReasonDisposable
{
	#region IReasonDisposable Members

	private string _reason;
	string IReasonDisposable.Reason => _reason;

	bool IReasonDisposable.Dispose(string reason)
	{
		if (!TryBeginDispose())
			return false;

		_reason = reason;

		try
		{
			DisposeManaged();
			DisposeNative();
		}
		finally
		{
			EndDispose();
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
}
