namespace Ecng.Interop
{
	using System;
#if !SILVERLIGHT
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
#endif
	using System.Text;
	using System.Runtime.InteropServices;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Localization;
	using Ecng.Serialization;

#if NETCOREAPP
	using System.IO;

	using CoreNativeLib = System.Runtime.InteropServices.NativeLibrary;
#endif

	/// <summary>
	/// Provides a collection of extended methods, that manipulate with class <see cref="Marshal"/>.
	/// </summary>
	public static class Marshaler
	{
#if !SILVERLIGHT
		#region Private Fields

		private static readonly List<object> _wrappers = new();

		/// <summary>
		/// For Newton collision's caching policy./
		/// </summary>
		private static readonly Dictionary<SafeHandle, object> _nativeObjects = new();

		#endregion
#endif

		/// <summary>
		/// Marshals data from an unmanaged block of memory to a newly allocated managed object of the specified type.
		/// </summary>
		/// <param name="ptr">A pointer to an unmanaged block of memory.</param>
		/// <returns>A managed object containing the data pointed to by the ptr parameter.</returns>
		public static T ToStruct<T>(this IntPtr ptr)
			where T : struct
		{
			return (T)Marshal.PtrToStructure(ptr, typeof(T));
		}

#if !SILVERLIGHT
		/// <summary>
		/// Marshals data from a managed object to an unmanaged block of memory.
		/// </summary>
		/// <param name="structure">A managed object holding the data to be marshaled. This object must be an instance of a formatted class.</param>
		/// <param name="size"></param>
		/// <returns>A pointer to an unmanaged block of memory.</returns>
		public static IntPtr StructToPtr<T>(this T structure, int? size = null)
			where T : struct
		{
			var ptr = Marshal.AllocHGlobal(size ?? typeof(T).SizeOf());
			Marshal.StructureToPtr(structure, ptr, false);
			return ptr;
		}

		/// <summary>
		/// Wraps the specified COM object in an object of the specified type.
		/// </summary>
		/// <param name="target">The object to be wrapped.</param>
		/// <returns>The newly wrapped object.</returns>
		public static T Wrapper<T>(this object target)
		{
			return (T)Marshal.CreateWrapperOfType(target, typeof(T));
		}

		/// <summary>
		/// Obtains a running instance of the specified object from the Running Object Table (ROT).
		/// </summary>
		/// <param name="progId">The ProgID of the object being requested.</param>
		/// <returns>The object requested. You can cast this object to any COM interface that it supports.</returns>
		public static T GetActiveObject<T>(string progId)
		{
#if NETCOREAPP || NETSTANDARD
			throw new PlatformNotSupportedException();
#else
			return (T)Marshal.GetActiveObject(progId);
#endif
		}

		public static int ReleaseComObject(this object comObject)
		{
			return Marshal.ReleaseComObject(comObject);
		}
#endif

