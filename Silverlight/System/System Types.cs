namespace System
{
	using System.Reflection;
	using System.Reflection.Emit;

	public class SerializableAttribute : Attribute
	{
	}

	public interface ICloneable
	{
		object Clone();
	}

	public class ArgumentOutOfRangeExceptionEx : ArgumentOutOfRangeException
	{
		public ArgumentOutOfRangeExceptionEx(string param)
			: base(param)
		{
		}

		public ArgumentOutOfRangeExceptionEx(string param, string message)
			: base(param, message)
		{
		}

		public ArgumentOutOfRangeExceptionEx(string param, object actualValue, string message)
			: base(param, message)
		{
		}
	}

	public enum NormalizationForm
	{
		FormC,
		FormD,
		FormKC,
		FormKD,
	}

	public static class SystemHelper
	{
		public static AssemblyBuilder DefineDynamicAssembly(this AppDomain a, AssemblyName name, AssemblyBuilderAccess access, string dir)
		{
			throw new NotSupportedException();
		}

		public static string Normalize(this string s, NormalizationForm f)
		{
			throw new NotSupportedException();
		}
	}

	namespace Diagnostics
	{
		public class Stopwatch
		{
			private DateTime _beginTime;

			public TimeSpan Elapsed { get; private set; }

			public void Start()
			{
				_beginTime = DateTime.Now;
			}

			public void Stop()
			{
				this.Elapsed = DateTime.Now - _beginTime;
			}
		}
	}

	namespace Collections
	{
		namespace Specialized
		{
			using System.Collections.Generic;

			public class NameValueCollection : IEnumerable
			{
				private readonly Dictionary<string, string> _inner = new Dictionary<string, string>();

				public void Add(string key, string value)
				{
					_inner.Add(key , value);
				}

				public string this[string index]
				{
					get
					{
						string value;
						_inner.TryGetValue(index, out value);
						return value;
					}
					set { _inner[index] = value; }
				}

				public string[] GetValues(string key)
				{
					return new List<string>(_inner.Keys).ToArray();
				}

				public int Count
				{
					get { return _inner.Count; }
				}

				#region IEnumerable Members

				IEnumerator IEnumerable.GetEnumerator()
				{
					return _inner.Keys.GetEnumerator();
				}

				#endregion
			}
		}

		public class ArrayList
		{
		}

		namespace Generic
		{
			public class SynchronizedCollection<T> : IList<T>, IList
			{
				// Fields
				private List<T> items;
				private object sync;

				// Methods
				public SynchronizedCollection()
				{
					this.items = new List<T>();
					this.sync = new object();
				}

				public SynchronizedCollection(object syncRoot)
				{
					if (syncRoot == null)
					{
						throw new ArgumentNullException("syncRoot");
					}
					this.items = new List<T>();
					this.sync = syncRoot;
				}

				public SynchronizedCollection(object syncRoot, IEnumerable<T> list)
				{
					if (syncRoot == null)
					{
						throw new ArgumentNullException("syncRoot");
					}
					if (list == null)
					{
						throw new ArgumentNullException("list");
					}
					this.items = new List<T>(list);
					this.sync = syncRoot;
				}

				public SynchronizedCollection(object syncRoot, params T[] list)
				{
					if (syncRoot == null)
					{
						throw new ArgumentNullException("syncRoot");
					}
					if (list == null)
					{
						throw new ArgumentNullException("list");
					}
					this.items = new List<T>(list.Length);
					for (int i = 0; i < list.Length; i++)
					{
						this.items.Add(list[i]);
					}
					this.sync = syncRoot;
				}

				public void Add(T item)
				{
					lock (this.sync)
					{
						int count = this.items.Count;
						this.InsertItem(count, item);
					}
				}

				public void Clear()
				{
					lock (this.sync)
					{
						this.ClearItems();
					}
				}

				protected virtual void ClearItems()
				{
					this.items.Clear();
				}

				public bool Contains(T item)
				{
					lock (this.sync)
					{
						return this.items.Contains(item);
					}
				}

				public void CopyTo(T[] array, int index)
				{
					lock (this.sync)
					{
						this.items.CopyTo(array, index);
					}
				}

				public IEnumerator<T> GetEnumerator()
				{
					lock (this.sync)
					{
						return this.items.GetEnumerator();
					}
				}

				public int IndexOf(T item)
				{
					lock (this.sync)
					{
						return this.InternalIndexOf(item);
					}
				}

				public void Insert(int index, T item)
				{
					lock (this.sync)
					{
						if ((index < 0) || (index > this.items.Count))
						{
							throw new ArgumentOutOfRangeException("index");
						}
						this.InsertItem(index, item);
					}
				}

				protected virtual void InsertItem(int index, T item)
				{
					this.items.Insert(index, item);
				}

				private int InternalIndexOf(T item)
				{
					int count = this.items.Count;
					for (int i = 0; i < count; i++)
					{
						if (object.Equals(this.items[i], item))
						{
							return i;
						}
					}
					return -1;
				}

				public bool Remove(T item)
				{
					lock (this.sync)
					{
						int index = this.InternalIndexOf(item);
						if (index < 0)
						{
							return false;
						}
						this.RemoveItem(index);
						return true;
					}
				}

				public void RemoveAt(int index)
				{
					lock (this.sync)
					{
						if ((index < 0) || (index >= this.items.Count))
						{
							throw new ArgumentOutOfRangeException("index");
						}
						this.RemoveItem(index);
					}
				}

				protected virtual void RemoveItem(int index)
				{
					this.items.RemoveAt(index);
				}

				protected virtual void SetItem(int index, T item)
				{
					this.items[index] = item;
				}

				void ICollection.CopyTo(Array array, int index)
				{
					lock (this.sync)
					{
						this.items.CopyTo((T[])array, index);
					}
				}

				IEnumerator IEnumerable.GetEnumerator()
				{
					return this.items.GetEnumerator();
				}

				int IList.Add(object value)
				{
					SynchronizedCollection<T>.VerifyValueType(value);
					lock (this.sync)
					{
						this.Add((T)value);
						return (this.Count - 1);
					}
				}

				bool IList.Contains(object value)
				{
					SynchronizedCollection<T>.VerifyValueType(value);
					return this.Contains((T)value);
				}

				int IList.IndexOf(object value)
				{
					SynchronizedCollection<T>.VerifyValueType(value);
					return this.IndexOf((T)value);
				}

				void IList.Insert(int index, object value)
				{
					SynchronizedCollection<T>.VerifyValueType(value);
					this.Insert(index, (T)value);
				}

				void IList.Remove(object value)
				{
					SynchronizedCollection<T>.VerifyValueType(value);
					this.Remove((T)value);
				}

				private static void VerifyValueType(object value)
				{
					if (value == null)
					{
						if (typeof(T).IsValueType)
						{
							throw new ArgumentException("SynchronizedCollectionWrongTypeNull");
						}
					}
					else if (!(value is T))
					{
						throw new ArgumentException("SynchronizedCollectionWrongType1");
					}
				}

				// Properties
				public int Count
				{
					get
					{
						lock (this.sync)
						{
							return this.items.Count;
						}
					}
				}

				public T this[int index]
				{
					get
					{
						lock (this.sync)
						{
							return this.items[index];
						}
					}
					set
					{
						lock (this.sync)
						{
							if ((index < 0) || (index >= this.items.Count))
							{
								throw new ArgumentOutOfRangeException("index");
							}
							this.SetItem(index, value);
						}
					}
				}

				protected List<T> Items
				{
					get
					{
						return this.items;
					}
				}

				public object SyncRoot
				{
					get
					{
						return this.sync;
					}
				}

				bool ICollection<T>.IsReadOnly
				{
					get
					{
						return false;
					}
				}

				bool ICollection.IsSynchronized
				{
					get
					{
						return true;
					}
				}

				object ICollection.SyncRoot
				{
					get
					{
						return this.sync;
					}
				}

				bool IList.IsFixedSize
				{
					get
					{
						return false;
					}
				}

				bool IList.IsReadOnly
				{
					get
					{
						return false;
					}
				}

				object IList.this[int index]
				{
					get
					{
						return this[index];
					}
					set
					{
						SynchronizedCollection<T>.VerifyValueType(value);
						this[index] = (T)value;
					}
				}
			}

			public class SortedDictionary<K, V> : IDictionary<K, V>
			{
				public SortedDictionary()
				{
				}

				public SortedDictionary(IComparer<K> comparer)
				{
					
				}

				public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
				{
					throw new NotImplementedException();
				}

				IEnumerator IEnumerable.GetEnumerator()
				{
					return GetEnumerator();
				}

				public void Add(KeyValuePair<K, V> item)
				{
					throw new NotImplementedException();
				}

				public void Clear()
				{
					throw new NotImplementedException();
				}

				public bool Contains(KeyValuePair<K, V> item)
				{
					throw new NotImplementedException();
				}

				public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
				{
					throw new NotImplementedException();
				}

				public bool Remove(KeyValuePair<K, V> item)
				{
					throw new NotImplementedException();
				}

				public int Count
				{
					get { throw new NotImplementedException(); }
				}

				public bool IsReadOnly
				{
					get { throw new NotImplementedException(); }
				}

				public bool ContainsKey(K key)
				{
					throw new NotImplementedException();
				}

				public void Add(K key, V value)
				{
					throw new NotImplementedException();
				}

				public bool Remove(K key)
				{
					throw new NotImplementedException();
				}

				public bool TryGetValue(K key, out V value)
				{
					throw new NotImplementedException();
				}

				public V this[K key]
				{
					get { throw new NotImplementedException(); }
					set { throw new NotImplementedException(); }
				}

				public ICollection<K> Keys
				{
					get { throw new NotImplementedException(); }
				}

				public ICollection<V> Values
				{
					get { throw new NotImplementedException(); }
				}
			}
		}
	}

	namespace ComponentModel
	{
		public class PropertyDescriptorCollection
		{
			public virtual PropertyDescriptorCollection Sort(string[] names)
			{
				throw new NotSupportedException();
			}
		}
	}

	namespace Data
	{
		public enum DbType
		{
			AnsiString = 0,
			AnsiStringFixedLength = 0x16,
			Binary = 1,
			Boolean = 3,
			Byte = 2,
			Currency = 4,
			Date = 5,
			DateTime = 6,
			DateTime2 = 0x1a,
			DateTimeOffset = 0x1b,
			Decimal = 7,
			Double = 8,
			Guid = 9,
			Int16 = 10,
			Int32 = 11,
			Int64 = 12,
			Object = 13,
			SByte = 14,
			Single = 15,
			String = 0x10,
			StringFixedLength = 0x17,
			Time = 0x11,
			UInt16 = 0x12,
			UInt32 = 0x13,
			UInt64 = 20,
			VarNumeric = 0x15,
			Xml = 0x19
		}

		namespace Common
		{
			public class DbProviderFactory
			{
			}

			public class DbProviderFactories
			{
				public static DbProviderFactory GetFactory(string s)
				{
					throw new NotSupportedException();
				}
			}
		}
	}

	namespace Drawing
	{
		using Windows.Media;

		public static class ColorHelper
		{
			public static int ToArgb(this Color color)
			{
				return (color.A << 24) | (color.R << 16) | (color.G << 8) | (color.B);
			}
		}

		public static class ColorTranslator
		{
			public static Color FromHtml(string hex)
			{
				if (string.IsNullOrEmpty(hex))
					throw new ArgumentNullException("hex");

				//remove the # at the front
				hex = hex.Replace("#", "");

				byte a = 255;

				int start = 0;

				//handle ARGB strings (8 characters long)
				if (hex.Length == 8)
				{
					a = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
					start = 2;
				}

				//convert RGB characters to bytes
				var r = byte.Parse(hex.Substring(start, 2), System.Globalization.NumberStyles.HexNumber);
				var g = byte.Parse(hex.Substring(start + 2, 2), System.Globalization.NumberStyles.HexNumber);
				var b = byte.Parse(hex.Substring(start + 4, 2), System.Globalization.NumberStyles.HexNumber);

				return Color.FromArgb(a, r, g, b);
			}

			public static string ToHtml(Color color)
			{
				throw new NotImplementedException();
			}
		}
	}

	namespace Globalization
	{
		using System.Text.RegularExpressions;

		public static class TextInfoHelper
		{
			public static string ToTitleCase(this TextInfo ti, string value)
			{
				return Regex.Replace(value, @"w+", (m) =>
				{
					var tmp = m.Value;
					return char.ToUpper(tmp[0]) + tmp.Substring(1, tmp.Length - 1).ToLower();
				});
			}
		}
	}

	namespace Xml
	{
		public class XmlNode
		{
			public string OuterXml { get; set; }
		}

		public class XmlDocument : XmlNode
		{
			public void LoadXml(string xml)
			{
			}
		}
	}

	namespace Runtime
	{
		namespace Serialization
		{
			public class FormatterServices
			{
				public static object GetUninitializedObject(Type type)
				{
					throw new NotImplementedException();
				}
			}
		}
	}

	namespace Net
	{
		namespace Sockets
		{
			using System.IO;
			using System.Threading;

			public class NetworkStream : Stream
			{
				private readonly Socket _socket;
				private readonly bool _ownSocket;

				public NetworkStream(Socket socket)
					: this(socket, false)
				{
				}

				public NetworkStream(Socket socket, bool ownSocket)
				{
					if (socket == null)
						throw new ArgumentNullException("socket");

					_socket = socket;
					_ownSocket = ownSocket;
				}

				public override bool CanRead
				{
					get { return true; }
				}

				public override bool CanSeek
				{
					get { return false; }
				}

				public override bool CanWrite
				{
					get { return false; }
				}

				public override void Flush()
				{
					//throw new NotImplementedException();
				}

				public override long Length
				{
					get { throw new NotSupportedException(); }
				}

				public override long Position
				{
					get
					{
						throw new NotSupportedException();
					}
					set
					{
						throw new NotSupportedException();
					}
				}

				public override int Read(byte[] buffer, int offset, int count)
				{
					//System.Diagnostics.Debug.WriteLine("offset = " + offset);
					//System.Diagnostics.Debug.WriteLine("count = " + count);

					using (var waitEvent = new AutoResetEvent(false))
					{
						using (var arg = new SocketAsyncEventArgs())
						{
							arg.SetBuffer(buffer, offset, count);
							arg.Completed += (sender, e) => waitEvent.Set();

							_socket.ReceiveAsync(arg);
							waitEvent.WaitOne();

							//Debug.WriteLine("Error = " + arg.SocketError);

							if (arg.SocketError != SocketError.Success)
								throw new InvalidOperationException(arg.SocketError.ToString());

							//System.Diagnostics.Debug.WriteLine("BytesTransferred = " + arg.BytesTransferred);

							//var str = "";
							//foreach (var b in innerBuffer)
							//    str += b + ",";

							//System.Diagnostics.Debug.WriteLine("Response(C-i) = " + str);

							//if (arg.BytesTransferred < count)
							//{
							//    if (arg.BytesTransferred == 0)
							//        throw new InvalidOperationException("Server returned zero bytes.");

							//    offset += arg.BytesTransferred;
							//    Read(buffer, offset, count - arg.BytesTransferred);
							//}

							//return arg.Buffer.Length;

							return arg.BytesTransferred;
						}
					}
				}

				public override long Seek(long offset, SeekOrigin origin)
				{
					throw new NotSupportedException();
				}

				public override void SetLength(long value)
				{
					throw new NotSupportedException();
				}

				public override void Write(byte[] buffer, int offset, int count)
				{
					//System.Diagnostics.Debug.WriteLine("offset = " + offset);
					//System.Diagnostics.Debug.WriteLine("count = " + count);

					using (var waitEvent = new AutoResetEvent(false))
					{
						using (var arg = new SocketAsyncEventArgs())
						{
							arg.SetBuffer(buffer, offset, count);
							arg.Completed += (sender, e) => waitEvent.Set();

							_socket.SendAsync(arg);
							waitEvent.WaitOne();

							if (arg.SocketError != SocketError.Success)
								throw new InvalidOperationException(arg.SocketError.ToString());

							if (arg.BytesTransferred < count)
							{
								if (arg.BytesTransferred == 0)
									throw new InvalidOperationException("Client sent zero bytes.");

								offset += arg.BytesTransferred;
								Write(buffer, offset, count - arg.BytesTransferred);
							}
						}
					}
				}

				protected override void Dispose(bool disposing)
				{
					if (_ownSocket)
						_socket.Dispose();

					base.Dispose(disposing);
				}
			}
		}
	}

	namespace Web
	{
		using System.Text;
		using System.Collections.Specialized;

		public static class HttpUtility
		{
			public static string UrlEncode(string url, Encoding e)
			{
				return System.Windows.Browser.HttpUtility.UrlEncode(url);
			}

			public static string UrlDecode(string url, Encoding e)
			{
				return System.Windows.Browser.HttpUtility.UrlDecode(url);
			}

			public static NameValueCollection ParseQueryString(string query, Encoding encoding)
			{
				if (query == null)
				{
					throw new ArgumentNullException("query");
				}
				if (encoding == null)
				{
					throw new ArgumentNullException("encoding");
				}
				if ((query.Length > 0) && (query[0] == '?'))
				{
					query = query.Substring(1);
				}

				var collection = new NameValueCollection();

				int num = query.Length;
				for (int i = 0; i < num; i++)
				{
					int startIndex = i;
					int num4 = -1;
					while (i < num)
					{
						char ch = query[i];
						if (ch == '=')
						{
							if (num4 < 0)
							{
								num4 = i;
							}
						}
						else if (ch == '&')
						{
							break;
						}
						i++;
					}
					string str = null;
					string str2;
					if (num4 >= 0)
					{
						str = query.Substring(startIndex, num4 - startIndex);
						str2 = query.Substring(num4 + 1, (i - num4) - 1);
					}
					else
					{
						str2 = query.Substring(startIndex, i - startIndex);
					}
					//if (urlencoded)
					//{
					//    collection.Add(HttpUtility.UrlDecode(str, encoding), HttpUtility.UrlDecode(str2, encoding));
					//}
					//else
					{
						collection.Add(str, str2);
					}
					if ((i == (num - 1)) && (query[i] == '&'))
					{
						collection.Add(null, string.Empty);
					}
				}

				return collection;
			}
		}

		namespace UI
		{
			namespace WebControls
			{
				public enum SortDirection
				{
					Ascending = 0,
					Descending = 1,
				}
			}
		}

		namespace Configuration
		{
			public static class WebConfigurationManager
			{
				public static System.Configuration.Configuration OpenWebConfiguration(string s)
				{
					throw new NotSupportedException();
				}
			}
		}

		public sealed class HttpContext
		{
			public static HttpContext Current;
		}

		public sealed class HttpRuntime
		{
			public static string AppDomainAppVirtualPath;
		}
	}

	namespace Security
	{
		public sealed class SecureString
		{
			public void AppendChar(char c)
			{
				throw new NotSupportedException();
			}
		}
	}

	namespace Text
	{
		public static class EncodingHelper
		{
			public static string GetString(this Encoding e, byte[] b)
			{
				return e.GetString(b, 0, b.Length);
			}
		}
	}

	namespace Configuration
	{
		using System.Collections.Generic;

		public class ConfigurationSection
		{
			public object this[string index]
			{
				get { throw new NotSupportedException(); }
				set { throw new NotSupportedException(); }
			}
		}

		public class ConfigurationSectionCollection : List<ConfigurationSection>
		{
		}

		public class ConfigurationSectionGroup
		{
			public ConfigurationSectionCollection Sections { get; set; }
			public ConfigurationSectionGroupCollection SectionGroups { get; set; }
		}

		public class ConfigurationSectionGroupCollection : List<ConfigurationSectionGroup>
		{
		}

		public sealed class Configuration
		{
			public ConfigurationSectionCollection Sections { get; set; }
			public ConfigurationSectionGroupCollection SectionGroups { get; set; }

			public ConfigurationSection GetSection(string s)
			{
				throw new NotSupportedException();
			}

			public ConfigurationSectionGroup GetSectionGroup(string s)
			{
				throw new NotSupportedException();
			}
		}
	}

	namespace Reflection
	{
		using System.Collections.Generic;

		public sealed class CustomAttributeData
		{
			public CustomAttributeNamedArgument[] NamedArguments;
			public ConstructorInfo Constructor;
			public IList<CustomAttributeTypedArgument> ConstructorArguments;

			public static IList<CustomAttributeData> GetCustomAttributes(MemberInfo target)
			{
				throw new NotSupportedException();
			}

			public static IList<CustomAttributeData> GetCustomAttributes(ParameterInfo target)
			{
				throw new NotSupportedException();
			}
		}

		public struct CustomAttributeNamedArgument
		{
			public MemberInfo MemberInfo;
			public CustomAttributeTypedArgument TypedValue;
		}

		public class CustomAttributeTypedArgument
		{
			public object Value;
		}

		namespace Emit
		{
			public static class EmitHelper
			{
				public static void Save(this AssemblyBuilder builder, string name)
				{
					throw new NotSupportedException();
				}

				public static ModuleBuilder DefineDynamicModule(this AssemblyBuilder builder, string name)
				{
					throw new NotSupportedException();
				}
			}
		}
	}

	namespace Windows
	{
		namespace Threading
		{
			public enum DispatcherPriority
			{
				Normal
			}

			public static class DispatcherHelper
			{
				public static void BeginInvoke(this Dispatcher dispatcher, Delegate dlg, DispatcherPriority priority)
				{
					dispatcher.BeginInvoke(dlg);
				}
			}
		}
	}
}