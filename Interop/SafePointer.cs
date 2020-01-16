namespace Ecng.Interop
{
	using System;
	using System.Runtime.InteropServices;

	using Ecng.Serialization;

	public struct SafePointer
	{
		private IntPtr _pointer;
		private readonly int? _size;
		private int _origin;

		public SafePointer(IntPtr pointer, int? size)
		{
			if (pointer == default)
				throw new ArgumentOutOfRangeException(nameof(pointer));

			if (size < 0)
				throw new ArgumentOutOfRangeException(nameof(size));

			_pointer = pointer;
			_size = size;
			_origin = 0;
		}

		public IntPtr Pointer => _pointer;

		private void CheckBorder<TStruct>()
			where TStruct : struct
		{
			CheckBorder(typeof(TStruct).SizeOf());
		}

		private void CheckBorder(int offset)
		{
			if (_size != null && (_origin + offset) > _size.Value)
				throw new ArgumentOutOfRangeException(nameof(offset));
		}

		public TStruct ToStruct<TStruct>(bool autoShift = false)
			where TStruct : struct
		{
			CheckBorder<TStruct>();

			var value = _pointer.ToStruct<TStruct>();
			
			if (autoShift)
				Shift<TStruct>();

			return value;
		}

		public void Shift<TStruct>()
			where TStruct : struct
		{
			Shift(typeof(TStruct).SizeOf());
		}

		public void Shift(int offset)
		{
			CheckBorder(offset);

			_origin += offset;
			_pointer += offset;
		}

		public void CopyTo(byte[] buffer, bool autoShift = false)
		{
			CopyTo(buffer, 0, buffer.Length, autoShift);
		}

		public void CopyTo(byte[] buffer, int offset, int length, bool autoShift = false)
		{
			CheckBorder(length);

			Marshal.Copy(Pointer, buffer, offset, length);

			if (autoShift)
				Shift(length);
		}

		public TValue Read<TValue>(bool autoShift = false)
			where TValue : struct
		{
			CheckBorder<TValue>();

			var value = Pointer.Read<TValue>();

			if (autoShift)
				Shift<TValue>();

			return value;
		}

		public static implicit operator IntPtr(SafePointer pointer) => pointer.Pointer;
	}
}