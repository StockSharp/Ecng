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

using Ecng.Localization;

/// <summary>
/// Provides various network helper extension methods.
/// </summary>
public static class NetworkHelper
{
	/// <summary>
	/// Gets the Maximum Transmission Unit size.
	/// </summary>
	public const int MtuSize = 1600;

	/// <summary>
	/// Determines whether the specified endpoint is local.
	/// </summary>
	/// <param name="endPoint">The endpoint to check.</param>
	/// <returns><c>true</c> if the endpoint is local; otherwise, <c>false</c>.</returns>
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

	/// <summary>
	/// Checks whether the specified endpoint's IP addresses include a local address.
	/// </summary>
	/// <param name="endPoint">The endpoint to check.</param>
	/// <returns><c>true</c> if a local IP address is found; otherwise, <c>false</c>.</returns>
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

	/// <summary>
	/// Determines whether the specified socket is connected.
	/// </summary>
	/// <param name="socket">The socket to check.</param>
	/// <param name="timeOut">The timeout in microseconds.</param>
	/// <returns><c>true</c> if the socket is connected; otherwise, <c>false</c>.</returns>
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

	/// <summary>
	/// Reads the specified number of bytes from the socket into the provided buffer.
	/// </summary>
	/// <param name="socket">The source socket.</param>
	/// <param name="buffer">The buffer to store the data.</param>
	/// <param name="offset">The offset in the buffer.</param>
	/// <param name="len">The number of bytes to read.</param>
	public static void Read(this Socket socket, byte[] buffer, int offset, int len)
	{
		if (socket is null)
			throw new ArgumentNullException(nameof(socket));

		var left = len;

		while (left > 0)
		{
			var read = socket.Receive(buffer, offset + (len - left), left, SocketFlags.None);

			if (read <= 0)
				throw new IOException($"Stream returned '{read}' bytes.");

			left -= read;
		}
	}

	/// <summary>
	/// Reads the specified number of bytes from the stream into the provided buffer.
	/// </summary>
	/// <param name="stream">The source stream.</param>
	/// <param name="buffer">The buffer to store the data.</param>
	/// <param name="offset">The offset in the buffer.</param>
	/// <param name="bytesToRead">The number of bytes to read.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask{T}"/></returns>
	public static async ValueTask<int> ReadFullAsync(this Stream stream, byte[] buffer, int offset, int bytesToRead, CancellationToken cancellationToken)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		var totalBytesRead = 0;

		while (totalBytesRead < bytesToRead)
		{
			var bytesRead = await stream.ReadAsync(
#if NET5_0_OR_GREATER
				buffer.AsMemory(offset + totalBytesRead, bytesToRead - totalBytesRead)
#else
				buffer, offset + totalBytesRead, bytesToRead - totalBytesRead
#endif
				, cancellationToken
			).ConfigureAwait(false);

			if (bytesRead == 0)
				break;

			totalBytesRead += bytesRead;
		}

		if (totalBytesRead < bytesToRead)
			throw new IOException("Connection dropped.".Localize());

