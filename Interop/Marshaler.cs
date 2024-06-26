namespace Ecng.Interop
{
	using System;
	using System.ComponentModel;
	using System.Text;
	using System.Runtime.InteropServices;

	using Ecng.Common;

#if NETCOREAPP
	using CoreNativeLib = System.Runtime.InteropServices.NativeLibrary;
#endif

	/// <summary>
	/// Provides a collection of extended methods, that manipulate with class <see cref="Marshal"/>.
	/// </summary>
	public static class Marshaler
	{
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

		/// <summary>
		/// Marshals data from a managed object to an unmanaged block of memory.
		/// </summary>
		/// <param name="structure">A managed object holding the data to be marshaled. This object must be an instance of a formatted class.</param>
		/// <param name="size"></param>
		/// <returns>A pointer to an unmanaged block of memory.</returns>
		public static IntPtr StructToPtr<T>(this T structure, int? size = default)
			where T : struct
			=> structure.StructToPtrEx(size).ptr;

		public static (IntPtr ptr, int size) StructToPtrEx<T>(this T structure, int? size = default)
			where T : struct
		{
			size ??= typeof(T).SizeOf();
			var ptr = Marshal.AllocHGlobal(size.Value);
			Marshal.StructureToPtr(structure, ptr, false);
			return (ptr, size.Value);
		}

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
				throw new ArgumentException(typeof(T).Name, nameof(value));
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
				throw new ArgumentException(typeof(T).Name, nameof(ptr));

			return retVal.To<T>();
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

		public static string ToAnsi(this IntPtr ptr) => Marshal.PtrToStringAnsi(ptr);
		public static string ToAnsi(this IntPtr ptr, int len) => Marshal.PtrToStringAnsi(ptr, len);
		public static IntPtr FromAnsi(this string str) => Marshal.StringToHGlobalAnsi(str);

		public static string ToAuto(this IntPtr ptr) => Marshal.PtrToStringAuto(ptr);
		public static string ToAuto(this IntPtr ptr, int len) => Marshal.PtrToStringAuto(ptr, len);
		public static IntPtr FromAuto(this string str) => Marshal.StringToHGlobalAuto(str);

		public static string ToBSTR(this IntPtr ptr) => Marshal.PtrToStringBSTR(ptr);
		public static IntPtr FromBSTR(this string str) => Marshal.StringToBSTR(str);

		public static string ToUnicode(this IntPtr ptr) => Marshal.PtrToStringUni(ptr);
		public static string ToUnicode(this IntPtr ptr, int len) => Marshal.PtrToStringUni(ptr, len);
		public static IntPtr FromUnicode(this string str) => Marshal.StringToHGlobalUni(str);

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
				throw new ArgumentException($"Error load library '{dllPath}'.", nameof(dllPath), new Win32Exception());

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

			throw new ArgumentException($"Error load procedure {procName}.", nameof(procName), new Win32Exception());
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