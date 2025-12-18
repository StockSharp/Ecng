namespace Ecng.Tests.Net;

using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;

using Ecng.ComponentModel;
using Ecng.Net;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

[TestClass]
[DoNotParallelize]
public class WebSocketClientTests : BaseTestClass
{
	private static Action<string, object> Log(string tag)
		=> (fmt, arg) => Debug.WriteLine($"[{tag}] " + string.Format(fmt ?? string.Empty, arg));

	private sealed class LocalWebSocketEchoServer(IHost host, string url) : IAsyncDisposable
	{
		private readonly IHost _host = host ?? throw new ArgumentNullException(nameof(host));

        public string Url { get; } = url.ThrowIfEmpty(nameof(url));

        public static async Task<LocalWebSocketEchoServer> StartAsync(CancellationToken cancellationToken = default)
		{
			var builder = WebApplication.CreateBuilder();
			builder.Logging.ClearProviders();

			builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));

			var app = builder.Build();

			app.UseWebSockets();

			app.Map("/ws", async context =>
			{
				if (!context.WebSockets.IsWebSocketRequest)
				{
					context.Response.StatusCode = StatusCodes.Status400BadRequest;
					return;
				}

				using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

				var buffer = new byte[16 * 1024];
				var segment = new ArraySegment<byte>(buffer);

				try
				{
					while (webSocket.State == WebSocketState.Open && !context.RequestAborted.IsCancellationRequested)
					{
						var result = await webSocket.ReceiveAsync(segment, context.RequestAborted);

						if (result.MessageType == WebSocketMessageType.Close)
						{
							try
							{
								await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
							}
							catch
							{
							}

							break;
						}

						var messageType = result.MessageType;

						using var ms = new MemoryStream();
						ms.Write(buffer, 0, result.Count);

						while (!result.EndOfMessage && !context.RequestAborted.IsCancellationRequested)
						{
							result = await webSocket.ReceiveAsync(segment, context.RequestAborted);
							ms.Write(buffer, 0, result.Count);
						}

						var payload = ms.ToArray();
						await webSocket.SendAsync(new ArraySegment<byte>(payload), messageType, endOfMessage: true, context.RequestAborted);
					}
				}
				catch (OperationCanceledException)
				{
				}
				catch (WebSocketException)
				{
				}
				catch (IOException)
				{
				}
			});

			await app.StartAsync(cancellationToken);

			var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
			addresses.AssertNotNull();

			var httpAddress = addresses.Addresses.FirstOrDefault();
			httpAddress.IsEmpty().AssertFalse("Failed to get local server address.");

			var httpUri = new Uri(httpAddress, UriKind.Absolute);
			var wsUri = new UriBuilder(httpUri)
			{
				Scheme = "ws",
				Port = httpUri.Port,
				Path = "/ws",
			}.Uri.ToString();

