namespace Ecng.Tests.Common;

using System.Net;
using System.Net.Sockets;

[TestClass]
public class NtpClientTests
{
	private static byte[] BuildResponse(DateTime utc)
	{
		var seconds = (ulong)(utc - new DateTime(1900, 1, 1)).TotalSeconds;
		var fraction = (ulong)((utc - new DateTime(1900, 1, 1)).Ticks % TimeSpan.TicksPerSecond * 0x100000000L / TimeSpan.TicksPerSecond);
		var resp = new byte[48];
		resp[0] = 0x1B;

		for (var i = 3; i >= 0; i--)
		{
			resp[40 + i] = (byte)(seconds & 0xFF);
			seconds >>= 8;
		}

		for (var i = 3; i >= 0; i--)
		{
			resp[44 + i] = (byte)(fraction & 0xFF);
			fraction >>= 8;
		}

		return resp;
	}

	[TestMethod]
	public async Task GetUtcTime()
	{
		var server = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
		var ep = (IPEndPoint)server.Client.LocalEndPoint!;
		var expected = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);

		var task = Task.Run(async () =>
		{
			var req = await server.ReceiveAsync();
			var resp = BuildResponse(expected);
			await server.SendAsync(resp, resp.Length, req.RemoteEndPoint);
		});

		var client = new NtpClient(new IPEndPoint(IPAddress.Loopback, ep.Port));
		var actual = await client.GetUtcTimeAsync();
		server.Dispose();
		await task;
		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task GetLocalTime()
	{
		var zone = TimeZoneInfo.CreateCustomTimeZone("tz", TimeSpan.FromHours(2), "tz", "tz");
		var server = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
		var ep = (IPEndPoint)server.Client.LocalEndPoint!;
		var utc = new DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Utc);
		var expected = utc + zone.GetUtcOffset(utc);

		var task = Task.Run(async () =>
		{
			var req = await server.ReceiveAsync();
			var resp = BuildResponse(utc);
			await server.SendAsync(resp, resp.Length, req.RemoteEndPoint);
		});

		var client = new NtpClient(new IPEndPoint(IPAddress.Loopback, ep.Port));
		var actual = await client.GetLocalTimeAsync(zone);
		server.Dispose();
		await task;
		actual.AssertEqual(expected);
	}

	[TestMethod]
	public async Task StringCtor()
	{
		var server = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
		var ep = (IPEndPoint)server.Client.LocalEndPoint!;
		var expected = DateTime.UtcNow;

		var task = Task.Run(async () =>
		{
			var req = await server.ReceiveAsync();
			var resp = BuildResponse(expected);
			await server.SendAsync(resp, resp.Length, req.RemoteEndPoint);
		});

		var client = new NtpClient($"127.0.0.1:{ep.Port}");
		var actual = await client.GetUtcTimeAsync();
		(Math.Abs((actual - expected).TotalSeconds) < 1).AssertTrue();
		server.Dispose();
		await task;
	}

	[TestMethod]
	public async Task LocalTimeNull()
	{
		var client = new NtpClient(new IPEndPoint(IPAddress.Loopback, 1));
		await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await client.GetLocalTimeAsync(null));
	}

	[TestMethod]
	public async Task RealNtpServer()
	{
		// This test checks real NTP server connectivity. It is integration, not unit test.
		var client = new NtpClient(); // default: time-a.nist.gov:123
		var ntpTime = await client.GetUtcTimeAsync();
		// Should be within 10 minutes of system UTC time (allowing for clock drift and network delays)
		(Math.Abs((ntpTime - DateTime.UtcNow).TotalMinutes) < 1).AssertTrue();
	}
}