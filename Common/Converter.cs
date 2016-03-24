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
#if SILVERLIGHT
	using System.Windows.Media;
	using ArgumentOutOfRangeException = System.ArgumentOutOfRangeExceptionEx;
#else
	using System.Runtime.InteropServices;
	using System.Drawing;
#endif
	using System.Globalization;
	using System.Threading;
	using System.Xml;
	using System.Xml.Linq;

#if !SILVERLIGHT
	using WinColor = System.Drawing.Color;
	using WpfColorConverter = System.Windows.Media.ColorConverter;
#endif
	using WpfColor = System.Windows.Media.Color;

	public static class Converter
	{
		private static readonly Dictionary<Type, DbType> _dbTypes = new Dictionary<Type, DbType>();
		private static readonly Dictionary<string, Type> _aliases = new Dictionary<string, Type>();
		private static readonly Dictionary<Type, List<string>> _aliasesByValue = new Dictionary<Type, List<string>>();
		private static readonly Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();

		private static readonly Dictionary<Tuple<Type, Type>, Delegate> _typedConverters = new Dictionary<Tuple<Type, Type>, Delegate>();
		private static readonly Dictionary<Tuple<Type, Type>, Func<object, object>> _typedConverters2 = new Dictionary<Tuple<Type, Type>, Func<object, object>>();

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

			AddAlias(typeof(object), "object");
			AddAlias(typeof(bool), "bool");
			AddAlias(typeof(bool), "boolean");
			AddAlias(typeof(byte), "byte");
			AddAlias(typeof(sbyte), "sbyte");
			AddAlias(typeof(char), "char");
			AddAlias(typeof(char), "character");
			AddAlias(typeof(decimal), "decimal");
			AddAlias(typeof(decimal), "money");
			AddAlias(typeof(double), "double");
			AddAlias(typeof(float), "float");
			AddAlias(typeof(float), "single");
			AddAlias(typeof(float), "real");
			AddAlias(typeof(int), "int");
			AddAlias(typeof(uint), "uint");
			AddAlias(typeof(long), "long");
			AddAlias(typeof(ulong), "ulong");
			AddAlias(typeof(short), "short");
			AddAlias(typeof(ushort), "ushort");
			AddAlias(typeof(string), "string");
			AddAlias(typeof(DateTime), "date");
			AddAlias(typeof(DateTime), "datetime");
			AddAlias(typeof(TimeSpan), "time");
			AddAlias(typeof(TimeSpan), "timespan");
			AddAlias(typeof(IntPtr), "ptr");
			AddAlias(typeof(IntPtr), "intptr");
			AddAlias(typeof(UIntPtr), "uptr");
			AddAlias(typeof(UIntPtr), "uintptr");
			AddAlias(typeof(void), "void");
			AddAlias(typeof(Guid), "guid");

			AddTypedConverter<Type, DbType>(input =>
			{
				if (input.IsNullable())
				{
					input = input.GetGenericArguments()[0];
				}
				if (input.IsEnum())
					input = input.GetEnumBaseType();

				DbType dbType;

				if (_dbTypes.TryGetValue(input, out dbType))
					return dbType;
				else
					throw new ArgumentException(".NET type {0} doesn't have associated db type.".Put(input));
			});

			AddTypedConverter<DbType, Type>(input => _dbTypes.First(pair => pair.Value == input).Key);
			AddTypedConverter<string, byte[]>(input => Encoding.Unicode.GetBytes(input));
			AddTypedConverter<byte[], string>(input => Encoding.Unicode.GetString(input));
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
			AddTypedConverter<string, IPAddress>(IPAddress.Parse);
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
					var port = input.Substring(index + 1).To<int>();

					IPAddress addr;

					if (!IPAddress.TryParse(host, out addr))
						return new DnsEndPoint(host, port);

					return new IPEndPoint(addr, port);
				}
				else
					throw new FormatException("Invalid endpoint format.");
			});
			AddTypedConverter<EndPoint, string>(input => input.GetHost() + ":" + input.GetPort());
			AddTypedConverter<IPEndPoint, string>(input => input.TypedTo<EndPoint, string>());
			AddTypedConverter<DnsEndPoint, string>(input => input.TypedTo<EndPoint, string>());
			AddTypedConverter<string, Type>(input =>
			{
				Type type;
				var key = input.ToLowerInvariant();

				if (!_aliases.TryGetValue(key, out type))
				{
					if (!_typeCache.TryGetValue(key, out type))
					{
						lock (_typeCache)
						{
							if (!_typeCache.TryGetValue(key, out type))
							{
								type = Type.GetType(input, false, true);

#if !SILVERLIGHT
								// в строке может быть записаное не AssemblyQualifiedName, а только полное имя типа + имя сборки.
								if (type == null)
								{
									var parts = input.Split(", ");
									if (parts.Length == 2)
									{
										var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == parts[1]) ?? Assembly.LoadWithPartialName(parts[1]);

										if (asm != null)
										{
											type = asm.GetType(parts[0]);
										}
									}
								}
#endif

								if (type != null)
									_typeCache.Add(key, type);
								else
									throw new ArgumentException("Type {0} doesn't exists.".Put(input), nameof(input));
							}
						}
					}
				}

				return type;
			});
			AddTypedConverter<Type, string>(input => input.AssemblyQualifiedName);
			AddTypedConverter<StringBuilder, string>(input => input.ToString());
			AddTypedConverter<string, StringBuilder>(input => new StringBuilder(input));
			AddTypedConverter<DbConnectionStringBuilder, string>(input => input.ToString());
			AddTypedConverter<string, DbConnectionStringBuilder>(input => new DbConnectionStringBuilder { ConnectionString = input });
			AddTypedConverter<SecureString, string>(input =>
			{
				var bstr = Marshal.SecureStringToBSTR(input);

				using (bstr.MakeDisposable(Marshal.ZeroFreeBSTR))
				{
					return Marshal.PtrToStringBSTR(bstr);
				}
			});
			AddTypedConverter<string, SecureString>(input => input.ToCharArray().TypedTo<char[], SecureString>());
			AddTypedConverter<char[], SecureString>(input =>
			{
				var s = new SecureString();

				foreach (var c in input)
					s.AppendChar(c);

				return s;
			});
			AddTypedConverter<byte[], SecureString>(input =>
			{
				var charArray = new char[input.Length / 2];

				var offset = 0;
				for (var i = 0; i < input.Length; i += 2)
					charArray[offset++] = BitConverter.ToChar(new[] { input[i], input[i + 1] }, 0);

				return charArray.TypedTo<char[], SecureString>();
			});


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
			AddTypedConverter<string, bool>(bool.Parse);
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
		}

		public static void AddTypedConverter<TFrom, TTo>(Func<TFrom, TTo> converter)
		{
			if (converter == null)
				throw new ArgumentNullException(nameof(converter));

			var key = Tuple.Create(typeof(TFrom), typeof(TTo));

			_typedConverters.Add(key, converter);
			_typedConverters2.Add(key, input => converter((TFrom)input));
		}

		public static Func<TFrom, TTo> GetTypedConverter<TFrom, TTo>()
		{
			return (Func<TFrom, TTo>)_typedConverters[Tuple.Create(typeof(TFrom), typeof(TTo))];
		}

		public static Func<object, object> GetTypedConverter(Type from, Type to)
		{
			return (Func<object, object>)_typedConverters[Tuple.Create(from, to)];
		}

		public static string GetHost(this EndPoint endPoint)
		{
			if (endPoint == null)
				throw new ArgumentNullException(nameof(endPoint));

			if (endPoint is IPEndPoint)
			{
				return ((IPEndPoint)endPoint).Address.ToString();
			}
			else if (endPoint is DnsEndPoint)
			{
				return ((DnsEndPoint)endPoint).Host;
			}
			else
				throw new InvalidOperationException("Unknown endpoint {0}.".Put(endPoint));
		}

		public static int GetPort(this EndPoint endPoint)
		{
			if (endPoint == null)
				throw new ArgumentNullException(nameof(endPoint));

			if (endPoint is IPEndPoint)
			{
				return ((IPEndPoint)endPoint).Port;
			}
			else if (endPoint is DnsEndPoint)
			{
				return ((DnsEndPoint)endPoint).Port;
			}
			else
				throw new InvalidOperationException("Unknown endpoint {0}.".Put(endPoint));
		}

		public static TTo TypedTo<TFrom, TTo>(this TFrom from)
		{
			return GetTypedConverter<TFrom, TTo>()(from);
		}

		/// <summary>
		/// Convert value into a instance of <paramref name="destinationType"/>.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="destinationType">Type of the destination.</param>
		/// <returns>Converted object.</returns>
		public static object To(this object value, Type destinationType)
		{
			if (destinationType == null)
				throw new ArgumentNullException(nameof(destinationType));

			try
			{
				if (value == null)
				{
					if ((destinationType.IsValueType || destinationType.IsEnum()) && !destinationType.IsNullable())
						throw new ArgumentNullException(nameof(value));

					return null;
				}

				Func<object, object> typedConverter;

				if (_typedConverters2.TryGetValue(Tuple.Create(value.GetType(), destinationType), out typedConverter))
					return typedConverter(value);

				var sourceType = value.GetType();

				if (destinationType.IsAssignableFrom(sourceType))
					return value;
				else if ((value is string || sourceType.IsPrimitive) && destinationType.IsEnum())
				{
					if (value is string)
						return Enum.Parse(destinationType, (string)value, true);
					else
						return Enum.ToObject(destinationType, value);
				}
#if !SILVERLIGHT
				else if (value is string && destinationType == typeof(DbProviderFactory))
					return DbProviderFactories.GetFactory((string)value);
#endif
				else if (destinationType == typeof(Type[]))
				{
					if (!(value is IEnumerable<object>))
						value = new[] { value };

					return ((IEnumerable<object>)value).Select(arg => arg?.GetType() ?? typeof(void)).ToArray();
				}
				else if (value is Stream && destinationType == typeof(string))
				{
					return value.To<byte[]>().To<string>();
				}
				else if (value is Stream && destinationType == typeof(byte[]))
				{
					var stream = (Stream)value;

					MemoryStream output;

					if (stream is MemoryStream)
						output = (MemoryStream)stream;
					else
					{
						const int buffSize = 1024;
						const int capacity = buffSize * 4;

						output = new MemoryStream(capacity);

						var buffer = new byte[buffSize];

						//int offset = 0;

						while (true)
						{
							int readBytes = stream.Read(buffer, 0, buffSize);
							if (readBytes == 0)
								break;

							output.Write(buffer, 0, readBytes);
							//offset += readBytes;
						}
					}

					return output.ToArray();
				}
				else if (value is byte[] && (destinationType == typeof(Stream) || destinationType == typeof(MemoryStream)))
				{
					var stream = new MemoryStream(((byte[])value).Length);
					stream.Write((byte[])value, 0, stream.Capacity);
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

					if (_typedConverters2.TryGetValue(Tuple.Create(value.GetType(), destinationType), out typedConverter))
						return typedConverter(value);
				}
				else if (value is byte[])
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

					if (_typedConverters2.TryGetValue(Tuple.Create(value.GetType(), destinationType), out typedConverter))
						retVal = typedConverter(value);
					else
						throw new ArgumentException("Can't convert byte array to '{0}'.".Put(destinationType), nameof(value));

					if (enumType != null)
						retVal = Enum.ToObject(enumType, retVal);

					return retVal;
				}
#if !SILVERLIGHT
				else if (value is WinColor && destinationType == typeof(int))
					return ((WinColor)value).ToArgb();
				else if (value is int && destinationType == typeof(WinColor))
				{
					var intValue = (int)value;
					return WinColor.FromArgb((byte)((intValue >> 0x18) & 0xffL), (byte)((intValue >> 0x10) & 0xffL), (byte)((intValue >> 8) & 0xffL), (byte)(intValue & 0xffL));
					//retVal = Color.FromArgb(intValue);
				}
				else if (value is WinColor && destinationType == typeof(string))
					return ColorTranslator.ToHtml((WinColor)value);
				else if (value is string && destinationType == typeof(WinColor))
					return ColorTranslator.FromHtml((string)value);
#endif
				else if (value is WpfColor && destinationType == typeof(int))
				{
					var color = (WpfColor)value;
					return (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
				}
				else if (value is int && destinationType == typeof(WpfColor))
				{
					var intValue = (int)value;
					return WpfColor.FromArgb((byte)(intValue >> 24), (byte)(intValue >> 16), (byte)(intValue >> 8), (byte)(intValue));
				}
				else if (value is WpfColor && destinationType == typeof(string))
					return ((WpfColor)value).ToString();
				else if (value is string && destinationType == typeof(WpfColor))
#if SILVERLIGHT
				{
					//this would be initialized somewhere else, I assume
					var hex = (string)value;

					//strip out any # if they exist
					hex = hex.Replace("#", string.Empty);

					var r = (byte)(Convert.ToUInt32(hex.Substring(0, 2), 16));
					var g = (byte)(Convert.ToUInt32(hex.Substring(2, 2), 16));
					var b = (byte)(Convert.ToUInt32(hex.Substring(4, 2), 16));

					retVal = Color.FromArgb(255, r, g, b);
				}
#else
					return WpfColorConverter.ConvertFromString((string)value);
#endif
				else if (value is Uri && destinationType == typeof(string))
					return value.ToString();
				else if (value is string && destinationType == typeof(Uri))
					return new Uri((string)value);
				else if (value is Version && destinationType == typeof(string))
					return value.ToString();
				else if (value is string && destinationType == typeof(Version))
					return new Version((string)value);
				else if (value is int && destinationType == typeof(IntPtr))
					return new IntPtr((int)value);
				else if (value is long && destinationType == typeof(IntPtr))
					return new IntPtr((long)value);
				else if (value is uint && destinationType == typeof(UIntPtr))
					return new UIntPtr((uint)value);
				else if (value is ulong && destinationType == typeof(UIntPtr))
					return new UIntPtr((ulong)value);
				else if (value is IntPtr && destinationType == typeof(int))
					return ((IntPtr)value).ToInt32();
				else if (value is IntPtr && destinationType == typeof(long))
					return ((IntPtr)value).ToInt64();
				else if (value is UIntPtr && destinationType == typeof(uint))
					return ((UIntPtr)value).ToUInt32();
				else if (value is UIntPtr && destinationType == typeof(ulong))
					return ((UIntPtr)value).ToUInt64();
#if !SILVERLIGHT
				else if (value is CultureInfo && destinationType == typeof(int))
					return ((CultureInfo)value).LCID;
				else if (value is int && destinationType == typeof(CultureInfo))
					return new CultureInfo((int)value);
#endif
				else if (destinationType.GetUnderlyingType() != null)
				{
					if (value is string && (string)value == string.Empty)
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
				else if (value is string && destinationType == typeof(TimeSpan))
					return TimeSpan.Parse((string)value);
				else if (value is TimeSpan && destinationType == typeof(string))
					return value.ToString();
				else if (value is DateTime && destinationType == typeof(string))
				{
					var date = (DateTime)value;
					return date.Millisecond > 0 ? date.ToString("o") : value.ToString();
				}
				else if (value is DateTimeOffset && destinationType == typeof(string))
				{
					var dto = (DateTimeOffset)value;
					return dto.Millisecond > 0 ? dto.ToString("o") : value.ToString();
				}
				else if (value is string && destinationType == typeof(DateTimeOffset))
				{
					return DateTimeOffset.Parse((string)value);
				}
#if !SILVERLIGHT
				else if (value is string && destinationType == typeof(TimeZoneInfo))
					return TimeZoneInfo.FromSerializedString((string)value);
				else if (value is TimeZoneInfo && destinationType == typeof(string))
					return ((TimeZoneInfo)value).ToSerializedString();
#endif
				else if (value is string && destinationType == typeof(Guid))
					return new Guid((string)value);
				else if (value is Guid && destinationType == typeof(string))
					return value.ToString();
				else if (value is string && destinationType == typeof(XDocument))
					return XDocument.Parse((string)value);
				else if (value is string && destinationType == typeof(XElement))
					return XElement.Parse((string)value);
				else if (value is XNode && destinationType == typeof(string))
					return value.ToString();
				else if (value is string && destinationType == typeof(XmlDocument))
				{
					var doc = new XmlDocument();
					doc.LoadXml((string)value);
					return doc;
				}
				else if (value is XmlNode && destinationType == typeof(string))
					return ((XmlNode)value).OuterXml;
				else if (value is string && destinationType == typeof(decimal))
					return decimal.Parse((string)value, NumberStyles.Any, null);
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

					var convertible = value as IConvertible;

					if (convertible != null)
					{
						return Convert.ChangeType(value, destinationType, null);
					}
					else
					{
						//var key = Tuple.Create(sourceType, destinationType);

						MethodInfo method;

						//if (!_castOperators.TryGetValue(key, out method))
						{
							var methods = sourceType.GetMethods(BindingFlags.Public | BindingFlags.Static);
							method = methods.FirstOrDefault(mi => (mi.Name == "op_Implicit" || mi.Name == "op_Explicit") && mi.ReturnType == destinationType);
							//_castOperators.Add(key, method);
						}

						if (method != null)
							return method.Invoke(null, new[] { value });
					}
				}

				throw new ArgumentException("Can't convert {0} to type '{1}'.".Put(value, destinationType), nameof(value));
			}
			catch (Exception ex)
			{
				throw new InvalidCastException("Cannot convert {0} to {1}.".Put(value, destinationType), ex);
			}
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

		public static void AddAlias(Type type, string name)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			_aliases.Add(name, type);

			List<string> aliases;

			if (!_aliasesByValue.TryGetValue(type, out aliases))
			{
				aliases = new List<string>();
				_aliasesByValue.Add(type, aliases);
			}

			aliases.Add(name);
		}

		public static string GetAlias(Type type)
		{
			List<string> aliases;
			return _aliasesByValue.TryGetValue(type, out aliases) ? aliases.FirstOrDefault() : null;
		}

		public static T DoInCulture<T>(this CultureInfo cultureInfo, Func<T> func)
		{
			if (cultureInfo == null)
				throw new ArgumentNullException(nameof(cultureInfo));

			if (func == null)
				throw new ArgumentNullException(nameof(func));

			var prevCi = Thread.CurrentThread.CurrentCulture;
			Thread.CurrentThread.CurrentCulture = cultureInfo;

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
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			cultureInfo.DoInCulture<object>(() =>
			{
				action();
				return null;
			});
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
				throw new ArgumentOutOfRangeException(nameof(radix), radix, "The radix must be >= 2 and <= {0}.".Put(digits.Length));

			if (decimalNumber == 0)
				return "0";

			var index = bitsInLong - 1;
			var currentNumber = Math.Abs(decimalNumber);
			var charArray = new char[bitsInLong];

			while (currentNumber != 0)
			{
				var remainder = (int)(currentNumber % radix);
				charArray[index--] = digits[remainder];
				currentNumber = currentNumber / radix;
			}

			var result = new string(charArray, index + 1, bitsInLong - index - 1);
			
			if (decimalNumber < 0)
				result = "-" + result;

			return result;
		}

		public static byte[] ChangeOrder(this byte[] bytes, int length, bool isLittleEndian)
		{
			if (isLittleEndian == BitConverter.IsLittleEndian)
				return bytes;

			for (var i = 0; i < length / 2; i++)
			{
				var temp = bytes[i];
				bytes[i] = bytes[length - i - 1];
				bytes[length - i - 1] = temp;
			}

			return bytes;
		}
	}
}