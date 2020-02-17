namespace Ecng.Net
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Net.Security;
	using System.Net.Sockets;
	using System.Security;
	using System.Security.Authentication;
	using System.Security.Cryptography.X509Certificates;
	using System.Web;

	using Ecng.Common;
	using Ecng.Localization;

	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;
	using Newtonsoft.Json.Serialization;

	public static class NetworkHelper
	{
		public const int MtuSize = 1600;

		/// <summary>
		/// Gets the user address.
		/// </summary>
		/// <value>The user address.</value>
		public static IPAddress UserAddress => 
#if NETCOREAPP
			throw new PlatformNotSupportedException();
#else
			(HttpContext.Current == null ? ChannelHelper.GetClientEndPoint().Address : HttpContext.Current.Request.UserHostAddress.To<IPAddress>());
#endif

		public static bool IsLocal(this EndPoint endPoint)
		{
			if (endPoint == null)
				throw new ArgumentNullException(nameof(endPoint));

			if (endPoint is IPEndPoint ip)
				return IPAddress.IsLoopback(ip.Address);
			else if (endPoint is DnsEndPoint dns)
				return dns.Host.CompareIgnoreCase("localhost");
			else
				throw new ArgumentOutOfRangeException(nameof(endPoint), endPoint, "Invalid argument value.".Translate());
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
				throw new ArgumentNullException(nameof(socket));

			try
			{
				return !(socket.Poll(timeOut, SelectMode.SelectRead) && socket.Available == 0);
			}
			catch (SocketException)
			{
				return false;
			}
		}

		public static void Read(this Socket socket, byte[] buffer, int offset, int len)
		{
			var left = len;

			while (left > 0)
			{
				var read = socket.Receive(buffer, offset + (len - left), left, SocketFlags.None);

				if (read <= 0)
					throw new IOException("Stream returned '{0}' bytes.".Translate().Put(read));

				left -= read;
			}
		}

		public static bool Wait(this Socket socket, int timeOut)
		{
			if (socket == null)
				throw new ArgumentNullException(nameof(socket));

			return socket.Poll(timeOut, SelectMode.SelectRead) && socket.Available != 0;
		}

		public static void JoinMulticast(this Socket socket, IPAddress address)
		{
			if (address == null)
				throw new ArgumentNullException(nameof(address));

			socket.JoinMulticast(new MulticastSourceAddress { GroupAddress = address });
		}

		public static void JoinMulticast(this Socket socket, MulticastSourceAddress address)
		{
			if (socket == null)
				throw new ArgumentNullException(nameof(socket));

			if (address.SourceAddress == null)
				socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(address.GroupAddress));
			else
				socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddSourceMembership, GetBytes(address));
		}

		public static void LeaveMulticast(this Socket socket, IPAddress address)
		{
			if (address == null)
				throw new ArgumentNullException(nameof(address));

			socket.LeaveMulticast(new MulticastSourceAddress { GroupAddress = address });
		}

		public static void LeaveMulticast(this Socket socket, MulticastSourceAddress address)
		{
			if (socket == null)
				throw new ArgumentNullException(nameof(socket));

			if (address.SourceAddress == null)
				socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, new MulticastOption(address.GroupAddress));
			else
				socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropSourceMembership, GetBytes(address));
		}

		private static byte[] GetBytes(MulticastSourceAddress address)
		{
			if (address == null)
				throw new ArgumentNullException(nameof(address));

			// https://social.msdn.microsoft.com/Forums/en-US/e8063f6d-22f5-445e-a00c-bf46b46c1561/how-to-join-source-specific-multicast-group-in-c?forum=netfxnetcom

			var maddr = new byte[12];
			Array.Copy(address.GroupAddress.GetAddressBytes(), 0, maddr, 0, 4); // <ip> from "config.xml"
			Array.Copy(address.SourceAddress.GetAddressBytes(), 0, maddr, 4, 4); // <src-ip> from "config.xml"
			Array.Copy(IPAddress.Any.GetAddressBytes(), 0, maddr, 8, 4);
			return maddr;
		}

		public static SslStream ToSsl(this Stream stream, SslProtocols sslProtocol,
			bool checkCertificateRevocation, bool validateRemoteCertificates,
			string targetHost, string sslCertificate, SecureString sslCertificatePassword,
			RemoteCertificateValidationCallback certificateValidationCallback = null,
			LocalCertificateSelectionCallback certificateSelectionCallback = null)
		{
			var ssl = validateRemoteCertificates
				? new SslStream(stream, true, certificateValidationCallback, certificateSelectionCallback)
				: new SslStream(stream);

			if (sslCertificate.IsEmpty())
				ssl.AuthenticateAsClient(targetHost);
			else
			{
				var cert = new X509Certificate2(sslCertificate, sslCertificatePassword);
				ssl.AuthenticateAsClient(targetHost, new X509CertificateCollection { cert }, sslProtocol, checkCertificateRevocation);
			}

			return ssl;
		}

		public static void Connect(this TcpClient client, EndPoint address)
		{
			if (client == null)
				throw new ArgumentNullException(nameof(client));

			client.Connect(address.GetHost(), address.GetPort());
		}

		public static T DeserializeObject<T>(this string content)
		{
			return (T)content.DeserializeObject(typeof(T));
		}

		public static T DeserializeObject<T>(this JToken token)
		{
			return (T)token.DeserializeObject(typeof(T));
		}

		public static object DeserializeObject(this string content, Type type)
		{
			if (content.IsEmpty())
				throw new ArgumentNullException(nameof(content));

			if (type == null)
				throw new ArgumentNullException(nameof(type));

			try
			{
				if (content == "null")
					return null;

				return JsonConvert.DeserializeObject(content, type);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Can't convert {content} to type '{type.Name}'.", ex);
			}
		}

		public static object DeserializeObject(this JToken token, Type type)
		{
			if (token == null)
				throw new ArgumentNullException(nameof(token));

			if (type == null)
				throw new ArgumentNullException(nameof(type));

			try
			{
				if (token.Type == JTokenType.String && (string)token == "null")
					return null;

				return token.ToObject(type);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Can't convert {token} to type '{type.Name}'.", ex);
			}
		}

		public static JsonSerializerSettings CreateJsonSerializerSettings()
		{
			return new JsonSerializerSettings
			{
				FloatParseHandling = FloatParseHandling.Decimal,
				NullValueHandling = NullValueHandling.Ignore,
				ContractResolver = new DefaultContractResolver
				{
					NamingStrategy = new SnakeCaseNamingStrategy()
				}
			};
		}
	}
}