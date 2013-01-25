namespace Ecng.Interop
{
	#region Using Directives

	using System;
	using System.Runtime.InteropServices;

	using Ecng.Common;

	#endregion

	/// <summary>
	/// Generic version of structure <see cref="T:System.Runtime.InteropServices.GCHandle"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class GCHandle<T> : Wrapper<GCHandle>
	{
		#region GCHandle.ctor()

		/// <summary>
		/// Initializes a new instance of the <see cref="GCHandle{T}"/> class.
		/// </summary>
		/// <param name="value">The native value.</param>
		public GCHandle(T value)
			: base(GCHandle.Alloc(value))
		{
		}

		#endregion

		#region Disposable Members

		/// <summary>
		/// Disposes the native values.
		/// </summary>
		protected override void DisposeNative()
		{
			base.Value.Free();
			base.DisposeNative();
		}

		#endregion

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