namespace Ecng.Interop
{
	using System.Runtime.InteropServices;

	using Ecng.Common;

	/// <summary>
	/// Disposable wrapper for COM objects.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class DisposableComObject<T> : Wrapper<T>
		where T : class
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="comObject"></param>
		public DisposableComObject(T comObject)
			: base(comObject)
		{
		}

		/// <summary>
		/// Disposes the native resources.
		/// </summary>
		protected override void DisposeNative()
		{
			Marshal.ReleaseComObject(base.Value);
			base.DisposeNative();
		}

		/// <summary>
		/// Creates a new object that is a copy of the current instance.
		/// </summary>
		/// <returns>
		/// A new object that is a copy of this instance.
		/// </returns>
		public override Wrapper<T> Clone()
		{
			return new DisposableComObject<T>(base.Value);
		}
	}
}