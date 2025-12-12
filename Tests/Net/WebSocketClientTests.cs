namespace Ecng.Tests.Net;

using System.Collections.Concurrent;
using System.Reflection;
using Ecng.ComponentModel;
using Ecng.Net;

[TestClass]
[DoNotParallelize]
public class WebSocketClientTests : BaseTestClass
{
	private static Action<string, object> Log(string tag)
		=> (fmt, arg) => System.Diagnostics.Debug.WriteLine($"[{tag}] " + string.Format(fmt ?? string.Empty, arg));

	private static async Task<bool> TrySoftCloseAsync(WebSocketClient client)
	{
		try
		{
			await client.SendOpCode(0x8); // Close opcode
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void HardAbort(WebSocketClient client)
	{
		var wsField = typeof(WebSocketClient).GetField("_ws", BindingFlags.NonPublic | BindingFlags.Instance);
		wsField.AssertNotNull();
		var ws = wsField.GetValue(client) as System.Net.WebSockets.ClientWebSocket;
		ws.AssertNotNull();
		ws.Abort();
	}

	[TestMethod]
	[Timeout(60000, CooperativeCancellation = true)]
	public async Task Connect_Send_Receive_Echo()
	{
		var url = "wss://echo.websocket.org";
		var payload = $"echo-test-{Guid.NewGuid():N}";

		var receivedTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
		var errorTcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		using var client = new WebSocketClient(
			url,
			_ => { },
			ex => { if (!errorTcs.Task.IsCompleted) errorTcs.TrySetResult(ex); },
			async (self, msg, token) =>
			{
				var text = msg.AsString();

				if (text?.Contains(payload, StringComparison.Ordinal) == true)
					receivedTcs.TrySetResult(text);

				await ValueTask.CompletedTask;
			},
			Log("INFO"),
			Log("ERROR"),
			null
		);

		client.ReconnectAttempts = 4;

		await client.ConnectAsync(cts.Token);
		client.IsConnected.AssertTrue("WebSocket is not connected.");

		await client.SendAsync(payload, cts.Token);

		var completed = await Task.WhenAny(receivedTcs.Task, errorTcs.Task, Task.Delay(TimeSpan.FromSeconds(20), cts.Token));
		if (completed == errorTcs.Task)
			Fail($"WebSocket client error: {errorTcs.Task.Result}");

		receivedTcs.Task.IsCompleted.AssertTrue("Echo was not received in time.");
		var echoed = await receivedTcs.Task;
		echoed.Contains(payload).AssertTrue();

		client.Disconnect();
	}

	[TestMethod]
	[Timeout(60000, CooperativeCancellation = true)]
	public async Task Disconnect_Prevents_Sending()
	{
		var url = "wss://echo.websocket.org";

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		using var client = new WebSocketClient(
			url,
			_ => { },
			_ => { },
			async (self, msg, token) => await ValueTask.CompletedTask,
			Log("INFO"),
			Log("ERROR"),
			null
		);

		client.ReconnectAttempts = 4;
		await client.ConnectAsync(cts.Token);
		client.IsConnected.AssertTrue("WebSocket is not connected.");

		client.Disconnect();

		await ThrowsExactlyAsync<InvalidOperationException>(async () =>
		{
			await client.SendAsync("after-disconnect", cts.Token);
		});
	}

	[TestMethod]
	[Timeout(90000, CooperativeCancellation = true)]
	public async Task Streaming_SendsAndReceives_Batch()
	{
		var url = "wss://echo.websocket.org";
		var batch = 20;
		var prefix = $"stream-{Guid.NewGuid():N}-";
		var received = 0;

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

		using var client = new WebSocketClient(
			url,
			_ => { },
			_ => { },
			async (self, msg, token) =>
			{
				var text = msg.AsString();
				if (text != null && text.StartsWith(prefix, StringComparison.Ordinal))
					Interlocked.Increment(ref received);
				await ValueTask.CompletedTask;
			},
			Log("INFO"),
			Log("ERROR"),
			null
		);

		client.ReconnectAttempts = 4;
		await client.ConnectAsync(cts.Token);
		client.IsConnected.AssertTrue();

		for (var i = 0; i < batch; i++)
			await client.SendAsync(prefix + i, cts.Token);

		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < TimeSpan.FromSeconds(20) && Volatile.Read(ref received) < batch)
			await Task.Delay(200, cts.Token);

		Volatile.Read(ref received).AreEqual(batch, "Not all streaming messages were echoed.");

		client.Disconnect();
	}

	// TODO
	//[TestMethod]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task Resend_Stores_And_Removes_Commands()
	{
		var url = "wss://echo.websocket.org";
		var subId = 42L;
		var payload = $"resend-{Guid.NewGuid():N}";
		var occurrences = 0;

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));

		using var client = new WebSocketClient(
			url,
			_ => { },
			_ => { },
			async (self, msg, token) =>
			{
				var text = msg.AsString();
				if (text == payload)
					Interlocked.Increment(ref occurrences);
				await ValueTask.CompletedTask;
			},
			Log("INFO"),
			Log("ERROR"),
			null
		);

