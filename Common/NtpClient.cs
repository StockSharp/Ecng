namespace Ecng.Common
{
	using System;
	using System.Net;
	using System.Net.Sockets;

	public class NtpClient
	{
		private readonly EndPoint _ntpServer;

		/// <summary>
		/// Create <see cref="NtpClient"/>.
		/// </summary>
		/// <param name="ntpServer">NTP server.</param>
		public NtpClient(string ntpServer = "time-a.nist.gov:123")
			: this(ntpServer.To<EndPoint>())
		{
			//var address = Dns.GetHostEntry(ntpServer).AddressList;

			//if (address is null || address.Length == 0)
			//    throw new ArgumentException(string.Format("Could not resolve ip address from '{0}'.", ntpServer), "ntpServer");

			//_endPoint = new IPEndPoint(address[0], 123);
		}

		/// <summary>
		/// Create <see cref="NtpClient"/>.
		/// </summary>
		/// <param name="ntpServer">NTP server.</param>
		public NtpClient(EndPoint ntpServer)
		{
			_ntpServer = ntpServer ?? throw new ArgumentNullException(nameof(ntpServer));
		}

		public DateTime GetLocalTime(TimeZoneInfo info, int timeout = 5000)
		{
			if (info is null)
				throw new ArgumentNullException(nameof(info));

			var utcTime = GetUtcTime(timeout);
			return utcTime + info.GetUtcOffset(utcTime);
		}

		public DateTime GetUtcTime(int timeout = 5000)
		{
			using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			s.SendTimeout = timeout;
			s.ReceiveTimeout = timeout;

			s.Connect(_ntpServer);

			var ntpData = new byte[48]; // RFC 2030
			ntpData[0] = 0x1B;
			for (var i = 1; i < 48; i++)
				ntpData[i] = 0;

			s.Send(ntpData);
			s.Receive(ntpData);

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
}