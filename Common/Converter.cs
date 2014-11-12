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
		#region Private Fields

		private static readonly Dictionary<Type, DbType> _mappedTypes = new Dictionary<Type, DbType>();
		private static readonly Dictionary<string, Type> _aliases = new Dictionary<string, Type>();
		private static readonly Dictionary<Type, List<string>> _aliasesByValue = new Dictionary<Type, List<string>>();
		private static readonly Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();

		#endregion

		#region Converter.cctor()

		static Converter()
		{
			_mappedTypes.Add(typeof(string), DbType.String);
			_mappedTypes.Add(typeof(char), DbType.String);
			_mappedTypes.Add(typeof(short), DbType.Int16);
			_mappedTypes.Add(typeof(int), DbType.Int32);
			_mappedTypes.Add(typeof(long), DbType.Int64);
			_mappedTypes.Add(typeof(ushort), DbType.UInt16);
			_mappedTypes.Add(typeof(uint), DbType.UInt32);
			_mappedTypes.Add(typeof(ulong), DbType.UInt64);
			_mappedTypes.Add(typeof(float), DbType.Single);
			_mappedTypes.Add(typeof(double), DbType.Double);
			_mappedTypes.Add(typeof(decimal), DbType.Decimal);
			_mappedTypes.Add(typeof(DateTime), DbType.DateTime);
			_mappedTypes.Add(typeof(TimeSpan), DbType.Time);
			_mappedTypes.Add(typeof(Guid), DbType.Guid);
			_mappedTypes.Add(typeof(byte[]), DbType.Binary);
			_mappedTypes.Add(typeof(byte), DbType.Byte);
			_mappedTypes.Add(typeof(sbyte), DbType.SByte);
			_mappedTypes.Add(typeof(bool), DbType.Boolean);
			_mappedTypes.Add(typeof(object), DbType.Object);

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
		}

		#endregion

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

		/// <summary>
		/// Convert value into a instance of <paramref name="destinationType"/>.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="destinationType">Type of the destination.</param>
		/// <returns>Converted object.</returns>
		public static object To(this object value, Type destinationType)
		{
			if (destinationType == null)
				throw new ArgumentNullException("destinationType");

			try
			{
				if (value == null)
				{
					if ((destinationType.IsValueType || destinationType.IsEnum()) && !destinationType.IsNullable())
						throw new ArgumentNullException("value");

					return null;
				}

				object retVal;
				var sourceType = value.GetType();

				if (destinationType.IsAssignableFrom(sourceType))
					retVal = value;
				else if (value is Type && destinationType == typeof(DbType))
				{
					var type = (Type)value;

					if (type.IsNullable())
					{
						type = type.GetGenericArguments()[0];
					}
					if (type.IsEnum())
						type = type.GetEnumBaseType();

					DbType dbType;

					if (_mappedTypes.TryGetValue(type, out dbType))
						retVal = dbType;
					else
						throw new ArgumentException(".NET type {0} doesn't have associated db type.".Put(type));
				}
				else if (value is DbType && destinationType == typeof(Type))
					retVal = _mappedTypes.Values.First(arg => ((DbType)value) == arg);
				else if (value is string && destinationType == typeof(byte[]))
				{
					retVal = Encoding.Unicode.GetBytes((string)value);
				}
				else if (value is byte[] && destinationType == typeof(string))
					retVal = Encoding.Unicode.GetString((byte[])value);
				else if (value is bool[] && destinationType == typeof(BitArray))
				{
					retVal = new BitArray((bool[])value);
				}
				else if (value is BitArray && destinationType == typeof(bool[]))
				{
					var array = (BitArray)value;
					var source = new bool[array.Length];
					array.CopyTo(source, 0);
					retVal = source;
				}
				else if (value is byte[] && destinationType == typeof(BitArray))
				{
					retVal = new BitArray((byte[])value);
				}
				else if (value is BitArray && destinationType == typeof(byte[]))
				{
					var array = (BitArray)value;
					var source = new byte[(int)((double)array.Length / 8).Ceiling()];
					array.CopyTo(source, 0);
					retVal = source;
				}
				else if (value is IPAddress)
				{
					var addr = (IPAddress)value;

					if (destinationType == typeof(string))
						retVal = addr.ToString();
					else if (destinationType == typeof(byte[]))
						retVal = addr.GetAddressBytes();
					else if (destinationType == typeof(long))
					{
						switch (addr.AddressFamily)
						{
							case AddressFamily.InterNetworkV6:
							{
								retVal = addr.ScopeId;
								break;
							}
							case AddressFamily.InterNetwork:
							{
								var byteIp = addr.GetAddressBytes();
								retVal = ((((byteIp[3] << 0x18) | (byteIp[2] << 0x10)) | (byteIp[1] << 8)) | byteIp[0]) & (0xffffffff);
								//retVal = BitConverter.ToInt32(addr.GetAddressBytes(), 0);
								break;
							}
							default:
								throw new ArgumentException("Can't convert IPAddress to long.", "value");
						}
					}
					else
						throw new ArgumentException("Can't convert IPAddress to type '{0}'.".Put(destinationType), "value");
				}
				else if (destinationType == typeof(IPAddress))
				{
					if (value is string)
						retVal = IPAddress.Parse((string)value);
					else if (value is byte[])
						retVal = new IPAddress((byte[])value);
					else if (value is long)
						retVal = new IPAddress((long)value);
					else
						throw new ArgumentException("Can't convert type '{0}' to IPAddress.".Put(destinationType), "value");
				}
				else if (value is string && typeof(EndPoint).IsAssignableFrom(destinationType))
				{
					var str = (string)value;
					var index = str.LastIndexOf(':');

					if (index != -1)
					{
						var host = str.Substring(0, index);
						var port = str.Substring(index + 1).To<int>();

						IPAddress addr;

						if (destinationType == typeof(IPEndPoint))
							addr = host.To<IPAddress>();
						else
						{
#if SILVERLIGHT
							addr = host.To<IPAddress>();
#else
							if (!IPAddress.TryParse(host, out addr))
								return new DnsEndPoint(host, port);
#endif
						}

						return new IPEndPoint(addr, port);
					}
					else
						throw new FormatException("Invalid endpoint format.");
				}
				else if (destinationType == typeof(string) && value is EndPoint)
				{
					var endPoint = (EndPoint)value;
					retVal = endPoint.GetHost() + ":" + endPoint.GetPort();
				}
				else if (destinationType.IsEnum() && (value is string || sourceType.IsPrimitive))
				{
					if (value is string)
						retVal = Enum.Parse(destinationType, (string)value, true);
					else
						retVal = Enum.ToObject(destinationType, value);
				}
				else if (value is string && destinationType == typeof(Type))
				{
					Type type;
					var str = (string)value;

					var key = str.ToLowerInvariant();

					if (!_aliases.TryGetValue(key, out type))
					{
						if (!_typeCache.TryGetValue(key, out type))
						{
							lock (_typeCache)
							{
								if (!_typeCache.TryGetValue(key, out type))
								{
									type = Type.GetType(str, false, true);

#if !SILVERLIGHT
									// в строке может быть записаное не AssemblyQualifiedName, а только полное имя типа + имя сборки.
									if (type == null)
									{
										var parts = str.Split(", ");
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
										throw new ArgumentException("Type {0} doesn't exists.".Put(value), "value");
								}
							}
						}
					}

					retVal = type;
				}
				else if (value is Type && destinationType == typeof(string))
					retVal = ((Type)value).AssemblyQualifiedName;
				else if (value is string && destinationType == typeof(StringBuilder))
					retVal = new StringBuilder((string)value);
				else if (value is StringBuilder && destinationType == typeof(string))
					retVal = value.ToString();
#if !SILVERLIGHT
				else if (value is string && destinationType == typeof(DbConnectionStringBuilder))
					retVal = new DbConnectionStringBuilder { ConnectionString = (string)value };
				else if (value is DbConnectionStringBuilder && destinationType == typeof(string))
					retVal = value.ToString();
				else if (value is SecureString && destinationType == typeof(string))
				{
					var bstr = Marshal.SecureStringToBSTR((SecureString)value);

					using (bstr.MakeDisposable(Marshal.ZeroFreeBSTR))
					{
						retVal = Marshal.PtrToStringBSTR(bstr);
					}
				}
#endif
				else if (value is string && destinationType == typeof(SecureString))
					retVal = ((string)value).ToCharArray().To(destinationType);
				else if (value is byte[] && destinationType == typeof(SecureString))
				{
					var byteArray = (byte[])value;

					var charArray = new char[byteArray.Length / 2];

					var offset = 0;
					for (var i = 0; i < byteArray.Length; i += 2)
						charArray[offset++] = BitConverter.ToChar(new[] { byteArray[i], byteArray[i + 1] }, 0);

					retVal = charArray.To(destinationType);
				}
				else if (value is char[] && destinationType == typeof(SecureString))
				{
					var s = new SecureString();

					foreach (var c in (char[])value)
						s.AppendChar(c);

					retVal = s;
				}
				else if (value is string && destinationType == typeof(DbProviderFactory))
					retVal = DbProviderFactories.GetFactory((string)value);
				else if (destinationType == typeof(Type[]))
				{
					if (!(value is IEnumerable<object>))
						value = new[] { value };

					retVal = ((IEnumerable<object>)value).Select(arg => arg == null ? typeof(void) : arg.GetType()).ToArray();
				}
				else if (value is Stream && destinationType == typeof(string))
				{
					retVal = value.To<byte[]>().To<string>();
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

					retVal = output.ToArray();
				}
				else if (value is byte[] && destinationType == typeof(Stream))
				{
					var stream = new MemoryStream(((byte[])value).Length);
					stream.Write((byte[])value, 0, stream.Capacity);
					stream.Position = 0;
					retVal = stream;
				}
				else if (value is string && destinationType == typeof(Stream))
				{
					retVal = value.To<byte[]>().To<Stream>();
				}
				else if (destinationType == typeof(byte[]))
				{
					if (value is Enum)
						value = value.To(sourceType.GetEnumBaseType());

					if (value is byte)
						retVal = new[] { (byte)value };
					else if (value is bool)
						retVal = BitConverter.GetBytes((bool)value);
					else if (value is char)
						retVal = BitConverter.GetBytes((char)value);
					else if (value is short)
						retVal = BitConverter.GetBytes((short)value);
					else if (value is int)
						retVal = BitConverter.GetBytes((int)value);
					else if (value is long)
						retVal = BitConverter.GetBytes((long)value);
					else if (value is ushort)
						retVal = BitConverter.GetBytes((ushort)value);
					else if (value is uint)
						retVal = BitConverter.GetBytes((uint)value);
					else if (value is ulong)
						retVal = BitConverter.GetBytes((ulong)value);
					else if (value is float)
						retVal = BitConverter.GetBytes((float)value);
					else if (value is double)
						retVal = BitConverter.GetBytes((double)value);
					else if (value is DateTime)
						retVal = BitConverter.GetBytes(((DateTime)value).Ticks);
					else if (value is Guid)
						retVal = ((Guid)value).ToByteArray();
					else if (value is TimeSpan)
						retVal = BitConverter.GetBytes(((TimeSpan)value).Ticks);
					else if (value is decimal)
					{
						var bits = decimal.GetBits((decimal)value);
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

						retVal = bytes;
					}
					else
						throw new ArgumentException("Can't convert type '{0}' to byte[].".Put(sourceType), "value");
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

					if (destinationType == typeof(byte))
						retVal = ((byte[])value)[0];
					else if (destinationType == typeof(bool))
						retVal = BitConverter.ToBoolean((byte[])value, 0);
					else if (destinationType == typeof(char))
						retVal = BitConverter.ToChar((byte[])value, 0);
					else if (destinationType == typeof(short))
						retVal = BitConverter.ToInt16((byte[])value, 0);
					else if (destinationType == typeof(int))
						retVal = BitConverter.ToInt32((byte[])value, 0);
					else if (destinationType == typeof(long))
						retVal = BitConverter.ToInt64((byte[])value, 0);
					else if (destinationType == typeof(ushort))
						retVal = BitConverter.ToUInt16((byte[])value, 0);
					else if (destinationType == typeof(uint))
						retVal = BitConverter.ToUInt32((byte[])value, 0);
					else if (destinationType == typeof(ulong))
						retVal = BitConverter.ToUInt64((byte[])value, 0);
					else if (destinationType == typeof(float))
						retVal = BitConverter.ToSingle((byte[])value, 0);
					else if (destinationType == typeof(double))
						retVal = BitConverter.ToDouble((byte[])value, 0);
					else if (destinationType == typeof(DateTime))
						retVal = new DateTime(BitConverter.ToInt64((byte[])value, 0));
					else if (destinationType == typeof(Guid))
						retVal = new Guid((byte[])value);
					else if (destinationType == typeof(TimeSpan))
						retVal = new TimeSpan(BitConverter.ToInt64((byte[])value, 0));
					else if (destinationType == typeof(decimal))
					{
						var bytes = (byte[])value;

						var bits = new[]
						{
							((bytes[0] | (bytes[1] << 8)) | (bytes[2] << 0x10)) | (bytes[3] << 0x18), //lo
							((bytes[4] | (bytes[5] << 8)) | (bytes[6] << 0x10)) | (bytes[7] << 0x18), //mid
							((bytes[8] | (bytes[9] << 8)) | (bytes[10] << 0x10)) | (bytes[11] << 0x18), //hi
							((bytes[12] | (bytes[13] << 8)) | (bytes[14] << 0x10)) | (bytes[15] << 0x18) //flags
						};

						return new decimal(bits);
					}
					else
						throw new ArgumentException("Can't convert byte[] to type '{0}'.".Put(destinationType), "value");

					if (enumType != null)
						retVal = Enum.ToObject(enumType, retVal);
				}
				else if (value is TimeSpan && destinationType == typeof(long))
					retVal = ((TimeSpan)value).Ticks;
				else if (value is long && destinationType == typeof(TimeSpan))
					retVal = new TimeSpan((long)value);
				else if (value is DateTime && destinationType == typeof(long))
					retVal = ((DateTime)value).Ticks;
				else if (value is long && destinationType == typeof(DateTime))
					retVal = new DateTime((long)value);
				else if (value is DateTimeOffset && destinationType == typeof(long))
					retVal = ((DateTimeOffset)value).UtcTicks;
				else if (value is long && destinationType == typeof(DateTimeOffset))
					retVal = new DateTimeOffset((long)value, TimeSpan.Zero);
				else if (value is DateTime && destinationType == typeof(double))
					retVal = ((DateTime)value).ToOADate();
				else if (value is double && destinationType == typeof(DateTime))
					retVal = DateTime.FromOADate((double)value);
#if !SILVERLIGHT
				else if (value is WinColor && destinationType == typeof(int))
					retVal = ((WinColor)value).ToArgb();
				else if (value is int && destinationType == typeof(WinColor))
				{
					var intValue = (int)value;
					retVal = WinColor.FromArgb((byte)((intValue >> 0x18) & 0xffL), (byte)((intValue >> 0x10) & 0xffL), (byte)((intValue >> 8) & 0xffL), (byte)(intValue & 0xffL));
					//retVal = Color.FromArgb(intValue);
				}
				else if (value is WinColor && destinationType == typeof(string))
					retVal = ColorTranslator.ToHtml((WinColor)value);
				else if (value is string && destinationType == typeof(WinColor))
					retVal = ColorTranslator.FromHtml((string)value);
#endif
				else if (value is WpfColor && destinationType == typeof(int))
				{
					var color = (WpfColor)value;
					retVal = (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
				}
				else if (value is int && destinationType == typeof(WpfColor))
				{
					var intValue = (int)value;
					retVal = WpfColor.FromArgb((byte)(intValue >> 24), (byte)(intValue >> 16), (byte)(intValue >> 8), (byte)(intValue));
				}
				else if (value is WpfColor && destinationType == typeof(string))
					retVal = ((WpfColor)value).ToString();
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
					retVal = WpfColorConverter.ConvertFromString((string)value);
#endif
				else if (value is Uri && destinationType == typeof(string))
					retVal = value.ToString();
				else if (value is string && destinationType == typeof(Uri))
					retVal = new Uri((string)value);
				else if (value is Version && destinationType == typeof(string))
					retVal = value.ToString();
				else if (value is string && destinationType == typeof(Version))
					retVal = new Version((string)value);
				else if (value is int && destinationType == typeof(IntPtr))
					retVal = new IntPtr((int)value);
				else if (value is long && destinationType == typeof(IntPtr))
					retVal = new IntPtr((long)value);
				else if (value is uint && destinationType == typeof(UIntPtr))
					retVal = new UIntPtr((uint)value);
				else if (value is ulong && destinationType == typeof(UIntPtr))
					retVal = new UIntPtr((ulong)value);
				else if (value is IntPtr && destinationType == typeof(int))
					retVal = ((IntPtr)value).ToInt32();
				else if (value is IntPtr && destinationType == typeof(long))
					retVal = ((IntPtr)value).ToInt64();
				else if (value is UIntPtr && destinationType == typeof(uint))
					retVal = ((UIntPtr)value).ToUInt32();
				else if (value is UIntPtr && destinationType == typeof(ulong))
					retVal = ((UIntPtr)value).ToUInt64();
#if !SILVERLIGHT
				else if (value is CultureInfo && destinationType == typeof(int))
					retVal = ((CultureInfo)value).LCID;
				else if (value is int && destinationType == typeof(CultureInfo))
					retVal = new CultureInfo((int)value);
#endif
				else if (destinationType.GetUnderlyingType() != null)
				{
					if (value is string && (string)value == string.Empty)
						retVal = destinationType.CreateInstance<object>();
					else
						retVal = destinationType.CreateInstance<object>(value.To(destinationType.GetUnderlyingType()));
				}
				else if (value is string && destinationType == typeof(TimeSpan))
					retVal = TimeSpan.Parse((string)value);
				else if (value is TimeSpan && destinationType == typeof(string))
					retVal = value.ToString();
				else if (value is DateTime && destinationType == typeof(string))
				{
					var date = (DateTime)value;
					retVal = date.Millisecond > 0 ? date.ToString("o") : value.ToString();
				}
#if !SILVERLIGHT
				else if (value is string && destinationType == typeof(TimeZoneInfo))
					retVal = TimeZoneInfo.FromSerializedString((string)value);
				else if (value is TimeZoneInfo && destinationType == typeof(string))
					retVal = ((TimeZoneInfo)value).ToSerializedString();
#endif
				else if (value is string && destinationType == typeof(Guid))
					retVal = new Guid((string)value);
				else if (value is Guid && destinationType == typeof(string))
					retVal = value.ToString();
				else if (value is string && destinationType == typeof(XDocument))
					retVal = XDocument.Parse((string)value);
				else if (value is string && destinationType == typeof(XElement))
					retVal = XElement.Parse((string)value);
				else if (value is XNode && destinationType == typeof(string))
					retVal = value.ToString();
				else if (value is string && destinationType == typeof(XmlDocument))
				{
					var doc = new XmlDocument();
					doc.LoadXml((string)value);
					retVal = doc;
				}
				else if (value is XmlNode && destinationType == typeof(string))
					retVal = ((XmlNode)value).OuterXml;
				else if (value is string && destinationType == typeof(decimal))
					retVal = decimal.Parse((string)value, NumberStyles.Any, null);
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

					try
					{
						retVal = Convert.ChangeType(value, destinationType, null);
					}
					catch
					{
						var methods = sourceType.GetMethods(BindingFlags.Public | BindingFlags.Static);
						var method = methods.FirstOrDefault(mi => (mi.Name == "op_Implicit" || mi.Name == "op_Explicit") && mi.ReturnType == destinationType);

						if (method == null)
							throw;

						retVal = method.Invoke(null, new[] { value });
					}
				}

				return retVal;
			}
			catch (Exception ex)
			{
				throw new InvalidCastException("Cannot convert {0} to type {1}.".Put(value, destinationType), ex);
			}
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

		public static void AddAlias(Type type, string name)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			if (name.IsEmpty())
				throw new ArgumentNullException("name");

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
				throw new ArgumentNullException("cultureInfo");

			if (func == null)
				throw new ArgumentNullException("func");

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
				throw new ArgumentNullException("action");

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
				throw new ArgumentOutOfRangeException("radix", radix, "The radix must be >= 2 and <= {0}.".Put(digits.Length));

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
	}
}