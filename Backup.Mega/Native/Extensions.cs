namespace Ecng.Backup.Mega.Native
{
	using Ecng.Backup.Mega.Native.Cryptography;
	using System;
	using System.Linq;
  using System.Text;
	using System.Text.RegularExpressions;

  internal static class Extensions
  {
    private static readonly DateTime s_epochStart = new DateTime(1970, 1, 1, 0, 0, 0, 0);

    public static string ToBase64(this byte[] data)
    {
      var sb = new StringBuilder();
      sb.Append(Convert.ToBase64String(data));
      sb.Replace('+', '-');
      sb.Replace('/', '_');
      sb.Replace("=", string.Empty);

      return sb.ToString();
    }

    public static byte[] FromBase64(this string data)
    {
      var sb = new StringBuilder();
      sb.Append(data);
      sb.Append(string.Empty.PadRight((4 - data.Length % 4) % 4, '='));
      sb.Replace('-', '+');
      sb.Replace('_', '/');
      sb.Replace(",", string.Empty);

      return Convert.FromBase64String(sb.ToString());
    }

    public static string ToUTF8String(this byte[] data)
    {
      return Encoding.UTF8.GetString(data);
    }

    public static byte[] ToBytes(this string data)
    {
      return Encoding.UTF8.GetBytes(data);
    }

    public static byte[] ToBytesPassword(this string data)
    {
      // Store bytes characters in uint array
      // discards bits 8-31 of multibyte characters for backwards compatibility
      var array = new uint[(data.Length + 3) >> 2];
      for (var i = 0; i < data.Length; i++)
      {
        array[i >> 2] |= (uint)(data[i] << (24 - (i & 3) * 8));
      }

      return [.. array.SelectMany(x =>
      {
        var bytes = BitConverter.GetBytes(x);
        if (BitConverter.IsLittleEndian)
        {
          Array.Reverse(bytes);
        }

        return bytes;
      })];
    }

    public static T[] CopySubArray<T>(this T[] source, int length, int offset = 0)
    {
      var result = new T[length];
      while (--length >= 0)
      {
        if (source.Length > offset + length)
        {
          result[length] = source[offset + length];
        }
      }

      return result;
    }

    public static BigInteger FromMPINumber(this byte[] data)
    {
      // First 2 bytes defines the size of the component
      var dataLength = (data[0] * 256 + data[1] + 7) / 8;

      var result = new byte[dataLength];
      Array.Copy(data, 2, result, 0, result.Length);

      return new BigInteger(result);
    }

    public static DateTime ToDateTime(this long seconds)
    {
      return s_epochStart.AddSeconds(seconds).ToLocalTime();
    }

    public static long ToEpoch(this DateTime datetime)
    {
      return (long)datetime.ToUniversalTime().Subtract(s_epochStart).TotalSeconds;
    }

    public static long DeserializeToLong(this byte[] data, int index, int length)
    {
      var p = data[index];

      long result = 0;

      if ((p > sizeof(ulong)) || (p >= length))
      {
        throw new ArgumentException("Invalid value");
      }

      while (p > 0)
      {
        result = (result << 8) + data[index + p--];
      }

      return result;
    }

    public static byte[] SerializeToBytes(this long data)
    {
      var result = new byte[sizeof(long) + 1];

      byte p = 0;
      while (data != 0)
      {
        result[++p] = (byte)data;
        data >>= 8;
      }

      result[0] = p;
      Array.Resize(ref result, result[0] + 1);

      return result;
    }

		public static void GetPartsFromUri(this Uri uri, out string id, out byte[] iv, out byte[] metaMac, out byte[] key)
		{
			if (!TryGetPartsFromUri(uri, out id, out var decryptedKey, out var isFolder)
				&& !TryGetPartsFromLegacyUri(uri, out id, out decryptedKey, out isFolder))
			{
				throw new ArgumentException(string.Format("Invalid uri. Unable to extract Id and Key from the uri {0}", uri));
			}

			if (isFolder)
			{
				iv = null;
				metaMac = null;
				key = decryptedKey;
			}
			else
			{
				Crypto.GetPartsFromDecryptedKey(decryptedKey, out iv, out metaMac, out key);
			}
		}

		public static bool TryGetPartsFromUri(Uri uri, out string id, out byte[] decryptedKey, out bool isFolder)
		{
			var uriRegex = new Regex(@"/(?<type>(file|folder))/(?<id>[^#]+)#(?<key>[^$/]+)");
			var match = uriRegex.Match(uri.PathAndQuery + uri.Fragment);
			if (match.Success)
			{
				id = match.Groups["id"].Value;
				decryptedKey = match.Groups["key"].Value.FromBase64();
				isFolder = match.Groups["type"].Value == "folder";
				return true;
			}
			else
			{
				id = null;
				decryptedKey = null;
				isFolder = default;
				return false;
			}
		}

		public static bool TryGetPartsFromLegacyUri(Uri uri, out string id, out byte[] decryptedKey, out bool isFolder)
		{
			var uriRegex = new Regex(@"#(?<type>F?)!(?<id>[^!]+)!(?<key>[^$!\?]+)");
			var match = uriRegex.Match(uri.Fragment);
			if (match.Success)
			{
				id = match.Groups["id"].Value;
				decryptedKey = match.Groups["key"].Value.FromBase64();
				isFolder = match.Groups["type"].Value == "F";
				return true;
			}
			else
			{
				id = null;
				decryptedKey = null;
				isFolder = default;
				return false;
			}
		}
	}
}
