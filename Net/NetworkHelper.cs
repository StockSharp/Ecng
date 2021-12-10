namespace Ecng.Net
{
	using System;
	using System.Collections.Specialized;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Net.Security;
	using System.Net.Sockets;
	using System.Net.Http;
	using System.Security;
	using System.Security.Authentication;
	using System.Security.Cryptography.X509Certificates;
	using System.Security.Cryptography;
	using System.Text;
	using System.Web;

	using Ecng.Common;
	using Ecng.Collections;

	public static class NetworkHelper
	{
		public const int MtuSize = 1600;

		public static bool IsLocal(this EndPoint endPoint)
		{
			if (endPoint is null)
				throw new ArgumentNullException(nameof(endPoint));

			if (endPoint is IPEndPoint ip)
				return IPAddress.IsLoopback(ip.Address);
			else if (endPoint is DnsEndPoint dns)
				return dns.Host.EqualsIgnoreCase("localhost");
			else
				throw new ArgumentOutOfRangeException(nameof(endPoint), endPoint, "Invalid argument value.");
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
			if (socket is null)
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
					throw new IOException($"Stream returned '{read}' bytes.");

				left -= read;
			}
		}

		public static bool Wait(this Socket socket, int timeOut)
		{
			if (socket is null)
				throw new ArgumentNullException(nameof(socket));

			return socket.Poll(timeOut, SelectMode.SelectRead) && socket.Available != 0;
		}

		public static void JoinMulticast(this Socket socket, IPAddress address)
		{
			if (address is null)
				throw new ArgumentNullException(nameof(address));

			socket.JoinMulticast(new MulticastSourceAddress { GroupAddress = address });
		}

		public static void JoinMulticast(this Socket socket, MulticastSourceAddress address)
		{
			if (socket is null)
				throw new ArgumentNullException(nameof(socket));

			if (address.SourceAddress is null)
				socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(address.GroupAddress));
			else
				socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddSourceMembership, GetBytes(address));
		}

		public static void LeaveMulticast(this Socket socket, IPAddress address)
		{
			if (address is null)
				throw new ArgumentNullException(nameof(address));

			socket.LeaveMulticast(new MulticastSourceAddress { GroupAddress = address });
		}

		public static void LeaveMulticast(this Socket socket, MulticastSourceAddress address)
		{
			if (socket is null)
				throw new ArgumentNullException(nameof(socket));

			if (address.SourceAddress is null)
				socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, new MulticastOption(address.GroupAddress));
			else
				socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropSourceMembership, GetBytes(address));
		}

		private static byte[] GetBytes(MulticastSourceAddress address)
		{
			if (address is null)
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
			if (client is null)
				throw new ArgumentNullException(nameof(client));

			client.Connect(address.GetHost(), address.GetPort());
		}

		public static string EncodeToHtml(this string text)
		{
			return HttpUtility.HtmlEncode(text);
		}

		public static string DecodeFromHtml(this string text)
		{
			return HttpUtility.HtmlDecode(text);
		}

		private static readonly Encoding _urlEncoding = Encoding.UTF8;

		public static string EncodeUrl(this string url)
		{
			return HttpUtility.UrlEncode(url, _urlEncoding);
		}

		public static string DecodeUrl(this string url)
		{
			return HttpUtility.UrlDecode(url, _urlEncoding);
		}

		public static NameValueCollection ParseUrl(this string url)
		{
			return HttpUtility.ParseQueryString(url, _urlEncoding);
		}

		public static string UrlEncodeToUpperCase(this string url)
		{
			if (url.IsEmpty())
				return url;

			var temp = url.ToCharArray();

			for (var i = 0; i < temp.Length - 2; i++)
			{
				if (temp[i] != '%')
					continue;

				temp[i + 1] = temp[i + 1].ToUpper(false);
				temp[i + 2] = temp[i + 2].ToUpper(false);
			}

			return new string(temp);
		}

		public static string XmlEscape(this string content)
			=> content.IsEmpty() ? content : SecurityElement.Escape(content);

		public static string ClearUrl(this string url)
		{
			if (url.IsEmpty())
				return url;

			var chars = new List<char>(url);

			var count = chars.Count;

			for (var i = 0; i < count; i++)
			{
				if (!IsUrlSafeChar(chars[i]))
				{
					chars.RemoveAt(i);
					count--;
					i--;
				}
			}

			return new string(chars.ToArray());
		}

		public static bool IsUrlSafeChar(this char ch)
		{
			if (((ch < 'a') || (ch > 'z')) && ((ch < 'A') || (ch > 'Z')) && ((ch < '0') || (ch > '9')))
			{
				switch (ch)
				{
					case '(':
					case ')':
					//case '*':
					case '-':
					//case '.':
					case '!':
						break;

					case '+':
					case ',':
					case '.':
					case '%':
					case '*':
						return false;

					default:
						if (ch != '_')
							return false;

						break;
				}
			}

			return true;
		}

		private static readonly SynchronizedSet<string> _imgExts = new()
		{
			".png", ".jpg", ".jpeg", ".bmp", ".gif", ".svg"
		};

		public static bool IsImage(this string fileName)
		{
			var ext = Path.GetExtension(fileName);

			if (ext.IsEmpty())
				return false;

			return _imgExts.Contains(ext.ToLowerInvariant());
		}

		private static readonly string[] _urlParts = { "href=", "http:", "https:", "ftp:" };

		public static bool CheckContainsUrl(this string url)
		{
			return !url.IsEmpty() && _urlParts.Any(url.ContainsIgnoreCase);
		}

		public static bool IsLocalhost(this Uri url)
		{
			if (url is null)
				throw new ArgumentNullException(nameof(url));

			return url.Host.EqualsIgnoreCase("localhost");
		}

		public static string CheckUrl(this string str) => str.ToLatin().LightScreening().ClearUrl();

		public static bool IsLoopback(this IPAddress address) => IPAddress.IsLoopback(address);

		public static string GetGravatarUrl(this string email, int size)
		{
			using var md5Hasher = MD5.Create();

			var hash = md5Hasher.ComputeHash(Encoding.Default.GetBytes(email)).Digest().ToLowerInvariant();

			return $"https://www.gravatar.com/avatar/{hash}?size={size}";
		}

		public static bool Unauthorized(this HttpRequestException ex)
			=> ex.Is(HttpStatusCode.Unauthorized, "Unauthorized");

		public static bool NotFound(this HttpRequestException ex)
			=> ex.Is(HttpStatusCode.NotFound, "not found");

		private static bool Is(this HttpRequestException ex, HttpStatusCode code, string msg)
			=> ex.CheckOnNull(nameof(ex)).Message.Contains(((int)code).To<string>()) || ex.Message.ContainsIgnoreCase(msg.CheckOnNull(nameof(msg)));
	}
}