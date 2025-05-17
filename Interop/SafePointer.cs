namespace Ecng.Interop;

using System;

using Ecng.Common;

/// <summary>
/// Represents a safe wrapper around an unmanaged memory pointer with bounds checking and shifting capabilities.
/// </summary>
public struct SafePointer
{
	private IntPtr _pointer;
	private readonly int? _size;
	private int _origin;

	/// <summary>
	/// Initializes a new instance of the <see cref="SafePointer"/> struct with a specified pointer and optional size.
	/// </summary>
	/// <param name="pointer">The unmanaged memory pointer to wrap. Must not be <see cref="IntPtr.Zero"/>.</param>
	/// <param name="size">The optional size of the memory block in bytes. If specified, must be non-negative.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when <paramref name="pointer"/> is <see cref="IntPtr.Zero"/> or <paramref name="size"/> is negative.
	/// </exception>
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

	/// <summary>
	/// Gets the current unmanaged memory pointer.
	/// </summary>
	public readonly IntPtr Pointer => _pointer;

	private void CheckBorder<TStruct>()
		where TStruct : struct
	{
		CheckBorder(typeof(TStruct).SizeOf());
	}

	private readonly void CheckBorder(int offset)
	{
		if (_size != null && (_origin + offset) > _size.Value)
			throw new ArgumentOutOfRangeException(nameof(offset));
	}

	/// <summary>
	/// Reads a structure of type <typeparamref name="TStruct"/> from the current pointer position.
	/// </summary>
	/// <typeparam name="TStruct">The type of the structure to read. Must be a value type.</typeparam>
	/// <param name="autoShift">If <c>true</c>, shifts the pointer by the size of <typeparamref name="TStruct"/> after reading.</param>
	/// <returns>The structure read from the pointer.</returns>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when the operation exceeds the defined size boundary.
	/// </exception>
	public TStruct ToStruct<TStruct>(bool autoShift = false)
		where TStruct : struct
	{
		CheckBorder<TStruct>();

		var value = _pointer.ToStruct<TStruct>();
		
		if (autoShift)
			Shift<TStruct>();

		return value;
	}

	/// <summary>
	/// Shifts the pointer forward by the size of the specified structure type.
	/// </summary>
	/// <typeparam name="TStruct">The type of the structure whose size determines the shift. Must be a value type.</typeparam>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when the shift exceeds the defined size boundary.
	/// </exception>
	public void Shift<TStruct>()
		where TStruct : struct
	{
		Shift(typeof(TStruct).SizeOf());
	}

	/// <summary>
	/// Shifts the pointer forward by the specified offset in bytes.
	/// </summary>
	/// <param name="offset">The number of bytes to shift the pointer.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when the shift exceeds the defined size boundary.
	/// </exception>
	public void Shift(int offset)
	{
		CheckBorder(offset);

		_origin += offset;
		_pointer += offset;
	}

	/// <summary>
	/// Copies data from the current pointer position to a byte array.
	/// </summary>
	/// <param name="buffer">The target byte array to copy data into.</param>
	/// <param name="autoShift">If <c>true</c>, shifts the pointer by the length of the copied data after copying.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when the operation exceeds the defined size boundary.
	/// </exception>
	public void CopyTo(byte[] buffer, bool autoShift = false)
	{
		CopyTo(buffer, 0, buffer.Length, autoShift);
	}

	/// <summary>
	/// Copies a specified amount of data from the current pointer position to a byte array.
	/// </summary>
	/// <param name="buffer">The target byte array to copy data into.</param>
	/// <param name="offset">The starting index in the buffer where data should be copied.</param>
	/// <param name="length">The number of bytes to copy.</param>
	/// <param name="autoShift">If <c>true</c>, shifts the pointer by <paramref name="length"/> after copying.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when the operation exceeds the defined size boundary.
	/// </exception>
	public void CopyTo(byte[] buffer, int offset, int length, bool autoShift = false)
	{
		CheckBorder(length);

		Pointer.CopyTo(buffer, offset, length);

		if (autoShift)
			Shift(length);
	}

	/// <summary>
	/// Reads a value of type <typeparamref name="TValue"/> from the current pointer position.
	/// </summary>
	/// <typeparam name="TValue">The type of the value to read. Must be a value type.</typeparam>
	/// <param name="autoShift">If <c>true</c>, shifts the pointer by the size of <typeparamref name="TValue"/> after reading.</param>
	/// <returns>The value read from the pointer.</returns>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when the operation exceeds the defined size boundary.
	/// </exception>
	public TValue Read<TValue>(bool autoShift = false)
		where TValue : struct
	{
		CheckBorder<TValue>();

		var value = Pointer.Read<TValue>();

		if (autoShift)
			Shift<TValue>();

		return value;
	}

	/// <summary>
	/// Implicitly converts a <see cref="SafePointer"/> to an <see cref="IntPtr"/>.
	/// </summary>
	/// <param name="pointer">The <see cref="SafePointer"/> instance to convert.</param>
	/// <returns>The underlying <see cref="IntPtr"/> value.</returns>
	public static implicit operator IntPtr(SafePointer pointer) => pointer.Pointer;
}