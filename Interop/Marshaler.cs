namespace Ecng.Interop;

using System;
using System.ComponentModel;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Collections.Concurrent;
using System.Linq;

using Ecng.Common;

#if NETCOREAPP
using CoreNativeLib = System.Runtime.InteropServices.NativeLibrary;
#endif

/// <summary>
/// Provides a collection of extended methods that manipulate and extend the functionality of the <see cref="Marshal"/> class for interoperating with unmanaged memory and libraries.
/// </summary>
public unsafe static class Marshaler
{
	/// <summary>
	/// Marshals data from an unmanaged block of memory to a newly allocated managed object of the specified type.
	/// </summary>
	/// <typeparam name="T">The type of the structure to marshal. Must be a value type.</typeparam>
	/// <param name="ptr">The pointer to the unmanaged block of memory.</param>
	/// <returns>A managed object of type <typeparamref name="T"/> containing the data from the unmanaged memory.</returns>
	public static T ToStruct<T>(this IntPtr ptr)
		where T : struct
	{
		return (T)Marshal.PtrToStructure(ptr, typeof(T));
	}

	/// <summary>
	/// Marshals data from a managed object to an unmanaged block of memory and returns the pointer.
	/// </summary>
	/// <typeparam name="T">The type of the structure to marshal. Must be a value type.</typeparam>
	/// <param name="structure">The managed object to marshal.</param>
	/// <param name="size">The optional size of the unmanaged memory block. If null, the size of <typeparamref name="T"/> is used.</param>
	/// <returns>A pointer to the allocated unmanaged memory containing the marshaled data.</returns>
	public static IntPtr StructToPtr<T>(this T structure, int? size = default)
		where T : struct
		=> structure.StructToPtrEx(size).ptr;

	/// <summary>
	/// Marshals data from a managed object to an unmanaged block of memory and returns the pointer along with the size.
	/// </summary>
	/// <typeparam name="T">The type of the structure to marshal. Must be a value type.</typeparam>
	/// <param name="structure">The managed object to marshal.</param>
	/// <param name="size">The optional size of the unmanaged memory block. If null, the size of <typeparamref name="T"/> is used.</param>
	/// <returns>A tuple containing the pointer to the unmanaged memory and its size in bytes.</returns>
	public static (IntPtr ptr, int size) StructToPtrEx<T>(this T structure, int? size = default)
		where T : struct
	{
		size ??= typeof(T).SizeOf();
		var ptr = Marshal.AllocHGlobal(size.Value);
		Marshal.StructureToPtr(structure, ptr, false);
		return (ptr, size.Value);
	}

	/// <summary>
	/// Writes a value to the specified unmanaged memory location.
	/// </summary>
	/// <typeparam name="T">The type of the value to write. Must be a supported primitive type (e.g., byte, short, int, long, IntPtr).</typeparam>
	/// <param name="ptr">The address in unmanaged memory to write to.</param>
	/// <param name="value">The value to write.</param>
	/// <exception cref="ArgumentException">Thrown when <typeparamref name="T"/> is not a supported type.</exception>
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
	/// Reads a value from the specified unmanaged memory location.
	/// </summary>
	/// <typeparam name="T">The type of the value to read. Must be a supported primitive type (e.g., byte, short, int, long, IntPtr).</typeparam>
	/// <param name="ptr">The address in unmanaged memory to read from.</param>
	/// <returns>The value read from the unmanaged memory, cast to type <typeparamref name="T"/>.</returns>
	/// <exception cref="ArgumentException">Thrown when <typeparamref name="T"/> is not a supported type.</exception>
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
	/// Converts an unmanaged function pointer to a delegate of the specified type.
	/// </summary>
	/// <typeparam name="T">The type of the delegate to create.</typeparam>
	/// <param name="ptr">The unmanaged function pointer to convert.</param>
	/// <returns>A delegate of type <typeparamref name="T"/> that wraps the unmanaged function.</returns>
	public static T GetDelegateForFunctionPointer<T>(this IntPtr ptr)
	{
		return Marshal.GetDelegateForFunctionPointer(ptr, typeof(T)).To<T>();
	}

