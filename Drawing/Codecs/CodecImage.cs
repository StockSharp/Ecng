namespace Ecng.Drawing.Codecs;

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;

internal sealed class CodecImage : IDisposable
{
	private readonly byte[] _pixels;
	private readonly bool _isPooled;
	private readonly ImageProcessingLimits _limits;
	private bool _isDisposed;

	private CodecImage(int width, int height, CodecPixelFormats pixelFormat, byte[] pixels, bool isPooled, ImageProcessingLimits limits)
	{
		RasterImage.EnsureDecodable(width, height, limits);
		Width = width;
		Height = height;
		PixelFormat = pixelFormat;
		_pixels = pixels;
		_isPooled = isPooled;
		_limits = limits;
		if (width < 1 || height < 1 || pixels.Length < checked(width * height * BytesPerPixel(pixelFormat)))
			throw new InvalidDataException("Invalid decoded image buffer.");
	}

	public int Width { get; }
	public int Height { get; }
	public CodecPixelFormats PixelFormat { get; }
	public ImageMetadata Metadata { get; } = new();

	public static CodecImage Create(int width, int height, CodecPixelFormats pixelFormat, ImageProcessingLimits limits)
	{
		RasterImage.EnsureDecodable(width, height, limits);
		return new(width, height, pixelFormat, new byte[checked(width * height * BytesPerPixel(pixelFormat))], false, limits);
	}

	public static CodecImage FromBuffer(int width, int height, CodecPixelFormats pixelFormat, byte[] pixels, bool owned, ImageProcessingLimits limits)
		=> new(width, height, pixelFormat, pixels, owned, limits);

	public Span<byte> GetRowSpan(int row)
		=> GetPixelSpan().Slice(row * Width * BytesPerPixel(PixelFormat), Width * BytesPerPixel(PixelFormat));

	public Span<byte> GetPixelSpan()
	{
		if (_isDisposed)
			throw new ObjectDisposedException(nameof(CodecImage));
		return _pixels.AsSpan(0, Width * Height * BytesPerPixel(PixelFormat));
	}

	public RasterImage ToRaster()
	{
		var image = new RasterImage(Width, Height, _limits);
		WriteRgba(GetPixelSpan(), PixelFormat, image.Pixels);
		return image;
	}

	public static void WriteRgba(ReadOnlySpan<byte> bytes, CodecPixelFormats format, Span<byte> pixels)
	{
		var stride = BytesPerPixel(format);
		var highDepth = format is CodecPixelFormats.Gray16 or CodecPixelFormats.Rgb48 or CodecPixelFormats.Rgba64;
		var gray = format is CodecPixelFormats.Gray8 or CodecPixelFormats.Gray16;
		if (bytes.Length % stride != 0 || pixels.Length != bytes.Length / stride * 4)
			throw new InvalidDataException("Invalid codec row dimensions.");
		for (var i = 0; i < bytes.Length / stride; i++)
		{
			var source = bytes.Slice(i * stride, stride);
			var to = i * 4;
			if (format == CodecPixelFormats.Cmyk32)
			{
				pixels[to] = (byte)((255 - source[0]) * (255 - source[3]) / 255);
				pixels[to + 1] = (byte)((255 - source[1]) * (255 - source[3]) / 255);
				pixels[to + 2] = (byte)((255 - source[2]) * (255 - source[3]) / 255);
			}
			else
			{
				pixels[to] = Channel(source, 0, highDepth);
				pixels[to + 1] = Channel(source, gray ? 0 : 1, highDepth);
				pixels[to + 2] = Channel(source, gray ? 0 : 2, highDepth);
			}
			pixels[to + 3] = format is CodecPixelFormats.Rgba32 or CodecPixelFormats.Rgba64
				? Channel(source, 3, highDepth) : (byte)255;
		}
	}

	public void Dispose()
	{
		if (_isDisposed)
			return;
		_isDisposed = true;
		if (_isPooled)
			ArrayPool<byte>.Shared.Return(_pixels);
	}

	private static byte Channel(ReadOnlySpan<byte> source, int channel, bool highDepth)
		=> highDepth ? (byte)((BitConverter.IsLittleEndian
			? BinaryPrimitives.ReadUInt16LittleEndian(source[(channel * 2)..])
			: BinaryPrimitives.ReadUInt16BigEndian(source[(channel * 2)..])) >> 8) : source[channel];

	public static int BytesPerPixel(CodecPixelFormats format)
		=> format switch
		{
			CodecPixelFormats.Gray8 => 1,
			CodecPixelFormats.Gray16 => 2,
			CodecPixelFormats.Rgb24 => 3,
			CodecPixelFormats.Rgba32 or CodecPixelFormats.Cmyk32 => 4,
			CodecPixelFormats.Rgb48 => 6,
			CodecPixelFormats.Rgba64 => 8,
			_ => throw new NotSupportedException("Unsupported decoded pixel layout."),
		};
}
