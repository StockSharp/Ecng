namespace Ecng.Interop
{
	using System;
	using System.Runtime.InteropServices;

	using Ecng.Common;

	/// <summary>
	/// Generic version of structure <see cref="T:System.Runtime.InteropServices.GCHandle"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class GCHandle<T> : Wrapper<GCHandle>
	{
		private readonly int? _size;

		/// <summary>
		/// Initializes a new instance of the <see cref="GCHandle{T}"/> class.
		/// </summary>
		/// <param name="value">The native value.</param>
		/// <param name="type">One of the <see cref="GCHandleType"/> values, indicating the type of <see cref="GCHandle"/> to create.</param>
		public GCHandle(byte[] value, GCHandleType type = GCHandleType.Pinned)
			: this(value.To<T>(), type, value.Length)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GCHandle{T}"/> class.
		/// </summary>
		/// <param name="value">The native value.</param>
		/// <param name="type">One of the <see cref="GCHandleType"/> values, indicating the type of <see cref="GCHandle"/> to create.</param>
		/// <param name="size">Size of the <paramref name="value"/>.</param>
		public GCHandle(T value, GCHandleType type, int? size)
			: this(GCHandle.Alloc(value, type))
		{
			if (size < 0)
				throw new ArgumentOutOfRangeException(nameof(size));

			_size = size;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GCHandle{T}"/> class.
		/// </summary>
		public GCHandle(GCHandle handle)
			: base(handle)
		{
		}

		public SafePointer CreatePointer() => new SafePointer(Value.AddrOfPinnedObject(), _size);

		/// <summary>
		/// Disposes the native values.
		/// </summary>
		protected override void DisposeNative()
		{
			Value.Free();
			base.DisposeNative();
		}

		/// <summary>
		/// Creates a new object that is a copy of the current instance.
		/// </summary>
		/// <returns>
		/// A new object that is a copy of this instance.
		/// </returns>
		public override Wrapper<GCHandle> Clone()
		{
			throw new NotSupportedException();
		}
	}
}