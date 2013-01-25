namespace Ecng.Serialization
{
	using System;
	using System.IO;
#if !SILVERLIGHT
	using System.Runtime.InteropServices;
#endif

	using Ecng.Common;

	public static class BinaryHelper
	{
		public static void Copy(this Stream source, Stream destination, Action completed, Action<Exception> error)
		{
			source.Copy(destination, (int)source.Length, completed, error);
		}

		private sealed class AsyncCopier
		{
			private readonly Stream _source;
			private readonly Stream _destination;
			private readonly int _count;
			private readonly Action _completed;
			private readonly Action<Exception> _error;
			private readonly byte[] _buffer;

			private int _offset;

			public AsyncCopier(Stream source, Stream destination, int count, Action completed, Action<Exception> error)
			{
				if (source == null)
					throw new ArgumentNullException("source");

				if (destination == null)
					throw new ArgumentNullException("destination");

				if (completed == null)
					throw new ArgumentNullException("completed");

				if (error == null)
					throw new ArgumentNullException("error");

				if (count < 0)
					throw new ArgumentOutOfRangeException("count");

				if (count == 0)
					completed();
				else
				{
					_source = source;
					_destination = destination;
					_count = count;
					_completed = completed;
					_error = error;

					_buffer = new byte[_count];
					_source.BeginRead(_buffer, 0, _count, OnBeginReadCallback, null);
				}
			}

			private void OnBeginReadCallback(IAsyncResult result)
			{
				try
				{
					int read = _source.EndRead(result);
					if (read > 0)
					{
						_destination.BeginWrite(_buffer, 0, read, writeResult =>
						{
							_offset += read;

							try
							{
								_destination.EndWrite(writeResult);

								if (_offset < _count)
									_source.BeginRead(_buffer, 0, _count - _offset, OnBeginReadCallback, null);
								else
									_completed();
							}
							catch (Exception exc)
							{
								_error(exc);
							}
						}, null);
					}
					else
						_error(new ArgumentException("Insufficient source stream."));
				}
				catch (Exception exc)
				{
					_error(exc);
				}
			}
		}

		public static void Copy(this Stream source, Stream destination, int count, Action completed, Action<Exception> error)
		{
			new AsyncCopier(source, destination, count, completed, error);
		}

		public static byte[] ReadBuffer(this Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			return stream.ReadBuffer((int)(stream.Length - stream.Position));
		}

		public static byte[] ReadBuffer(this Stream stream, int size)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			if (size < 0)
				throw new ArgumentOutOfRangeException("size", "Size has negative value '{0}'.".Put(size));

			var buffer = new byte[size];

			if (size == 1)
			{
				var @byte = stream.ReadByte();
				
				if (@byte == -1)
					throw new ArgumentException("Insufficient stream size '{0}'.".Put(size), "stream");

				buffer[0] = (byte)@byte;
			}
			else
			{
				var offset = 0;
				do
				{
					var readBytes = stream.Read(buffer, offset, size - offset);
					
					if (readBytes == 0)
						throw new ArgumentException("Insufficient stream size '{0}'.".Put(size), "stream");

					offset += readBytes;
				}
				while (offset < size);
			}

			return buffer;
		}

		#region Write

		public static void Write(this Stream stream, object value)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			if (value == null)
				throw new ArgumentNullException("value");

			if (value is Stream)
				stream.Write((int)((Stream)value).Length);
			else if (value is byte[])
				stream.Write(((byte[])value).Length);
			else if (value is string)
				stream.Write(((string)value).Length);

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
				throw new ArgumentNullException("stream");

			if (buffer == null)
				throw new ArgumentNullException("buffer");

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
				throw new ArgumentNullException("stream");

			if (type == null)
				throw new ArgumentNullException("type");

			if (size < 0)
				throw new ArgumentOutOfRangeException("size", "Size has negative value '{0}'.".Put(size));

			if (size == 0 && !(type == typeof(string) || type == typeof(byte[]) || type == typeof(Stream)))
				throw new ArgumentOutOfRangeException("size", "Size has zero value.");

			if (type == typeof(string))
				size *= 2;

			if (size > int.MaxValue / 10)
				throw new ArgumentOutOfRangeException("size", "Size has too big value {0}.".Put(size));

			var buffer = size > 0 ? stream.ReadBuffer(size) : new byte[0];

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
			if (type == typeof(DateTime))
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
	}
}