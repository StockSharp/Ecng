namespace Ecng.Common;

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

using Ecng.Common.TimeZoneConverter;

/// <summary>
/// The converter class.
/// </summary>
public static class Converter
{
	private static readonly Dictionary<Type, DbType> _dbTypes = [];
	private static readonly Dictionary<string, Type> _sharpAliases = [];
	private static readonly Dictionary<Type, string> _sharpAliasesByValue = [];
	private static readonly Dictionary<string, Type> _typeCache = [];

	private static readonly Dictionary<(Type, Type), Delegate> _typedConverters = [];
	private static readonly Dictionary<(Type, Type), Func<object, object>> _typedConverters2 = [];

	static Converter()
	{
		_dbTypes.Add(typeof(string), DbType.String);
		_dbTypes.Add(typeof(char), DbType.String);
		_dbTypes.Add(typeof(short), DbType.Int16);
		_dbTypes.Add(typeof(int), DbType.Int32);
		_dbTypes.Add(typeof(long), DbType.Int64);
		_dbTypes.Add(typeof(ushort), DbType.UInt16);
		_dbTypes.Add(typeof(uint), DbType.UInt32);
		_dbTypes.Add(typeof(ulong), DbType.UInt64);
		_dbTypes.Add(typeof(float), DbType.Single);
		_dbTypes.Add(typeof(double), DbType.Double);
		_dbTypes.Add(typeof(decimal), DbType.Decimal);
		_dbTypes.Add(typeof(DateTime), DbType.DateTime);
		_dbTypes.Add(typeof(DateTimeOffset), DbType.DateTimeOffset);
		_dbTypes.Add(typeof(TimeSpan), DbType.Time);
		_dbTypes.Add(typeof(Guid), DbType.Guid);
		_dbTypes.Add(typeof(byte[]), DbType.Binary);
		_dbTypes.Add(typeof(byte), DbType.Byte);
		_dbTypes.Add(typeof(sbyte), DbType.SByte);
		_dbTypes.Add(typeof(bool), DbType.Boolean);
		_dbTypes.Add(typeof(object), DbType.Object);

		AddCSharpAlias(typeof(object), "object");
		AddCSharpAlias(typeof(bool), "bool");
		AddCSharpAlias(typeof(byte), "byte");
		AddCSharpAlias(typeof(sbyte), "sbyte");
		AddCSharpAlias(typeof(char), "char");
		AddCSharpAlias(typeof(decimal), "decimal");
		AddCSharpAlias(typeof(double), "double");
		AddCSharpAlias(typeof(float), "float");
		AddCSharpAlias(typeof(int), "int");
		AddCSharpAlias(typeof(uint), "uint");
		AddCSharpAlias(typeof(long), "long");
		AddCSharpAlias(typeof(ulong), "ulong");
		AddCSharpAlias(typeof(short), "short");
		AddCSharpAlias(typeof(ushort), "ushort");
		AddCSharpAlias(typeof(string), "string");
		AddCSharpAlias(typeof(void), "void");

		AddTypedConverter<Type, DbType>(input =>
		{
			if (input.IsNullable())
				input = input.GetGenericArguments()[0];

			if (input.IsEnum())
				input = input.GetEnumBaseType();


			if (_dbTypes.TryGetValue(input, out var dbType))
				return dbType;
			else
				throw new ArgumentException($".NET type {input} doesn't have associated db type.");
		});

		AddTypedConverter<DbType, Type>(input => _dbTypes.First(pair => pair.Value == input).Key);
		AddTypedConverter<string, byte[]>(input => input.Unicode());
		AddTypedConverter<byte[], string>(input => input.Unicode());
		AddTypedConverter<bool[], BitArray>(input => new BitArray(input));
		AddTypedConverter<BitArray, bool[]>(input =>
		{
			var source = new bool[input.Length];
			input.CopyTo(source, 0);
			return source;
		});
		AddTypedConverter<byte[], BitArray>(input => new BitArray(input));
		AddTypedConverter<BitArray, byte[]>(input =>
		{
			var source = new byte[(int)((double)input.Length / 8).Ceiling()];
			input.CopyTo(source, 0);
			return source;
		});
		AddTypedConverter<IPAddress, string>(input => input.ToString());
		AddTypedConverter<string, IPAddress>(s => s.IsEmpty() ? null : IPAddress.Parse(s));
		AddTypedConverter<IPAddress, byte[]>(input => input.GetAddressBytes());
		AddTypedConverter<byte[], IPAddress>(input => new IPAddress(input));
		AddTypedConverter<IPAddress, long>(input =>
		{
			switch (input.AddressFamily)
			{
				case AddressFamily.InterNetworkV6:
				{
					return input.ScopeId;
				}
				case AddressFamily.InterNetwork:
				{
					var byteIp = input.GetAddressBytes();
					return ((((byteIp[3] << 0x18) | (byteIp[2] << 0x10)) | (byteIp[1] << 8)) | byteIp[0]) & (0xffffffff);
					//retVal = BitConverter.ToInt32(addr.GetAddressBytes(), 0);
				}
				default:
					throw new ArgumentException("Can't convert IPAddress to long.", nameof(input));
			}
		});
		AddTypedConverter<long, IPAddress>(input => new IPAddress(input));
		AddTypedConverter<string, IPEndPoint>(input => (IPEndPoint)input.TypedTo<string, EndPoint>());
		AddTypedConverter<string, DnsEndPoint>(input => (DnsEndPoint)input.TypedTo<string, EndPoint>());
		AddTypedConverter<string, EndPoint>(input =>
		{
			var index = input.LastIndexOf(':');

			if (index != -1)
			{
				var host = input.Substring(0, index);

				var portStr = input.Substring(index + 1);
				if (portStr.Length > 0 && portStr.Last() == '/')
					portStr = portStr.Substring(0, portStr.Length - 1);

				var port = portStr.To<int>();

				if (!IPAddress.TryParse(host, out var addr))
					return new DnsEndPoint(host, port);

				return new IPEndPoint(addr, port);
			}
			else
			{
				if (input.IsEmpty())
					return null;

				throw new FormatException("Invalid endpoint format.");
			}
		});
		AddTypedConverter<EndPoint, string>(input => input.GetHost() + ":" + input.GetPort());
		AddTypedConverter<string, Type>(input =>
		{
			var key = input.ToLowerInvariant();

			if (_sharpAliases.TryGetValue(key, out var type))
				return type;

			if (_typeCache.TryGetValue(key, out type))
				return type;

			lock (_typeCache)
			{
				if (_typeCache.TryGetValue(key, out type))
					return type;

				type = Type.GetType(input, false, true);

				// в строке может быть записаное не AssemblyQualifiedName, а только полное имя типа + имя сборки.
				if (type is null)
				{
					var parts = input.SplitBySep(", ");
					if (parts.Length == 2 || parts.Length == 5)
					{
						var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == parts[1]);
						
						if (asm is null)
						{
							try
							{
								asm = Assembly.Load(parts[1]);
							}
							catch (FileNotFoundException)
							{
							}
						}

						if (asm != null)
						{
							type = asm.GetType(parts[0]);
						}

						if (type is null)
						{
							var asmName = parts[1].Trim();

							if	(
								asmName.EqualsIgnoreCase("System.Private.CoreLib") ||
								asmName.EqualsIgnoreCase("mscorlib")
								)
							{
								asm = typeof(object).Assembly;
								type = asm.GetType(parts[0]);
							}
						}
					}
				}

				if (type != null)
					_typeCache.Add(key, type);
				else
					throw new ArgumentException($"Type {input} doesn't exists.", nameof(input));
			}

			return type;
		});
		AddTypedConverter<Type, string>(input => input.AssemblyQualifiedName);
		AddTypedConverter<StringBuilder, string>(input => input.ToString());
		AddTypedConverter<string, StringBuilder>(input => new StringBuilder(input));
		AddTypedConverter<DbConnectionStringBuilder, string>(input => input.ToString());
		AddTypedConverter<string, DbConnectionStringBuilder>(input => new DbConnectionStringBuilder { ConnectionString = input });
		AddTypedConverter<SecureString, string>(input => input.UnSecure());
		AddTypedConverter<string, SecureString>(input => input.Secure());
		AddTypedConverter<char[], SecureString>(input =>
		{
			unsafe static SecureString Secure(char[] input)
			{
				if (input.Length == 0)
					return new();

				fixed (char* p = input)
					return new(p, input.Length);
			}

			return Secure(input);
		});
		AddTypedConverter<byte[], SecureString>(input =>
		{
			var charArray = new char[input.Length / 2];

			var offset = 0;
			for (var i = 0; i < input.Length; i += 2)
				charArray[offset++] = BitConverter.ToChar([input[i], input[i + 1]], 0);

			return charArray.TypedTo<char[], SecureString>();
		});
		AddTypedConverter<string, char[]>(input => input.ToCharArray());
		AddTypedConverter<char[], string>(input => new string(input));
		AddTypedConverter<byte, byte[]>(input => [input]);
		AddTypedConverter<byte[], byte>(input => input[0]);
		AddTypedConverter<bool, byte[]>(BitConverter.GetBytes);
		AddTypedConverter<byte[], bool>(input => BitConverter.ToBoolean(input, 0));
		AddTypedConverter<char, byte[]>(BitConverter.GetBytes);
		AddTypedConverter<byte[], char>(input => BitConverter.ToChar(input, 0));
		AddTypedConverter<short, byte[]>(BitConverter.GetBytes);
		AddTypedConverter<byte[], short>(input => BitConverter.ToInt16(input, 0));
		AddTypedConverter<int, byte[]>(BitConverter.GetBytes);
		AddTypedConverter<byte[], int>(input => BitConverter.ToInt32(input, 0));
		AddTypedConverter<long, byte[]>(BitConverter.GetBytes);
		AddTypedConverter<byte[], long>(input => BitConverter.ToInt64(input, 0));
		AddTypedConverter<ushort, byte[]>(BitConverter.GetBytes);
		AddTypedConverter<byte[], ushort>(input => BitConverter.ToUInt16(input, 0));
		AddTypedConverter<uint, byte[]>(BitConverter.GetBytes);
		AddTypedConverter<byte[], uint>(input => BitConverter.ToUInt32(input, 0));
		AddTypedConverter<ulong, byte[]>(BitConverter.GetBytes);
		AddTypedConverter<byte[], ulong>(input => BitConverter.ToUInt64(input, 0));
		AddTypedConverter<float, byte[]>(BitConverter.GetBytes);
		AddTypedConverter<byte[], float>(input => BitConverter.ToSingle(input, 0));
		AddTypedConverter<double, byte[]>(BitConverter.GetBytes);
		AddTypedConverter<byte[], double>(input => BitConverter.ToDouble(input, 0));
		AddTypedConverter<DateTime, byte[]>(input => BitConverter.GetBytes(input.Ticks));
		AddTypedConverter<byte[], DateTime>(input => new DateTime(BitConverter.ToInt64(input, 0)));
		AddTypedConverter<DateTimeOffset, byte[]>(input => BitConverter.GetBytes(input.UtcTicks));
		AddTypedConverter<byte[], DateTimeOffset>(input => new DateTimeOffset(BitConverter.ToInt64(input, 0), TimeSpan.Zero));
		AddTypedConverter<TimeSpan, byte[]>(input => BitConverter.GetBytes(input.Ticks));
		AddTypedConverter<byte[], TimeSpan>(input => new TimeSpan(BitConverter.ToInt64(input, 0)));
		AddTypedConverter<Guid, byte[]>(input => input.ToByteArray());
		AddTypedConverter<byte[], Guid>(input => new Guid(input));

