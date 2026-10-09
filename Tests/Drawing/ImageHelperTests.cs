namespace Ecng.Tests.Drawing;

using System;
using System.IO;
using System.Linq;

using Ecng.Drawing;

using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using DrawingSize = System.Drawing.Size;

[TestClass]
public class ImageHelperTests : BaseTestClass
{
	private static byte[] CreateImage(int width = 240, int height = 120, bool transparent = false, bool jpeg = false)
	{
		using var image = new Image<Rgba32>(width, height,
			transparent ? new Rgba32(0, 0, 0, 0) : new Rgba32(0, 0, 0, 255));
		using var buffer = new MemoryStream();

		if (jpeg)
			image.SaveAsJpeg(buffer);
		else
			image.SaveAsPng(buffer);

		return buffer.ToArray();
	}

	[TestMethod]
	public void GetImageSize_PngJpegAndSpan()
	{
		CreateImage(513, 258).GetImageSize().AssertEqual(new DrawingSize(513, 258));
		CreateImage(321, 123, jpeg: true).GetImageSize().AssertEqual(new DrawingSize(321, 123));

		ReadOnlySpan<byte> data = CreateImage(17, 19);
		data.GetImageSize().AssertEqual(new DrawingSize(17, 19));
	}

	[TestMethod]
	public void GetImageSize_RejectsNullAndBadData()
	{
		byte[] missing = null;
		ThrowsExactly<ArgumentNullException>(() => missing.GetImageSize());
		ThrowsExactly<UnknownImageFormatException>(() => new byte[] { 1, 2, 3 }.GetImageSize());
	}

	[TestMethod]
	public void ResizeImage_ConstrainedByWidthOrHeight_KeepAspectRatio()
	{
		var source = CreateImage(400, 200);
		var resized = source.ResizeImage(120, 100);
		resized.GetImageSize().AssertEqual(new DrawingSize(120, 60));
		resized.IsPng().AssertTrue();
		source.GetImageSize().AssertEqual(new DrawingSize(400, 200));

		CreateImage(100, 400).ResizeImage(90, 100).GetImageSize().AssertEqual(new DrawingSize(25, 100));
		CreateImage(1, 300).ResizeImage(1, 10).GetImageSize().AssertEqual(new DrawingSize(1, 10));
	}

	[TestMethod]
	public void ResizeImage_NeverUpscales_AndAlwaysReturnsAnIndependentArray()
	{
		var data = CreateImage(40, 20, jpeg: true);
		var resized = data.ResizeImage(100, 100);
		resized.SequenceEqual(data).AssertTrue();
		ReferenceEquals(resized, data).AssertFalse();
	}

	[TestMethod]
	public void ResizeImage_PreservesJpegEncoding()
	{
		var result = CreateImage(400, 200, jpeg: true).ResizeImage(100, 100);
		result.GetImageSize().AssertEqual(new DrawingSize(100, 50));
		result.IsPng().AssertFalse();
		(result[0] == 0xFF && result[1] == 0xD8).AssertTrue();
	}

	[TestMethod]
	public void ResizeImage_ValidatesBoundsAndSource()
	{
		var source = CreateImage();
		ThrowsExactly<ArgumentOutOfRangeException>(() => source.ResizeImage(0, 5));
		ThrowsExactly<ArgumentOutOfRangeException>(() => source.ResizeImage(5, -1));

		byte[] missing = null;
		ThrowsExactly<ArgumentNullException>(() => missing.ResizeImage(5, 5));
	}

	[TestMethod]
	public void ConvertToPng_DecodesJpegAndKeepsDimensions()
	{
		var output = CreateImage(122, 73, jpeg: true).ConvertToPng();
		output.IsPng().AssertTrue();
		output.GetImageSize().AssertEqual(new DrawingSize(122, 73));
	}

	[TestMethod]
	public void ConvertToPng_PreservesTransparentPixels()
	{
		var source = CreateImage(30, 20, transparent: true);
		var output = source.ConvertToPng();
		using var decoded = Image.Load<Rgba32>(output);
		decoded[0, 0].A.AssertEqual((byte)0);
	}

