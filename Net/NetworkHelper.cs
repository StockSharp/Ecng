namespace Ecng.Net
{
	using System;
	using System.Linq;
	using System.Net;
	using System.Net.Sockets;
	using System.Web;

	using Ecng.Common;

	public static class NetworkHelper
	{
		/// <summary>
		/// Gets the user address.
		/// </summary>
		/// <value>The user address.</value>
		public static IPAddress UserAddress
		{
			get
			{
				return (HttpContext.Current == null ? ChannelHelper.GetClientEndPoint().Address : HttpContext.Current.Request.UserHostAddress.To<IPAddress>());
			}
		}

		public static bool IsLocalIpAddress(this EndPoint endPoint)
		{
			var host = endPoint.GetHost();

			IPAddress[] hostIPs, localIPs;

			try
			{
				// get host IP addresses
				hostIPs = Dns.GetHostAddresses(host);

				// get local IP addresses
				localIPs = Dns.GetHostAddresses(Dns.GetHostName());
			}
			catch (Exception)
			{
				return false;
			}

			// any host IP equals to any local IP or to localhost
			return hostIPs.Any(h => IPAddress.IsLoopback(h) || localIPs.Contains(h));
		}

		public static string GetHost(this EndPoint endPoint)
		{
			if (endPoint == null)
				throw new ArgumentNullException("endPoint");

			if (endPoint is IPEndPoint)
			{
				return ((IPEndPoint)endPoint).Address.ToString();
			}
			else if (endPoint is DnsEndPoint)
			{
				return ((DnsEndPoint)endPoint).Host;
			}
			else
				throw new InvalidOperationException("Неизвестная информация об адресе.");
		}

		public static int GetPort(this EndPoint endPoint)
		{
			if (endPoint == null)
				throw new ArgumentNullException("endPoint");

			if (endPoint is IPEndPoint)
			{
				return ((IPEndPoint)endPoint).Port;
			}
			else if (endPoint is DnsEndPoint)
			{
				return ((DnsEndPoint)endPoint).Port;
			}
			else
				throw new InvalidOperationException("Неизвестная информация об адресе.");
		}

		public static bool IsConnected(this Socket socket, int timeOut = 1)
		{
			if (socket == null)
				throw new ArgumentNullException("socket");

			try
			{
				return !(socket.Poll(timeOut, SelectMode.SelectRead) && socket.Available == 0);
			}
			catch (SocketException)
			{
				return false;
			}
		}

		public static bool Wait(this Socket socket, int timeOut)
		{
			if (socket == null)
				throw new ArgumentNullException("socket");

			return socket.Poll(timeOut, SelectMode.SelectRead) && socket.Available != 0;
		}
	}
}