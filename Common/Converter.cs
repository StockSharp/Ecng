namespace Ecng.Common
{
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

	using TimeZoneConverter;

	public static class Converter
	{
		private static readonly Dictionary<Type, DbType> _dbTypes = new();
		private static readonly Dictionary<string, Type> _sharpAliases = new();
		private static readonly Dictionary<Type, string> _sharpAliasesByValue = new();
		private static readonly Dictionary<string, Type> _typeCache = new();

		private static readonly Dictionary<(Type, Type), Delegate> _typedConverters = new();
		private static readonly Dictionary<(Type, Type), Func<object, object>> _typedConverters2 = new();

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
			AddTypedConverter<IPEndPoint, string>(input => input.TypedTo<EndPoint, string>());
			AddTypedConverter<DnsEndPoint, string>(input => input.TypedTo<EndPoint, string>());
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
					charArray[offset++] = BitConverter.ToChar(new[] { input[i], input[i + 1] }, 0);

				return charArray.TypedTo<char[], SecureString>();
			});
			AddTypedConverter<string, char[]>(input => input.ToCharArray());
			AddTypedConverter<char[], string>(input => new string(input));
			AddTypedConverter<byte, byte[]>(input => new[] { input });
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
			AddTypedConverter<float, string>(input => input.ToString());
			AddTypedConverter<string, float>(float.Parse);
			AddTypedConverter<double, string>(input => input.ToString());
			AddTypedConverter<string, double>(double.Parse);
			AddTypedConverter<decimal, string>(input => input.ToString());
			AddTypedConverter<string, decimal>(decimal.Parse);
			AddTypedConverter<short, string>(input => input.ToString());
			AddTypedConverter<string, short>(short.Parse);
			AddTypedConverter<int, string>(input => input.ToString());
			AddTypedConverter<string, int>(int.Parse);
			AddTypedConverter<long, string>(input => input.ToString());
			AddTypedConverter<string, long>(long.Parse);
			AddTypedConverter<ushort, string>(input => input.ToString());
			AddTypedConverter<string, ushort>(ushort.Parse);
			AddTypedConverter<uint, string>(input => input.ToString());
			AddTypedConverter<string, uint>(uint.Parse);
			AddTypedConverter<ulong, string>(input => input.ToString());
			AddTypedConverter<string, ulong>(ulong.Parse);
			AddTypedConverter<char, string>(input => input.ToString());
			AddTypedConverter<string, char>(char.Parse);
			AddTypedConverter<OSPlatform, string>(input => input.ToString());
			AddTypedConverter<string, OSPlatform>(OSPlatform.Create);
			AddTypedConverter<CultureInfo, string>(input => input.Name);
			AddTypedConverter<string, CultureInfo>(CultureInfo.GetCultureInfo);
		}

		public static void AddTypedConverter<TFrom, TTo>(Func<TFrom, TTo> converter)
		{
			if (converter is null)
				throw new ArgumentNullException(nameof(converter));

			var key = (typeof(TFrom), typeof(TTo));

			_typedConverters.Add(key, converter);
			_typedConverters2.Add(key, input => converter((TFrom)input));
		}

		public static void AddTypedConverter((Type, Type) key, Func<object, object> converter)
		{
			if (converter is null)
				throw new ArgumentNullException(nameof(converter));

			_typedConverters.Add(key, converter);
			_typedConverters2.Add(key, converter);
		}

		public static Func<TFrom, TTo> GetTypedConverter<TFrom, TTo>()
		{
			return (Func<TFrom, TTo>)_typedConverters[(typeof(TFrom), typeof(TTo))];
		}

		public static Func<object, object> GetTypedConverter(Type from, Type to)
		{
			return (Func<object, object>)_typedConverters[(from, to)];
		}

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

					if (value is IPAddress)
						return typeof(IPAddress);

					return value.GetType();
				}

				if (TryGetTypedConverter(GetValueType(), destinationType, out var typedConverter))
					return typedConverter(value);

				var sourceType = value.GetType();

				if (destinationType.IsAssignableFrom(sourceType))
					return value;
				else if ((value is string || sourceType.IsPrimitive) && destinationType.IsEnum())
				{
					if (value is string s)
						return Enum.Parse(destinationType, s, true);
					else
						return Enum.ToObject(destinationType, value);
				}
				else if (destinationType == typeof(Type[]))
				{
					if (value is not IEnumerable<object>)
						value = new[] { value };

					return ((IEnumerable<object>)value).Select(arg => arg?.GetType() ?? typeof(void)).ToArray();
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
				else if (value is Uri && destinationType == typeof(string))
					return value.ToString();
				else if (value is string s1 && destinationType == typeof(Uri))
					return new Uri(s1);
				else if (value is Version && destinationType == typeof(string))
					return value.ToString();
				else if (value is string s2 && destinationType == typeof(Version))
					return new Version(s2);
				else if (value is int i1 && destinationType == typeof(IntPtr))
					return new IntPtr(i1);
				else if (value is long l1 && destinationType == typeof(IntPtr))
					return new IntPtr(l1);
				else if (value is uint ui1 && destinationType == typeof(UIntPtr))
					return new UIntPtr(ui1);
				else if (value is ulong ul1 && destinationType == typeof(UIntPtr))
					return new UIntPtr(ul1);
				else if (value is IntPtr iptr1 && destinationType == typeof(int))
					return iptr1.ToInt32();
				else if (value is IntPtr iptr2 && destinationType == typeof(long))
					return iptr2.ToInt64();
				else if (value is UIntPtr uptr1 && destinationType == typeof(uint))
					return uptr1.ToUInt32();
				else if (value is UIntPtr uptr2 && destinationType == typeof(ulong))
					return uptr2.ToUInt64();
				else if (value is CultureInfo ci1 && destinationType == typeof(int))
					return ci1.LCID;
				else if (value is int i2 && destinationType == typeof(CultureInfo))
					return new CultureInfo(i2);
				else if (value is Encoding e1 && destinationType == typeof(int))
					return e1.CodePage;
				else if (value is int i3 && destinationType == typeof(Encoding))
					return Encoding.GetEncoding(i3);
				else if (destinationType.GetUnderlyingType() != null)
				{
					if (value is DBNull)
						return null;
					else if (value is string s3 && s3 == string.Empty)
					{
						if (destinationType == typeof(decimal?))
							return new decimal?();
						else if (destinationType == typeof(int?))
							return new decimal?();
						else if (destinationType == typeof(long?))
							return new decimal?();
						else
							return destinationType.CreateInstance<object>();
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
							return destinationType.CreateInstance<object>(value.To(destinationType.GetUnderlyingType()));
					}
				}
				else if (value is string s4 && destinationType == typeof(TimeSpan))
					return TimeSpan.Parse(s4);
				else if (value is TimeSpan && destinationType == typeof(string))
					return value.ToString();
				else if (value is DateTime dt && destinationType == typeof(string))
					return dt.Millisecond > 0 ? dt.ToString("o") : value.ToString();
				else if (value is DateTimeOffset dto && destinationType == typeof(string))
					return dto.Millisecond > 0 ? dto.ToString("o") : value.ToString();
				else if (value is string str4 && destinationType == typeof(DateTimeOffset))
					return DateTimeOffset.Parse(str4);
				else if (value is string str5 && destinationType == typeof(TimeZoneInfo))
					return TZConvert.TryGetTimeZoneInfo(str5, out var tz) ? tz : TimeZoneInfo.Utc;
				else if (value is TimeZoneInfo tz && destinationType == typeof(string))
					return tz.Id;
				else if (value is string s5 && destinationType == typeof(Guid))
					return new Guid(s5);
				else if (value is Guid && destinationType == typeof(string))
					return value.ToString();
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
				else if (value is string s9 && destinationType == typeof(decimal))
					return decimal.Parse(s9, NumberStyles.Any, null);
				else if (value is DBNull)
					return null;
				else
				{
					var attr = destinationType.GetAttribute<TypeConverterAttribute>();

					if (attr != null)
					{
						var ctors = attr.ConverterTypeName.To<Type>().GetConstructors();

						if (ctors.Length == 1)
						{
							var ctor = ctors[0];
							var converter = (TypeConverter)(ctor.GetParameters().Length == 0 ? ctor.Invoke(null) : ctor.Invoke(new object[] { destinationType }));
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

		private class CovarianceEnumerable<T> : IEnumerable<T>, ICovarianceEnumerable
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

			private readonly Array _array;

			public CovarianceEnumerable(Array array)
			{
				_array = array ?? throw new ArgumentNullException(nameof(array));
			}

			public IEnumerator<T> GetEnumerator()
				=> new CovarianceEnumerator(_array);

			IEnumerator IEnumerable.GetEnumerator()
				=> GetEnumerator();

			public ICovarianceEnumerable ChangeType(Type newType)
				=> typeof(CovarianceEnumerable<>).Make(newType).CreateInstance<ICovarianceEnumerable>(_array);
		}

		private static bool FinalTry(ref object value, Type sourceType, Type destinationType)
		{
			//var key = Tuple.Create(sourceType, destinationType);

			MethodInfo method;

			//if (!_castOperators.TryGetValue(key, out method))
			{
				method =
					sourceType
						.GetMethods(BindingFlags.Public | BindingFlags.Static)
						.FirstOrDefault(mi => mi.Name == "op_Implicit" && mi.ReturnType == destinationType)
					??
					destinationType
						.GetMethods(BindingFlags.Public | BindingFlags.Static)
						.FirstOrDefault(mi =>
						{
							if (mi.Name != "op_Explicit")
								return false;

							var parameters = mi.GetParameters();

							return parameters.Length == 1 && parameters[0].ParameterType == sourceType;
						});

				//_castOperators.Add(key, method);
			}

			if (method != null)
				value = method.Invoke(null, new[] { value });
			else if (destinationType == typeof(string))
				value = value.ToString();
			else
				return false;

			return true;
		}

		//private static readonly Dictionary<Tuple<Type, Type>, MethodInfo> _castOperators = new Dictionary<Tuple<Type, Type>, MethodInfo>();

		/// <summary>
		/// Convert value into a instance of destinationType.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>Converted object.</returns>
		public static T To<T>(this object value)
		{
			return (T)To(value, typeof(T));
		}

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

		public static string TryGetCSharpAlias(this Type type)
			=> _sharpAliasesByValue.TryGetValue(type, out var alias) ? alias : null;

		public static Type TryGetTypeByCSharpAlias(this string alias)
			=> _sharpAliases.TryGetValue(alias, out var type) ? type : null;

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

		public static Func<T> AsInvariant<T>(this Func<T> func)
		{
			if (func is null)
				throw new ArgumentNullException(nameof(func));

			return () => Do.Invariant(func);
		}

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
}