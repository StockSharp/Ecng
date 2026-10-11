namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

internal static class Vp8Interleave
{
	public static bool IsSupported => Sse2.IsSupported || AdvSimd.Arm64.IsSupported;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<byte> UnpackLow(Vector128<byte> a, Vector128<byte> b) =>
		Sse2.IsSupported ? Sse2.UnpackLow(a, b) : AdvSimd.Arm64.ZipLow(a, b);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<byte> UnpackHigh(Vector128<byte> a, Vector128<byte> b) =>
		Sse2.IsSupported ? Sse2.UnpackHigh(a, b) : AdvSimd.Arm64.ZipHigh(a, b);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<ushort> UnpackLow(Vector128<ushort> a, Vector128<ushort> b) =>
		Sse2.IsSupported ? Sse2.UnpackLow(a, b) : AdvSimd.Arm64.ZipLow(a, b);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<ushort> UnpackHigh(Vector128<ushort> a, Vector128<ushort> b) =>
		Sse2.IsSupported ? Sse2.UnpackHigh(a, b) : AdvSimd.Arm64.ZipHigh(a, b);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<uint> UnpackLow(Vector128<uint> a, Vector128<uint> b) =>
		Sse2.IsSupported ? Sse2.UnpackLow(a, b) : AdvSimd.Arm64.ZipLow(a, b);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<uint> UnpackHigh(Vector128<uint> a, Vector128<uint> b) =>
		Sse2.IsSupported ? Sse2.UnpackHigh(a, b) : AdvSimd.Arm64.ZipHigh(a, b);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<int> UnpackLow(Vector128<int> a, Vector128<int> b) =>
		Sse2.IsSupported ? Sse2.UnpackLow(a, b) : AdvSimd.Arm64.ZipLow(a, b);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<int> UnpackHigh(Vector128<int> a, Vector128<int> b) =>
		Sse2.IsSupported ? Sse2.UnpackHigh(a, b) : AdvSimd.Arm64.ZipHigh(a, b);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<long> UnpackLow(Vector128<long> a, Vector128<long> b) =>
		Sse2.IsSupported ? Sse2.UnpackLow(a, b) : AdvSimd.Arm64.ZipLow(a, b);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<long> UnpackHigh(Vector128<long> a, Vector128<long> b) =>
		Sse2.IsSupported ? Sse2.UnpackHigh(a, b) : AdvSimd.Arm64.ZipHigh(a, b);
}
