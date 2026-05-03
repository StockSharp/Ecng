namespace Ecng.Common;

using System.Threading;

/// <summary>
/// Provides <see cref="IDisposable"/>-scope extension methods for
/// <see cref="ReaderWriterLockSlim"/>, replacing the verbose
/// <c>try { rw.EnterXxxLock(); ... } finally { rw.ExitXxxLock(); }</c>
/// pattern with a single <c>using</c> statement.
/// </summary>
public static class ReaderWriterLockSlimExtensions
{
	/// <summary>
	/// Enters <paramref name="rw"/> in read mode and returns a scope that
	/// exits the read lock once on dispose.
	/// </summary>
	/// <param name="rw">Lock to enter.</param>
	/// <returns>Disposable scope tied to the read mode of <paramref name="rw"/>.</returns>
	public static Scope ReadLock(this ReaderWriterLockSlim rw)
	{
		if (rw is null)
			throw new ArgumentNullException(nameof(rw));

		rw.EnterReadLock();
		return new Scope(rw, ScopeKind.Read);
	}

	/// <summary>
	/// Enters <paramref name="rw"/> in write mode and returns a scope that
	/// exits the write lock once on dispose.
	/// </summary>
	/// <param name="rw">Lock to enter.</param>
	/// <returns>Disposable scope tied to the write mode of <paramref name="rw"/>.</returns>
	public static Scope WriteLock(this ReaderWriterLockSlim rw)
	{
		if (rw is null)
			throw new ArgumentNullException(nameof(rw));

		rw.EnterWriteLock();
		return new Scope(rw, ScopeKind.Write);
	}

	/// <summary>
	/// Enters <paramref name="rw"/> in upgradeable read mode and returns a
	/// scope that exits the upgradeable read lock once on dispose.
	/// </summary>
	/// <param name="rw">Lock to enter.</param>
	/// <returns>Disposable scope tied to the upgradeable read mode of <paramref name="rw"/>.</returns>
	public static Scope UpgradeableReadLock(this ReaderWriterLockSlim rw)
	{
		if (rw is null)
			throw new ArgumentNullException(nameof(rw));

		rw.EnterUpgradeableReadLock();
		return new Scope(rw, ScopeKind.Upgradeable);
	}

	internal enum ScopeKind : byte
	{
		Read,
		Write,
		Upgradeable,
	}

	/// <summary>
	/// Disposable scope returned by the lock-mode extension methods.
	/// Disposing exits the originating lock mode exactly once; further
	/// dispose calls on the same scope value are no-ops.
	/// </summary>
	public struct Scope : IDisposable
	{
		private ReaderWriterLockSlim _rw;
		private readonly ScopeKind _kind;

		internal Scope(ReaderWriterLockSlim rw, ScopeKind kind)
		{
			_rw = rw;
			_kind = kind;
		}

		/// <summary>
		/// Exits the originating lock mode of the lock that produced this
		/// scope. Idempotent: calling this on an already-disposed scope
		/// has no effect.
		/// </summary>
		public void Dispose()
		{
			var rw = _rw;

			if (rw is null)
				return;

			_rw = null;

			switch (_kind)
			{
				case ScopeKind.Read:
					rw.ExitReadLock();
					break;
				case ScopeKind.Write:
					rw.ExitWriteLock();
					break;
				case ScopeKind.Upgradeable:
					rw.ExitUpgradeableReadLock();
					break;
			}
		}
	}
}