		AddTypedConverter<decimal, byte[]>(input =>
		{
			var bits = decimal.GetBits(input);

			var lo = bits[0];
			var mid = bits[1];
			var hi = bits[2];
			var flags = bits[3];

			var bytes = new byte[16];

			bytes[0] = (byte)lo;
			bytes[1] = (byte)(lo >> 8);
			bytes[2] = (byte)(lo >> 0x10);
			bytes[3] = (byte)(lo >> 0x18);
			bytes[4] = (byte)mid;
			bytes[5] = (byte)(mid >> 8);
			bytes[6] = (byte)(mid >> 0x10);
			bytes[7] = (byte)(mid >> 0x18);
			bytes[8] = (byte)hi;
			bytes[9] = (byte)(hi >> 8);
			bytes[10] = (byte)(hi >> 0x10);
			bytes[11] = (byte)(hi >> 0x18);
			bytes[12] = (byte)flags;
			bytes[13] = (byte)(flags >> 8);
			bytes[14] = (byte)(flags >> 0x10);
			bytes[15] = (byte)(flags >> 0x18);

			return bytes;
		});
		AddTypedConverter<byte[], decimal>(input =>
		{
			var bytes = input;

			var bits = new[]
			{
				((bytes[0] | (bytes[1] << 8)) | (bytes[2] << 0x10)) | (bytes[3] << 0x18), //lo
				((bytes[4] | (bytes[5] << 8)) | (bytes[6] << 0x10)) | (bytes[7] << 0x18), //mid
				((bytes[8] | (bytes[9] << 8)) | (bytes[10] << 0x10)) | (bytes[11] << 0x18), //hi
				((bytes[12] | (bytes[13] << 8)) | (bytes[14] << 0x10)) | (bytes[15] << 0x18) //flags
			};

			return new decimal(bits);
		});

