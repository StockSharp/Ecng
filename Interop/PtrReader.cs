namespace Ecng.Interop
{
	using System;
	using System.Runtime.InteropServices;

	/// <summary>
	/// Provides methods to read data from an unmanaged memory pointer.
	/// </summary>
	public class PtrReader
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PtrReader"/> class with the specified pointer.
		/// </summary>
		/// <param name="ptr">The unmanaged memory pointer to read from.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="ptr"/> is <see cref="IntPtr.Zero"/>.</exception>
		public PtrReader(IntPtr ptr)
		{
			Ptr = ptr;
		}

		private IntPtr _ptr;

		/// <summary>
		/// Gets or sets the unmanaged memory pointer.
		/// </summary>
		/// <exception cref="ArgumentNullException">Thrown if the value is <see cref="IntPtr.Zero"/>.</exception>
		public IntPtr Ptr
		{
			get { return _ptr; }
			set
			{
				if (value == IntPtr.Zero)
					throw new ArgumentNullException(nameof(value));

				_ptr = value;
			}
		}

		/// <summary>
		/// Reads a byte from the current pointer and advances the pointer by the size of a byte.
		/// </summary>
		/// <returns>The byte read from the pointer.</returns>
		public byte GetByte()
		{
			var ret = Marshal.ReadByte(_ptr);
			_ptr += sizeof(byte);

			return ret;
		}

		/// <summary>
		/// Reads a 32-bit integer from the current pointer and advances the pointer by the size of an integer.
		/// </summary>
		/// <returns>The 32-bit integer read from the pointer.</returns>
		public int GetInt()
		{
			var ret = Marshal.ReadInt32(_ptr);
			_ptr += sizeof(int);

			return ret;
		}

		/// <summary>
		/// Reads a 64-bit integer from the current pointer and advances the pointer by the size of a long.
		/// </summary>
		/// <returns>The 64-bit integer read from the pointer.</returns>
		public long GetLong()
		{
			var ret = Marshal.ReadInt64(_ptr);
			_ptr += sizeof(long);

			return ret;
		}

		/// <summary>
		/// Reads a 16-bit integer from the current pointer and advances the pointer by the size of a short.
		/// </summary>
		/// <returns>The 16-bit integer read from the pointer.</returns>
		public short GetShort()
		{
			var ret = Marshal.ReadInt16(_ptr);
			_ptr += sizeof(short);

			return ret;
		}

		/// <summary>
		/// Reads an <see cref="IntPtr"/> from the current pointer and advances the pointer by the size of an <see cref="IntPtr"/>.
		/// </summary>
		/// <returns>The <see cref="IntPtr"/> read from the pointer.</returns>
		public IntPtr GetIntPtr()
		{
			var ret = Marshal.ReadIntPtr(_ptr);
			_ptr += IntPtr.Size;

			return ret;
		}

		/// <summary>
		/// Reads a null-terminated ANSI string from the current pointer and advances the pointer by the size of an <see cref="IntPtr"/>.
		/// </summary>
		/// <returns>The ANSI string read from the pointer. If the string is null, an empty string is returned.</returns>
		public string GetString()
		{
			var str = _ptr.ToAnsi();
			_ptr += IntPtr.Size;

			return str is null ? string.Empty : str.Trim();
		}

		/// <summary>
		/// Reads an ANSI string of the specified length from the current pointer and advances the pointer by the specified length.
		/// </summary>
		/// <param name="length">The length of the string to read.</param>
		/// <returns>The ANSI string read from the pointer. If the string is null, an empty string is returned.</returns>
		public string GetString(int length)
		{
			var str = _ptr.ToAnsi(length);

			_ptr += length;

			return str is null ? string.Empty : str.Trim();
		}
	}
}