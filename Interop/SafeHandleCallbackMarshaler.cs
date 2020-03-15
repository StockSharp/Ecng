namespace Ecng.Interop
{
    #region Using Directives

	using System;
	using System.Runtime.InteropServices;

	#endregion

	/// <summary>
	/// Special marshaler for marshaling safe handles in native callback functions.
	/// </summary>
    public class SafeHandleCallbackMarshaler : CustomMarshaler<SafeHandle>
    {
        #region SafeHandleCallbackMarshaler.ctor()

		/// <summary>
		/// Initializes a new instance of the <see cref="SafeHandleCallbackMarshaler"/> class.
		/// </summary>
        private SafeHandleCallbackMarshaler()
        {
        }

        #endregion

		#region ICustomMarshaler Members

		private static readonly SafeHandleCallbackMarshaler _instance = new SafeHandleCallbackMarshaler();

		/// <summary>
		/// Gets the instance.
		/// </summary>
		/// <param name="cookie">The cookie.</param>
		/// <returns></returns>
		internal static ICustomMarshaler GetInstance(string cookie)
		{
			return _instance;
		}

		#endregion
 
        #region CustomMarshaler<SafeHandle> Members

		/// <summary>
		/// Called when [marshal managed to native].
		/// </summary>
		/// <param name="managedData">The managed data.</param>
		/// <returns></returns>
        protected override IntPtr OnMarshalManagedToNative(SafeHandle managedData)
        {
            return managedData != null ? managedData.DangerousGetHandle() : IntPtr.Zero;
        }

		/// <summary>
		/// Called when [marshal native to managed].
		/// </summary>
		/// <param name="nativeData">The native data.</param>
		/// <returns></returns>
        protected override SafeHandle OnMarshalNativeToManaged(IntPtr nativeData)
        {
            return nativeData != IntPtr.Zero ? nativeData.GetSafeHandle() : null;
        }

        #endregion
    }
}