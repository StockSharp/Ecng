namespace Ecng.Interop
{
    #region Using Directives

	using System;
	using System.Runtime.InteropServices;

	#endregion

	/// <summary>
	/// Provide custom wrappers for handling method calls.
	/// </summary>
	/// <typeparam name="T"></typeparam>
    public abstract class CustomMarshaler<T> : ICustomMarshaler
    {
        #region ICustomMarshaler Members

		/// <summary>
		/// Cleans the up managed data.
		/// </summary>
		/// <param name="managedData">The managed data.</param>
        void ICustomMarshaler.CleanUpManagedData(object managedData)
        {
            OnCleanUpManagedData((T)managedData);
        }

		/// <summary>
		/// Cleans the up native data.
		/// </summary>
		/// <param name="nativeData">The native data.</param>
        void ICustomMarshaler.CleanUpNativeData(IntPtr nativeData)
        {
            OnCleanUpNativeData(nativeData);
        }

		/// <summary>
		/// Returns the size of the native data to be marshaled.
		/// </summary>
		/// <returns>The size in bytes of the native data.</returns>
        int ICustomMarshaler.GetNativeDataSize()
        {
            return -1;
        }

		/// <summary>
		/// Marshals the managed to native.
		/// </summary>
		/// <param name="managedData">The managed data.</param>
		/// <returns></returns>
        IntPtr ICustomMarshaler.MarshalManagedToNative(object managedData)
        {
            return OnMarshalManagedToNative((T)managedData);
        }

		/// <summary>
		/// Marshals the native to managed.
		/// </summary>
		/// <param name="nativeData">The native data.</param>
		/// <returns></returns>
        object ICustomMarshaler.MarshalNativeToManaged(IntPtr nativeData)
        {
            return OnMarshalNativeToManaged(nativeData);
        }

        #endregion

		/// <summary>
		/// Called when [clean up managed data].
		/// </summary>
		/// <param name="managedData">The managed data.</param>
        protected virtual void OnCleanUpManagedData(T managedData)
        {
        }

		/// <summary>
		/// Called when [clean up native data].
		/// </summary>
		/// <param name="nativeData">The native data.</param>
        protected virtual void OnCleanUpNativeData(IntPtr nativeData)
        {
        }

		/// <summary>
		/// Called when [marshal managed to native].
		/// </summary>
		/// <param name="managedData">The managed data.</param>
		/// <returns></returns>
        protected abstract IntPtr OnMarshalManagedToNative(T managedData);

		/// <summary>
		/// Called when [marshal native to managed].
		/// </summary>
		/// <param name="nativeData">The native data.</param>
		/// <returns></returns>
        protected abstract T OnMarshalNativeToManaged(IntPtr nativeData);
    }
}