namespace Ecng.Tests.ComponentModel;

using Ecng.ComponentModel;

[TestClass]
public class DispatcherObservableCollectionTests : BaseTestClass
{
	/// <summary>
	/// Test dispatcher whose <see cref="IDispatcher.CheckAccess"/> result is controllable,
	/// allowing deterministic simulation of background-thread vs dispatcher-thread access.
	/// <see cref="Invoke"/> and <see cref="IDispatcher.InvokeAsync"/> run synchronously
	/// so the deferred flush is applied inline once the queue timer fires.
	/// </summary>
	private sealed class ControllableDispatcher : IDispatcher
	{
		public bool HasAccess { get; set; } = true;

		bool IDispatcher.CheckAccess() => HasAccess;

		public void Invoke(Action action)
		{
			if (action is null)
				throw new ArgumentNullException(nameof(action));

			action();
		}

		void IDispatcher.InvokeAsync(Action action)
		{
			if (action is null)
				throw new ArgumentNullException(nameof(action));

			// Run synchronously so the deferred flush is applied immediately and the
			// test can observe the final state without extra synchronization.
			action();
		}

		IDisposable IDispatcher.InvokePeriodically(Action action, TimeSpan interval)
			=> throw new NotSupportedException();
	}

	// The deferred flush timer inside DispatcherObservableCollection is 300ms; wait well
	// past it so the queued actions have been replayed onto Items before asserting.
	private static readonly TimeSpan _flushWait = TimeSpan.FromMilliseconds(800);

	/// <summary>
	/// BUG: DispatcherObservableCollection.Clear() (ComponentModel\DispatcherObservableCollection.cs:181)
	/// takes the immediate dispatcher-thread path even while background actions are still queued,
	/// so the deferred Add is replayed onto Items after the immediate Clear.
	/// Expected: after everything settles, Items and the collection (_syncCopy) agree (both empty).
	/// Actual: the queued Add re-appears in Items as a ghost element absent from the collection.
	/// </summary>
	[TestMethod]
	public async Task DispatcherClear_AfterQueuedBackgroundAdd_KeepsItemsConsistent()
	{
		var dispatcher = new ControllableDispatcher();
		var backing = new ObservableCollectionEx<int>();
		var coll = new DispatcherObservableCollection<int>(dispatcher, backing);

		// Background thread enqueues an Add (deferred); Items still empty.
		dispatcher.HasAccess = false;
		coll.Add(42);

		// Dispatcher thread clears immediately.
		dispatcher.HasAccess = true;
		coll.Clear();

		// Allow the deferred queue timer to fire and replay onto Items.
		await Task.Delay(_flushWait, CancellationToken);

		AreEqual(0, coll.Count, "Collection (sync copy) must be empty after Clear.");
		AreEqual(coll.Count, backing.Count, "Backing Items must stay consistent with the collection.");
		IsFalse(backing.Contains(42), "Queued Add must not resurrect as a ghost element in Items.");
	}

	/// <summary>
	/// BUG: DispatcherObservableCollection dispatcher-thread mutations bypass the pending-action
	/// queue (ComponentModel\DispatcherObservableCollection.cs:92), breaking operation order and
	/// indices. A background AddRange is still queued (Items empty) when a dispatcher-thread
	/// RemoveAt(0) runs Items.RemoveAt(0) against the empty Items.
	/// Expected: no exception, and after settling Items and the collection agree (both empty).
	/// Actual: Items.RemoveAt(0) throws ArgumentOutOfRangeException / desyncs Items from _syncCopy.
	/// </summary>
	[TestMethod]
	public async Task DispatcherRemoveAt_AfterQueuedBackgroundAddRange_KeepsOrderConsistent()
	{
		var dispatcher = new ControllableDispatcher();
		var backing = new ObservableCollectionEx<int>();
		var coll = new DispatcherObservableCollection<int>(dispatcher, backing);

		// Background thread enqueues an AddRange (deferred); Items still empty.
		dispatcher.HasAccess = false;
		coll.AddRange([7]);

		// Dispatcher thread removes index 0 immediately. With correct ordering this is
		// queued/ordered against the pending AddRange and must not touch an empty Items.
		dispatcher.HasAccess = true;
		coll.RemoveAt(0);

		// Allow the deferred queue timer to fire and replay onto Items.
		await Task.Delay(_flushWait, CancellationToken);

		AreEqual(0, coll.Count, "Collection (sync copy) must be empty after add+remove.");
		AreEqual(coll.Count, backing.Count, "Backing Items must stay consistent with the collection.");
		IsFalse(backing.Contains(7), "Queued AddRange must not resurrect as a ghost element in Items.");
	}
}