	/// <summary>
	/// Converts an unmanaged ANSI string pointer to a managed string.
	/// </summary>
	/// <param name="ptr">The pointer to the ANSI string in unmanaged memory.</param>
	/// <returns>The managed string representation of the ANSI string.</returns>
	public static string ToAnsi(this IntPtr ptr) => Marshal.PtrToStringAnsi(ptr);

	/// <summary>
	/// Converts an unmanaged ANSI string pointer to a managed string with a specified length.
	/// </summary>
	/// <param name="ptr">The pointer to the ANSI string in unmanaged memory.</param>
	/// <param name="len">The length of the string to read.</param>
	/// <returns>The managed string representation of the ANSI string.</returns>
	public static string ToAnsi(this IntPtr ptr, int len) => Marshal.PtrToStringAnsi(ptr, len);

	/// <summary>
	/// Converts a managed string to an unmanaged ANSI string and returns a pointer to it.
	/// </summary>
	/// <param name="str">The managed string to convert.</param>
	/// <returns>A pointer to the unmanaged ANSI string.</returns>
	public static IntPtr FromAnsi(this string str) => Marshal.StringToHGlobalAnsi(str);

	/// <summary>
	/// Converts an unmanaged string pointer (platform-dependent encoding) to a managed string.
	/// </summary>
	/// <param name="ptr">The pointer to the string in unmanaged memory.</param>
	/// <returns>The managed string representation.</returns>
	public static string ToAuto(this IntPtr ptr) => Marshal.PtrToStringAuto(ptr);

	/// <summary>
	/// Converts an unmanaged string pointer (platform-dependent encoding) to a managed string with a specified length.
	/// </summary>
	/// <param name="ptr">The pointer to the string in unmanaged memory.</param>
	/// <param name="len">The length of the string to read.</param>
	/// <returns>The managed string representation.</returns>
	public static string ToAuto(this IntPtr ptr, int len) => Marshal.PtrToStringAuto(ptr, len);

	/// <summary>
	/// Converts a managed string to an unmanaged string (platform-dependent encoding) and returns a pointer to it.
	/// </summary>
	/// <param name="str">The managed string to convert.</param>
	/// <returns>A pointer to the unmanaged string.</returns>
	public static IntPtr FromAuto(this string str) => Marshal.StringToHGlobalAuto(str);

	/// <summary>
	/// Converts an unmanaged BSTR pointer to a managed string.
	/// </summary>
	/// <param name="ptr">The pointer to the BSTR in unmanaged memory.</param>
	/// <returns>The managed string representation of the BSTR.</returns>
	public static string ToBSTR(this IntPtr ptr) => Marshal.PtrToStringBSTR(ptr);

	/// <summary>
	/// Converts a managed string to an unmanaged BSTR and returns a pointer to it.
	/// </summary>
	/// <param name="str">The managed string to convert.</param>
	/// <returns>A pointer to the unmanaged BSTR.</returns>
	public static IntPtr FromBSTR(this string str) => Marshal.StringToBSTR(str);

	/// <summary>
	/// Converts an unmanaged Unicode string pointer to a managed string.
	/// </summary>
	/// <param name="ptr">The pointer to the Unicode string in unmanaged memory.</param>
	/// <returns>The managed string representation of the Unicode string.</returns>
	public static string ToUnicode(this IntPtr ptr) => Marshal.PtrToStringUni(ptr);

	/// <summary>
	/// Converts an unmanaged Unicode string pointer to a managed string with a specified length.
	/// </summary>
	/// <param name="ptr">The pointer to the Unicode string in unmanaged memory.</param>
	/// <param name="len">The length of the string to read.</param>
	/// <returns>The managed string representation of the Unicode string.</returns>
	public static string ToUnicode(this IntPtr ptr, int len) => Marshal.PtrToStringUni(ptr, len);

