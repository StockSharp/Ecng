namespace Ecng.Interop
{
	using System;
	using System.Runtime.InteropServices;

	public class PtrReader
	{
		public PtrReader(IntPtr ptr)
		{
			Ptr = ptr;
		}

		private IntPtr _ptr;

		public IntPtr Ptr
		{
			get { return _ptr; }
			set
			{
				if (value == IntPtr.Zero)
					throw new ArgumentNullException("value");

				_ptr = value;
			}
		}

		public byte GetByte()
		{
			var ret = Marshal.ReadByte(_ptr);
			_ptr += sizeof(byte);

			return ret;
		}

		public int GetInt()
		{
			var ret = Marshal.ReadInt32(_ptr);
			_ptr += sizeof(int);

			return ret;
		}

		public long GetLong()
		{
			var ret = Marshal.ReadInt64(_ptr);
			_ptr += sizeof(long);

			return ret;
		}

		public short GetShort()
		{
			var ret = Marshal.ReadInt16(_ptr);
			_ptr += sizeof(short);

			return ret;
		}

		public IntPtr GetIntPtr()
		{
			var ret = Marshal.ReadIntPtr(_ptr);
			_ptr += IntPtr.Size;

			return ret;
		}

		public string GetString()
		{
			var str = _ptr.ToAnsi();
			_ptr += IntPtr.Size;

			return str == null ? string.Empty : str.Trim();
		}

		public string GetString(int length)
		{
			var str = _ptr.ToAnsi(length);

			_ptr += length;

			return str == null ? string.Empty : str.Trim();
		}
	}
}