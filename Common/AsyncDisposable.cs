namespace Ecng.Common;

using System;
using System.Threading.Tasks;

/// <summary>
/// Provides a base class for implementing the async dispose pattern.
/// This class helps manage the disposal of managed and native resources asynchronously.
/// </summary>
public abstract class AsyncDisposable : DisposableBase, IAsyncDisposable, IDisposable
{
	#region IAsyncDisposable Members

	/// <summary>
	/// Performs tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
	/// </summary>
	public async ValueTask DisposeAsync()
	{
		if (!TryBeginDispose())
			return;

		try
		{
			await DisposeManaged().NoWait();
			DisposeNative();
		}
		finally
		{
			EndDispose();
		}
	}

	#endregion

	#region IDisposable Members

	/// <summary>
	/// Performs tasks associated with freeing, releasing, or resetting unmanaged resources synchronously.
	/// </summary>
	public void Dispose()
	{
		DisposeAsync().AsTask().Wait();
	}

	#endregion

	/// <summary>
	/// Disposes the managed resources asynchronously.
	/// Override this method to add custom clean up of managed resources.
	/// </summary>
	protected virtual ValueTask DisposeManaged()
	{
		return default;
	}
}
