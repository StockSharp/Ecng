namespace Ecng.Drawing;

using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

/// <summary>
/// Image operations using the Windows GDI+ subsystem. Does not require external NuGet
/// image decoders, fonts or native binaries. Other operating systems can still read PNG
/// dimensions via <see cref="PngHelper"/>, but have no built-in cross-platform image
/// rendering backend in .NET 6/10.
/// </summary>
public static class ImageHelper
{
	/// <summary>
	/// Obtains the pixel dimensions of a PNG on any platform, or another supported
	/// image format through Windows GDI+. PNG header reading does not decode pixels.
	/// </summary>
	public static Size GetImageSize(this byte[] data)
		=> GetImageSize((ReadOnlySpan<byte>)(data ?? throw new ArgumentNullException(nameof(data))));

	/// <inheritdoc cref="GetImageSize(byte[])"/>
	public static Size GetImageSize(this ReadOnlySpan<byte> data)
	{
		if (data.TryGetPngSize(out var pngSize))
		{
			if (pngSize.Width <= 0 || pngSize.Height <= 0)
				throw new InvalidDataException("PNG dimensions must be positive.");

			return pngSize;
		}

		EnsureWindows();
		using var source = NativeImage.Open(data.ToArray());
		return new Size(source.Width, source.Height);
	}

	/// <summary>
	/// Fits the picture into the maximum width and height, without increasing its size.
	/// Maintains the aspect ratio and original encoding (PNG, JPEG, BMP, GIF).
	/// GIF output contains the first frame only. Windows only.
	/// </summary>
	public static byte[] ResizeImage(this byte[] data, int maxWidth, int maxHeight)
	{
		ArgumentNullException.ThrowIfNull(data);

		if (maxWidth <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxWidth));

