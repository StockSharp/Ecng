namespace Ecng.Common;

using System;
using System.Threading;

/// <summary>
/// A lightweight synchronization scope that enters a lock on construction and exits on dispose.
/// </summary>
public readonly ref struct SyncScope
{
	private readonly object _syncRoot;
	private readonly bool _useMonitor;
	private readonly Lock.Scope _lockScope;

	/// <summary>
	/// Initializes a new synchronization scope and enters the provided synchronization object.
	/// </summary>
	/// <param name="syncRoot">
	/// Synchronization object. If it's a <see cref="Lock"/>, <see cref="Lock.EnterScope"/> is used; otherwise, <see cref="Monitor.Enter(object)"/> is used.
	/// </param>
	public SyncScope(object syncRoot)
	{
		_syncRoot = syncRoot ?? throw new ArgumentNullException(nameof(syncRoot));

		if (syncRoot is Lock @lock)
		{
			_lockScope = @lock.EnterScope();
		}
		else
		{
			Monitor.Enter(syncRoot);
			_useMonitor = true;
		}
	}

	/// <summary>
	/// Creates a new synchronization scope and enters the provided <see cref="Lock"/>.
	/// </summary>
	/// <param name="lock">The typed <see cref="Lock"/> to enter.</param>
	public SyncScope(Lock @lock)
	{
        if (@lock is null)
            throw new ArgumentNullException(nameof(@lock));
        
        _lockScope = @lock.EnterScope();
	}

	/// <summary>
	/// Exits the lock acquired in the constructor.
	/// </summary>
	public readonly void Dispose()
	{
		if (_useMonitor)
			Monitor.Exit(_syncRoot);
		else
			_lockScope.Dispose();
	}
}