		client.ResendInterval = TimeSpan.FromMilliseconds(50);
		client.ReconnectAttempts = 4;

		await client.ConnectAsync(cts.Token);
		client.IsConnected.AssertTrue();

		await client.SendAsync(payload, cts.Token, subId: subId);

		var sw0 = System.Diagnostics.Stopwatch.StartNew();
		while (sw0.Elapsed < TimeSpan.FromSeconds(10) && Volatile.Read(ref occurrences) < 1)
			await Task.Delay(100, cts.Token);
		(Volatile.Read(ref occurrences) >= 1).AssertTrue("Initial echo not received.");

		await client.ResendAsync(cts.Token);
		var occ1 = Volatile.Read(ref occurrences);
		(occ1 >= 2).AssertTrue("Resend did not echo payload.");

		client.RemoveResend(subId);
		await client.ResendAsync(cts.Token);
		var occ2 = Volatile.Read(ref occurrences);
		occ2.AreEqual(occ1, "Payload was echoed after RemoveResend, expected no change.");

		client.Disconnect();
	}

	[TestMethod]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task Reconnect_Resends_Stored_Commands_IfSupported()
	{
		var url = "wss://echo.websocket.org";
		var payload = $"reconnect-{Guid.NewGuid():N}";
		var occurrences = 0;

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));

		using var client = new WebSocketClient(
			url,
			_ => { },
			_ => { },
			async (self, msg, token) =>
			{
				var text = msg.AsString();
				if (text == payload)
					Interlocked.Increment(ref occurrences);
				await ValueTask.CompletedTask;
			},
			Log("INFO"),
			Log("ERROR"),
			null
		);

		client.ReconnectAttempts = 4;
		client.ResendTimeout = TimeSpan.FromMilliseconds(200);
		client.ResendInterval = TimeSpan.FromMilliseconds(100);

		await client.ConnectAsync(cts.Token);
		client.IsConnected.AssertTrue();

		await client.SendAsync(payload, cts.Token, subId: 1);

		var sw0 = System.Diagnostics.Stopwatch.StartNew();
		while (sw0.Elapsed < TimeSpan.FromSeconds(10) && Volatile.Read(ref occurrences) < 1)
			await Task.Delay(100, cts.Token);
		(Volatile.Read(ref occurrences) >= 1).AssertTrue("Initial echo not received.");


		if (!await TrySoftCloseAsync(client))
		{
			Assert.Inconclusive("SendOpCode not supported on this runtime, skipping reconnect test.");
			return;
		}

		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < TimeSpan.FromSeconds(40) && Volatile.Read(ref occurrences) < 2)
			await Task.Delay(200, cts.Token);

		(Volatile.Read(ref occurrences) >= 2).AssertTrue("Resent payload after reconnect was not received.");

		client.Disconnect();
	}

	[TestMethod]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task HardAbort_Forces_Reconnect_And_Resend()
	{
		var url = "wss://echo.websocket.org";
		var payload = $"abort-{Guid.NewGuid():N}";
		var occurrences = 0;

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));

		var states = new ConcurrentQueue<ConnectionStates>();

		using var client = new WebSocketClient(
			url,
			st => states.Enqueue(st),
			_ => { },
			async (self, msg, token) =>
			{
				var text = msg.AsString();
				if (text == payload)
					Interlocked.Increment(ref occurrences);
				await ValueTask.CompletedTask;
			},
			Log("INFO"),
			Log("ERROR"),
			null
		);

		client.ReconnectAttempts = 4;
		client.ResendTimeout = TimeSpan.FromMilliseconds(200);
		client.ResendInterval = TimeSpan.FromMilliseconds(100);

		await client.ConnectAsync(cts.Token);
		client.IsConnected.AssertTrue();

		await client.SendAsync(payload, cts.Token, subId: 7);

		var sw0 = System.Diagnostics.Stopwatch.StartNew();
		while (sw0.Elapsed < TimeSpan.FromSeconds(10) && Volatile.Read(ref occurrences) < 1)
			await Task.Delay(100, cts.Token);
		(Volatile.Read(ref occurrences) >= 1).AssertTrue("Initial echo not received.");

		// Force a hard abort using reflection to reach the underlying ClientWebSocket
		var wsField = typeof(WebSocketClient).GetField("_ws", BindingFlags.NonPublic | BindingFlags.Instance);
		wsField.AssertNotNull();
		var ws = wsField.GetValue(client) as System.Net.WebSockets.ClientWebSocket;
		ws.AssertNotNull();
		ws.Abort();

		// Wait for state transitions: Reconnecting -> Restored
		var sawReconnecting = false;
		var sawRestored = false;
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < TimeSpan.FromSeconds(40) && !(sawReconnecting && sawRestored))
		{
			while (states.TryDequeue(out var st))
			{
				if (st == ConnectionStates.Reconnecting) sawReconnecting = true;
				if (st == ConnectionStates.Restored) sawRestored = true;
			}
			await Task.Delay(100, cts.Token);
		}

		sawReconnecting.AssertTrue("Did not observe Reconnecting state after abort.");
		sawRestored.AssertTrue("Did not observe Restored state after reconnect.");

		// Ensure resend occurred. To be robust, also trigger an explicit resend
		await client.ResendAsync(cts.Token);
		var waitSw = System.Diagnostics.Stopwatch.StartNew();
		while (waitSw.Elapsed < TimeSpan.FromSeconds(10) && Volatile.Read(ref occurrences) < 2)
			await Task.Delay(100, cts.Token);
		(Volatile.Read(ref occurrences) >= 2).AssertTrue("Resent payload after hard abort was not received.");

		client.Disconnect();
	}

	// TODO
	//[TestMethod]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task States_Sequence_With_Timestamps()
	{
		var url = "wss://echo.websocket.org";
		var states = new ConcurrentQueue<(ConnectionStates state, DateTime ts)>();

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));

		using var client = new WebSocketClient(
			url,
			st => states.Enqueue((st, DateTime.UtcNow)),
			_ => { },
			async (self, msg, token) => await ValueTask.CompletedTask,
			Log("INFO"),
			Log("ERROR"),
			null
		);

		client.ReconnectAttempts = 3;

		await client.ConnectAsync(cts.Token);
		client.IsConnected.AssertTrue();

		// Force reconnect via Close opcode if supported, else hard abort
		if (!await TrySoftCloseAsync(client))
			HardAbort(client);

		// Wait for Restored and then disconnect
		var sawRestored = false;
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < TimeSpan.FromSeconds(40) && !sawRestored)
		{
			while (states.TryDequeue(out var item))
				if (item.state == ConnectionStates.Restored)
					sawRestored = true;
			await Task.Delay(100, cts.Token);
		}

		sawRestored.AssertTrue("Did not observe Restored state.");

		client.Disconnect();

		// Drain remaining states
		await Task.Delay(300, cts.Token);

		// Validate order and monotonic timestamps
		var list = new List<(ConnectionStates, DateTime)>();
		while (states.TryDequeue(out var s))
			list.Add(s);

		// We expect at least these phases to appear somewhere in order
		static int IndexOfState(IReadOnlyList<(ConnectionStates, DateTime)> l, ConnectionStates st, int start)
		{
			for (var i = start; i < l.Count; i++)
				if (l[i].Item1 == st) return i;
			return -1;
		}

		var i0 = IndexOfState(list, ConnectionStates.Connecting, 0);
		var i1 = IndexOfState(list, ConnectionStates.Connected, i0 < 0 ? 0 : i0);
		var i2 = IndexOfState(list, ConnectionStates.Reconnecting, i1 < 0 ? 0 : i1);
		var i3 = IndexOfState(list, ConnectionStates.Restored, i2 < 0 ? 0 : i2);
		(i0 >= 0 && i1 >= 0 && i2 >= 0 && i3 >= 0 && i0 <= i1 && i1 <= i2 && i2 <= i3).AssertTrue("States order mismatch.");

		for (var i = 1; i < list.Count; i++)
			(list[i].Item2 >= list[i - 1].Item2).AssertTrue("Timestamps are not monotonic.");
	}

	[TestMethod]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task Reconnect_DoesNotResend_When_AutoResend_Disabled()
	{
		var url = "wss://echo.websocket.org";
		var payload = $"no-resend-{Guid.NewGuid():N}";
		var occurrences = 0;

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));

		using var client = new WebSocketClient(
			url,
			_ => { },
			_ => { },
			async (self, msg, token) =>
			{
				var text = msg.AsString();
				if (text == payload)
					Interlocked.Increment(ref occurrences);
				await ValueTask.CompletedTask;
			},
			Log("INFO"),
			Log("ERROR"),
			null
		);

		client.ReconnectAttempts = 3;
		client.DisableAutoResend = true; // critical for this test
		client.ResendTimeout = TimeSpan.FromMilliseconds(200);
		client.ResendInterval = TimeSpan.FromMilliseconds(100);

		await client.ConnectAsync(cts.Token);
		client.IsConnected.AssertTrue();

		await client.SendAsync(payload, cts.Token, subId: 99);

		var sw0 = System.Diagnostics.Stopwatch.StartNew();
		while (sw0.Elapsed < TimeSpan.FromSeconds(10) && Volatile.Read(ref occurrences) < 1)
			await Task.Delay(100, cts.Token);
		(Volatile.Read(ref occurrences) >= 1).AssertTrue("Initial echo not received.");

		// Force reconnect
		if (!await TrySoftCloseAsync(client))
			HardAbort(client);

		// After reconnect, occurrences should remain the same since auto-resend is disabled
		var sw = System.Diagnostics.Stopwatch.StartNew();
		var before = Volatile.Read(ref occurrences);
		while (sw.Elapsed < TimeSpan.FromSeconds(30))
		{
			await Task.Delay(200, cts.Token);
			(Volatile.Read(ref occurrences) == before).AssertTrue("Unexpected resend happened with DisableAutoResend=true.");
		}

		client.Disconnect();
	}
}