		/// <summary>
		/// Writes a value to unmanaged memory.
		/// </summary>
		/// <param name="ptr">The address in unmanaged memory from which to write.</param>
		/// <param name="value">The value to write.</param>
		public static void Write<T>(this IntPtr ptr, T value)
			where T : struct
		{
			if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte))
				Marshal.WriteByte(ptr, value.To<byte>());
			else if (typeof(T) == typeof(short) || typeof(T) == typeof(ushort))
				Marshal.WriteInt16(ptr, value.To<short>());
			else if (typeof(T) == typeof(int) || typeof(T) == typeof(uint))
				Marshal.WriteInt32(ptr, value.To<int>());
			else if (typeof(T) == typeof(long) || typeof(T) == typeof(ulong))
				Marshal.WriteInt64(ptr, value.To<long>());
			else if (typeof(T) == typeof(IntPtr) || typeof(T) == typeof(UIntPtr))
				Marshal.WriteIntPtr(ptr, value.To<IntPtr>());
			else
				throw new ArgumentException(nameof(ptr));
		}

		/// <summary>
		/// Reads a value from an unmanaged pointer.
		/// </summary>
		/// <param name="ptr">The address in unmanaged memory from which to read.</param>
		/// <returns>The value read from the ptr parameter.</returns>
		public static T Read<T>(this IntPtr ptr)
			where T : struct
		{
			object retVal;

			if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte))
				retVal = Marshal.ReadByte(ptr);
			else if (typeof(T) == typeof(short) || typeof(T) == typeof(ushort))
				retVal = Marshal.ReadInt16(ptr);
			else if (typeof(T) == typeof(int) || typeof(T) == typeof(uint))
				retVal = Marshal.ReadInt32(ptr);
			else if (typeof(T) == typeof(long) || typeof(T) == typeof(ulong))
				retVal = Marshal.ReadInt64(ptr);
			else if (typeof(T) == typeof(IntPtr) || typeof(T) == typeof(UIntPtr))
				retVal = Marshal.ReadIntPtr(ptr);
			else
				throw new ArgumentException(nameof(ptr));

			return retVal.To<T>();
		}

		/// <summary>
		/// Wraps the delegate for prevent garbage collection.
		/// </summary>
		/// <param name="delegate">The @delegate.</param>
		/// <returns></returns>
		public static T WrapDelegate<T>(T @delegate)
		{
			var handle = new GCHandle<T>(@delegate, GCHandleType.Normal, null);
			_wrappers.Add(handle);
			return @delegate;
		}

		/// <summary>
		/// Converts an unmanaged function pointer to a delegate.
		/// </summary>
		/// <typeparam name="T">The type of the delegate to be returned.</typeparam>
		/// <param name="ptr">An <see cref="IntPtr"/> type that is the unmanaged function pointer to be converted.</param>
		/// <returns>A delegate instance that can be cast to the appropriate delegate type.</returns>
		public static T GetDelegateForFunctionPointer<T>(this IntPtr ptr)
		{
			return Marshal.GetDelegateForFunctionPointer(ptr, typeof(T)).To<T>();
		}

		/// <summary>
		/// Registers the specified native object.
		/// </summary>
		/// <param name="handle">The handle of native object.</param>
		/// <param name="nativeObject">The native object.</param>
		public static void RegisterObject(this SafeHandle handle, object nativeObject)
		{
			if (handle is null)
				throw new ArgumentNullException(nameof(handle));

			if (nativeObject is null)
				throw new ArgumentNullException(nameof(nativeObject));

			_nativeObjects.SafeAdd(handle, key => nativeObject);
		}

		/// <summary>
		/// Gets the registered native object. If object for <paramref name="handle"/> isn't registered, return null.
		/// </summary>
		/// <param name="handle">The handle.</param>
		/// <returns>The registered native object.</returns>
		public static T GetObject<T>(this SafeHandle handle)
		{
			if (handle is null)
				throw new ArgumentNullException(nameof(handle));

			return _nativeObjects.TryGetValue(handle).To<T>();
		}

		/// <summary>
		/// Gets the cached safe handle for the specified IntPtr.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns></returns>
		public static SafeHandle GetSafeHandle(this IntPtr value)
		{
			return _nativeObjects.Keys.First(handle => handle.DangerousGetHandle() == value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ptr"></param>
		/// <returns></returns>
		public static string ToAnsi(this IntPtr ptr)
		{
			return Marshal.PtrToStringAnsi(ptr);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ptr"></param>
		/// <param name="len"></param>
		/// <returns></returns>
		public static string ToAnsi(this IntPtr ptr, int len)
		{
			return Marshal.PtrToStringAnsi(ptr, len);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static IntPtr FromAnsi(this string str)
		{
			return Marshal.StringToHGlobalAnsi(str);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ptr"></param>
		/// <returns></returns>
		public static string ToAuto(this IntPtr ptr)
		{
			return Marshal.PtrToStringAuto(ptr);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ptr"></param>
		/// <param name="len"></param>
		/// <returns></returns>
		public static string ToAuto(this IntPtr ptr, int len)
		{
			return Marshal.PtrToStringAuto(ptr, len);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static IntPtr FromAuto(this string str)
		{
			return Marshal.StringToHGlobalAuto(str);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ptr"></param>
		/// <returns></returns>
		public static string ToBSTR(this IntPtr ptr)
		{
			return Marshal.PtrToStringBSTR(ptr);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static IntPtr FromBSTR(this string str)
		{
			return Marshal.StringToBSTR(str);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ptr"></param>
		/// <returns></returns>
		public static string ToUnicode(this IntPtr ptr)
		{
			return Marshal.PtrToStringUni(ptr);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ptr"></param>
		/// <param name="len"></param>
		/// <returns></returns>
		public static string ToUnicode(this IntPtr ptr, int len)
		{
			return Marshal.PtrToStringUni(ptr, len);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static IntPtr FromUnicode(this string str)
		{
			return Marshal.StringToHGlobalUni(str);
		}

		public static HGlobalSafeHandle ToHGlobal(this int ptr)
		{
			return ((IntPtr)ptr).ToHGlobal();
		}

		public static HGlobalSafeHandle ToHGlobal(this IntPtr ptr)
		{
			return new HGlobalSafeHandle(Marshal.AllocHGlobal(ptr));
		}

		public static HGlobalSafeHandle ToHGlobal(this Encoding encoding, string data)
		{
			if (encoding is null)
				throw new ArgumentNullException(nameof(encoding));

			var dataEncoded = encoding.GetBytes(data);
			var size = typeof(byte).SizeOf() * dataEncoded.Length;
			var pData = size.ToHGlobal();

			Marshal.Copy(dataEncoded, 0, pData.DangerousGetHandle(), dataEncoded.Length);

			return pData;
		}

		public static string ToString(this Encoding encoding, IntPtr pData)
		{
			if (encoding is null)
				throw new ArgumentNullException(nameof(encoding));

			var errStr = pData.ToAnsi();
			var length = errStr.Length;

			var data = new byte[length];
			Marshal.Copy(pData, data, 0, length);

			return encoding.GetString(data);
		}

		public static T GetHandler<T>(this IntPtr library, string procName)
			=> GetDelegateForFunctionPointer<T>(GetProcAddress(library, procName));

		public static T TryGetHandler<T>(this IntPtr library, string procName)
			where T : Delegate
		{
			if (TryGetProcAddress(library, procName, out var address))
				return GetDelegateForFunctionPointer<T>(address);

			return null;
		}

#if !NETCOREAPP
		[DllImport("kernel32", SetLastError = true, CallingConvention = CallingConvention.StdCall, EntryPoint = "LoadLibrary")]
		private static extern IntPtr Kernel32LoadLibrary([In] string dllname);

		[DllImport("kernel32", SetLastError = true, CallingConvention = CallingConvention.StdCall, EntryPoint = "FreeLibrary")]
		private static extern bool Kernel32FreeLibrary([In] IntPtr hModule);

		[DllImport("kernel32", SetLastError = true, CallingConvention = CallingConvention.StdCall, EntryPoint = "GetProcAddress")]
		private static extern IntPtr Kernel32GetProcAddress([In] IntPtr hModule, [In] string procName);
#endif

		public static IntPtr LoadLibrary(string dllPath)
		{
			if (dllPath.IsEmpty())
				throw new ArgumentNullException(nameof(dllPath));

#if NETCOREAPP
			var handler = CoreNativeLib.Load(dllPath);
#else
			var handler = Kernel32LoadLibrary(dllPath);
#endif

			if (handler == IntPtr.Zero)
				throw new ArgumentException("Error load library {0}.".Translate().Put(dllPath), nameof(dllPath), new Win32Exception());

			return handler;
		}

		public static bool FreeLibrary(this IntPtr hModule)
		{
#if NETCOREAPP
			CoreNativeLib.Free(hModule);
			return true;
#else
			return Kernel32FreeLibrary(hModule);
#endif
		}

		public static IntPtr GetProcAddress(this IntPtr hModule, string procName)
		{
			if (TryGetProcAddress(hModule, procName, out var addr))
				return addr;

			throw new ArgumentException("Error load procedure {0}.".Translate().Put(procName), nameof(procName), new Win32Exception());
		}

		public static bool TryGetProcAddress(this IntPtr hModule, string procName, out IntPtr address)
		{
#if NETCOREAPP
			return CoreNativeLib.TryGetExport(hModule, procName, out address);
#else
			address = Kernel32GetProcAddress(hModule, procName);
			return address != IntPtr.Zero;
#endif
		}

		public static unsafe string GetUnsafeString(this Encoding encoding, ref byte srcChar, int maxBytes)
		{
			if (encoding is null)
				throw new ArgumentNullException(nameof(encoding));

			if (maxBytes < 0)
				throw new ArgumentOutOfRangeException(nameof(maxBytes));

			if (srcChar == 0)
				return null;

			var charBuffer = stackalloc char[maxBytes];

			fixed (byte* ptr8 = &srcChar)
			{
				encoding.GetChars(ptr8, maxBytes, charBuffer, maxBytes);
				return new string(charBuffer);
			}
		}

		public static unsafe void SetUnsafeString(this Encoding encoding, ref byte tgtChar, int maxBytes, string value)
		{
			if (encoding is null)
				throw new ArgumentNullException(nameof(encoding));

			if (maxBytes < 0)
				throw new ArgumentOutOfRangeException(nameof(maxBytes));

			if (value.IsEmpty())
				return;

			var charBuffer = stackalloc char[maxBytes];

			fixed (byte* ptr8 = &tgtChar)
			{
				for (var b = 0; b < maxBytes; b++)
					ptr8[b] = 0;

				if (value.Length >= maxBytes)
					throw new ArgumentOutOfRangeException();

				for (var c = 0; c < value.Length; c++)
					charBuffer[c] = value[c];

				encoding.GetBytes(charBuffer, value.Length, ptr8, maxBytes);
			}
		}

		public static void CopyTo(this IntPtr ptr, byte[] buffer)
		{
			ptr.CopyTo(buffer, 0, buffer.Length);
		}

		public static void CopyTo(this IntPtr ptr, byte[] buffer, int offset, int length)
		{
			Marshal.Copy(ptr, buffer, offset, length);
		}

		public static void FreeHGlobal(this IntPtr ptr) => Marshal.FreeHGlobal(ptr);
	}
}