	/// <summary>
	/// Converts a managed string to an unmanaged Unicode string and returns a pointer to it.
	/// </summary>
	/// <param name="str">The managed string to convert.</param>
	/// <returns>A pointer to the unmanaged Unicode string.</returns>
	public static IntPtr FromUnicode(this string str) => Marshal.StringToHGlobalUni(str);

	/// <summary>
	/// Allocates unmanaged memory of the specified size and wraps it in a safe handle.
	/// </summary>
	/// <param name="ptr">The size of the memory to allocate, interpreted as an integer.</param>
	/// <returns>A <see cref="HGlobalSafeHandle"/> wrapping the allocated unmanaged memory.</returns>
	public static HGlobalSafeHandle ToHGlobal(this int ptr)
	{
		return ((IntPtr)ptr).ToHGlobal();
	}

	/// <summary>
	/// Allocates unmanaged memory of the specified size and wraps it in a safe handle.
	/// </summary>
	/// <param name="ptr">The size of the memory to allocate, as an <see cref="IntPtr"/>.</param>
	/// <returns>A <see cref="HGlobalSafeHandle"/> wrapping the allocated unmanaged memory.</returns>
	public static HGlobalSafeHandle ToHGlobal(this IntPtr ptr)
	{
		return new HGlobalSafeHandle(Marshal.AllocHGlobal(ptr));
	}

	/// <summary>
	/// Encodes a string using the specified encoding, allocates unmanaged memory for it, and returns a safe handle.
	/// </summary>
	/// <param name="encoding">The encoding to use for the string.</param>
	/// <param name="data">The string to encode and allocate.</param>
	/// <returns>A <see cref="HGlobalSafeHandle"/> wrapping the allocated unmanaged memory containing the encoded string.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="encoding"/> is null.</exception>
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

	/// <summary>
	/// Decodes an unmanaged ANSI string from a pointer into a managed string using the specified encoding.
	/// </summary>
	/// <param name="encoding">The encoding to use for decoding the string.</param>
	/// <param name="pData">The pointer to the ANSI string in unmanaged memory.</param>
	/// <returns>The decoded managed string.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="encoding"/> is null.</exception>
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

	/// <summary>
	/// Retrieves a delegate for a named procedure from an unmanaged library.
	/// </summary>
	/// <typeparam name="T">The type of the delegate to retrieve.</typeparam>
	/// <param name="library">The handle to the loaded unmanaged library.</param>
	/// <param name="procName">The name of the procedure to retrieve.</param>
	/// <returns>A delegate of type <typeparamref name="T"/> for the specified procedure.</returns>
	/// <exception cref="ArgumentException">Thrown when the procedure cannot be found in the library.</exception>
	public static T GetHandler<T>(this IntPtr library, string procName)
		=> GetDelegateForFunctionPointer<T>(GetProcAddress(library, procName));

	/// <summary>
	/// Attempts to retrieve a delegate for a named procedure from an unmanaged library.
	/// </summary>
	/// <typeparam name="T">The type of the delegate to retrieve. Must inherit from <see cref="Delegate"/>.</typeparam>
	/// <param name="library">The handle to the loaded unmanaged library.</param>
	/// <param name="procName">The name of the procedure to retrieve.</param>
	/// <returns>A delegate of type <typeparamref name="T"/> if found; otherwise, <c>null</c>.</returns>
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

	/// <summary>
	/// Loads an unmanaged library from the specified path.
	/// </summary>
	/// <param name="dllPath">The file path to the unmanaged library (DLL).</param>
	/// <returns>A handle to the loaded library.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="dllPath"/> is null or empty.</exception>
	/// <exception cref="ArgumentException">Thrown when the library cannot be loaded.</exception>
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

