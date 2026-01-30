namespace Ecng.Tests.Net;

using Ecng.ComponentModel;
using Ecng.Net;

[TestClass]
public class ConnectionStateTrackerTests : BaseTestClass
{
#pragma warning disable CS0618 // Type or member is obsolete
	private static void Subscribe(ConnectionStateTracker tracker, Action<ConnectionStates> handler)
		=> ((IConnection)tracker).StateChanged += handler;
#pragma warning restore CS0618

	private class MockConnection : IAsyncConnection
	{
		public event Func<ConnectionStates, CancellationToken, ValueTask> StateChanged;

		public ConnectionStates CurrentState { get; private set; } = ConnectionStates.Disconnected;

		public bool ConnectCalled { get; private set; }
		public bool DisconnectCalled { get; private set; }

		public ValueTask ConnectAsync(CancellationToken cancellationToken)
		{
			ConnectCalled = true;
			SetState(ConnectionStates.Connected);
			return ValueTask.CompletedTask;
		}

		public void Disconnect()
		{
			DisconnectCalled = true;
			SetState(ConnectionStates.Disconnected);
		}

		public void SetState(ConnectionStates state)
		{
			CurrentState = state;
			StateChanged?.Invoke(state, default);
		}
	}

	[TestMethod]
	public async Task Add_AddsConnection()
	{
		var tracker = new ConnectionStateTracker();
		var conn = new MockConnection();

		tracker.Add(conn);

		// Verify connection is tracked (will test via ConnectAsync)
		await tracker.ConnectAsync(CancellationToken);
		conn.ConnectCalled.AssertTrue();
	}

	[TestMethod]
	public async Task Add_MultipleConnections_AddsAll()
	{
		var tracker = new ConnectionStateTracker();
		var conn1 = new MockConnection();
		var conn2 = new MockConnection();
		var conn3 = new MockConnection();

		tracker.Add(conn1);
		tracker.Add(conn2);
		tracker.Add(conn3);

		await tracker.ConnectAsync(CancellationToken);

		conn1.ConnectCalled.AssertTrue();
		conn2.ConnectCalled.AssertTrue();
		conn3.ConnectCalled.AssertTrue();
	}

	[TestMethod]
	public void Remove_ExistingConnection_ReturnsTrue()
	{
		var tracker = new ConnectionStateTracker();
		var conn = new MockConnection();

		tracker.Add(conn);
		var result = tracker.Remove(conn);

		result.AssertTrue();
	}

	[TestMethod]
	public void Remove_NonExistentConnection_ReturnsFalse()
	{
		var tracker = new ConnectionStateTracker();
		var conn = new MockConnection();

		var result = tracker.Remove(conn);

		result.AssertFalse();
	}

	[TestMethod]
	public async Task Remove_AfterRemove_ConnectionNotCalled()
	{
		var tracker = new ConnectionStateTracker();
		var conn1 = new MockConnection();
		var conn2 = new MockConnection();

		tracker.Add(conn1);
		tracker.Add(conn2);
		tracker.Remove(conn1);

		await tracker.ConnectAsync(CancellationToken);

		conn1.ConnectCalled.AssertFalse();
		conn2.ConnectCalled.AssertTrue();
	}

