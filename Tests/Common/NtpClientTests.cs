namespace Ecng.Tests.Common;

using System.Net;
using System.Net.Sockets;

[TestClass]
public class NtpClientTests
{
	private static byte[] BuildResponse(DateTime utc)
	{
		var ms = (ulong)(utc - new DateTime(1900, 1, 1)).TotalMilliseconds;
		var intpart = ms; // 1000;
		var fractpart = (ms % 1000) * 0x100000000; // 1000;
		var resp = new byte[48];
		resp[0] = 0x1B;
		for (var i = 3; i >= 0; i--)
		{
			resp[40 + i] = (byte)(intpart & 0xFF);
			intpart >>= 8;
		}
		for (var i = 3; i >= 0; i--)
		{
			resp[44 + i] = (byte)(fractpart & 0xFF);
			fractpart >>= 8;
		}
		return resp;
	}

	[TestMethod]
	public void GetUtcTime()
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
		var actual = client.GetUtcTime();
		server.Dispose();
		task.Wait();
		actual.AssertEqual(expected);
	}

	[TestMethod]
	public void GetLocalTime()
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
		var actual = client.GetLocalTime(zone);
		server.Dispose();
		task.Wait();
		actual.AssertEqual(expected);
	}

	[TestMethod]
	public void StringCtor()
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
		client.GetUtcTime().AssertEqual(expected);
		server.Dispose();
		task.Wait();
	}

	[TestMethod]
	public void LocalTimeNull()
	{
		var client = new NtpClient(new IPEndPoint(IPAddress.Loopback, 1));
		Assert.ThrowsExactly<ArgumentNullException>(() => client.GetLocalTime(null));
	}
}