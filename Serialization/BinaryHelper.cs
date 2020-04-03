namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Reflection;
	using System.Runtime.Serialization;
	using System.Text;

	using Ecng.Reflection;
#if !SILVERLIGHT
	using System.Runtime.InteropServices;
#endif

	using Ecng.Common;
	using Ecng.Localization;

	public static unsafe class BinaryHelper
	{
		public static void UndoDispose(this MemoryStream stream)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			stream.SetValue("_isOpen", true);
			stream.SetValue("_writable", true);
			stream.SetValue("_expandable", true);
		}

		public static void WriteBytes(this Stream stream, byte[] bytes, int len, int pos = 0)
		{
			stream.Write(bytes, pos, len);
		}

		public static byte[] ReadBytes(this Stream stream, byte[] buffer, int len, int pos = 0)
		{
			var left = len;

			while (left > 0)
			{
				var read = stream.Read(buffer, pos + (len - left), left);

				if (read <= 0)
					throw new IOException("Stream returned '{0}' bytes.".Translate().Put(read));

				left -= read;
			}

			return buffer;
		}

		public static byte ReadByteEx(this Stream stream, byte[] buffer)
		{
			return stream.ReadBytes(buffer, 1)[0];
		}

		public static void WriteByteEx(this Stream stream, byte[] buffer, byte value)
		{
			buffer[0] = value;
			stream.Write(buffer, 0, 1);
		}

		public static void WriteShort(this Stream stream, byte[] buffer, short value, bool isLittleEndian)
		{
			fixed (byte* b = buffer)
				*((short*)b) = value;

			stream.WriteBytes(buffer.ChangeOrder(2, isLittleEndian), 2);
		}

		public static short ReadShort(this Stream stream, byte[] buffer, bool isLittleEndian)
		{
			return BitConverter.ToInt16(stream.ReadBytes(buffer, 2).ChangeOrder(2, isLittleEndian), 0);
		}
		
		[CLSCompliant(false)]
		public static void WriteUShort(this Stream stream, byte[] buffer, ushort value, bool isLittleEndian)
		{
			fixed (byte* b = buffer)
				*((ushort*)b) = value;

			stream.WriteBytes(buffer.ChangeOrder(2, isLittleEndian), 2);
		}

		[CLSCompliant(false)]
		public static ushort ReadUShort(this Stream stream, byte[] buffer, bool isLittleEndian)
		{
			return BitConverter.ToUInt16(stream.ReadBytes(buffer, 2).ChangeOrder(2, isLittleEndian), 0);
		}

		public static void WriteInt(this Stream stream, byte[] buffer, int value, bool isLittleEndian)
		{
			fixed (byte* b = buffer)
				*((int*)b) = value;

			stream.WriteBytes(buffer.ChangeOrder(4, isLittleEndian), 4);
		}

		public static int ReadInt(this Stream stream, byte[] buffer, bool isLittleEndian)
		{
			return BitConverter.ToInt32(stream.ReadBytes(buffer, 4).ChangeOrder(4, isLittleEndian), 0);
		}

		[CLSCompliant(false)]
		public static void WriteUInt(this Stream stream, byte[] buffer, uint value, bool isLittleEndian)
		{
			fixed (byte* b = buffer)
				*((uint*)b) = value;

			stream.WriteBytes(buffer.ChangeOrder(4, isLittleEndian), 4);
		}

		[CLSCompliant(false)]
		public static uint ReadUInt(this Stream stream, byte[] buffer, bool isLittleEndian)
		{
			return BitConverter.ToUInt32(stream.ReadBytes(buffer, 4).ChangeOrder(4, isLittleEndian), 0);
		}

		public static void WriteLong(this Stream stream, byte[] buffer, long value, bool isLittleEndian, int len = 8)
		{
			fixed (byte* b = buffer)
				*((long*)b) = value;

			stream.WriteBytes(buffer.ChangeOrder(len, isLittleEndian), 8);
		}

		public static long ReadLong(this Stream stream, byte[] buffer, bool isLittleEndian, int len = 8)
		{
			return BitConverter.ToInt64(stream.ReadBytes(buffer, len).ChangeOrder(len, isLittleEndian), 0);
		}

		[CLSCompliant(false)]
		public static void WriteULong(this Stream stream, byte[] buffer, ulong value, bool isLittleEndian, int len = 8)
		{
			fixed (byte* b = buffer)
				*((ulong*)b) = value;

			stream.WriteBytes(buffer.ChangeOrder(len, isLittleEndian), 8);
		}

		[CLSCompliant(false)]
		public static ulong ReadULong(this Stream stream, byte[] buffer, bool isLittleEndian, int len = 8)
		{
			return BitConverter.ToUInt64(stream.ReadBytes(buffer, len).ChangeOrder(4, isLittleEndian), 0);
		}

		// убрать когда перейдем на 4.5 полностью
		private class LeaveOpenStreamReader : StreamReader
		{
			public LeaveOpenStreamReader(Stream stream, Encoding encoding)
				: base(stream, encoding ?? Encoding.UTF8)
			{
				this.SetValue("_closable", false);
			}
		}

		public static IEnumerable<string> EnumerateLines(this Stream stream, Encoding encoding = null)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			using (var sr = new LeaveOpenStreamReader(stream, encoding ?? Encoding.UTF8))
			{
				while (!sr.EndOfStream)
					yield return sr.ReadLine();
			}
		}

		public static void CopySync(this Stream source, Stream destination, int count)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			if (destination == null)
				throw new ArgumentNullException(nameof(destination));

			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			var buffer = new byte[count.Min(short.MaxValue)];
			int read;

			while (count > 0 && (read = source.Read(buffer, 0, buffer.Length.Min(count))) > 0)
			{
				destination.Write(buffer, 0, read);
				count -= read;
			}
		}

		public static void CopyAsync(this Stream source, Stream destination, Action completed, Action<Exception> error)
		{
			source.CopyAsync(destination, (int)source.Length, completed, error);
		}

		public static void CopyAsync(this Stream source, Stream destination, int count, Action completed, Action<Exception> error)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			if (destination == null)
				throw new ArgumentNullException(nameof(destination));

			if (completed == null)
				throw new ArgumentNullException(nameof(completed));

			if (error == null)
				throw new ArgumentNullException(nameof(error));

			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (count == 0)
				completed();
			else
			{
				var buffer = new byte[count];
				var offset = 0;

				AsyncCallback callback = null;
				callback = result =>
				{
					try
					{
						var read = source.EndRead(result);

						if (read > 0)
						{
							destination.BeginWrite(buffer, 0, read, writeResult =>
							{
								offset += read;

								try
								{
									destination.EndWrite(writeResult);

									if (offset < count)
										source.BeginRead(buffer, offset, count - offset, callback, null);
									else
										completed();
								}
								catch (Exception exc)
								{
									error(exc);
								}
							}, null);
						}
						else
							error(new ArgumentException("Insufficient source stream."));
					}
					catch (Exception exc)
					{
						error(exc);
					}
				};

				source.BeginRead(buffer, offset, count, callback, null);
			}
		}

		public static byte[] ReadBuffer(this Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			return stream.ReadBuffer((int)(stream.Length - stream.Position));
		}

		public static byte[] ReadBuffer(this Stream stream, int size)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			if (size < 0)
				throw new ArgumentOutOfRangeException(nameof(size), "Size has negative value '{0}'.".Put(size));

			var buffer = new byte[size];

			if (size == 1)
			{
				var b = stream.ReadByte();
				
				if (b == -1)
					throw new ArgumentException("Insufficient stream size '{0}'.".Put(size), nameof(stream));

				buffer[0] = (byte)b;
			}
			else
			{
				var offset = 0;
				do
				{
					var readBytes = stream.Read(buffer, offset, size - offset);
					
					if (readBytes == 0)
						throw new ArgumentException("Insufficient stream size '{0}'.".Put(size), nameof(stream));

					offset += readBytes;
				}
				while (offset < size);
			}

			return buffer;
		}

		public static IEnumerable<string> ReadLines(this Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			using (var reader = new StreamReader(stream))
			{
				while (reader.Peek() >= 0)
				{
					yield return reader.ReadLine();
				}
			}
		}

		#region Write

		[Obsolete("Use WriteEx method.")]
		public static void Write(this Stream stream, object value)
		{
			stream.WriteEx(value);
		}

		public static void WriteEx(this Stream stream, object value)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			if (value == null)
				throw new ArgumentNullException(nameof(value));

			if (value is Stream s)
				stream.WriteEx((int)s.Length);
			else if (value is byte[] a1)
				stream.WriteEx(a1.Length);
			else if (value is string str)
				stream.WriteEx(str.Length);

			stream.WriteRaw(value);
		}

		#endregion

		public static void WriteRaw(this Stream stream, object value)
		{
			stream.WriteRaw(value.To<byte[]>());
		}

		public static void WriteRaw(this Stream stream, byte[] buffer)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));

			stream.Write(buffer, 0, buffer.Length);
		}

		#region Read

		public static T Read<T>(this Stream stream)
		{
			return (T)stream.Read(typeof(T));
		}

		public static object Read(this Stream stream, Type type)
		{
			int size;

			if (type == typeof(byte[]) || type == typeof(string) || type == typeof(Stream))
				size = stream.Read<int>();
			else
				size = type.SizeOf();

			return stream.Read(type, size);
		}

		public static object Read(this Stream stream, Type type, int size)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			if (type == null)
				throw new ArgumentNullException(nameof(type));

			if (size < 0)
				throw new ArgumentOutOfRangeException(nameof(size), "Size has negative value '{0}'.".Put(size));

			if (size == 0 && !(type == typeof(string) || type == typeof(byte[]) || type == typeof(Stream)))
				throw new ArgumentOutOfRangeException(nameof(size), "Size has zero value.");

			if (type == typeof(string))
				size *= 2;

			if (size > int.MaxValue / 10)
				throw new ArgumentOutOfRangeException(nameof(size), "Size has too big value {0}.".Put(size));

			var buffer = size > 0 ? stream.ReadBuffer(size) : ArrayHelper.Empty<byte>();

			if (type == typeof(byte[]))
				return buffer;
			else
				return buffer.To(type);
		}

		#endregion

		/// <summary>
		/// Returns the size of an unmanaged type in bytes.
		/// </summary>
		/// <typeparam name="T">The Type whose size is to be returned.</typeparam>
		/// <returns>The size of the structure parameter in unmanaged code.</returns>
		public static int SizeOf<T>()
		{
			return SizeOf(typeof(T));
		}

		/// <summary>
		/// Returns the size of an unmanaged type in bytes.
		/// </summary>
		/// <param name="type">The Type whose size is to be returned.</param>
		/// <returns>The size of the structure parameter in unmanaged code.</returns>
		public static int SizeOf(this Type type)
		{
			if (type.IsDateTime())
				type = typeof(long);
			else if (type == typeof(TimeSpan))
				type = typeof(long);
			else if (type.IsEnum())
				type = type.GetEnumBaseType();
			else if (type == typeof(bool))
				type = typeof(byte);
			else if (type == typeof(char))
				type = typeof(short);

#if !SILVERLIGHT
			return Marshal.SizeOf(type);
#else
			if (type == typeof(byte))
				return 1;
			//else if (type == typeof(bool))
			//    return 1;
			//else if (type == typeof(char))
			//    return 2;
			else if (type == typeof(short) || type == typeof(ushort))
				return 2;
			else if (type == typeof(int) || type == typeof(uint))
				return 4;
			else if (type == typeof(long) || type == typeof(ulong))
				return 8;
			else if (type == typeof(float))
				return 4;
			else if (type == typeof(double))
				return 8;
			else if (type == typeof(Guid))
				return 16;
			else
				throw new ArgumentException(type.AssemblyQualifiedName, "type");
#endif
		}

		public static Stream Save(this Stream stream, string fileName)
		{
			var pos = stream.CanSeek ? stream.Position : 0;

			using (var file = File.Create(fileName))
				stream.CopyTo(file);

			if (stream.CanSeek)
				stream.Position = pos;

			return stream;
		}

		public static byte[] Save(this byte[] data, string fileName)
		{
			data.To<Stream>().Save(fileName);
			return data;
		}

		public static bool TrySave(this byte[] data, string fileName, Action<Exception> errorHandler)
		{
			if (errorHandler == null)
				throw new ArgumentNullException(nameof(errorHandler));

			try
			{
				data.To<Stream>().Save(fileName);
				return true;
			}
			catch (Exception e)
			{
				errorHandler(e);
				return false;
			}
		}

		public static void Truncate(this StreamWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException(nameof(writer));

			writer.Flush();
			writer.BaseStream.SetLength(0);
		}

		public static T FromString<T>(this string value)
			where T : MemberInfo
		{
			if (value.IsEmpty())
				throw new ArgumentNullException(nameof(value));

			var parts = value.Split('/');

			var type = parts[0].To<Type>();
			return parts.Length == 1 ? type.To<T>() : type.GetMember<T>(parts[1]);
		}

		public static string ToString<T>(this T member, bool isAssemblyQualifiedName)
			where T : MemberInfo
		{
			if (member == null)
				throw new ArgumentNullException(nameof(member));

			if (member.ReflectedType != null)
				return member.ReflectedType.GetTypeName(isAssemblyQualifiedName) + "/" + member.Name;
			else
				return member.To<Type>().GetTypeName(isAssemblyQualifiedName);
		}

		public static T DeserializeDataContract<T>(this Stream stream)
		{
			return (T)new DataContractSerializer(typeof(T)).ReadObject(stream);
		}

		public static void SerializeDataContract<T>(this Stream stream, T value)
		{
			new DataContractSerializer(typeof(T)).WriteObject(stream, value);
		}
	}
}