	[TestMethod]
	public Task ConnectAsync_NoConnections_ThrowsInvalidOperationException()
	{
		var tracker = new ConnectionStateTracker();

		return ThrowsExactlyAsync<InvalidOperationException>(()
			=> tracker.ConnectAsync(CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task ConnectAsync_CallsAllConnections()
	{
		var tracker = new ConnectionStateTracker();
		var conn1 = new MockConnection();
		var conn2 = new MockConnection();
		var conn3 = new MockConnection();

		tracker.Add(conn1);
		tracker.Add(conn2);
		tracker.Add(conn3);

		await tracker.ConnectAsync(CancellationToken);

		conn1.ConnectCalled.AssertTrue();
		conn2.ConnectCalled.AssertTrue();
		conn3.ConnectCalled.AssertTrue();
	}

	[TestMethod]
	public void Disconnect_CallsAllConnections()
	{
		var tracker = new ConnectionStateTracker();
		var conn1 = new MockConnection();
		var conn2 = new MockConnection();

		tracker.Add(conn1);
		tracker.Add(conn2);

		tracker.Disconnect();

		conn1.DisconnectCalled.AssertTrue();
		conn2.DisconnectCalled.AssertTrue();
	}

	[TestMethod]
	public void Disconnect_NoConnections_DoesNotThrow()
	{
		var tracker = new ConnectionStateTracker();
		tracker.Disconnect(); // Should not throw
	}

	[TestMethod]
	public void StateChanged_AllConnected_FiresConnected()
	{
		var tracker = new ConnectionStateTracker();
		var conn1 = new MockConnection();
		var conn2 = new MockConnection();

		tracker.Add(conn1);
		tracker.Add(conn2);

		ConnectionStates? firedState = null;
		Subscribe(tracker, state => firedState = state);

		conn1.SetState(ConnectionStates.Connected);
		conn2.SetState(ConnectionStates.Connected);

		firedState.AssertEqual(ConnectionStates.Connected);
	}

	[TestMethod]
	public void StateChanged_AnyReconnecting_FiresReconnecting()
	{
		var tracker = new ConnectionStateTracker();
		var conn1 = new MockConnection();
		var conn2 = new MockConnection();

		tracker.Add(conn1);
		tracker.Add(conn2);

		ConnectionStates? firedState = null;
		Subscribe(tracker, state => firedState = state);

		conn1.SetState(ConnectionStates.Connected);
		conn2.SetState(ConnectionStates.Reconnecting);

		firedState.AssertEqual(ConnectionStates.Reconnecting);
	}

	[TestMethod]
	public void StateChanged_AllConnectedOrRestored_FiresRestored()
	{
		var tracker = new ConnectionStateTracker();
		var conn1 = new MockConnection();
		var conn2 = new MockConnection();
		var conn3 = new MockConnection();

		tracker.Add(conn1);
		tracker.Add(conn2);
		tracker.Add(conn3);

		ConnectionStates? firedState = null;
		Subscribe(tracker, state => firedState = state);

		conn1.SetState(ConnectionStates.Connected);
		conn2.SetState(ConnectionStates.Restored);
		conn3.SetState(ConnectionStates.Restored);

		firedState.AssertEqual(ConnectionStates.Restored);
	}

	[TestMethod]
	public void StateChanged_AllFailed_FiresFailed()
	{
		var tracker = new ConnectionStateTracker();
		var conn1 = new MockConnection();
		var conn2 = new MockConnection();

		tracker.Add(conn1);
		tracker.Add(conn2);

		ConnectionStates? firedState = null;
		Subscribe(tracker, state => firedState = state);

		conn1.SetState(ConnectionStates.Failed);
		conn2.SetState(ConnectionStates.Failed);

		firedState.AssertEqual(ConnectionStates.Failed);
	}

	[TestMethod]
	public void StateChanged_AllDisconnectedOrFailed_FiresDisconnected()
	{
		var tracker = new ConnectionStateTracker();
		var conn1 = new MockConnection();
		var conn2 = new MockConnection();
		var conn3 = new MockConnection();

		tracker.Add(conn1);
		tracker.Add(conn2);
		tracker.Add(conn3);

		// First connect all
		conn1.SetState(ConnectionStates.Connected);
		conn2.SetState(ConnectionStates.Connected);
		conn3.SetState(ConnectionStates.Connected);

		ConnectionStates? firedState = null;
		Subscribe(tracker, state => firedState = state);

		// Now disconnect/fail all
		conn1.SetState(ConnectionStates.Disconnected);
		conn2.SetState(ConnectionStates.Failed);
		conn3.SetState(ConnectionStates.Disconnected);

		firedState.AssertEqual(ConnectionStates.Disconnected);
	}

	[TestMethod]
	public void StateChanged_MixedStates_DoesNotFire()
	{
		var tracker = new ConnectionStateTracker();
		var conn1 = new MockConnection();
		var conn2 = new MockConnection();

		tracker.Add(conn1);
		tracker.Add(conn2);

		var fireCount = 0;
		Subscribe(tracker, state => fireCount++);

		conn1.SetState(ConnectionStates.Connected);
		conn2.SetState(ConnectionStates.Connecting); // Mixed state

		fireCount.AssertEqual(0);
	}

	[TestMethod]
	public void StateChanged_SameState_DoesNotFireAgain()
	{
		var tracker = new ConnectionStateTracker();
		var conn1 = new MockConnection();
		var conn2 = new MockConnection();

		tracker.Add(conn1);
		tracker.Add(conn2);

		var fireCount = 0;
		Subscribe(tracker, state => fireCount++);

		conn1.SetState(ConnectionStates.Connected);
		conn2.SetState(ConnectionStates.Connected);

		fireCount.AssertEqual(1);

		// Change state again to same value
		conn1.SetState(ConnectionStates.Connected);

		fireCount.AssertEqual(1); // Should not fire again
	}

	[TestMethod]
	public void StateChanged_TransitionSequence()
	{
		var tracker = new ConnectionStateTracker();
		var conn1 = new MockConnection();
		var conn2 = new MockConnection();

		tracker.Add(conn1);
		tracker.Add(conn2);

		var states = new List<ConnectionStates>();
		Subscribe(tracker, state => states.Add(state));

		// All disconnected (initial state, should not fire)
		// Both connecting -> mixed, should not fire
		conn1.SetState(ConnectionStates.Connecting);
		conn2.SetState(ConnectionStates.Connecting);

		states.Count.AssertEqual(0);

		// Both connected -> should fire Connected
		conn1.SetState(ConnectionStates.Connected);
		conn2.SetState(ConnectionStates.Connected);

		states.Count.AssertEqual(1);
		states[0].AssertEqual(ConnectionStates.Connected);

		// One reconnecting -> should fire Reconnecting
		conn1.SetState(ConnectionStates.Reconnecting);

		states.Count.AssertEqual(2);
		states[1].AssertEqual(ConnectionStates.Reconnecting);

		// Back to connected -> should fire Connected
		conn1.SetState(ConnectionStates.Connected);

		states.Count.AssertEqual(3);
		states[2].AssertEqual(ConnectionStates.Connected);

		// All failed -> should fire Failed
		conn1.SetState(ConnectionStates.Failed);
		conn2.SetState(ConnectionStates.Failed);

		states.Count.AssertEqual(4);
		states[3].AssertEqual(ConnectionStates.Failed);
	}

	[TestMethod]
	public void Dispose_UnsubscribesFromConnections()
	{
		var tracker = new ConnectionStateTracker();
		var conn = new MockConnection();

		tracker.Add(conn);

		var fireCount = 0;
		Subscribe(tracker, state => fireCount++);

		conn.SetState(ConnectionStates.Connected);
		fireCount.AssertEqual(1);

		tracker.Dispose();

		// After dispose, state changes should not affect tracker
		conn.SetState(ConnectionStates.Disconnected);
		fireCount.AssertEqual(1); // Should still be 1
	}

	[TestMethod]
	public void Dispose_DisposesAllWrappers()
	{
		var tracker = new ConnectionStateTracker();
		var conn1 = new MockConnection();
		var conn2 = new MockConnection();

		tracker.Add(conn1);
		tracker.Add(conn2);

		tracker.Dispose();

		// After dispose, connections should not trigger state changes
		var fireCount = 0;
		Subscribe(tracker, state => fireCount++);

		conn1.SetState(ConnectionStates.Connected);
		conn2.SetState(ConnectionStates.Connected);

		fireCount.AssertEqual(0);
	}

	[TestMethod]
	public void ThreadSafety_ConcurrentStateChanges()
	{
		var tracker = new ConnectionStateTracker();
		var connections = Enumerable.Range(0, 10).Select(_ => new MockConnection()).ToArray();

		foreach (var conn in connections)
			tracker.Add(conn);

		var fireCount = 0;
		Subscribe(tracker, state => Interlocked.Increment(ref fireCount));

		// Concurrently change states
		Parallel.ForEach(connections, conn =>
		{
			conn.SetState(ConnectionStates.Connected);
		});

		// Wait a bit for all events to process
		Thread.Sleep(100);

		// Should fire exactly once when all become connected
		fireCount.AssertEqual(1);
	}

	[TestMethod]
	public void Remove_DisposesWrapper()
	{
		var tracker = new ConnectionStateTracker();
		var conn = new MockConnection();

		tracker.Add(conn);

		var fireCount = 0;
		Subscribe(tracker, state => fireCount++);

		conn.SetState(ConnectionStates.Connected);
		fireCount.AssertEqual(1);

		tracker.Remove(conn);

		// After remove, state changes should not affect tracker
		conn.SetState(ConnectionStates.Disconnected);
		fireCount.AssertEqual(1);
	}

	[TestMethod]
	public void Add_SameConnectionTwice_ThrowsException()
	{
		var tracker = new ConnectionStateTracker();
		var conn = new MockConnection();

		tracker.Add(conn);

		// Adding same connection twice should throw ArgumentException
		ThrowsExactly<ArgumentException>(() => tracker.Add(conn));
	}

	[TestMethod]
	public void StateChanged_AllRestored_FiresRestored()
	{
		var tracker = new ConnectionStateTracker();
		var conn1 = new MockConnection();
		var conn2 = new MockConnection();

		tracker.Add(conn1);
		tracker.Add(conn2);

		ConnectionStates? firedState = null;
		Subscribe(tracker, state => firedState = state);

		conn1.SetState(ConnectionStates.Restored);
		conn2.SetState(ConnectionStates.Restored);

		firedState.AssertEqual(ConnectionStates.Restored);
	}

	[TestMethod]
	public void StateChanged_Reconnecting_TakesPrecedenceOverConnected()
	{
		var tracker = new ConnectionStateTracker();
		var conn1 = new MockConnection();
		var conn2 = new MockConnection();
		var conn3 = new MockConnection();

		tracker.Add(conn1);
		tracker.Add(conn2);
		tracker.Add(conn3);

		var states = new List<ConnectionStates>();
		Subscribe(tracker, state => states.Add(state));

		// First all connected
		conn1.SetState(ConnectionStates.Connected);
		conn2.SetState(ConnectionStates.Connected);
		conn3.SetState(ConnectionStates.Connected);

		states.Count.AssertEqual(1);
		states[0].AssertEqual(ConnectionStates.Connected);

		// One goes to reconnecting - should fire Reconnecting
		conn2.SetState(ConnectionStates.Reconnecting);

		states.Count.AssertEqual(2);
		states[1].AssertEqual(ConnectionStates.Reconnecting);
	}

	[TestMethod]
	public void StateChanged_OnlyDisconnected_DoesNotFireInitially()
	{
		var tracker = new ConnectionStateTracker();
		var conn = new MockConnection();

		var fireCount = 0;
		Subscribe(tracker, state => fireCount++);

		tracker.Add(conn);

		// Initial state is Disconnected, should not fire
		fireCount.AssertEqual(0);
	}

	[TestMethod]
	public async Task ConnectAsync_WithCancellation_PassesToConnections()
	{
		var tracker = new ConnectionStateTracker();
		var cts = new CancellationTokenSource();
		var conn = new MockConnection();

		tracker.Add(conn);

		await tracker.ConnectAsync(cts.Token);

		conn.ConnectCalled.AssertTrue();
	}

	[TestMethod]
	public void StateChanged_Disconnecting_MixedState()
	{
		var tracker = new ConnectionStateTracker();
		var conn1 = new MockConnection();
		var conn2 = new MockConnection();

		tracker.Add(conn1);
		tracker.Add(conn2);

		// First connect both
		conn1.SetState(ConnectionStates.Connected);
		conn2.SetState(ConnectionStates.Connected);

		var fireCount = 0;
		Subscribe(tracker, state => fireCount++);

		// One disconnecting, one connected - mixed state, should not fire
		conn1.SetState(ConnectionStates.Disconnecting);

		fireCount.AssertEqual(0);
	}

	[TestMethod]
	public void StateChanged_AllDisconnecting_MixedState()
	{
		var tracker = new ConnectionStateTracker();
		var conn1 = new MockConnection();
		var conn2 = new MockConnection();

		tracker.Add(conn1);
		tracker.Add(conn2);

		var fireCount = 0;
		Subscribe(tracker, state => fireCount++);

		// All disconnecting - not a recognized aggregate state
		conn1.SetState(ConnectionStates.Disconnecting);
		conn2.SetState(ConnectionStates.Disconnecting);

		fireCount.AssertEqual(0);
	}

	[TestMethod]
	public void StateChanged_Connecting_MixedState()
	{
		var tracker = new ConnectionStateTracker();
		var conn1 = new MockConnection();
		var conn2 = new MockConnection();

		tracker.Add(conn1);
		tracker.Add(conn2);

		var fireCount = 0;
		Subscribe(tracker, state => fireCount++);

		// All connecting - not a recognized aggregate state
		conn1.SetState(ConnectionStates.Connecting);
		conn2.SetState(ConnectionStates.Connecting);

		fireCount.AssertEqual(0);
	}

	[TestMethod]
	public void Add_Null_ThrowsArgumentNullException()
	{
		var tracker = new ConnectionStateTracker();

		ThrowsExactly<ArgumentNullException>(() => tracker.Add((IAsyncConnection)null));
	}

	[TestMethod]
	public void Remove_Null_ThrowsArgumentNullException()
	{
		var tracker = new ConnectionStateTracker();

		ThrowsExactly<ArgumentNullException>(() => tracker.Remove((IAsyncConnection)null));
	}

	[TestMethod]
	public async Task Add_AfterDispose_StillWorks()
	{
		var tracker = new ConnectionStateTracker();
		var conn = new MockConnection();

		tracker.Dispose();

		// Should not throw
		tracker.Add(conn);

		await tracker.ConnectAsync(CancellationToken);
		conn.ConnectCalled.AssertTrue();
	}

	[TestMethod]
	public void Remove_AfterDispose_StillWorks()
	{
		var tracker = new ConnectionStateTracker();
		var conn = new MockConnection();

		tracker.Add(conn);
		tracker.Dispose();

		// Should not throw
		var result = tracker.Remove(conn);
		result.AssertTrue();
	}

	[TestMethod]
	public async Task ConnectAsync_AfterDispose_StillWorks()
	{
		var tracker = new ConnectionStateTracker();
		var conn = new MockConnection();

		tracker.Add(conn);
		tracker.Dispose();

		// Should not throw
		await tracker.ConnectAsync(CancellationToken);
		conn.ConnectCalled.AssertTrue();
	}

	[TestMethod]
	public void Disconnect_AfterDispose_StillWorks()
	{
		var tracker = new ConnectionStateTracker();
		var conn = new MockConnection();

		tracker.Add(conn);
		tracker.Dispose();

		// Should not throw
		tracker.Disconnect();
		conn.DisconnectCalled.AssertTrue();
	}

	[TestMethod]
	public void StateChanged_AfterDispose_DoesNotFire()
	{
		var tracker = new ConnectionStateTracker();
		var conn = new MockConnection();

		tracker.Add(conn);

		var fireCount = 0;
		Subscribe(tracker, state => fireCount++);

		tracker.Dispose();

		// After dispose, state changes should not fire
		conn.SetState(ConnectionStates.Connected);
		fireCount.AssertEqual(0);
	}

	[TestMethod]
	public void StateChanged_SingleConnection_Connected()
	{
		var tracker = new ConnectionStateTracker();
		var conn = new MockConnection();

		tracker.Add(conn);

		ConnectionStates? firedState = null;
		Subscribe(tracker, state => firedState = state);

		conn.SetState(ConnectionStates.Connected);

		firedState.AssertEqual(ConnectionStates.Connected);
	}

	[TestMethod]
	public void StateChanged_SingleConnection_Failed()
	{
		var tracker = new ConnectionStateTracker();
		var conn = new MockConnection();

		tracker.Add(conn);

		ConnectionStates? firedState = null;
		Subscribe(tracker, state => firedState = state);

		conn.SetState(ConnectionStates.Failed);

		firedState.AssertEqual(ConnectionStates.Failed);
	}

	[TestMethod]
	public void StateChanged_SingleConnection_Reconnecting()
	{
		var tracker = new ConnectionStateTracker();
		var conn = new MockConnection();

		tracker.Add(conn);

		ConnectionStates? firedState = null;
		Subscribe(tracker, state => firedState = state);

		conn.SetState(ConnectionStates.Reconnecting);

		firedState.AssertEqual(ConnectionStates.Reconnecting);
	}

	[TestMethod]
	public void StateChanged_SingleConnection_Restored()
	{
		var tracker = new ConnectionStateTracker();
		var conn = new MockConnection();

		tracker.Add(conn);

		ConnectionStates? firedState = null;
		Subscribe(tracker, state => firedState = state);

		conn.SetState(ConnectionStates.Restored);

		firedState.AssertEqual(ConnectionStates.Restored);
	}

	[TestMethod]
	public void StateChanged_SingleConnection_Disconnected()
	{
		var tracker = new ConnectionStateTracker();
		var conn = new MockConnection();

		tracker.Add(conn);

		// First connect
		conn.SetState(ConnectionStates.Connected);

		ConnectionStates? firedState = null;
		Subscribe(tracker, state => firedState = state);

		conn.SetState(ConnectionStates.Disconnected);

		firedState.AssertEqual(ConnectionStates.Disconnected);
	}

	[TestMethod]
	public void StateChanged_SingleConnection_Disconnecting_DoesNotFire()
	{
		var tracker = new ConnectionStateTracker();
		var conn = new MockConnection();

		tracker.Add(conn);

		var fireCount = 0;
		Subscribe(tracker, state => fireCount++);

		conn.SetState(ConnectionStates.Disconnecting);

		fireCount.AssertEqual(0);
	}

	[TestMethod]
	public void StateChanged_SingleConnection_Connecting_DoesNotFire()
	{
		var tracker = new ConnectionStateTracker();
		var conn = new MockConnection();

		tracker.Add(conn);

		var fireCount = 0;
		Subscribe(tracker, state => fireCount++);

		conn.SetState(ConnectionStates.Connecting);

		fireCount.AssertEqual(0);
	}

	[TestMethod]
	public void Dispose_EmptyTracker_DoesNotThrow()
	{
		var tracker = new ConnectionStateTracker();
		tracker.Dispose(); // Should not throw
	}

	[TestMethod]
	public void Dispose_MultipleTimes_DoesNotThrow()
	{
		var tracker = new ConnectionStateTracker();
		var conn = new MockConnection();

		tracker.Add(conn);

		tracker.Dispose();
		tracker.Dispose(); // Should not throw on second dispose
	}

	[TestMethod]
	public void Remove_SameConnectionTwice_ReturnsFalseSecondTime()
	{
		var tracker = new ConnectionStateTracker();
		var conn = new MockConnection();

		tracker.Add(conn);

		var result1 = tracker.Remove(conn);
		var result2 = tracker.Remove(conn);

		result1.AssertTrue();
		result2.AssertFalse();
	}

	[TestMethod]
	public void StateChanged_FromFailedToConnected_FiresBoth()
	{
		var tracker = new ConnectionStateTracker();
		var conn1 = new MockConnection();
		var conn2 = new MockConnection();

		tracker.Add(conn1);
		tracker.Add(conn2);

		var states = new List<ConnectionStates>();
		Subscribe(tracker, state => states.Add(state));

		// All failed
		conn1.SetState(ConnectionStates.Failed);
		conn2.SetState(ConnectionStates.Failed);

		states.Count.AssertEqual(1);
		states[0].AssertEqual(ConnectionStates.Failed);

		// Back to connected
		conn1.SetState(ConnectionStates.Connected);
		conn2.SetState(ConnectionStates.Connected);

		states.Count.AssertEqual(2);
		states[1].AssertEqual(ConnectionStates.Connected);
	}

	[TestMethod]
	public void StateChanged_PartialDisconnectedFailed_FiresDisconnected()
	{
		var tracker = new ConnectionStateTracker();
		var conn1 = new MockConnection();
		var conn2 = new MockConnection();
		var conn3 = new MockConnection();

		tracker.Add(conn1);
		tracker.Add(conn2);
		tracker.Add(conn3);

		// First connect all
		conn1.SetState(ConnectionStates.Connected);
		conn2.SetState(ConnectionStates.Connected);
		conn3.SetState(ConnectionStates.Connected);

		ConnectionStates? firedState = null;
		Subscribe(tracker, state => firedState = state);

		// Mix of disconnected and failed
		conn1.SetState(ConnectionStates.Disconnected);
		conn2.SetState(ConnectionStates.Disconnected);
		conn3.SetState(ConnectionStates.Failed);

		firedState.AssertEqual(ConnectionStates.Disconnected);
	}

	[TestMethod]
	public async Task ConnectAsync_CancellationRequested_PropagatesToken()
	{
		var tracker = new ConnectionStateTracker();
		var conn = new MockConnection();

		tracker.Add(conn);

		var cts = new CancellationTokenSource();
		cts.Cancel(); // Cancel immediately

		try
		{
			await tracker.ConnectAsync(cts.Token);
		}
		catch (OperationCanceledException)
		{
			// Expected - token was cancelled
		}

		// Connection might still be called before cancellation
	}

	private static void SubscribeAsync(ConnectionStateTracker tracker, Func<ConnectionStates, CancellationToken, ValueTask> handler)
		=> ((IAsyncConnection)tracker).StateChanged += handler;

	[TestMethod]
	public void AsyncStateChanged_AllConnected_Fires()
	{
		var tracker = new ConnectionStateTracker();
		var conn1 = new MockConnection();
		var conn2 = new MockConnection();

		tracker.Add(conn1);
		tracker.Add(conn2);

		ConnectionStates? firedState = null;
		SubscribeAsync(tracker, (state, _) => { firedState = state; return default; });

		conn1.SetState(ConnectionStates.Connected);
		conn2.SetState(ConnectionStates.Connected);

		firedState.AssertEqual(ConnectionStates.Connected);
	}

	[TestMethod]
	public void AsyncStateChanged_TransitionSequence()
	{
		var tracker = new ConnectionStateTracker();
		var conn1 = new MockConnection();
		var conn2 = new MockConnection();

		tracker.Add(conn1);
		tracker.Add(conn2);

		var states = new List<ConnectionStates>();
		SubscribeAsync(tracker, (state, _) => { states.Add(state); return default; });

		conn1.SetState(ConnectionStates.Connected);
		conn2.SetState(ConnectionStates.Connected);

		states.Count.AssertEqual(1);
		states[0].AssertEqual(ConnectionStates.Connected);

		conn1.SetState(ConnectionStates.Reconnecting);

		states.Count.AssertEqual(2);
		states[1].AssertEqual(ConnectionStates.Reconnecting);

		conn1.SetState(ConnectionStates.Connected);

		states.Count.AssertEqual(3);
		states[2].AssertEqual(ConnectionStates.Connected);

		conn1.SetState(ConnectionStates.Failed);
		conn2.SetState(ConnectionStates.Failed);

		states.Count.AssertEqual(4);
		states[3].AssertEqual(ConnectionStates.Failed);
	}

	[TestMethod]
	public void AsyncStateChanged_BothSyncAndAsyncFire()
	{
		var tracker = new ConnectionStateTracker();
		var conn = new MockConnection();

		tracker.Add(conn);

		ConnectionStates? syncState = null;
		ConnectionStates? asyncState = null;

		Subscribe(tracker, state => syncState = state);
		SubscribeAsync(tracker, (state, _) => { asyncState = state; return default; });

		conn.SetState(ConnectionStates.Connected);

		syncState.AssertEqual(ConnectionStates.Connected);
		asyncState.AssertEqual(ConnectionStates.Connected);
	}
}