		return totalBytesRead;
	}

	/// <summary>
	/// Waits for data availability on the socket.
	/// </summary>
	/// <param name="socket">The socket to wait on.</param>
	/// <param name="timeOut">The timeout in microseconds.</param>
	/// <returns><c>true</c> if data is available; otherwise, <c>false</c>.</returns>
	public static bool Wait(this Socket socket, int timeOut)
	{
		if (socket is null)
			throw new ArgumentNullException(nameof(socket));

		return socket.Poll(timeOut, SelectMode.SelectRead) && socket.Available != 0;
	}

	/// <summary>
	/// Joins the specified multicast group using the provided IP address.
	/// </summary>
	/// <param name="socket">The socket to configure.</param>
	/// <param name="address">The multicast group IP address.</param>
	public static void JoinMulticast(this Socket socket, IPAddress address)
	{
		if (address is null)
			throw new ArgumentNullException(nameof(address));

		socket.JoinMulticast(new MulticastSourceAddress { GroupAddress = address });
	}

	/// <summary>
	/// Joins the specified source-specific multicast group.
	/// </summary>
	/// <param name="socket">The socket to configure.</param>
	/// <param name="address">The multicast source address configuration.</param>
	public static void JoinMulticast(this Socket socket, MulticastSourceAddress address)
	{
		if (socket is null)
			throw new ArgumentNullException(nameof(socket));

		if (address.SourceAddress is null)
			socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(address.GroupAddress));
		else
			socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddSourceMembership, GetBytes(address));
	}

	/// <summary>
	/// Leaves the specified multicast group using the provided IP address.
	/// </summary>
	/// <param name="socket">The socket to configure.</param>
	/// <param name="address">The multicast group IP address.</param>
	public static void LeaveMulticast(this Socket socket, IPAddress address)
	{
		if (address is null)
			throw new ArgumentNullException(nameof(address));

		socket.LeaveMulticast(new MulticastSourceAddress { GroupAddress = address });
	}

	/// <summary>
	/// Leaves the specified source-specific multicast group.
	/// </summary>
	/// <param name="socket">The socket to configure.</param>
	/// <param name="address">The multicast source address configuration.</param>
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

	/// <summary>
	/// Converts the provided stream into an SSL stream with the specified options.
	/// </summary>
	/// <param name="stream">The underlying stream.</param>
	/// <param name="sslProtocol">The SSL protocol to use.</param>
	/// <param name="checkCertificateRevocation">Whether to check certificate revocation.</param>
	/// <param name="validateRemoteCertificates">Whether to validate remote certificates.</param>
	/// <param name="targetHost">The target host name.</param>
	/// <param name="sslCertificate">The certificate file path.</param>
	/// <param name="sslCertificatePassword">The certificate password.</param>
	/// <param name="certificateValidationCallback">Optional certificate validation callback.</param>
	/// <param name="certificateSelectionCallback">Optional certificate selection callback.</param>
	/// <returns>An authenticated SslStream.</returns>
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

	/// <summary>
	/// Connects the TcpClient to the specified endpoint.
	/// </summary>
	/// <param name="client">The TcpClient instance.</param>
	/// <param name="address">The endpoint to connect to.</param>
	public static void Connect(this TcpClient client, EndPoint address)
	{
		if (client is null)
			throw new ArgumentNullException(nameof(client));

		if (address is null)
			throw new ArgumentNullException(nameof(address));

		client.Connect(address.GetHost(), address.GetPort());
	}

	/// <summary>
	/// Encodes the specified text into its HTML-encoded representation.
	/// </summary>
	/// <param name="text">The text to encode.</param>
	/// <returns>The HTML-encoded string.</returns>
	public static string EncodeToHtml(this string text)
	{
		return HttpUtility.HtmlEncode(text);
	}

	/// <summary>
	/// Decodes the specified HTML-encoded text.
	/// </summary>
	/// <param name="text">The text to decode.</param>
	/// <returns>The decoded string.</returns>
	public static string DecodeFromHtml(this string text)
	{
		return HttpUtility.HtmlDecode(text);
	}

	private static readonly Encoding _urlEncoding = Encoding.UTF8;

	/// <summary>
	/// Encodes a URL using UTF8 encoding.
	/// </summary>
	/// <param name="url">The URL to encode.</param>
	/// <returns>The encoded URL string.</returns>
	public static string EncodeUrl(this string url)
	{
		return HttpUtility.UrlEncode(url, _urlEncoding);
	}

	/// <summary>
	/// Encodes a URL ensuring uppercase encoding.
	/// </summary>
	/// <param name="url">The URL to encode.</param>
	/// <returns>The URL encoded in uppercase.</returns>
	public static string EncodeUrlUpper(this string url)
	{
		return WebUtility.UrlEncode(url);
	}

	/// <summary>
	/// Decodes the specified URL.
	/// </summary>
	/// <param name="url">The URL to decode.</param>
	/// <returns>The decoded URL string.</returns>
	public static string DecodeUrl(this string url)
	{
		return HttpUtility.UrlDecode(url, _urlEncoding);
	}

	/// <summary>
	/// Parses the query string from the URL.
	/// </summary>
	/// <param name="url">The URL query string.</param>
	/// <returns>A collection of query string parameters.</returns>
	public static NameValueCollection ParseUrl(this string url)
	{
		return HttpUtility.ParseQueryString(url, _urlEncoding);
	}

	/// <summary>
	/// Enumerates key-value pairs from the collection, excluding empty keys.
	/// </summary>
	/// <param name="col">The name-value collection.</param>
	/// <returns>An enumerable of non-empty key-value pairs.</returns>
	public static IEnumerable<(string key, string value)> ExcludeEmpty(this NameValueCollection col)
	{
		if (col is null)
			throw new ArgumentNullException(nameof(col));

		foreach (var key in col.AllKeys.Where(k => !k.IsEmpty()))
			yield return (key, col[key]);
	}

	/// <summary>
	/// Attempts to extract an Encoding from the Content-Type header.
	/// </summary>
	/// <param name="contentType">The Content-Type header value.</param>
	/// <returns>An Encoding if found; otherwise, <c>null</c>.</returns>
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

	/// <summary>
	/// URL encodes the string ensuring that hexadecimal values are in uppercase.
	/// </summary>
	/// <param name="url">The URL to encode.</param>
	/// <returns>An uppercase URL encoded string.</returns>
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

	/// <summary>
	/// Escapes special XML characters in the content.
	/// </summary>
	/// <param name="content">The content to escape.</param>
	/// <returns>The escaped XML string.</returns>
	public static string XmlEscape(this string content)
		=> content.IsEmpty() ? content : SecurityElement.Escape(content);

	/// <summary>
	/// Removes unsafe characters from the URL.
	/// </summary>
	/// <param name="url">The URL to clear.</param>
	/// <returns>The cleared URL string.</returns>
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

	/// <summary>
	/// Determines whether the specified character is safe for URLs.
	/// </summary>
	/// <param name="ch">The character to check.</param>
	/// <returns><c>true</c> if the character is URL safe; otherwise, <c>false</c>.</returns>
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

	/// <summary>
	/// Determines whether the specified file name represents an image.
	/// </summary>
	/// <param name="fileName">The file name to check.</param>
	/// <returns><c>true</c> if the file is an image; otherwise, <c>false</c>.</returns>
	public static bool IsImage(this string fileName)
		=> fileName.IsImage(_imgExts);

	private static readonly SynchronizedSet<string> _imgVectorExts = new(StringComparer.InvariantCultureIgnoreCase)
	{
		".svg"
	};

	/// <summary>
	/// Determines whether the specified file name represents a vector image.
	/// </summary>
	/// <param name="fileName">The file name to check.</param>
	/// <returns><c>true</c> if the file is a vector image; otherwise, <c>false</c>.</returns>
	public static bool IsImageVector(this string fileName)
		=> fileName.IsImage(_imgVectorExts);

	private static readonly string[] _urlParts = ["href=", "http:", "https:", "ftp:"];


	/// <summary>
	/// Checks if the URL contains any of the predefined URL parts.
	/// </summary>
	/// <param name="url">The URL to check.</param>
	/// <returns><c>true</c> if the URL contains one of the parts; otherwise, <c>false</c>.</returns>
	public static bool CheckContainsUrl(this string url)
	{
		return !url.IsEmpty() && _urlParts.Any(url.ContainsIgnoreCase);
	}

	/// <summary>
	/// Determines whether the specified Uri represents localhost.
	/// </summary>
	/// <param name="url">The Uri to check.</param>
	/// <returns><c>true</c> if the Uri is localhost; otherwise, <c>false</c>.</returns>
	public static bool IsLocalhost(this Uri url)
	{
		if (url is null)
			throw new ArgumentNullException(nameof(url));

		return url.Host.StartsWithIgnoreCase("localhost");
	}

	/// <summary>
	/// Checks and cleans a URL by converting to Latin characters, screening, and clearing unsafe characters.
	/// </summary>
	/// <param name="str">The URL string to check.</param>
	/// <param name="latin">if set to <c>true</c> converts characters to Latin.</param>
	/// <param name="screen">if set to <c>true</c> performs light screening.</param>
	/// <param name="clear">if set to <c>true</c> clears unsafe URL characters.</param>
	/// <returns>The processed URL.</returns>
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

	/// <summary>
	/// Determines if the specified IPAddress represents a loopback address.
	/// </summary>
	/// <param name="address">The IPAddress to check.</param>
	/// <returns><c>true</c> if the IPAddress is loopback; otherwise, <c>false</c>.</returns>
	public static bool IsLoopback(this IPAddress address) => IPAddress.IsLoopback(address);

	/// <summary>
	/// Computes the Gravatar token for the specified email.
	/// </summary>
	/// <param name="email">The email address.</param>
	/// <returns>The computed Gravatar token.</returns>
	public static string GetGravatarToken(this string email)
	{
		if (email.IsEmpty())
			throw new ArgumentNullException(nameof(email));

		using var md5Hasher = MD5.Create();

		return md5Hasher.ComputeHash(email.Default()).Digest().ToLowerInvariant();
	}

	/// <summary>
	/// Constructs the Gravatar URL using the provided token and size.
	/// </summary>
	/// <param name="token">The Gravatar token.</param>
	/// <param name="size">The size of the Gravatar image.</param>
	/// <returns>The full Gravatar image URL.</returns>
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

	/// <summary>
	/// Sets a custom phrase for a specific HTTP status code.
	/// </summary>
	/// <param name="code">The HTTP status code.</param>
	/// <param name="str">The phrase to associate with the status code.</param>
	public static void SetStatusCodePhrase(HttpStatusCode code, string str)
		=> _phrases[code] = str;

	/// <summary>
	/// Attempts to retrieve an HTTP status code from the specified HttpRequestException.
	/// </summary>
	/// <param name="ex">The HttpRequestException to analyze.</param>
	/// <returns>The associated HttpStatusCode if found; otherwise, <c>null</c>.</returns>
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

	/// <summary>
	/// Creates an HttpRequestException for the specified HTTP status code and message.
	/// </summary>
	/// <param name="code">The HTTP status code.</param>
	/// <param name="message">The error message.</param>
	/// <returns>An instance of HttpRequestException.</returns>
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

	/// <summary>
	/// Formats the value as a string with optional URL encoding.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="value">The value to format.</param>
	/// <param name="encode">Whether to URL encode the result.</param>
	/// <returns>The formatted string.</returns>
	public static string Format<T>(this T value, bool encode)
	{
		var str = value?.ToString();

		if (encode && !str.IsEmpty())
			str = str.EncodeUrl();

		return str;
	}

	/// <summary>
	/// Converts a sequence of key-value pairs into a query string.
	/// </summary>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	/// <param name="args">The key-value pairs.</param>
	/// <param name="encodeValue">Whether to encode the value.</param>
	/// <returns>A query string representation of the key-value pairs.</returns>
	public static string ToQueryString<TValue>(this IEnumerable<KeyValuePair<string, TValue>> args, bool encodeValue = false)
		=> args.Select(p => $"{p.Key}={p.Value.Format(encodeValue)}").JoinAnd();

	/// <summary>
	/// Converts a sequence of tuple key-value pairs into a query string.
	/// </summary>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	/// <param name="args">The tuple key-value pairs.</param>
	/// <param name="encodeValue">Whether to encode the value.</param>
	/// <returns>A query string representation of the tuples.</returns>
	public static string ToQueryString<TValue>(this IEnumerable<(string key, TValue value)> args, bool encodeValue = false)
		=> args.Select(p => $"{p.key}={p.value.Format(encodeValue)}").JoinAnd();

	// https://stackoverflow.com/a/56461160

	/// <summary>
	/// Determines whether the specified IPv4 or IPv6 address is part of the given subnet.
	/// </summary>
	/// <param name="address">The IP address to check.</param>
	/// <param name="subnetMask">The subnet mask in "IP/PrefixLength" format.</param>
	/// <returns><c>true</c> if the address is in the subnet; otherwise, <c>false</c>.</returns>
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

	/// <summary>
	/// Applies a Chrome user agent to the HttpClient.
	/// </summary>
	/// <param name="client">The HttpClient instance.</param>
	public static void ApplyChromeAgent(this HttpClient client)
		=> client.CheckOnNull(nameof(client)).DefaultRequestHeaders.Add(HttpHeaders.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36");

	/// <summary>
	/// Sets a bearer token for authorization on the HttpClient.
	/// </summary>
	/// <param name="client">The HttpClient instance.</param>
	/// <param name="token">The secure bearer token.</param>
	public static void SetBearer(this HttpClient client, SecureString token)
		=> client.CheckOnNull(nameof(client)).DefaultRequestHeaders.Add(HttpHeaders.Authorization, AuthSchemas.Bearer.FormatAuth(token));

	/// <summary>
	/// Attempts to retrieve the underlying socket error from an exception.
	/// </summary>
	/// <param name="ex">The exception to inspect.</param>
	/// <returns>The SocketError if found; otherwise, <c>null</c>.</returns>
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

	/// <summary>
	/// Calculates the delay for a retry based on the current attempt number.
	/// </summary>
	/// <param name="policy">The retry policy information.</param>
	/// <param name="attemptNumber">The current attempt number.</param>
	/// <returns>A TimeSpan representing the delay.</returns>
	public static TimeSpan GetDelay(this RetryPolicyInfo policy, int attemptNumber)
	{
		if (policy is null)
			throw new ArgumentNullException(nameof(policy));

		var delay = (policy.InitialDelay.Ticks * 2.Pow(attemptNumber - 1)).To<TimeSpan>();
		var jitter = RandomGen.GetDouble() * delay.TotalMilliseconds * 0.1;

		return TimeSpan.FromMilliseconds(delay.TotalMilliseconds + jitter).Max(policy.MaxDelay);
	}

	/// <summary>
	/// Attempts to repeatedly execute a function based on the retry policy.
	/// </summary>
	/// <typeparam name="T">The type of the result.</typeparam>
	/// <param name="policy">The retry policy information.</param>
	/// <param name="handler">The asynchronous function to execute.</param>
	/// <param name="maxCount">The maximum number of attempts.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The result of the function if successful.</returns>
	public static async Task<T> TryRepeat<T>(this RetryPolicyInfo policy, Func<CancellationToken, Task<T>> handler, int maxCount, CancellationToken cancellationToken)
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
				return await handler(cancellationToken);
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