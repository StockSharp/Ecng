namespace Ecng.Tests.Common;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

[TestClass]
public class LockTests
{
	[TestMethod]
	public void EnterExit_MutualExclusion()
	{
		var l = new System.Threading.Lock();
		var counter = 0;

		Parallel.For(0, 1000, _ =>
		{
			using (l.EnterScope())
				counter++;
		});

		counter.AssertEqual(1000);
	}

	[TestMethod]
	public void Reentrancy_SameThread()
	{
		var l = new System.Threading.Lock();

		l.Enter();
		l.IsHeldByCurrentThread.AssertTrue();

		l.Enter();
		l.IsHeldByCurrentThread.AssertTrue();

		l.Exit();
		l.IsHeldByCurrentThread.AssertTrue();

		l.Exit();
		l.IsHeldByCurrentThread.AssertFalse();
	}

	[TestMethod]
	public void TryEnter_TimeoutsAndAcquisition()
	{
		var l = new System.Threading.Lock();
		var started = new ManualResetEventSlim();
		var hold = new ManualResetEventSlim();
		var acquired = false;
		var failedFast = false;

		var t = Task.Run(() =>
		{
			using (l.EnterScope())
			{
				started.Set();
				hold.Wait(TimeSpan.FromMilliseconds(200));
			}
		});

		started.Wait();

		failedFast = !l.TryEnter(0);
		failedFast.AssertTrue();

		acquired = l.TryEnter(TimeSpan.FromMilliseconds(50));
		acquired.AssertFalse();

		hold.Set();
		t.Wait();

		// Now it should be free
		acquired = l.TryEnter(1000);
		acquired.AssertTrue();
		l.IsHeldByCurrentThread.AssertTrue();
		l.Exit();
	}

	[TestMethod]
	public void EnterScope_ReleasesOnDispose()
	{
		var l = new System.Threading.Lock();
		using (l.EnterScope())
		{
			l.IsHeldByCurrentThread.AssertTrue();
			using (l.EnterScope())
				l.IsHeldByCurrentThread.AssertTrue();
		}

		l.IsHeldByCurrentThread.AssertFalse();

		// Another thread should be able to acquire
		var ok = false;
		Parallel.For(0, 1, _ => { using (l.EnterScope()) ok = true; });
		ok.AssertTrue();
	}

	[TestMethod]
	public void Exit_WithoutOwnership_Throws()
	{
		var l = new System.Threading.Lock();
		Assert.ThrowsExactly<SynchronizationLockException>(() => l.Exit());
	}

	[TestMethod]
	public void Reentrancy_BlocksOtherThreadUntilFullExit()
	{
		var l = new System.Threading.Lock();
		using (l.EnterScope())
		{
			l.Enter(); // reenter

			// Other thread cannot acquire now
			var couldEnter = false;
			var done = new ManualResetEventSlim();
			var t = Task.Run(() =>
			{
				couldEnter = l.TryEnter(50);
				done.Set();
			});

			done.Wait();
			couldEnter.AssertFalse();

			// Exit once, still held
			l.Exit();

			// Another thread still should not be able to enter
			couldEnter = false;
			done = new ManualResetEventSlim();
			Task.Run(() => { couldEnter = l.TryEnter(50); done.Set(); });
			done.Wait();
			couldEnter.AssertFalse();
		}

		// After full exit, acquisition should succeed
		l.TryEnter(200).AssertTrue();
		l.Exit();
	}
}