	/// <summary>
	/// Frees a previously loaded unmanaged library.
	/// </summary>
	/// <param name="hModule">The handle to the library to free.</param>
	/// <returns><c>true</c> if the library was successfully freed; otherwise, <c>false</c>.</returns>
	public static bool FreeLibrary(this IntPtr hModule)
	{
#if NETCOREAPP
		CoreNativeLib.Free(hModule);
		return true;
#else
		return Kernel32FreeLibrary(hModule);
#endif
	}

	/// <summary>
	/// Retrieves the address of a named procedure from an unmanaged library.
	/// </summary>
	/// <param name="hModule">The handle to the loaded unmanaged library.</param>
	/// <param name="procName">The name of the procedure to retrieve.</param>
	/// <returns>The address of the procedure in unmanaged memory.</returns>
	/// <exception cref="ArgumentException">Thrown when the procedure cannot be found in the library.</exception>
	public static IntPtr GetProcAddress(this IntPtr hModule, string procName)
	{
		if (TryGetProcAddress(hModule, procName, out var addr))
			return addr;

		throw new ArgumentException($"Error load procedure {procName}.", nameof(procName), new Win32Exception());
	}

	/// <summary>
	/// Attempts to retrieve the address of a named procedure from an unmanaged library.
	/// </summary>
	/// <param name="hModule">The handle to the loaded unmanaged library.</param>
	/// <param name="procName">The name of the procedure to retrieve.</param>
	/// <param name="address">When this method returns, contains the address of the procedure if found; otherwise, <see cref="IntPtr.Zero"/>.</param>
	/// <returns><c>true</c> if the procedure address was found; otherwise, <c>false</c>.</returns>
	public static bool TryGetProcAddress(this IntPtr hModule, string procName, out IntPtr address)
	{
#if NETCOREAPP
		return CoreNativeLib.TryGetExport(hModule, procName, out address);
#else
		address = Kernel32GetProcAddress(hModule, procName);
		return address != IntPtr.Zero;
#endif
	}

	/// <summary>
	/// Converts an unmanaged byte reference to a managed string using the specified encoding, with a maximum byte length.
	/// </summary>
	/// <param name="encoding">The encoding to use for decoding the string.</param>
	/// <param name="srcChar">A reference to the starting byte in unmanaged memory.</param>
	/// <param name="maxBytes">The maximum number of bytes to read.</param>
	/// <returns>The decoded managed string, or <c>null</c> if the source is zero.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="encoding"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxBytes"/> is negative.</exception>
	public static string GetUnsafeString(this Encoding encoding, ref byte srcChar, int maxBytes)
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

	/// <summary>
	/// Writes a managed string to an unmanaged byte reference using the specified encoding, with a maximum byte length.
	/// </summary>
	/// <param name="encoding">The encoding to use for encoding the string.</param>
	/// <param name="tgtChar">A reference to the target byte in unmanaged memory.</param>
	/// <param name="maxBytes">The maximum number of bytes to write.</param>
	/// <param name="value">The string to write.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="encoding"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when <paramref name="maxBytes"/> is negative or the string length exceeds <paramref name="maxBytes"/>.
	/// </exception>
	public static void SetUnsafeString(this Encoding encoding, ref byte tgtChar, int maxBytes, string value)
	{
		if (encoding is null)
			throw new ArgumentNullException(nameof(encoding));

		if (maxBytes < 0)
			throw new ArgumentOutOfRangeException(nameof(maxBytes));

		if (value.IsEmpty())
			return;

		if (value.Length >= maxBytes)
			throw new ArgumentOutOfRangeException(nameof(maxBytes), maxBytes, "Invalid value.");

		var charBuffer = stackalloc char[maxBytes];

		fixed (byte* ptr8 = &tgtChar)
		{
			for (var b = 0; b < maxBytes; b++)
				ptr8[b] = 0;

			for (var c = 0; c < value.Length; c++)
				charBuffer[c] = value[c];

			encoding.GetBytes(charBuffer, value.Length, ptr8, maxBytes);
		}
	}