		AddTypedConverter<int[], decimal>(input => new decimal(input));
		AddTypedConverter<decimal, int[]>(decimal.GetBits);

		AddTypedConverter<TimeSpan, long>(input => input.Ticks);
		AddTypedConverter<long, TimeSpan>(input => new TimeSpan(input));
		AddTypedConverter<DateTime, long>(input => input.Ticks);
		AddTypedConverter<long, DateTime>(input => new DateTime(input));
		AddTypedConverter<DateTimeOffset, long>(input => input.UtcTicks);
		AddTypedConverter<long, DateTimeOffset>(input => new DateTimeOffset(input, TimeSpan.Zero));

		AddTypedConverter<DateTime, double>(input => input.ToOADate());
		AddTypedConverter<double, DateTime>(DateTime.FromOADate);

		AddTypedConverter<DateTime, DateTimeOffset>(input =>
		{
			if (input == DateTime.MinValue)
				return DateTimeOffset.MinValue;
			else if (input == DateTime.MaxValue)
				return DateTimeOffset.MaxValue;
			else
				return new DateTimeOffset(input);
		});
		AddTypedConverter<DateTimeOffset, DateTime>(input =>
		{
			if (input == DateTimeOffset.MinValue)
				return DateTime.MinValue;
			else if (input == DateTimeOffset.MaxValue)
				return DateTime.MaxValue;
			else
				return input.UtcDateTime;
		});