	[TestMethod]
	public void ConvertToPng_BadDataAndNull()
	{
		ThrowsExactly<UnknownImageFormatException>(() => new byte[] { 2, 4 }.ConvertToPng());

		byte[] missing = null;
		ThrowsExactly<ArgumentNullException>(() => missing.ConvertToPng());
	}

	[TestMethod]
	public void AddTextWatermark_ChangesPixelsButNotTheOriginalBuffer()
	{
		var source = CreateImage(400, 170);
		var original = (byte[])source.Clone();
		var font = SystemFonts.Families.First().Name;

		var output = source.AddTextWatermark("StockSharp", fontSize: 24, opacity: 220, margin: 10, fontFamily: font);
		output.IsPng().AssertTrue();
		output.GetImageSize().AssertEqual(new DrawingSize(400, 170));
		source.SequenceEqual(original).AssertTrue();

		using var decoded = Image.Load<Rgba32>(output);
		decoded[0, 0].R.AssertEqual((byte)0);
		var touched = false;

		for (var y = 0; y < decoded.Height && !touched; y++)
			for (var x = 0; x < decoded.Width; x++)
				if (decoded[x, y].R > 0)
				{
					touched = true;
					break;
				}

		touched.AssertTrue();
	}

	[TestMethod]
	public void AddTextWatermark_FitsLongTextWithinMargins()
	{
		var font = SystemFonts.Families.First().Name;
		var result = CreateImage(350, 140).AddTextWatermark(
			"Long watermark text for checking resizing", fontSize: 50, margin: 8, fontFamily: font);

		using var decoded = Image.Load<Rgba32>(result);
		decoded.Width.AssertEqual(350);
		decoded.Height.AssertEqual(140);

		// No watermark should cross the top-left padding.
		decoded[0, 0].R.AssertEqual((byte)0);
		decoded[0, 0].A.AssertEqual((byte)255);
	}

	[TestMethod]
	public void AddTextWatermark_ConvertsJpegToPng()
	{
		var font = SystemFonts.Families.First().Name;
		var output = CreateImage(300, 150, jpeg: true).AddTextWatermark("ECNG", fontFamily: font);
		output.IsPng().AssertTrue();
		output.GetImageSize().AssertEqual(new DrawingSize(300, 150));
	}

	[TestMethod]
	public void AddTextWatermark_UsesVerdanaWhenAvailableOrReportsMissingFont()
	{
		var source = CreateImage(320, 120);

		if (SystemFonts.TryGet("Verdana", out _))
			source.AddTextWatermark("ECNG").IsPng().AssertTrue();
		else
			ThrowsExactly<InvalidOperationException>(() => source.AddTextWatermark("ECNG"));
	}

	[TestMethod]
	public void AddTextWatermark_ValidatesArguments()
	{
		var source = CreateImage();
		var font = SystemFonts.Families.First().Name;

		ThrowsExactly<ArgumentException>(() => source.AddTextWatermark(" "));
		ThrowsExactly<ArgumentOutOfRangeException>(() => source.AddTextWatermark("text", fontSize: 0));
		ThrowsExactly<ArgumentOutOfRangeException>(() => source.AddTextWatermark("text", fontSize: float.NaN));
		ThrowsExactly<ArgumentOutOfRangeException>(() => source.AddTextWatermark("text", fontSize: float.PositiveInfinity));
		ThrowsExactly<ArgumentOutOfRangeException>(() => source.AddTextWatermark("text", margin: -1));
		ThrowsExactly<ArgumentOutOfRangeException>(() => source.AddTextWatermark("text", margin: 200, fontFamily: font));
		ThrowsExactly<ArgumentException>(() => source.AddTextWatermark("text", fontFamily: " "));
		ThrowsExactly<InvalidOperationException>(() => source.AddTextWatermark("text", fontFamily: "DefinitelyNonexistentEcngTestFont"));

		byte[] missing = null;
		ThrowsExactly<ArgumentNullException>(() => missing.AddTextWatermark("X"));
	}
}