	/// <summary>
	/// Copies data from an unmanaged pointer to a byte array.
	/// </summary>
	/// <param name="ptr">The pointer to the unmanaged memory to copy from.</param>
	/// <param name="buffer">The byte array to copy the data into.</param>
	public static void CopyTo(this IntPtr ptr, byte[] buffer)
	{
		ptr.CopyTo(buffer, 0, buffer.Length);
	}

	/// <summary>
	/// Copies a specified amount of data from an unmanaged pointer to a byte array.
	/// </summary>
	/// <param name="ptr">The pointer to the unmanaged memory to copy from.</param>
	/// <param name="buffer">The byte array to copy the data into.</param>
	/// <param name="offset">The starting index in the buffer where data should be copied.</param>
	/// <param name="length">The number of bytes to copy.</param>
	public static void CopyTo(this IntPtr ptr, byte[] buffer, int offset, int length)
	{
		Marshal.Copy(ptr, buffer, offset, length);
	}

	/// <summary>
	/// Frees an unmanaged memory block previously allocated with <see cref="Marshal.AllocHGlobal(IntPtr)"/>.
	/// </summary>
	/// <param name="ptr">The pointer to the unmanaged memory to free.</param>
	public static void FreeHGlobal(this IntPtr ptr) => Marshal.FreeHGlobal(ptr);

	/// <summary>
	/// Copies the contents of the string into unmanaged memory using the specified encoding.
	/// </summary>
	/// <param name="value">The managed string to copy.</param>
	/// <param name="encoding">The encoding to use.</param>
	/// <param name="ptr">A pointer to the target unmanaged memory.</param>
	/// <param name="bytesCount">The maximum size of the unmanaged memory block in bytes.</param>
	public static void FillString(this string value, Encoding encoding, byte* ptr, int bytesCount)
	{
		if (value is null)		throw new ArgumentNullException(nameof(value));
		if (encoding is null)	throw new ArgumentNullException(nameof(encoding));

		if (value.Length == 0)
			return;

		fixed (char* s = value)
			encoding.GetBytes(s, value.Length, ptr, bytesCount);
	}

	/// <summary>
	/// Formats a field's value to a string using the specified encoding.
	/// </summary>
	/// <param name="f">The field information.</param>
	/// <param name="value">The value of the field.</param>
	/// <param name="encoding">The encoding to use when converting byte arrays to strings.</param>
	/// <returns>The formatted string representation of the field value.</returns>
	private static object FormatToString(FieldInfo f, object value, Encoding encoding)
	{
		if (f is null)
			throw new ArgumentNullException(nameof(f));

		var attr = f.GetAttribute<FixedBufferAttribute>();

		if (attr != null)
		{
			using var hdl = new GCHandle<object>(value, GCHandleType.Pinned, null);

			var array = new byte[attr.Length];

			var b = (byte*)hdl.Value.AddrOfPinnedObject();

			for (var i = 0; i < array.Length; ++i)
			{
				array[i] = *(b + i);
			}

			value = encoding.GetString(array).Replace("\0", "");
		}
		else if (value is Enum)
		{
			value = value.To<byte>();
		}

		return value;
	}

	private static readonly ConcurrentDictionary<Type, FieldInfo[]> _fields = [];

	/// <summary>
	/// Formats the object's fields and values into a string using the specified encoding.
	/// </summary>
	/// <param name="obj">The object whose fields are to be formatted.</param>
	/// <param name="encoding">The encoding to use for formatting byte arrays.</param>
	/// <returns>A string representation of the object's fields and values.</returns>
	public static string FormatToString(this object obj, Encoding encoding)
	{
		if (obj is null)
			throw new ArgumentNullException(nameof(obj));

		if (encoding is null)
			throw new ArgumentNullException(nameof(encoding));

		var type = obj.GetType();

		return _fields
			.GetOrAdd(type, key => key.GetFields())
			.Select(f => $"{f.Name}={FormatToString(f, f.GetValue(obj), encoding)}")
			.JoinCommaSpace();
	}

