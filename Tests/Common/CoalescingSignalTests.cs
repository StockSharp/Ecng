namespace Ecng.Tests.Common;

/// <summary>
/// What the signal does in place of a clock: it wakes a reader when something changed, holds a run
/// of changes together so they are one wake-up, and leaves the reader asleep while nothing changes.
/// </summary>
[TestClass]
public class CoalescingSignalTests : BaseTestClass
{
	// Long against the probes below on purpose: what is under test is the order of things, and a
	// machine that takes a moment to schedule a continuation is not the signal being wrong.
	private static readonly TimeSpan _window = TimeSpan.FromMilliseconds(300);

	private static readonly TimeSpan _shortEnoughToStillBeInsideTheWindow = TimeSpan.FromMilliseconds(50);
	private static readonly TimeSpan _longEnoughForAWakeUpToHaveHappened = TimeSpan.FromSeconds(2);

	/// <summary>
	/// Starts a wait, makes the change, and awaits the round it earns - the shape every reader of the
	/// signal has, written once.
	/// </summary>
	private static async Task WaitFor(CoalescingSignal signal, Action change)
	{
		var wait = signal.WaitAsync(CancellationToken.None);

		change();

		await wait;
	}

	/// <summary>
	/// The whole point of the signal: nothing changed, so nobody is woken.
	/// </summary>
	[TestMethod]
	public async Task NothingChangedIsNoWakeUp()
	{
		using var signal = new CoalescingSignal(_window);
		using var cts = new CancellationTokenSource();

		var wait = signal.WaitAsync(cts.Token);

		await Task.Delay(_longEnoughForAWakeUpToHaveHappened);

		wait.IsCompleted.AssertFalse("nothing was raised, so the reader has nothing to do and stays asleep");

		cts.Cancel();
	}

	/// <summary>
	/// Something changed, so the reader is woken - one window later, not at once.
	/// </summary>
	[TestMethod]
	public async Task AChangeWakesTheReaderOnceTheWindowHasPassed()
	{
		using var signal = new CoalescingSignal(_window);

		var wait = signal.WaitAsync(CancellationToken.None);

		signal.Raise();

		wait.IsCompleted.AssertFalse("the window is what makes a run of changes one wake-up, so it is held first");

		await wait;
	}

	/// <summary>
	/// A run of changes inside one window is one round of work, however many there were. This is
	/// what the signal exists for: a client that opens two hundred subscriptions must not cost two
	/// hundred rounds.
	/// </summary>
	[TestMethod]
	public async Task ARunOfChangesInsideOneWindowIsOneWakeUp()
	{
		using var signal = new CoalescingSignal(_window);
		using var cts = new CancellationTokenSource();

		await WaitFor(signal, () =>
		{
			for (var i = 0; i < 200; i++)
				signal.Raise();
		});

		var second = signal.WaitAsync(cts.Token);

		await Task.Delay(_longEnoughForAWakeUpToHaveHappened);

		second.IsCompleted.AssertFalse("two hundred changes were taken in one round, and there is no second round owed");

		cts.Cancel();
	}

	/// <summary>
	/// A change made while the window is being held belongs to the round the reader is about to do:
	/// the reader reads the state that change left behind, so it must not also be woken again for a
	/// round with nothing left in it.
	/// </summary>
	[TestMethod]
	public async Task AChangeMadeWhileTheWindowIsHeldBelongsToThatRound()
	{
		using var signal = new CoalescingSignal(_window);
		using var cts = new CancellationTokenSource();

		var first = signal.WaitAsync(cts.Token);

		signal.Raise();

		// Inside the window the first wait is holding.
		await Task.Delay(_shortEnoughToStillBeInsideTheWindow);
		signal.Raise();

		await first;

		var second = signal.WaitAsync(cts.Token);

		await Task.Delay(_longEnoughForAWakeUpToHaveHappened);

		second.IsCompleted.AssertFalse(
			"the change was made before the round read the state, so that round covers it and no second one is owed");

		cts.Cancel();
	}

	/// <summary>
	/// A change made after the round has been handed over is a change that round did not see, and it
	/// earns a round of its own.
	/// </summary>
	[TestMethod]
	public async Task AChangeMadeAfterTheRoundEarnsTheNextOne()
	{
		using var signal = new CoalescingSignal(_window);

		await WaitFor(signal, signal.Raise);

		var second = signal.WaitAsync(CancellationToken.None);

		signal.Raise();

		await second;
	}

