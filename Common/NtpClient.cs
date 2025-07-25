﻿namespace Ecng.Common;

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Nito.AsyncEx;

/// <summary>
/// Provides functionality to retrieve time from a remote NTP server.
/// </summary>
/// <param name="ntpServer">The endpoint of the NTP server.</param>
public class NtpClient(EndPoint ntpServer)
{
	private readonly EndPoint _ntpServer = ntpServer ?? throw new ArgumentNullException(nameof(ntpServer));

	/// <summary>
	/// Initializes a new instance of the <see cref="NtpClient"/> class using the NTP server address.
	/// </summary>
	/// <param name="ntpServer">The NTP server address in the format "hostname:port". Default is "time-a.nist.gov:123".</param>
	public NtpClient(string ntpServer = "time-a.nist.gov:123")
		: this(ntpServer.To<EndPoint>())
	{
	}

	/// <summary>
	/// Retrieves the local time based on the specified time zone.
	/// </summary>
	/// <param name="info">The time zone information.</param>
	/// <param name="timeout">The timeout in milliseconds for the NTP request. Default is 5000ms.</param>
	/// <returns>The local time adjusted to the specified time zone.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="info"/> is null.</exception>
	public DateTime GetLocalTime(TimeZoneInfo info, int timeout = 5000)
		=> AsyncContext.Run(() => GetLocalTimeAsync(info, timeout));

	/// <summary>
	/// Retrieves the local time asynchronously based on the specified time zone.
	/// </summary>
	public async Task<DateTime> GetLocalTimeAsync(TimeZoneInfo info, int timeout = 5000, CancellationToken cancellationToken = default)
	{
		if (info is null)
			throw new ArgumentNullException(nameof(info));

		var utcTime = await GetUtcTimeAsync(timeout, cancellationToken).ConfigureAwait(false);
		return utcTime + info.GetUtcOffset(utcTime);
	}

	/// <summary>
	/// Retrieves the UTC time from the NTP server.
	/// </summary>
	/// <param name="timeout">The timeout in milliseconds for the NTP request. Default is 5000ms.</param>
	/// <returns>The UTC time as provided by the NTP server.</returns>
	public DateTime GetUtcTime(int timeout = 5000)
		=> AsyncContext.Run(() => GetUtcTimeAsync(timeout));

	/// <summary>
	/// Retrieves the UTC time from the NTP server asynchronously.
	/// </summary>
	public async Task<DateTime> GetUtcTimeAsync(int timeout = 5000, CancellationToken cancellationToken = default)
	{
		using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

		s.SendTimeout = timeout;
		s.ReceiveTimeout = timeout;

#if NET5_0_OR_GREATER
		await s.ConnectAsync(_ntpServer, cancellationToken);
#else
		s.Connect(_ntpServer);
#endif

		var ntpData = new byte[48]; // RFC 2030
		ntpData[0] = 0x1B;
		for (var i = 1; i < 48; i++)
			ntpData[i] = 0;

#if NET5_0_OR_GREATER
		await s.SendAsync(ntpData, SocketFlags.None, cancellationToken).ConfigureAwait(false);
		await s.ReceiveAsync(ntpData, SocketFlags.None, cancellationToken).ConfigureAwait(false);
#else
		await Task.Run(() =>
		{
			s.Send(ntpData);
			s.Receive(ntpData);
		}, cancellationToken).ConfigureAwait(false);
#endif

		const byte offsetTransmitTime = 40;
		ulong intpart = 0;
		ulong fractpart = 0;

		for (var i = 0; i <= 3; i++)
			intpart = 256 * intpart + ntpData[offsetTransmitTime + i];

		for (var i = 4; i <= 7; i++)
			fractpart = 256 * fractpart + ntpData[offsetTransmitTime + i];

		var milliseconds = (intpart * 1000 + (fractpart * 1000) / 0x100000000L);

		var timeSpan = TimeSpan.FromMilliseconds(milliseconds);

		var dateTime = new DateTime(1900, 1, 1);
		dateTime += timeSpan;

		return dateTime;
	}
}