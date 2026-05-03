namespace Ecng.Tests.Common;

[TestClass]
public class ReaderWriterLockSlimExtensionsTests : BaseTestClass
{
	[TestMethod]
	public void ReadLock_EntersAndExitsOnDispose()
	{
		using var rw = new ReaderWriterLockSlim();

		using (rw.ReadLock())
			rw.IsReadLockHeld.AssertTrue();

		rw.IsReadLockHeld.AssertFalse();
	}

	[TestMethod]
	public void WriteLock_EntersAndExitsOnDispose()
	{
		using var rw = new ReaderWriterLockSlim();

		using (rw.WriteLock())
			rw.IsWriteLockHeld.AssertTrue();

		rw.IsWriteLockHeld.AssertFalse();
	}

	[TestMethod]
	public void UpgradeableReadLock_EntersAndExitsOnDispose()
	{
		using var rw = new ReaderWriterLockSlim();

		using (rw.UpgradeableReadLock())
		{
			rw.IsUpgradeableReadLockHeld.AssertTrue();

			using (rw.WriteLock())
				rw.IsWriteLockHeld.AssertTrue();

			rw.IsWriteLockHeld.AssertFalse();
		}

		rw.IsUpgradeableReadLockHeld.AssertFalse();
	}

	[TestMethod]
	public void Dispose_IsIdempotent()
	{
		using var rw = new ReaderWriterLockSlim();

		var scope = rw.WriteLock();
		scope.Dispose();
		scope.Dispose();

		rw.IsWriteLockHeld.AssertFalse();
	}

	[TestMethod]
	public void ReadLock_PermitsConcurrentReaders()
	{
		using var rw = new ReaderWriterLockSlim();

		using var firstEntered = new ManualResetEventSlim();
		using var release = new ManualResetEventSlim();

		var second = Task.Run(() =>
		{
			firstEntered.Wait(CancellationToken);
			using (rw.ReadLock())
				return rw.IsReadLockHeld;
		}, CancellationToken);

		using (rw.ReadLock())
		{
			firstEntered.Set();
			second.Wait(2_000, CancellationToken).AssertTrue();
		}

		second.Result.AssertTrue();
	}

	[TestMethod]
	public void WriteLock_BlocksReaders()
	{
		using var rw = new ReaderWriterLockSlim();

		using var writerEntered = new ManualResetEventSlim();
		using var readerStarted = new ManualResetEventSlim();
		using var readerObservedHeld = new ManualResetEventSlim();

		var writer = Task.Run(() =>
		{
			using (rw.WriteLock())
			{
				writerEntered.Set();
				readerStarted.Wait(CancellationToken);
				Thread.Sleep(50);
				rw.IsWriteLockHeld.AssertTrue();
			}
		}, CancellationToken);

		writerEntered.Wait(CancellationToken);
		readerStarted.Set();

		var reader = Task.Run(() =>
		{
			using (rw.ReadLock())
				readerObservedHeld.Set();
		}, CancellationToken);

		writer.Wait(2_000, CancellationToken).AssertTrue();
		reader.Wait(2_000, CancellationToken).AssertTrue();
		readerObservedHeld.IsSet.AssertTrue();
	}

	[TestMethod]
	public void Null_ThrowsArgumentNullException()
	{
		ReaderWriterLockSlim rw = null;
		Assert.ThrowsExactly<ArgumentNullException>(() => rw.ReadLock());
		Assert.ThrowsExactly<ArgumentNullException>(() => rw.WriteLock());
		Assert.ThrowsExactly<ArgumentNullException>(() => rw.UpgradeableReadLock());
	}
}