	/// <summary>
	/// A change made while nobody is waiting is kept rather than lost: the reader may be busy with
	/// the round before, and what changed while it worked still has to be taken.
	/// </summary>
	[TestMethod]
	public async Task AChangeMadeWithNobodyWaitingIsKeptForTheNextWait()
	{
		using var signal = new CoalescingSignal(_window);

		signal.Raise();

		await signal.WaitAsync(CancellationToken.None);
	}

	/// <summary>
	/// With no window there is nothing to hold: the reader is woken as soon as something changes.
	/// </summary>
	[TestMethod]
	public async Task WithNoWindowTheReaderIsWokenAtOnce()
	{
		using var signal = new CoalescingSignal(TimeSpan.Zero);

		signal.Raise();

		await signal.WaitAsync(CancellationToken.None);
	}

	/// <summary>
	/// A reader that is no longer wanted stops waiting, rather than being left holding a task nothing
	/// will ever complete.
	/// </summary>
	[TestMethod]
	public async Task AReaderThatIsCancelledStopsWaiting()
	{
		using var signal = new CoalescingSignal(_window);
		using var cts = new CancellationTokenSource();

		var wait = signal.WaitAsync(cts.Token);

		cts.Cancel();

		await ThrowsAsync<OperationCanceledException>(() => wait);
	}

	/// <summary>
	/// Cancelling inside the window stops the round too: shutdown does not have to wait out a window
	/// for work that is about to be thrown away.
	/// </summary>
	[TestMethod]
	public async Task CancellingInsideTheWindowStopsTheRound()
	{
		using var signal = new CoalescingSignal(TimeSpan.FromSeconds(30));
		using var cts = new CancellationTokenSource();

		var wait = signal.WaitAsync(cts.Token);

		signal.Raise();

		await Task.Delay(_shortEnoughToStillBeInsideTheWindow);

		cts.Cancel();

		await ThrowsAsync<OperationCanceledException>(() => wait);
	}

	/// <summary>
	/// One signal serves one reader. Two would take turns and each would miss the changes the other
	/// took, which is a fault worth hearing about rather than a quiet halving of the work.
	/// </summary>
	[TestMethod]
	public async Task ASecondReaderIsRefusedRatherThanQuietlyServed()
	{
		using var signal = new CoalescingSignal(_window);
		using var cts = new CancellationTokenSource();

		var first = signal.WaitAsync(cts.Token);

		await ThrowsAsync<InvalidOperationException>(() => signal.WaitAsync(cts.Token));

		cts.Cancel();

		await ThrowsAsync<OperationCanceledException>(() => first);
	}

	/// <summary>
	/// And once the first reader is done the signal is free again, so the ordinary loop - wait, work,
	/// wait - is not mistaken for two readers.
	/// </summary>
	[TestMethod]
	public async Task TheSignalIsFreeAgainOnceTheReaderIsDone()
	{
		using var signal = new CoalescingSignal(TimeSpan.Zero);

		signal.Raise();
		await signal.WaitAsync(CancellationToken.None);

		signal.Raise();
		await signal.WaitAsync(CancellationToken.None);
	}

	/// <summary>
	/// A change noted while the process is shutting down is not a fault: raising runs on the paths
	/// that see every message, and those do not stop the instant something is disposed.
	/// </summary>
	[TestMethod]
	public void ARaiseAfterDisposalIsNotAFault()
	{
		var signal = new CoalescingSignal(_window);

		signal.Dispose();
		signal.Raise();
		signal.Dispose();
	}

	/// <summary>
	/// Waiting on a disposed signal, on the other hand, is a caller's mistake: it would wait for a
	/// wake-up that can no longer come.
	/// </summary>
	[TestMethod]
	public async Task WaitingOnADisposedSignalIsRefused()
	{
		var signal = new CoalescingSignal(_window);

		signal.Dispose();

		await ThrowsAsync<ObjectDisposedException>(() => signal.WaitAsync(CancellationToken.None));
	}

	/// <summary>
	/// A window cannot run backwards.
	/// </summary>
	[TestMethod]
	public void ANegativeWindowIsRefused()
		=> ThrowsExactly<ArgumentOutOfRangeException>(() => new CoalescingSignal(TimeSpan.FromSeconds(-1)));

	/// <summary>
	/// The window a signal was built with is readable back, so a caller can state what it expects of
	/// one it was handed.
	/// </summary>
	[TestMethod]
	public void TheWindowIsReadableBack()
	{
		using var signal = new CoalescingSignal(_window);

		signal.Window.AssertEqual(_window);
	}
}
