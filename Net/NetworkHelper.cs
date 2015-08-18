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

		public static bool IsLocal(this EndPoint endPoint)
		{
			if (endPoint == null)
				throw new ArgumentNullException("endPoint");

			if (endPoint is IPEndPoint)
				return IPAddress.IsLoopback(((IPEndPoint)endPoint).Address);
			else if (endPoint is DnsEndPoint)
				return ((DnsEndPoint)endPoint).Host.CompareIgnoreCase("localhost");
			else
				throw new InvalidOperationException("Неизвестная информация об адресе.");
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

		public static void JoinMulticast(this Socket socket, MulticastSourceAddress address)
		{
			if (socket == null)
				throw new ArgumentNullException("socket");

			if (address.SourceAddress == null)
				socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(address.GroupAddress));
			else
				socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddSourceMembership, GetBytes(address));
		}

		public static void LeaveMulticast(this Socket socket, MulticastSourceAddress address)
		{
			if (socket == null)
				throw new ArgumentNullException("socket");

			if (address.SourceAddress == null)
				socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, new MulticastOption(address.GroupAddress));
			else
				socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropSourceMembership, GetBytes(address));
		}

		private static byte[] GetBytes(MulticastSourceAddress address)
		{
			if (address == null)
				throw new ArgumentNullException("address");

			// https://social.msdn.microsoft.com/Forums/en-US/e8063f6d-22f5-445e-a00c-bf46b46c1561/how-to-join-source-specific-multicast-group-in-c?forum=netfxnetcom

			var maddr = new byte[12];
			Array.Copy(address.GroupAddress.GetAddressBytes(), 0, maddr, 0, 4); // <ip> from "config.xml"
			Array.Copy(address.SourceAddress.GetAddressBytes(), 0, maddr, 4, 4); // <src-ip> from "config.xml"
			Array.Copy(IPAddress.Any.GetAddressBytes(), 0, maddr, 8, 4);
			return maddr;
		}
	}
}