		if (maxHeight <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxHeight));

		EnsureWindows();
		var format = DetectFormat(data);
		using var source = NativeImage.Open(data);
		var ratio = Math.Min((double)maxWidth / source.Width, (double)maxHeight / source.Height);

		if (ratio >= 1)
			return (byte[])data.Clone();

		var width = Math.Max(1, (int)Math.Floor(source.Width * ratio));
		var height = Math.Max(1, (int)Math.Floor(source.Height * ratio));
		var canvas = CreateCanvas(width, height);

		try
		{
			using (var graphics = new NativeGraphics(canvas))
				graphics.Draw(source.Handle, source.Width, source.Height, width, height);

			return Save(canvas, format);
		}
		finally
		{
			Gdi.Check(Gdi.GdipDisposeImage(canvas));
		}
	}

	/// <summary>
	/// Re-encodes a supported picture as PNG, including transparency when present.
	/// Animated input is flattened to its first frame. Windows only.
	/// </summary>
	public static byte[] ConvertToPng(this byte[] data)
	{
		ArgumentNullException.ThrowIfNull(data);
		EnsureWindows();
		DetectFormat(data);

		using var source = NativeImage.Open(data);
		var canvas = CreateCanvas(source.Width, source.Height);

		try
		{
			using (var graphics = new NativeGraphics(canvas))
				graphics.Draw(source.Handle, source.Width, source.Height, source.Width, source.Height);

			return Save(canvas, Encoder.Png);
		}
		finally
		{
			Gdi.Check(Gdi.GdipDisposeImage(canvas));
		}
	}

	/// <summary>
	/// Places semi-transparent white text at the bottom-right and returns a PNG.
	/// Requires Verdana (or the specified alternate font family) installed in Windows.
	/// The text is scaled down if necessary to fit within the margins. Windows only.
	/// </summary>
	public static byte[] AddTextWatermark(
		this byte[] data,
		string text,
		float fontSize = 24,
		byte opacity = 160,
		int margin = 12,
		string fontFamily = "Verdana")
	{
		ArgumentNullException.ThrowIfNull(data);

		if (string.IsNullOrWhiteSpace(text))
			throw new ArgumentException("Watermark text must not be empty.", nameof(text));

		if (fontSize <= 0 || float.IsNaN(fontSize) || float.IsInfinity(fontSize))
			throw new ArgumentOutOfRangeException(nameof(fontSize));

		if (margin < 0)
			throw new ArgumentOutOfRangeException(nameof(margin));

		if (string.IsNullOrWhiteSpace(fontFamily))
			throw new ArgumentException("A font family is required.", nameof(fontFamily));

		EnsureWindows();
		DetectFormat(data);

		using var source = NativeImage.Open(data);

		if (2L * margin >= source.Width || 2L * margin >= source.Height)
			throw new ArgumentOutOfRangeException(nameof(margin), "Margins leave no space for text.");

		var canvas = CreateCanvas(source.Width, source.Height);

		try
		{
			using (var graphics = new NativeGraphics(canvas))
			{
				graphics.Draw(source.Handle, source.Width, source.Height, source.Width, source.Height);
				graphics.DrawWatermark(text, fontSize, opacity, margin, fontFamily, source.Width, source.Height);
			}

			return Save(canvas, Encoder.Png);
		}
		finally
		{
			Gdi.Check(Gdi.GdipDisposeImage(canvas));
		}
	}

	private static void EnsureWindows()
	{
		if (!OperatingSystem.IsWindows())
			throw new PlatformNotSupportedException("Image rendering without external dependencies uses Windows GDI+. PNG dimensions are available on all platforms.");
	}

	private enum Encoder { Png, Jpeg, Gif, Bmp }

	private static Encoder DetectFormat(ReadOnlySpan<byte> bytes)
	{
		if (bytes.IsPng())
			return Encoder.Png;

		if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
			return Encoder.Jpeg;

		if (bytes.Length >= 6 &&
			(bytes[..6].SequenceEqual("GIF87a"u8) || bytes[..6].SequenceEqual("GIF89a"u8)))
			return Encoder.Gif;

		if (bytes.Length >= 2 && bytes[0] == (byte)'B' && bytes[1] == (byte)'M')
			return Encoder.Bmp;

		throw new NotSupportedException("Only PNG, JPEG, GIF and BMP are supported.");
	}

	private static IntPtr CreateCanvas(int width, int height)
	{
		// PixelFormat32bppARGB is writable, supports alpha and permits drawing text.
		Gdi.Check(Gdi.GdipCreateBitmapFromScan0(width, height, 0, 0x26200A, IntPtr.Zero, out var canvas));
		return canvas;
	}

	private static byte[] Save(IntPtr image, Encoder format)
	{
		var encoder = format switch
		{
			Encoder.Png => new Guid("557cf406-1a04-11d3-9a73-0000f81ef32e"),
			Encoder.Jpeg => new Guid("557cf401-1a04-11d3-9a73-0000f81ef32e"),
			Encoder.Gif => new Guid("557cf402-1a04-11d3-9a73-0000f81ef32e"),
			Encoder.Bmp => new Guid("557cf400-1a04-11d3-9a73-0000f81ef32e"),
			_ => throw new ArgumentOutOfRangeException(nameof(format)),
		};

		var file = Path.Combine(Path.GetTempPath(), $"ecng-image-{Guid.NewGuid():N}.tmp");

		try
		{
			Gdi.Check(Gdi.GdipSaveImageToFile(image, file, ref encoder, IntPtr.Zero));
			return File.ReadAllBytes(file);
		}
		finally
		{
			File.Delete(file);
		}
	}

	private sealed class NativeImage : IDisposable
	{
		private readonly string _tempFile;
		private IntPtr _startupToken;

		public IntPtr Handle { get; private set; }
		public int Width { get; private set; }
		public int Height { get; private set; }

		private NativeImage(string tempFile) => _tempFile = tempFile;

		public static NativeImage Open(byte[] data)
		{
			var file = Path.Combine(Path.GetTempPath(), $"ecng-image-{Guid.NewGuid():N}.tmp");
			var image = new NativeImage(file);

			try
			{
				Gdi.Check(Gdi.GdiplusStartup(out image._startupToken, ref Gdi.StartupInput, IntPtr.Zero));
				File.WriteAllBytes(file, data);

				Gdi.Check(Gdi.GdipLoadImageFromFile(file, out var handle));
				image.Handle = handle;
				Gdi.Check(Gdi.GdipGetImageWidth(handle, out var width));
				Gdi.Check(Gdi.GdipGetImageHeight(handle, out var height));

				if (width == 0 || height == 0 || width > int.MaxValue || height > int.MaxValue)
					throw new InvalidDataException("Invalid picture dimensions.");

				image.Width = (int)width;
				image.Height = (int)height;
				return image;
			}
			catch
			{
				image.Dispose();
				throw;
			}
		}

		public void Dispose()
		{
			if (Handle != IntPtr.Zero)
			{
				Gdi.GdipDisposeImage(Handle);
				Handle = IntPtr.Zero;
			}

			if (_startupToken != IntPtr.Zero)
			{
				Gdi.GdiplusShutdown(_startupToken);
				_startupToken = IntPtr.Zero;
			}

			if (File.Exists(_tempFile))
				File.Delete(_tempFile);
		}
	}

	private sealed class NativeGraphics : IDisposable
	{
		private IntPtr _graphics;

		public NativeGraphics(IntPtr image)
		{
			Gdi.Check(Gdi.GdipGetImageGraphicsContext(image, out _graphics));
			Gdi.Check(Gdi.GdipSetInterpolationMode(_graphics, 7)); // HighQualityBicubic
			Gdi.Check(Gdi.GdipSetTextRenderingHint(_graphics, 4)); // AntiAlias
		}

		public void Draw(IntPtr source, int sourceWidth, int sourceHeight, int width, int height)
			=> Gdi.Check(Gdi.GdipDrawImageRectRectI(
				_graphics, source, 0, 0, width, height, 0, 0, sourceWidth, sourceHeight,
				2, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero));

		public void DrawWatermark(string text, float fontSize, byte opacity, int margin, string familyName, int width, int height)
		{
			var familyStatus = Gdi.GdipCreateFontFamilyFromName(familyName, IntPtr.Zero, out var family);

			if (familyStatus == 14) // FontFamilyNotFound
				throw new InvalidOperationException($"Font '{familyName}' is not installed.");

			Gdi.Check(familyStatus);

			IntPtr font = IntPtr.Zero;
			IntPtr brush = IntPtr.Zero;
			IntPtr format = IntPtr.Zero;

			try
			{
				Gdi.Check(Gdi.GdipCreateStringFormat(0, 0, out format));
				Gdi.Check(Gdi.GdipSetStringFormatAlign(format, 2)); // right
				Gdi.Check(Gdi.GdipSetStringFormatLineAlign(format, 2)); // bottom
				Gdi.Check(Gdi.GdipCreateSolidFill(unchecked((int)((uint)opacity << 24 | 0x00FFFFFF)), out brush));

				var allowedWidth = width - 2 * margin;
				var allowedHeight = height - 2 * margin;
				var size = fontSize;
				var measured = default(Gdi.RectF);
				var bounds = new Gdi.RectF(0, 0, 100000, 100000);

				for (var attempt = 0; attempt < 12; attempt++)
				{
					if (font != IntPtr.Zero)
					{
						Gdi.GdipDeleteFont(font);
						font = IntPtr.Zero;
					}

					Gdi.Check(Gdi.GdipCreateFont(family, size, 0, 3, out font)); // UnitPoint
					Gdi.Check(Gdi.GdipMeasureString(_graphics, text, text.Length, font, ref bounds, format, out measured, out _, out _));

					if (measured.Width <= allowedWidth && measured.Height <= allowedHeight)
						break;

					var factor = Math.Min((double)allowedWidth / measured.Width, (double)allowedHeight / measured.Height);

					if (factor <= 0 || double.IsNaN(factor))
						throw new ArgumentException("Watermark does not fit in the image.", nameof(text));

					size = (float)(size * Math.Min(factor * 0.95, 0.9));

					if (size < 1)
						throw new ArgumentException("Watermark cannot fit inside the requested margins.", nameof(text));
				}

				if (measured.Width > allowedWidth || measured.Height > allowedHeight)
					throw new ArgumentException("Watermark does not fit in the image.", nameof(text));

				var layout = new Gdi.RectF(margin, margin, allowedWidth, allowedHeight);
				Gdi.Check(Gdi.GdipDrawString(_graphics, text, text.Length, font, ref layout, format, brush));
			}
			finally
			{
				if (brush != IntPtr.Zero) Gdi.GdipDeleteBrush(brush);
				if (font != IntPtr.Zero) Gdi.GdipDeleteFont(font);
				if (format != IntPtr.Zero) Gdi.GdipDeleteStringFormat(format);
				Gdi.GdipDeleteFontFamily(family);
			}
		}

		public void Dispose()
		{
			if (_graphics != IntPtr.Zero)
			{
				Gdi.GdipDeleteGraphics(_graphics);
				_graphics = IntPtr.Zero;
			}
		}
	}

	private static class Gdi
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct RectF
		{
			public float X, Y, Width, Height;
			public RectF(float x, float y, float width, float height)
				=> (X, Y, Width, Height) = (x, y, width, height);
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct StartupInfo
		{
			public uint Version;
			public IntPtr DebugEventCallback;
			public int SuppressBackgroundThread;
			public int SuppressExternalCodecs;
		}

		public static StartupInfo StartupInput = new() { Version = 1 };

		public static void Check(int code)
		{
			if (code != 0)
				throw new InvalidDataException($"Windows GDI+ operation failed with status {code}.");
		}

		[DllImport("gdiplus.dll", ExactSpelling = true)]
		public static extern int GdiplusStartup(out IntPtr token, ref StartupInfo input, IntPtr output);

		[DllImport("gdiplus.dll", ExactSpelling = true)]
		public static extern void GdiplusShutdown(IntPtr token);

		[DllImport("gdiplus.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern int GdipLoadImageFromFile(string filename, out IntPtr image);

		[DllImport("gdiplus.dll", ExactSpelling = true)]
		public static extern int GdipGetImageWidth(IntPtr image, out uint width);

		[DllImport("gdiplus.dll", ExactSpelling = true)]
		public static extern int GdipGetImageHeight(IntPtr image, out uint height);

		[DllImport("gdiplus.dll", ExactSpelling = true)]
		public static extern int GdipDisposeImage(IntPtr image);

		[DllImport("gdiplus.dll", ExactSpelling = true)]
		public static extern int GdipCreateBitmapFromScan0(int width, int height, int stride, int format, IntPtr scan0, out IntPtr bitmap);

		[DllImport("gdiplus.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern int GdipSaveImageToFile(IntPtr image, string filename, ref Guid clsid, IntPtr parameters);

		[DllImport("gdiplus.dll", ExactSpelling = true)]
		public static extern int GdipGetImageGraphicsContext(IntPtr image, out IntPtr graphics);

		[DllImport("gdiplus.dll", ExactSpelling = true)]
		public static extern int GdipDeleteGraphics(IntPtr graphics);

		[DllImport("gdiplus.dll", ExactSpelling = true)]
		public static extern int GdipSetInterpolationMode(IntPtr graphics, int mode);

		[DllImport("gdiplus.dll", ExactSpelling = true)]
		public static extern int GdipSetTextRenderingHint(IntPtr graphics, int mode);

		[DllImport("gdiplus.dll", ExactSpelling = true)]
		public static extern int GdipDrawImageRectRectI(IntPtr graphics, IntPtr image, int dx, int dy, int dw, int dh,
			int sx, int sy, int sw, int sh, int srcUnit, IntPtr attributes, IntPtr callback, IntPtr callbackData);

		[DllImport("gdiplus.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern int GdipCreateFontFamilyFromName(string name, IntPtr collection, out IntPtr family);

		[DllImport("gdiplus.dll", ExactSpelling = true)]
		public static extern int GdipDeleteFontFamily(IntPtr family);

		[DllImport("gdiplus.dll", ExactSpelling = true)]
		public static extern int GdipCreateFont(IntPtr family, float size, int style, int unit, out IntPtr font);

		[DllImport("gdiplus.dll", ExactSpelling = true)]
		public static extern int GdipDeleteFont(IntPtr font);

		[DllImport("gdiplus.dll", ExactSpelling = true)]
		public static extern int GdipCreateStringFormat(int flags, int language, out IntPtr format);

		[DllImport("gdiplus.dll", ExactSpelling = true)]
		public static extern int GdipDeleteStringFormat(IntPtr format);

		[DllImport("gdiplus.dll", ExactSpelling = true)]
		public static extern int GdipSetStringFormatAlign(IntPtr format, int align);

		[DllImport("gdiplus.dll", ExactSpelling = true)]
		public static extern int GdipSetStringFormatLineAlign(IntPtr format, int align);

		[DllImport("gdiplus.dll", ExactSpelling = true)]
		public static extern int GdipCreateSolidFill(int argb, out IntPtr brush);

		[DllImport("gdiplus.dll", ExactSpelling = true)]
		public static extern int GdipDeleteBrush(IntPtr brush);

		[DllImport("gdiplus.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern int GdipMeasureString(IntPtr graphics, string text, int length, IntPtr font,
			ref RectF layout, IntPtr format, out RectF boundingBox, out int codepointsFitted, out int linesFilled);

		[DllImport("gdiplus.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern int GdipDrawString(IntPtr graphics, string text, int length, IntPtr font,
			ref RectF layout, IntPtr format, IntPtr brush);
	}
}