	private static readonly Encoding _utf8 = Encoding.UTF8;
	private static readonly Encoding _ascii = Encoding.ASCII;

	/// <summary>
	/// Converts a pointer to a <see cref="Encoding.UTF8"/> encoded string of a specified size into a managed string.
	/// </summary>
	/// <param name="size">The size of the <see cref="Encoding.UTF8"/> encoded string in bytes.</param>
	/// <param name="ptr">A pointer to the <see cref="Encoding.UTF8"/> encoded string in unmanaged memory.</param>
	/// <returns>A managed string representation of the <see cref="Encoding.UTF8"/> encoded string.</returns>
	public static string ToUtf8(this int size, byte* ptr)
		=> _utf8.GetString(ptr, size).TrimEnd('\0').TrimEnd();

	/// <summary>
	/// Fills a <see cref="Encoding.UTF8"/> encoded unmanaged memory block with the contents of a managed string.
	/// </summary>
	/// <param name="value">The managed string to encode and copy into unmanaged memory.</param>
	/// <param name="ptr">A pointer to the unmanaged memory block to fill.</param>
	/// <param name="size">The maximum size of the unmanaged memory block in bytes.</param>
	public static void ToUtf8(this string value, byte* ptr, int size)
	{
		if (value.IsEmpty())
			return;

		if (_utf8.GetByteCount(value) > size)
			throw new ArgumentOutOfRangeException(nameof(size), size, "Invalid value.");

		value.FillString(_utf8, ptr, size);
	}

	/// <summary>
	/// Converts a pointer to a <see cref="Encoding.ASCII"/> encoded string of a specified size into a managed string.
	/// </summary>
	/// <param name="size">The size of the <see cref="Encoding.ASCII"/> encoded string in bytes.</param>
	/// <param name="ptr">A pointer to the <see cref="Encoding.ASCII"/> encoded string in unmanaged memory.</param>
	/// <returns>A managed string representation of the <see cref="Encoding.UTF8"/> encoded string.</returns>
	public static string ToAscii(this int size, byte* ptr)
		=> _ascii.GetString(ptr, size).TrimEnd('\0').TrimEnd();

	/// <summary>
	/// Fills a <see cref="Encoding.ASCII"/> encoded unmanaged memory block with the contents of a managed string.
	/// </summary>
	/// <param name="value">The managed string to encode and copy into unmanaged memory.</param>
	/// <param name="ptr">A pointer to the unmanaged memory block to fill.</param>
	/// <param name="size">The maximum size of the unmanaged memory block in bytes.</param>
	public static void ToAscii(this string value, byte* ptr, int size)
	{
		if (value.IsEmpty())
			return;

		if (_ascii.GetByteCount(value) > size)
			throw new ArgumentOutOfRangeException(nameof(size), size, "Invalid value.");

		value.FillString(_ascii, ptr, size);
	}

	/// <summary>
	/// Creates a span from the unmanaged memory pointer.
	/// </summary>
	/// <param name="ptr">The unmanaged memory pointer.</param>
	/// <param name="length">The number of bytes in the span.</param>
	/// <returns>A <see cref="Span{Byte}"/> representing the unmanaged memory.</returns>
	public static Span<byte> ToSpan(this IntPtr ptr, int length)
		=> new(ptr.ToPointer(), length);

	/// <summary>
	/// Creates a read-only span from the unmanaged memory pointer.
	/// </summary>
	/// <param name="ptr">The unmanaged memory pointer.</param>
	/// <param name="length">The number of bytes in the span.</param>
	/// <returns>A <see cref="ReadOnlySpan{Byte}"/> representing the unmanaged memory.</returns>
	public static ReadOnlySpan<byte> ToReadOnlySpan(this IntPtr ptr, int length)
		=> new(ptr.ToPointer(), length);
}