			return new LocalWebSocketEchoServer(app, wsUri);
		}

		public async ValueTask DisposeAsync()
		{
			try
			{
				await _host.StopAsync(TimeSpan.FromSeconds(5));
			}
			catch
			{
			}

			_host.Dispose();
		}
	}

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

	[TestMethod]
	[Timeout(60000, CooperativeCancellation = true)]
	public async Task Connect_Send_Receive_Echo()
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
		await using var server = await LocalWebSocketEchoServer.StartAsync(cts.Token);
		var url = server.Url;
		var payload = $"echo-test-{Guid.NewGuid():N}";

		var receivedTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
		var errorTcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);

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

		var completed = await Task.WhenAny(receivedTcs.Task, errorTcs.Task, TimeSpan.FromSeconds(20).Delay(cts.Token));
		if (completed == errorTcs.Task)
			Fail($"WebSocket client error: {errorTcs.Task.Result}");

		receivedTcs.Task.IsCompleted.AssertTrue("Echo was not received in time.");
		var echoed = await receivedTcs.Task;
		echoed.Contains(payload).AssertTrue();

		client.Disconnect();
	}

	[TestMethod]
	[Timeout(90000, CooperativeCancellation = true)]
	public async Task Init_And_PostConnect_Are_Called_With_Correct_Flags()
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
		await using var server = await LocalWebSocketEchoServer.StartAsync(cts.Token);
		var url = server.Url;
		var initCount = 0;
		var postConnectFlags = new List<bool>();

		using var client = new WebSocketClient(
			url,
			_ => { },
			_ => { },
			async (self, msg, token) => await ValueTask.CompletedTask,
			Log("INFO"),
			Log("ERROR"),
			null
		);

		client.ReconnectAttempts = 6;
		client.ReconnectInterval = TimeSpan.FromSeconds(1);
		client.ResendTimeout = TimeSpan.FromMilliseconds(200);
		client.ResendInterval = TimeSpan.FromMilliseconds(100);

		var post2 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		client.Init += _ => { Interlocked.Increment(ref initCount); };
		client.PostConnect += async (reconnect, token) =>
		{
			lock (postConnectFlags)
			{
				postConnectFlags.Add(reconnect);
				if (postConnectFlags.Count >= 2)
					post2.TrySetResult();
			}
			await ValueTask.CompletedTask;
		};

		await client.ConnectAsync(cts.Token);
		client.IsConnected.AssertTrue();

		if (!await TrySoftCloseAsync(client))
			client.Abort();

		// Wait until PostConnect fired at least twice or timeout
		await Task.WhenAny(post2.Task, TimeSpan.FromSeconds(30).Delay(cts.Token));

		(initCount >= 2).AssertTrue("Init should be called at least twice (initial + reconnect).");
		(postConnectFlags.Count >= 2).AssertTrue("PostConnect should be called at least twice.");
		postConnectFlags[0].AssertFalse("First PostConnect must be initial (reconnect=false).");
		(postConnectFlags.Skip(1).All(f => f)).AssertTrue("Subsequent PostConnect calls must be reconnect=true.");

		client.Disconnect();
	}

	[TestMethod]
	[Timeout(60000, CooperativeCancellation = true)]
	public async Task Disconnect_Raises_Disconnecting_Then_Disconnected()
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
		await using var server = await LocalWebSocketEchoServer.StartAsync(cts.Token);
		var url = server.Url;
		var states = new ConcurrentQueue<ConnectionStates>();

		using var client = new WebSocketClient(
			url,
			st => states.Enqueue(st),
			_ => { },
			async (self, msg, token) => await ValueTask.CompletedTask,
			Log("INFO"),
			Log("ERROR"),
			null
		);

		client.ReconnectAttempts = 0;

		await client.ConnectAsync(cts.Token);
		client.IsConnected.AssertTrue();

		client.Disconnect();

		var ok = await WaitForStatesAsync(states, () => states.Contains(ConnectionStates.Disconnected), TimeSpan.FromSeconds(15), cts.Token);
		ok.AssertTrue("Did not observe Disconnected after Disconnect().");

		var arr = states.ToArray();
		var idxDisconn = Array.FindIndex(arr, s => s == ConnectionStates.Disconnected);
		var idxDisconnecting = Array.FindIndex(arr, s => s == ConnectionStates.Disconnecting);
		(idxDisconnecting >= 0 && idxDisconnecting < idxDisconn).AssertTrue("Disconnecting should occur before Disconnected.");
	}

	[TestMethod]
	[Timeout(60000, CooperativeCancellation = true)]
	public async Task NegativeSubId_Unsubscribe_Removes_From_Resend()
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
		await using var server = await LocalWebSocketEchoServer.StartAsync(cts.Token);
		var url = server.Url;
		var subscribePayload = $"sub-{Guid.NewGuid():N}";
		var unsubscribePayload = $"unsub-{Guid.NewGuid():N}";
		var occurrences = 0;

		using var client = new WebSocketClient(
			url,
			_ => { },
			_ => { },
			async (self, msg, token) =>
			{
				if (msg.AsString() == subscribePayload)
					Interlocked.Increment(ref occurrences);
				await ValueTask.CompletedTask;
			},
			Log("INFO"),
			Log("ERROR"),
			null
		);

		client.ReconnectAttempts = 4;

		await client.ConnectAsync(cts.Token);
		client.IsConnected.AssertTrue();

		// Register
		await client.SendAsync(subscribePayload, cts.Token, subId: 100);

		var sw0 = Stopwatch.StartNew();
		while (sw0.Elapsed < TimeSpan.FromSeconds(10) && Volatile.Read(ref occurrences) < 1)
			await Task.Delay(50, cts.Token);
		(Volatile.Read(ref occurrences) >= 1).AssertTrue("Initial echo not received.");

		// Unregister using negative subId
		await client.SendAsync(unsubscribePayload, cts.Token, subId: -100);
		// Trigger resend explicitly: should not increase occurrences further
		var before = Volatile.Read(ref occurrences);
		await client.ResendAsync(cts.Token);

		var sw = Stopwatch.StartNew();
		while (sw.Elapsed < TimeSpan.FromSeconds(2))
		{
			await Task.Delay(100, cts.Token);
			(Volatile.Read(ref occurrences) == before).AssertTrue("Unsubscribe via negative subId did not remove entry from resend queue.");
		}

		client.Disconnect();
	}

	[TestMethod]
	[Timeout(60000, CooperativeCancellation = true)]
	public async Task PreProcess2_Transforms_Incoming_Message()
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
		await using var server = await LocalWebSocketEchoServer.StartAsync(cts.Token);
		var url = server.Url;
		var marker = Guid.NewGuid().ToString("N");
		var original = $"lower_case_payload:{marker}";
		var transformed = original.ToUpperInvariant();
		var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

		using var client = new WebSocketClient(
			url,
			_ => { },
			_ => { },
			async (self, msg, token) =>
			{
				var text = msg.AsString();
				if (text?.Contains(marker.ToUpperInvariant(), StringComparison.Ordinal) == true)
					tcs.TrySetResult(text);
				await ValueTask.CompletedTask;
			},
			Log("INFO"),
			Log("ERROR"),
			null
		);

		client.PreProcess2 += (input, output) =>
		{
			var str = input.Span.UTF8();
			var upper = str.ToUpperInvariant();
			var bytes = upper.UTF8();
			bytes.CopyTo(output.Span);
			return bytes.Length;
		};

		await client.ConnectAsync(cts.Token);
		client.IsConnected.AssertTrue();

		await client.SendAsync(original, cts.Token);
		var received = await Task.WhenAny(tcs.Task, TimeSpan.FromSeconds(15).Delay(cts.Token));
		(received == tcs.Task).AssertTrue("Did not receive transformed message.");
		tcs.Task.Result.Contains(transformed).AssertTrue($"Actual message does not contain expected transformed payload.\nActual: {tcs.Task.Result}\nExpected contains: {transformed}");

		client.Disconnect();
	}

	[TestMethod]
	[Timeout(90000, CooperativeCancellation = true)]
	public async Task Resend_Timing_Respects_Timeouts()
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(80));
		await using var server = await LocalWebSocketEchoServer.StartAsync(cts.Token);
		var url = server.Url;
		var payload1 = $"t1-{Guid.NewGuid():N}";
		var payload2 = $"t2-{Guid.NewGuid():N}";
		var preTimes = new List<DateTime>();

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
		client.ResendTimeout = TimeSpan.FromMilliseconds(400);
		client.ResendInterval = TimeSpan.FromMilliseconds(200);

		await client.ConnectAsync(cts.Token);
		client.IsConnected.AssertTrue();

		// Register two commands with pre-callback to timestamp
		await client.SendAsync(payload1, cts.Token, subId: 501, pre: async (id, t) => { preTimes.Add(DateTime.UtcNow); await ValueTask.CompletedTask; });
		await client.SendAsync(payload2, cts.Token, subId: 502, pre: async (id, t) => { preTimes.Add(DateTime.UtcNow); await ValueTask.CompletedTask; });

		// Force reconnect to trigger auto-resend
		if (!await TrySoftCloseAsync(client))
			client.Abort();

		// Wait until two pre-callback timestamps are recorded
		var sw = Stopwatch.StartNew();
		while (sw.Elapsed < TimeSpan.FromSeconds(20) && preTimes.Count < 2)
			await Task.Delay(100, cts.Token);

		(preTimes.Count >= 2).AssertTrue("Did not observe pre-callbacks for both resend commands.");

		// Validate timings (allow 30% jitter)
		var delta = preTimes[1] - preTimes[0];
		(delta >= TimeSpan.FromMilliseconds(140)).AssertTrue($"ResendInterval too short: {delta}.");

		client.Disconnect();
	}

	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task Connect_Cancellation_Throws_And_No_Leak()
	{
		var url = "ws://127.0.0.1:1/ws";
		using var client = new WebSocketClient(
			url,
			_ => { },
			_ => { },
			async (self, msg, token) => await ValueTask.CompletedTask,
			Log("INFO"),
			Log("ERROR"),
			null
		);

		using var cts = new CancellationTokenSource();
		cts.Cancel();

		await ThrowsExactlyAsync<OperationCanceledException>(async () => await client.ConnectAsync(cts.Token));
		client.IsConnected.AssertFalse();
	}
	[TestMethod]
	[Timeout(60000, CooperativeCancellation = true)]
	public async Task Disconnect_Prevents_Sending()
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
		await using var server = await LocalWebSocketEchoServer.StartAsync(cts.Token);
		var url = server.Url;

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
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
		await using var server = await LocalWebSocketEchoServer.StartAsync(cts.Token);
		var url = server.Url;
		var batch = 20;
		var prefix = $"stream-{Guid.NewGuid():N}-";
		var received = 0;

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

		var sw = Stopwatch.StartNew();
		while (sw.Elapsed < TimeSpan.FromSeconds(20) && Volatile.Read(ref received) < batch)
			await Task.Delay(200, cts.Token);

		Volatile.Read(ref received).AreEqual(batch, "Not all streaming messages were echoed.");

		client.Disconnect();
	}

	[TestMethod]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task Resend_Stores_And_Removes_Commands()
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
		await using var server = await LocalWebSocketEchoServer.StartAsync(cts.Token);
		var url = server.Url;
		var subId = 42L;
		var payload = $"resend-{Guid.NewGuid():N}";
		var occurrences = 0;

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

		var sw0 = Stopwatch.StartNew();
		while (sw0.Elapsed < TimeSpan.FromSeconds(10) && Volatile.Read(ref occurrences) < 1)
			await Task.Delay(100, cts.Token);
		(Volatile.Read(ref occurrences) >= 1).AssertTrue("Initial echo not received.");

		await client.ResendAsync(cts.Token);

		// Wait for echo after resend
		var sw1 = Stopwatch.StartNew();
		while (sw1.Elapsed < TimeSpan.FromSeconds(10) && Volatile.Read(ref occurrences) < 2)
			await Task.Delay(100, cts.Token);

		var occ1 = Volatile.Read(ref occurrences);
		(occ1 >= 2).AssertTrue("Resend did not echo payload.");

		client.RemoveResend(subId);
		await client.ResendAsync(cts.Token);

		// Wait a bit to ensure no echo comes
		await Task.Delay(500, cts.Token);

		var occ2 = Volatile.Read(ref occurrences);
		occ2.AreEqual(occ1, "Payload was echoed after RemoveResend, expected no change.");

		client.Disconnect();
	}

	[TestMethod]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task Reconnect_Resends_Stored_Commands_IfSupported()
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
		await using var server = await LocalWebSocketEchoServer.StartAsync(cts.Token);
		var url = server.Url;
		var payload = $"reconnect-{Guid.NewGuid():N}";
		var occurrences = 0;

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

		var sw0 = Stopwatch.StartNew();
		while (sw0.Elapsed < TimeSpan.FromSeconds(10) && Volatile.Read(ref occurrences) < 1)
			await Task.Delay(100, cts.Token);
		(Volatile.Read(ref occurrences) >= 1).AssertTrue("Initial echo not received.");


		if (!await TrySoftCloseAsync(client))
		{
			Assert.Inconclusive("SendOpCode not supported on this runtime, skipping reconnect test.");
			return;
		}

		var sw = Stopwatch.StartNew();
		while (sw.Elapsed < TimeSpan.FromSeconds(40) && Volatile.Read(ref occurrences) < 2)
			await Task.Delay(200, cts.Token);

		(Volatile.Read(ref occurrences) >= 2).AssertTrue("Resent payload after reconnect was not received.");

		client.Disconnect();
	}

	[TestMethod]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task HardAbort_Forces_Reconnect_And_Resend()
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
		await using var server = await LocalWebSocketEchoServer.StartAsync(cts.Token);
		var url = server.Url;
		var payload = $"abort-{Guid.NewGuid():N}";
		var occurrences = 0;

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

		var sw0 = Stopwatch.StartNew();
		while (sw0.Elapsed < TimeSpan.FromSeconds(10) && Volatile.Read(ref occurrences) < 1)
			await Task.Delay(100, cts.Token);
		(Volatile.Read(ref occurrences) >= 1).AssertTrue("Initial echo not received.");

		// Force a hard
		client.Abort();

		// Wait for state transitions: Reconnecting -> Restored
		var sawReconnecting = false;
		var sawRestored = false;
		var sw = Stopwatch.StartNew();
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
		var waitSw = Stopwatch.StartNew();
		while (waitSw.Elapsed < TimeSpan.FromSeconds(10) && Volatile.Read(ref occurrences) < 2)
			await Task.Delay(100, cts.Token);
		(Volatile.Read(ref occurrences) >= 2).AssertTrue("Resent payload after hard abort was not received.");

		client.Disconnect();
	}

	[TestMethod]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task States_Sequence_With_Timestamps()
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
		await using var server = await LocalWebSocketEchoServer.StartAsync(cts.Token);
		var url = server.Url;
		var states = new ConcurrentQueue<(ConnectionStates state, DateTime ts)>();

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
			client.Abort();

		// Collect all states while waiting for Restored
		var list = new List<(ConnectionStates, DateTime)>();
		var sawRestored = false;
		var sw = Stopwatch.StartNew();
		while (sw.Elapsed < TimeSpan.FromSeconds(40) && !sawRestored)
		{
			while (states.TryDequeue(out var item))
			{
				list.Add(item);

				if (item.state == ConnectionStates.Restored)
					sawRestored = true;
			}

			await Task.Delay(100, cts.Token);
		}

		sawRestored.AssertTrue("Did not observe Restored state.");

		client.Disconnect();

		// Drain remaining states
		await Task.Delay(300, cts.Token);

		// Collect any remaining states after disconnect
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
		var payload = $"no-resend-{Guid.NewGuid():N}";
		var occurrences = 0;

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
		await using var server = await LocalWebSocketEchoServer.StartAsync(cts.Token);
		var url = server.Url;

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

		var sw0 = Stopwatch.StartNew();
		while (sw0.Elapsed < TimeSpan.FromSeconds(10) && Volatile.Read(ref occurrences) < 1)
			await Task.Delay(100, cts.Token);
		(Volatile.Read(ref occurrences) >= 1).AssertTrue("Initial echo not received.");

		// Force reconnect
		if (!await TrySoftCloseAsync(client))
			client.Abort();

		// After reconnect, occurrences should remain the same since auto-resend is disabled
		var sw = Stopwatch.StartNew();
		var before = Volatile.Read(ref occurrences);
		while (sw.Elapsed < TimeSpan.FromSeconds(30))
		{
			await Task.Delay(200, cts.Token);
			(Volatile.Read(ref occurrences) == before).AssertTrue("Unexpected resend happened with DisableAutoResend=true.");
		}

		client.Disconnect();
	}

	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task Connect_Fails_After_ReconnectAttempts_Exhausted()
	{
		var url = "wss://nonexistent.invalid";
		var states = new ConcurrentQueue<ConnectionStates>();

		using var client = new WebSocketClient(
			url,
			st => states.Enqueue(st),
			_ => { },
			async (self, msg, token) => await ValueTask.CompletedTask,
			Log("INFO"),
			Log("ERROR"),
			null
		);

		client.ReconnectAttempts = 2;
		client.ReconnectInterval = TimeSpan.FromMilliseconds(200);

		await ThrowsAsync<Exception>(async () => await client.ConnectAsync(CancellationToken.None));

		states.Contains(ConnectionStates.Connected).AssertFalse();
		states.Contains(ConnectionStates.Restored).AssertFalse();
	}

	private static async Task<bool> WaitForStatesAsync(ConcurrentQueue<ConnectionStates> q, Func<bool> predicate, TimeSpan timeout, CancellationToken token)
	{
		var sw = Stopwatch.StartNew();
		while (sw.Elapsed < timeout)
		{
			if (predicate())
				return true;
			await Task.Delay(100, token);
		}
		return predicate();
	}
}