		AddTypedConverter<byte, string>(input => input.ToString());
		AddTypedConverter<string, byte>(byte.Parse);
		AddTypedConverter<sbyte, string>(input => input.ToString());
		AddTypedConverter<string, sbyte>(sbyte.Parse);
		AddTypedConverter<bool, string>(input => input.ToString());
		AddTypedConverter<string, bool>(input =>
		{
			if (input == "1")
				return true;
			else if (input == "0")
				return false;
			else
				return bool.Parse(input);
		});
		AddTypedConverter<string, bool?>(s => s.IsEmpty() ? null : s.To<bool>());
		AddTypedConverter<float, string>(input => input.ToString());
		AddTypedConverter<string, float>(float.Parse);
		AddTypedConverter<string, float?>(s => s.IsEmpty() ? null : float.Parse(s));
		AddTypedConverter<double, string>(input => input.ToString());
		AddTypedConverter<string, double>(double.Parse);
		AddTypedConverter<string, double?>(s => s.IsEmpty() ? null : double.Parse(s));
		AddTypedConverter<decimal, string>(input => input.ToString());
		AddTypedConverter<string, decimal>(decimal.Parse);
		AddTypedConverter<string, decimal?>(s => s.IsEmpty() ? null : decimal.Parse(s));
		AddTypedConverter<short, string>(input => input.ToString());
		AddTypedConverter<string, short>(short.Parse);
		AddTypedConverter<string, short?>(s => s.IsEmpty() ? null : short.Parse(s));
		AddTypedConverter<int, string>(input => input.ToString());
		AddTypedConverter<string, int>(int.Parse);
		AddTypedConverter<string, int?>(s => s.IsEmpty() ? null : int.Parse(s));
		AddTypedConverter<long, string>(input => input.ToString());
		AddTypedConverter<string, long>(long.Parse);
		AddTypedConverter<string, long?>(s => s.IsEmpty() ? null : long.Parse(s));
		AddTypedConverter<ushort, string>(input => input.ToString());
		AddTypedConverter<string, ushort>(ushort.Parse);
		AddTypedConverter<string, ushort?>(s => s.IsEmpty() ? null : ushort.Parse(s));
		AddTypedConverter<uint, string>(input => input.ToString());
		AddTypedConverter<string, uint>(uint.Parse);
		AddTypedConverter<string, uint?>(s => s.IsEmpty() ? null : uint.Parse(s));
		AddTypedConverter<ulong, string>(input => input.ToString());
		AddTypedConverter<string, ulong>(ulong.Parse);
		AddTypedConverter<string, ulong?>(s => s.IsEmpty() ? null : ulong.Parse(s));
		AddTypedConverter<char, string>(input => input.ToString());
		AddTypedConverter<string, char>(char.Parse);
		AddTypedConverter<OSPlatform, string>(input => input.ToString());
		AddTypedConverter<string, OSPlatform>(OSPlatform.Create);
		AddTypedConverter<CultureInfo, string>(input => input.Name);
		AddTypedConverter<string, CultureInfo>(CultureInfo.GetCultureInfo);
		AddTypedConverter<Uri, string>(input => input.ToString());
		AddTypedConverter<string, Uri>(input => new(input));
		AddTypedConverter<Version, string>(input => input.ToString());
		AddTypedConverter<string, Version>(input => new(input));
		AddTypedConverter<Encoding, int>(input => input.CodePage);
		AddTypedConverter<int, Encoding>(Encoding.GetEncoding);
		AddTypedConverter<CultureInfo, int>(input => input.LCID);
		AddTypedConverter<int, CultureInfo>(input => new(input));
		AddTypedConverter<IntPtr, int>(input => input.ToInt32());
		AddTypedConverter<int, IntPtr>(input => new(input));
		AddTypedConverter<IntPtr, long>(input => input.ToInt64());
		AddTypedConverter<long, IntPtr>(input => new(input));
		AddTypedConverter<UIntPtr, uint>(input => input.ToUInt32());
		AddTypedConverter<uint, UIntPtr>(input => new(input));
		AddTypedConverter<UIntPtr, ulong>(input => input.ToUInt64());
		AddTypedConverter<ulong, UIntPtr>(input => new(input));
		AddTypedConverter<TimeSpan, string>(input => input.ToString());
		AddTypedConverter<string, TimeSpan>(TimeSpan.Parse);
		AddTypedConverter<string, TimeSpan?>(s => s.IsEmpty() ? null : TimeSpan.Parse(s));
		AddTypedConverter<Guid, string>(input => input.ToString());
		AddTypedConverter<string, Guid>(Guid.Parse);
		AddTypedConverter<string, Guid?>(s => s.IsEmpty() ? null : Guid.Parse(s));
		AddTypedConverter<TimeZoneInfo, string>(input => input.Id);
		AddTypedConverter<string, TimeZoneInfo>(s => TZConvert.TryGetTimeZoneInfo(s, out var tz) ? tz : TimeZoneInfo.Utc);
	}

	/// <summary>
	/// Adds a typed converter function for converting from TFrom to TTo type.
	/// </summary>
	/// <typeparam name="TFrom">The source type to convert from.</typeparam>
	/// <typeparam name="TTo">The destination type to convert to.</typeparam>
	/// <param name="converter">The converter function to add.</param>
	/// <exception cref="ArgumentNullException">Thrown when converter is null.</exception>
	public static void AddTypedConverter<TFrom, TTo>(Func<TFrom, TTo> converter)
	{
		if (converter is null)
			throw new ArgumentNullException(nameof(converter));

		var key = (typeof(TFrom), typeof(TTo));

		_typedConverters.Add(key, converter);
		_typedConverters2.Add(key, input => converter((TFrom)input));
	}

	/// <summary>
	/// Adds a typed converter function for converting between specified types.
	/// </summary>
	/// <param name="key">Tuple containing source and destination types.</param>
	/// <param name="converter">The converter function to add.</param>
	/// <exception cref="ArgumentNullException">Thrown when converter is null.</exception>
	public static void AddTypedConverter((Type, Type) key, Func<object, object> converter)
	{
		if (converter is null)
			throw new ArgumentNullException(nameof(converter));

		_typedConverters.Add(key, converter);
		_typedConverters2.Add(key, converter);
	}

	/// <summary>
	/// Gets the typed converter function for converting between specified types.
	/// </summary>
	/// <typeparam name="TFrom">The source type to convert from.</typeparam>
	/// <typeparam name="TTo">The destination type to convert to.</typeparam>
	/// <returns>A converter function for the specified types.</returns>
	public static Func<TFrom, TTo> GetTypedConverter<TFrom, TTo>()
	{
		return (Func<TFrom, TTo>)_typedConverters[(typeof(TFrom), typeof(TTo))];
	}

	/// <summary>
	/// Gets the typed converter function for converting between specified types.
	/// </summary>
	/// <param name="from">The source type to convert from.</param>
	/// <param name="to">The destination type to convert to.</param>
	/// <returns>A converter function for the specified types.</returns>
	public static Func<object, object> GetTypedConverter(Type from, Type to)
	{
		return (Func<object, object>)_typedConverters[(from, to)];
	}

	/// <summary>
	/// Converts the value from source type to destination type using registered type converters.
	/// </summary>
	/// <typeparam name="TFrom">The source type to convert from.</typeparam>
	/// <typeparam name="TTo">The destination type to convert to.</typeparam>
	/// <param name="from">The value to convert.</param>
	/// <returns>The converted value.</returns>
	public static TTo TypedTo<TFrom, TTo>(this TFrom from)
	{
		return GetTypedConverter<TFrom, TTo>()(from);
	}

	private static bool TryGetTypedConverter(Type from, Type to, out Func<object, object> typedConverter)
	{
		return _typedConverters2.TryGetValue((from, to), out typedConverter);
	}

	/// <summary>
	/// Convert value into a instance of <paramref name="destinationType"/>.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <param name="destinationType">Type of the destination.</param>
	/// <returns>Converted object.</returns>
	public static object To(this object value, Type destinationType)
	{
		if (destinationType is null)
			throw new ArgumentNullException(nameof(destinationType));

		try
		{
			if (value is null)
			{
				if ((destinationType.IsValueType || destinationType.IsEnum()) && !destinationType.IsNullable())
					throw new ArgumentNullException(nameof(value));

				return null;
			}

			Type GetValueType()
			{
				if (value is Type)
					return typeof(Type);
				else if (value is IPAddress)
					return typeof(IPAddress);
				else if (value is EndPoint)
					return typeof(EndPoint);
				else if (value is Encoding)
					return typeof(Encoding);
				else if (value is CultureInfo)
					return typeof(CultureInfo);

				var type = value.GetType();
				type.EnsureRunClass();
				return type;
			}

			destinationType.EnsureRunClass();

			if (TryGetTypedConverter(GetValueType(), destinationType, out var typedConverter))
				return typedConverter(value);

			var sourceType = value.GetType();

			if (sourceType.Is(destinationType))
				return value;
			else if ((value is string || sourceType.IsPrimitive) && destinationType.IsEnum())
			{
				if (value is string s)
					return Enum.Parse(destinationType, s, true);
				else
					return Enum.ToObject(destinationType, value);
			}
			else if (value is Stream st1 && destinationType == typeof(string))
			{
				return st1.To<byte[]>().To<string>();
			}
			else if (value is Stream st2 && destinationType == typeof(byte[]))
			{
				MemoryStream output;

				if (st2 is MemoryStream memoryStream)
					output = memoryStream;
				else
				{
					const int capacity = FileSizes.KB * 4;

					output = new MemoryStream(capacity);

					var buffer = new byte[FileSizes.KB];

					//int offset = 0;

					while (true)
					{
						int readBytes = st2.Read(buffer, 0, FileSizes.KB);
						if (readBytes == 0)
							break;

						output.Write(buffer, 0, readBytes);
						//offset += readBytes;
					}
				}

				return output.ToArray();
			}
			else if (value is ArraySegment<byte> seg && (destinationType == typeof(Stream) || destinationType == typeof(MemoryStream)))
			{
				return new MemoryStream(seg.Array, seg.Offset, seg.Count);
			}
			else if (value is byte[] ba && (destinationType == typeof(Stream) || destinationType == typeof(MemoryStream)))
			{
				var stream = new MemoryStream(ba.Length);
				stream.Write(ba, 0, stream.Capacity);
				stream.Position = 0;
				return stream;
			}
			else if (value is string && (destinationType == typeof(Stream) || destinationType == typeof(MemoryStream)))
			{
				return value.To<byte[]>().To<Stream>();
			}
			else if (destinationType == typeof(byte[]))
			{
				if (value is Enum)
					value = value.To(sourceType.GetEnumBaseType());

				if (TryGetTypedConverter(GetValueType(), destinationType, out typedConverter))
					return typedConverter(value);
				else if (value is Array arr && ArrayCovariance(arr, destinationType, out var dest))
					return dest;
			}
			else if (value is byte[] bytes)
			{
				Type enumType;

				if (destinationType.IsEnum())
				{
					enumType = destinationType;
					destinationType = destinationType.GetEnumBaseType();
				}
				else
					enumType = null;

				object retVal;

				if (TryGetTypedConverter(typeof(byte[]), destinationType, out typedConverter))
					retVal = typedConverter(value);
				else if (destinationType.IsArray && ArrayCovariance(bytes, destinationType, out var dest))
					return dest;
				else
					throw new ArgumentException($"Can't convert byte array to '{destinationType}'.", nameof(value));

				if (enumType != null)
					retVal = Enum.ToObject(enumType, retVal);

				return retVal;
			}
			else if (value is DBNull)
				return null;
			else if (destinationType.GetUnderlyingType() is Type underlying)
			{
				if (value is null || (value is string s3 && s3.Length == 0))
				{
					return destinationType.CreateInstance();
				}
				else
				{
					if (destinationType == typeof(decimal?))
						return value.To(typeof(decimal));
					else if (destinationType == typeof(int?))
						return value.To(typeof(int));
					else if (destinationType == typeof(long?))
						return value.To(typeof(long));
					else
						return destinationType.CreateInstance(value.To(underlying));
				}
			}
			else if (value is DateTime dt && destinationType == typeof(string))
				return dt.Millisecond > 0 ? dt.ToString("o") : value.ToString();
			else if (value is DateTimeOffset dto && destinationType == typeof(string))
				return dto.Millisecond > 0 ? dto.ToString("o") : value.ToString();
			else if (value is string str4 && destinationType == typeof(DateTimeOffset))
				return DateTimeOffset.Parse(str4);
			else if (value is string s6 && destinationType == typeof(XDocument))
				return XDocument.Parse(s6);
			else if (value is string s7 && destinationType == typeof(XElement))
				return XElement.Parse(s7);
			else if (value is XNode && destinationType == typeof(string))
				return value.ToString();
			else if (value is string s8 && destinationType == typeof(XmlDocument))
			{
				var doc = new XmlDocument();
				doc.LoadXml(s8);
				return doc;
			}
			else if (value is XmlNode n1 && destinationType == typeof(string))
				return n1.OuterXml;
			else
			{
				var attr = destinationType.GetAttribute<TypeConverterAttribute>();

				if (attr != null)
				{
					var ctors = attr.ConverterTypeName.To<Type>().GetConstructors();

					if (ctors.Length == 1)
					{
						var ctor = ctors[0];
						var converter = (TypeConverter)(ctor.GetParameters().Length == 0 ? ctor.Invoke(null) : ctor.Invoke([destinationType]));
						if (converter.CanConvertFrom(sourceType))
							return converter.ConvertFrom(value);
					}
				}

				if (value is IConvertible)
				{
					try
					{
						return Convert.ChangeType(value, destinationType, null);
					}
					catch (InvalidCastException)
					{
						if (FinalTry(ref value, sourceType, destinationType))
							return value;
					}
				}

				if (value is Array arr && ArrayCovariance(arr, destinationType, out var dest))
					return dest;
				else if (value is ICovarianceEnumerable covarEnu && destinationType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
					return covarEnu.ChangeType(destinationType.GetGenericArguments().First());

				if (FinalTry(ref value, sourceType, destinationType))
					return value;
			}

			throw new ArgumentException($"Can't convert {value} of type '{value.GetType()}' to type '{destinationType}'.", nameof(value));
		}
		catch (Exception ex)
		{
			throw new InvalidCastException($"Can't convert {value} of type '{value?.GetType()}' to type '{destinationType}'.", ex);
		}
	}

	private static bool ArrayCovariance(Array source, Type destinationType, out object result)
	{
		result = null;

		if (destinationType.IsArray)
		{
			var elemType = destinationType.GetElementType();
			var destArr = Array.CreateInstance(elemType, source.Length);

			for (var i = 0; i < source.Length; i++)
				destArr.SetValue(source.GetValue(i).To(elemType), i);

			result = destArr;
			return true;
		}
		else if (destinationType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
		{
			var elemType = destinationType.GetGenericArguments().First();
			result = new CovarianceEnumerable<object>(source).ChangeType(elemType);
			return true;
		}
		else
			return false;
	}

	private interface ICovarianceEnumerable
	{
		ICovarianceEnumerable ChangeType(Type newType);
	}

	private class CovarianceEnumerable<T>(Array array) : IEnumerable<T>, ICovarianceEnumerable
	{
		private class CovarianceEnumerator : IEnumerator<T>
		{
			private readonly Array _array;
			private int _idx;

			public CovarianceEnumerator(Array array)
			{
				_array = array ?? throw new ArgumentNullException(nameof(array));
				Reset();
			}

			public T Current => _array.GetValue(_idx).To<T>();

			object IEnumerator.Current => Current;

			void IDisposable.Dispose()
			{
			}

			bool IEnumerator.MoveNext()
			{
				_idx++;
				return _idx < _array.Length;
			}

			public void Reset()
			{
				_idx = -1;
			}
		}

		private readonly Array _array = array ?? throw new ArgumentNullException(nameof(array));

		public IEnumerator<T> GetEnumerator()
			=> new CovarianceEnumerator(_array);

		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();

		public ICovarianceEnumerable ChangeType(Type newType)
			=> typeof(CovarianceEnumerable<>).Make(newType).CreateInstance<ICovarianceEnumerable>(_array);
	}

	private static bool FinalTry(ref object value, Type sourceType, Type destinationType)
	{
		static bool IsConversion(MethodInfo mi)
			=> mi.Name == "op_Implicit" || mi.Name == "op_Explicit";
		
		var method =
			sourceType
				.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.FirstOrDefault(mi => IsConversion(mi) && mi.ReturnType == destinationType)
			??
			destinationType
				.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.FirstOrDefault(mi =>
				{
					if (!IsConversion(mi))
						return false;

					var parameters = mi.GetParameters();

					return parameters.Length == 1 && parameters[0].ParameterType == sourceType;
				});

		if (method != null)
			value = method.Invoke(null, [value]);
		else if (destinationType == typeof(string))
			value = value.ToString();
		else
			return false;

		return true;
	}

	/// <summary>
	/// Convert value into a instance of destinationType.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <returns>Converted object.</returns>
	public static T To<T>(this object value)
	{
		return (T)To(value, typeof(T));
	}

	/// <summary>
	/// Adds a C# language alias for a type.
	/// </summary>
	/// <param name="type">The type to add an alias for.</param>
	/// <param name="alias">The C# alias to associate with the type.</param>
	/// <exception cref="ArgumentNullException">Thrown when type or alias is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when alias or type already exists.</exception>
	public static void AddCSharpAlias(Type type, string alias)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		if (alias.IsEmpty())
			throw new ArgumentNullException(nameof(alias));

		if (_sharpAliases.ContainsKey(alias))
			throw new ArgumentOutOfRangeException(nameof(alias));

		if (_sharpAliasesByValue.ContainsKey(type))
			throw new ArgumentOutOfRangeException(nameof(type));

		_sharpAliases.Add(alias, type);
		_sharpAliasesByValue.Add(type, alias);
	}

	/// <summary>
	/// Attempts to get the C# language alias for a type.
	/// </summary>
	/// <param name="type">The type to get the alias for.</param>
	/// <returns>The C# alias if found; otherwise null.</returns>
	public static string TryGetCSharpAlias(this Type type)
		=> _sharpAliasesByValue.TryGetValue(type, out var alias) ? alias : null;

	/// <summary>
	/// Attempts to get a type by its C# language alias.
	/// </summary>
	/// <param name="alias">The C# alias to look up.</param>
	/// <returns>The corresponding type if found; otherwise null.</returns>
	public static Type TryGetTypeByCSharpAlias(this string alias)
		=> _sharpAliases.TryGetValue(alias, out var type) ? type : null;

	/// <summary>
	/// Executes a function using the specified culture.
	/// </summary>
	/// <typeparam name="T">The return type of the function.</typeparam>
	/// <param name="cultureInfo">The culture to use during execution.</param>
	/// <param name="func">The function to execute.</param>
	/// <returns>The result of the function execution.</returns>
	/// <exception cref="ArgumentNullException">Thrown when cultureInfo or func is null.</exception>
	public static T DoInCulture<T>(this CultureInfo cultureInfo, Func<T> func)
	{
		if (func is null)
			throw new ArgumentNullException(nameof(func));

		var prevCi = Thread.CurrentThread.CurrentCulture;
		Thread.CurrentThread.CurrentCulture = cultureInfo ?? throw new ArgumentNullException(nameof(cultureInfo));

		try
		{
			return func();
		}
		finally
		{
			Thread.CurrentThread.CurrentCulture = prevCi;
		}
	}

	/// <summary>
	/// Executes an action using the specified culture.
	/// </summary>
	/// <param name="cultureInfo">The culture to use during execution.</param>
	/// <param name="action">The action to execute.</param>
	/// <exception cref="ArgumentNullException">Thrown when cultureInfo or action is null.</exception>
	public static void DoInCulture(this CultureInfo cultureInfo, Action action)
	{
		if (action is null)
			throw new ArgumentNullException(nameof(action));

		cultureInfo.DoInCulture<object>(() =>
		{
			action();
			return null;
		});
	}

	/// <summary>
	/// Wraps a function to execute using the invariant culture.
	/// </summary>
	/// <typeparam name="T">The return type of the function.</typeparam>
	/// <param name="func">The function to wrap.</param>
	/// <returns>A function that executes using the invariant culture.</returns>
	/// <exception cref="ArgumentNullException">Thrown when func is null.</exception>
	public static Func<T> AsInvariant<T>(this Func<T> func)
	{
		if (func is null)
			throw new ArgumentNullException(nameof(func));

		return () => Do.Invariant(func);
	}

	/// <summary>
	/// Wraps an action to execute using the invariant culture.
	/// </summary>
	/// <param name="action">The action to wrap.</param>
	/// <returns>An action that executes using the invariant culture.</returns>
	/// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
	public static Action AsInvariant(this Action action)
	{
		if (action is null)
			throw new ArgumentNullException(nameof(action));

		return () => Do.Invariant(action);
	}

	/// <summary>
	/// Converts the given decimal number to the numeral system with the
	/// specified radix (in the range [2, 36]).
	/// </summary>
	/// <param name="decimalNumber">The number to convert.</param>
	/// <param name="radix">The radix of the destination numeral system
	/// (in the range [2, 36]).</param>
	/// <returns></returns>
	public static string ToRadix(this long decimalNumber, int radix)
	{
		//
		// http://www.pvladov.com/2012/05/decimal-to-arbitrary-numeral-system.html
		//

		const int bitsInLong = 64;
		const string digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

		if (radix < 2 || radix > digits.Length)
			throw new ArgumentOutOfRangeException(nameof(radix), radix, $"The radix must be >= 2 and <= {digits.Length}.");

		if (decimalNumber == 0)
			return "0";

		var index = bitsInLong - 1;
		var currentNumber = Math.Abs(decimalNumber);
		var charArray = new char[bitsInLong];

		while (currentNumber != 0)
		{
			var remainder = (int)(currentNumber % radix);
			charArray[index--] = digits[remainder];
			currentNumber /= radix;
		}

		var result = new string(charArray, index + 1, bitsInLong - index - 1);

		if (decimalNumber < 0)
			result = "-" + result;

		return result;
	}

	/// <summary>
	/// Retrieves the host from the specified endpoint.
	/// </summary>
	/// <param name="endPoint">The endpoint to extract the host from.</param>
	/// <returns>The host string.</returns>
	/// <exception cref="ArgumentNullException">Thrown if endPoint is null.</exception>
	/// <exception cref="InvalidOperationException">Thrown if the endpoint type is unknown.</exception>
	public static string GetHost(this EndPoint endPoint)
	{
		if (endPoint is null)
			throw new ArgumentNullException(nameof(endPoint));

		if (endPoint is IPEndPoint ip)
		{
			return ip.Address.ToString();
		}
		else if (endPoint is DnsEndPoint dns)
		{
			return dns.Host;
		}
		else
			throw new InvalidOperationException($"Unknown endpoint {endPoint}.");
	}

	/// <summary>
	/// Sets the host on the specified endpoint.
	/// </summary>
	/// <param name="endPoint">The endpoint to modify.</param>
	/// <param name="host">The new host name or IP address.</param>
	/// <returns>The modified endpoint.</returns>
	/// <exception cref="ArgumentNullException">Thrown if endPoint is null.</exception>
	/// <exception cref="InvalidOperationException">Thrown if the endpoint type is unknown.</exception>
	public static EndPoint SetHost(this EndPoint endPoint, string host)
	{
		if (endPoint is null)
			throw new ArgumentNullException(nameof(endPoint));

		if (endPoint is IPEndPoint ip)
		{
			ip.Address = host.To<IPAddress>();
		}
		else if (endPoint is DnsEndPoint dns)
		{
			endPoint = new DnsEndPoint(host, dns.Port, dns.AddressFamily);
		}
		else
			throw new InvalidOperationException($"Unknown endpoint {endPoint}.");

		return endPoint;
	}

	/// <summary>
	/// Retrieves the port number from the specified endpoint.
	/// </summary>
	/// <param name="endPoint">The endpoint to extract the port from.</param>
	/// <returns>The port number.</returns>
	/// <exception cref="ArgumentNullException">Thrown if endPoint is null.</exception>
	/// <exception cref="InvalidOperationException">Thrown if the endpoint type is unknown.</exception>
	public static int GetPort(this EndPoint endPoint)
	{
		if (endPoint is null)
			throw new ArgumentNullException(nameof(endPoint));

		if (endPoint is IPEndPoint ip)
		{
			return ip.Port;
		}
		else if (endPoint is DnsEndPoint dns)
		{
			return dns.Port;
		}
		else
			throw new InvalidOperationException($"Unknown endpoint {endPoint}.");
	}

	/// <summary>
	/// Sets the port number on the specified endpoint.
	/// </summary>
	/// <param name="endPoint">The endpoint to modify.</param>
	/// <param name="port">The new port number.</param>
	/// <returns>The modified endpoint.</returns>
	/// <exception cref="ArgumentNullException">Thrown if endPoint is null.</exception>
	/// <exception cref="InvalidOperationException">Thrown if the endpoint type is unknown.</exception>
	public static EndPoint SetPort(this EndPoint endPoint, int port)
	{
		if (endPoint is null)
			throw new ArgumentNullException(nameof(endPoint));

		if (endPoint is IPEndPoint ip)
		{
			ip.Port = port;
		}
		else if (endPoint is DnsEndPoint dns)
		{
			endPoint = new DnsEndPoint(dns.Host, port, dns.AddressFamily);
		}
		else
			throw new InvalidOperationException($"Unknown endpoint {endPoint}.");

		return endPoint;
	}

	/// <summary>
	/// Reverses the byte order if the specified endianness differs from the system endianness.
	/// </summary>
	/// <param name="bytes">The byte array to modify.</param>
	/// <param name="length">The number of bytes to consider.</param>
	/// <param name="isLittleEndian">Specifies the desired endianness.</param>
	/// <param name="pos">The starting position within the array.</param>
	/// <returns>The modified byte array.</returns>
	public static byte[] ChangeOrder(this byte[] bytes, int length, bool isLittleEndian, int pos = 0)
	{
		if (isLittleEndian == BitConverter.IsLittleEndian)
			return bytes;

		var end = pos + length / 2;

		for (var i = pos; i < end; i++)
		{
			var start = i;
			var stop = pos + length - i - 1;

			var temp = bytes[start];
			bytes[start] = bytes[stop];
			bytes[stop] = temp;
		}

		return bytes;
	}
}