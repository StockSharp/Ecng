namespace Ecng.Net;

using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Reflection;

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
			ssl.AuthenticateAsClient(targetHost, [cert], sslProtocol, checkCertificateRevocation);
		}

		return ssl;
	}

	public static void Connect(this TcpClient client, EndPoint address)
	{
		if (client is null)
			throw new ArgumentNullException(nameof(client));

		if (address is null)
			throw new ArgumentNullException(nameof(address));

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

	public static string EncodeUrlUpper(this string url)
	{
		return WebUtility.UrlEncode(url);
	}

	public static string DecodeUrl(this string url)
	{
		return HttpUtility.UrlDecode(url, _urlEncoding);
	}

	public static NameValueCollection ParseUrl(this string url)
	{
		return HttpUtility.ParseQueryString(url, _urlEncoding);
	}

	public static IEnumerable<(string key, string value)> ExcludeEmpty(this NameValueCollection col)
	{
		if (col is null)
			throw new ArgumentNullException(nameof(col));

		foreach (var key in col.AllKeys.Where(k => !k.IsEmpty()))
			yield return (key, col[key]);
	}

	public static Encoding TryExtractEncoding(this string contentType)
	{
		try
		{
			if (contentType.IsEmpty())
				return null;

			const string charsetMarker = "charset=";

			var charsetIndex = contentType.IndexOfIgnoreCase(charsetMarker);

			if (charsetIndex < 0)
				return null;

			var charset = contentType.Substring(charsetIndex + charsetMarker.Length).Trim();

			var separatorIndex = charset.IndexOf(';');

			if (separatorIndex >= 0)
				charset = charset.Substring(0, separatorIndex);

			charset = charset.Trim(' ', '"');

			return Encoding.GetEncoding(charset);
		}
		catch
		{
			return null;
		}
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

		return new string([.. chars]);
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

	private static bool IsImage(this string fileName, SynchronizedSet<string> extensions)
	{
		var ext = Path.GetExtension(fileName);

		if (ext.IsEmpty())
			return false;

		return extensions.Contains(ext);
	}

	private static readonly SynchronizedSet<string> _imgExts = new(StringComparer.InvariantCultureIgnoreCase)
	{
		".png", ".jpg", ".jpeg", ".bmp", ".gif", ".svg", ".webp", ".ico", ".tiff", ".avif", ".apng"
	};

	public static bool IsImage(this string fileName)
		=> fileName.IsImage(_imgExts);

	private static readonly SynchronizedSet<string> _imgVectorExts = new(StringComparer.InvariantCultureIgnoreCase)
	{
		".svg"
	};

	public static bool IsImageVector(this string fileName)
		=> fileName.IsImage(_imgVectorExts);

	private static readonly string[] _urlParts = ["href=", "http:", "https:", "ftp:"];

	public static bool CheckContainsUrl(this string url)
	{
		return !url.IsEmpty() && _urlParts.Any(url.ContainsIgnoreCase);
	}

	public static bool IsLocalhost(this Uri url)
	{
		if (url is null)
			throw new ArgumentNullException(nameof(url));

		return url.Host.StartsWithIgnoreCase("localhost");
	}

	public static string CheckUrl(this string str, bool latin = true, bool screen = true, bool clear = true)
	{
		if (latin)
			str = str.ToLatin();

		if (screen)
			str = str.LightScreening();

		if (clear)
			str = str.ClearUrl();

		return str;
	}

	public static bool IsLoopback(this IPAddress address) => IPAddress.IsLoopback(address);

	public static string GetGravatarToken(this string email)
	{
		if (email.IsEmpty())
			throw new ArgumentNullException(nameof(email));

		using var md5Hasher = MD5.Create();

		return md5Hasher.ComputeHash(email.Default()).Digest().ToLowerInvariant();
	}

	public static string GetGravatarUrl(this string token, int size)
	{
		if (token.IsEmpty())
			throw new ArgumentNullException(nameof(token));

		return $"https://www.gravatar.com/avatar/{token}?size={size}";
	}

	private static readonly CachedSynchronizedDictionary<HttpStatusCode, string> _phrases = new()
	{
		{ HttpStatusCode.Unauthorized, nameof(HttpStatusCode.Unauthorized) },
		{ HttpStatusCode.Forbidden, nameof(HttpStatusCode.Forbidden) },
		{ HttpStatusCode.NotFound, "not found" },
		{ HttpStatusCode.Conflict, nameof(HttpStatusCode.Conflict) },
		{ HttpStatusCode.Gone, nameof(HttpStatusCode.Gone) },
	};

	public static void SetStatusCodePhrase(HttpStatusCode code, string str)
		=> _phrases[code] = str;

	public static HttpStatusCode? TryGetStatusCode(this HttpRequestException ex)
	{
		var msg = ex.CheckOnNull(nameof(ex)).Message;

		foreach (var pair in _phrases.CachedPairs)
		{
			if (msg.Contains(((int)pair.Key).To<string>()) || msg.ContainsIgnoreCase(pair.Value))
				return pair.Key;
		}

		return null;
	}

	// TODO can remove when .net standard 2.1 will be applied
	private static ConstructorInfo _ctorWithStatusCode;
	private static bool _initialized;

	public static HttpRequestException CreateHttpRequestException(this HttpStatusCode code, string message)
	{
		if (!_initialized)
		{
			_ctorWithStatusCode = typeof(HttpRequestException).GetConstructors().FirstOrDefault(c => c.GetParameters().Any(p => p.ParameterType == typeof(HttpStatusCode?)));
			_initialized = true;
		}

		if (_ctorWithStatusCode is null)
			return new HttpRequestException($"{(int)code} ({code}): {message}");
		else
			return (HttpRequestException)_ctorWithStatusCode.Invoke([message, null, code]);
	}

	public static string Format<T>(this T value, bool encode)
	{
		var str = value?.ToString();

		if (encode && !str.IsEmpty())
			str = str.EncodeUrl();

		return str;
	}

	public static string ToQueryString<TValue>(this IEnumerable<KeyValuePair<string, TValue>> args, bool encodeValue = false)
		=> args.Select(p => $"{p.Key}={p.Value.Format(encodeValue)}").JoinAnd();

	public static string ToQueryString<TValue>(this IEnumerable<(string key, TValue value)> args, bool encodeValue = false)
		=> args.Select(p => $"{p.key}={p.value.Format(encodeValue)}").JoinAnd();

	// https://stackoverflow.com/a/56461160
	public static bool IsInSubnet(this IPAddress address, string subnetMask)
	{
		var slashIdx = subnetMask.IndexOf("/");
		if (slashIdx == -1)
		{
			// We only handle netmasks in format "IP/PrefixLength".
			throw new NotSupportedException("Only SubNetMasks with a given prefix length are supported.");
		}

		// First parse the address of the netmask before the prefix length.
		var maskAddress = subnetMask.Substring(0, slashIdx).To<IPAddress>();

		if (maskAddress.AddressFamily != address.AddressFamily)
		{
			// We got something like an IPV4-Address for an IPv6-Mask. This is not valid.
			return false;
		}

		// Now find out how long the prefix is.
		var maskLength = subnetMask.Substring(slashIdx + 1).To<int>();

		if (maskLength == 0)
		{
			return true;
		}

		if (maskLength < 0)
		{
			throw new NotSupportedException("A Subnetmask should not be less than 0.");
		}

		var maskBytes = maskAddress.GetAddressBytes().Reverse().ToArray();
		var addrBytes = address.GetAddressBytes().Reverse().ToArray();

		if (maskAddress.AddressFamily == AddressFamily.InterNetwork)
		{
			// Convert the mask address to an unsigned integer.
			var maskAddressBits = maskBytes.To<uint>();

			// And convert the IpAddress to an unsigned integer.
			var ipAddressBits = addrBytes.To<uint>();

			// Get the mask/network address as unsigned integer.
			uint mask = uint.MaxValue << (32 - maskLength);

			// https://stackoverflow.com/a/1499284/3085985
			// Bitwise AND mask and MaskAddress, this should be the same as mask and IpAddress
			// as the end of the mask is 0000 which leads to both addresses to end with 0000
			// and to start with the prefix.
			return (maskAddressBits & mask) == (ipAddressBits & mask);
		}
		else if (maskAddress.AddressFamily == AddressFamily.InterNetworkV6)
		{
			// Convert the mask address to a BitArray. Reverse the BitArray to compare the bits of each byte in the right order.
			var maskAddressBits = new BitArray(maskBytes);

			// And convert the IpAddress to a BitArray. Reverse the BitArray to compare the bits of each byte in the right order.
			var ipAddressBits = new BitArray(addrBytes);
			var ipAddressLength = ipAddressBits.Length;

			if (maskAddressBits.Length != ipAddressBits.Length)
			{
				throw new ArgumentException("Length of IP Address and Subnet Mask do not match.");
			}

			// Compare the prefix bits.
			for (var i = ipAddressLength - 1; i >= ipAddressLength - maskLength; i--)
			{
				if (ipAddressBits[i] != maskAddressBits[i])
				{
					return false;
				}
			}

			return true;
		}
		else
			throw new NotSupportedException(maskAddress.AddressFamily.To<string>());
	}

	public static void ApplyChromeAgent(this HttpClient client)
		=> client.CheckOnNull(nameof(client)).DefaultRequestHeaders.Add(HttpHeaders.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36");

	public static void SetBearer(this HttpClient client, SecureString token)
		=> client.CheckOnNull(nameof(client)).DefaultRequestHeaders.Add(HttpHeaders.Authorization, AuthSchemas.Bearer.FormatAuth(token));

	public static SocketError? TryGetSocketError(this Exception ex)
	{
		while (ex != null)
		{
			if (ex is SocketException sockEx)
				return sockEx.SocketErrorCode;

			ex = ex.InnerException;
		}

		return null;
	}

	public static TimeSpan GetDelay(this RetryPolicyInfo policy, int attemptNumber)
	{
		if (policy is null)
			throw new ArgumentNullException(nameof(policy));

		var delay = (policy.InitialDelay.Ticks * 2.Pow(attemptNumber - 1)).To<TimeSpan>();
		var jitter = RandomGen.GetDouble() * delay.TotalMilliseconds * 0.1;

		return TimeSpan.FromMilliseconds(delay.TotalMilliseconds + jitter).Max(policy.MaxDelay);
	}

	public static async Task<T> TryRepeat<T>(this RetryPolicyInfo policy, Func<Task<T>> handler, int maxCount, CancellationToken cancellationToken)
	{
		if (policy is null)
			throw new ArgumentNullException(nameof(policy));

		if (handler is null)
			throw new ArgumentNullException(nameof(handler));

		var attemptNumber = 0;

		while (true)
		{
			attemptNumber++;

			try
			{
				return await handler();
			}
			catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
			{
				bool shouldRetry()
				{
					if (attemptNumber >= maxCount)
						return false;

					if (ex.TryGetSocketError() is not SocketError error)
						return false;

					return policy.Track.Contains(error);
				}

				if (!shouldRetry())
					throw;

				await policy.GetDelay(attemptNumber).Delay(cancellationToken);
			}
		}
	}
}