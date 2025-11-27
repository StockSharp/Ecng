#if !NET9_0_OR_GREATER
namespace System.Threading;

/// <summary>
/// Provides a mechanism for achieving mutual exclusion in regions of code between different threads.
/// Compatibility implementation for target frameworks below .NET 9.
/// </summary>
public sealed class Lock
{
	private readonly object _sync = new object();

	private int _ownerThreadId;
	private int _recursionCount;

	/// <summary>
	/// Initializes a new instance of the <see cref="Lock"/> class.
	/// </summary>
	public Lock()
	{
	}

	/// <summary>
	/// Enters the lock. If the lock is already held by the current thread, the enter is recursive.
	/// </summary>
	public void Enter()
	{
		var currentId = Environment.CurrentManagedThreadId;

		// Fast path for recursion: if already held by current thread, just increment our counter.
		if (_ownerThreadId == currentId && Monitor.IsEntered(_sync))
		{
			// Maintain Monitor's recursion level to keep Exit balanced.
			Monitor.Enter(_sync);
			_recursionCount++;
			return;
		}

		Monitor.Enter(_sync);

		// Now we own it.
		_ownerThreadId = currentId;
		_recursionCount = 1;
	}

	/// <summary>
	/// Tries to enter the lock without waiting.
	/// </summary>
	/// <returns>
	/// <c>true</c> if the lock was entered; otherwise, <c>false</c>.
	/// </returns>
	public bool TryEnter()
		=> TryEnter(0);

	/// <summary>
	/// Tries to enter the lock, waiting up to the specified timeout in milliseconds.
	/// </summary>
	/// <param name="millisecondsTimeout">
	/// The maximum number of milliseconds to wait to enter the lock.
	/// Specify <c>0</c> to avoid waiting; specify <see cref="Timeout.Infinite"/> or <c>-1</c> to wait indefinitely.
	/// </param>
	/// <returns>
	/// <c>true</c> if the lock was entered; otherwise, <c>false</c> when the wait timed out.
	/// </returns>
	public bool TryEnter(int millisecondsTimeout)
	{
		var currentId = Environment.CurrentManagedThreadId;

		if (_ownerThreadId == currentId && Monitor.IsEntered(_sync))
		{
			Monitor.Enter(_sync);
			_recursionCount++;
			return true;
		}

		if (!Monitor.TryEnter(_sync, millisecondsTimeout))
			return false;

		_ownerThreadId = currentId;
		_recursionCount = 1;
		return true;
	}

	/// <summary>
	/// Tries to enter the lock, waiting up to the specified timeout <see cref="TimeSpan"/>.
	/// </summary>
	/// <param name="timeout">
	/// The maximum amount of time to wait to enter the lock.
	/// Specify <see cref="Timeout.InfiniteTimeSpan"/> to wait indefinitely; specify <see cref="TimeSpan.Zero"/> to avoid waiting.
	/// </param>
	/// <returns>
	/// <c>true</c> if the lock was entered; otherwise, <c>false</c> when the wait timed out.
	/// </returns>
	public bool TryEnter(TimeSpan timeout)
	{
		var currentId = Environment.CurrentManagedThreadId;

		if (_ownerThreadId == currentId && Monitor.IsEntered(_sync))
		{
			Monitor.Enter(_sync);
			_recursionCount++;
			return true;
		}

		if (!Monitor.TryEnter(_sync, timeout))
			return false;

		_ownerThreadId = currentId;
		_recursionCount = 1;
		return true;
	}

	/// <summary>
	/// A disposable scope returned by <see cref="EnterScope"/> that exits the lock on dispose.
	/// </summary>
	public struct Scope : IDisposable
	{
		private Lock _lock;

		internal Scope(Lock @lock)
		{
			_lock = @lock;
		}

		/// <summary>
		/// Exits the lock when the scope is disposed.
		/// </summary>
		public void Dispose()
		{
			var l = _lock;
			if (l is not null)
			{
				_lock = null!;
				l.Exit();
			}
		}
	}

	/// <summary>
	/// Enters the lock and returns a scope that will exit on dispose.
	/// Designed for use with C# <c>using</c> statements.
	/// </summary>
	/// <returns>
	/// A <see cref="Scope"/> that, when disposed, exits the lock once.
	/// </returns>
	public Scope EnterScope()
	{
		Enter();
		return new Scope(this);
	}

	/// <summary>
	/// Exits the lock once. If the lock was entered multiple times by the current thread, this decrements the recursion level.
	/// </summary>
	/// <exception cref="SynchronizationLockException">Thrown if the current thread does not hold the lock.</exception>
	public void Exit()
	{
		if (!Monitor.IsEntered(_sync))
			throw new SynchronizationLockException();

		// Decrement our logical recursion count and clear owner if fully released.
		if (--_recursionCount == 0)
		{
			_ownerThreadId = 0;
		}

		Monitor.Exit(_sync);
	}

	/// <summary>
	/// Gets a value indicating whether the current thread holds the lock.
	/// </summary>
	public bool IsHeldByCurrentThread => Monitor.IsEntered(_sync);
}
#endif
