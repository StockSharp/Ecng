namespace Ecng.Tests.Drawing;

using System.Buffers.Binary;
using System.Drawing;

using Ecng.Drawing;

using static ImageTestHelper;

/// <summary>
/// A JPEG that carries an Exif orientation is handled the way it is displayed.
/// </summary>
[TestClass]
public class ExifOrientationTests : BaseTestClass
{
	// 32x16 baseline 4:4:4 JPEG: a red and a green quadrant above a blue and a yellow one.
	private static readonly byte[] _quadrants = Convert.FromBase64String(
		"/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAIBAQEBAQIBAQECAgICAgQDAgICAgUEBAMEBgUGBgYFBgYGBwkIBgcJBwYGCAsICQoKCgoKBggLDAsKDAkKCgr/2wBDAQICAgICAgUDAwUKBwYHCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgr/wAARCAAQACADAREAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwD53r8HP9aAoA94rxz/AJpwoA+J6/20P6cCgD9/K/5ez+tAoA//2Q==");

	private const byte _exifMarker = 0xE1;

	// Quadrant colors as the picture is displayed, by orientation: top left, top right, bottom left, bottom right.
	private static readonly string[] _layouts = [null, "RGBY", "GRYB", "YBGR", "BYRG", "RBGY", "BRYG", "YGBR", "GYRB"];

	[TestMethod]
	public void ConvertToPng_TurnsThePictureTheWayItIsDisplayed()
	{
		for (ushort orientation = 1; orientation <= 8; orientation++)
		{
			foreach (var bigEndian in new[] { true, false })
			{
				var (width, height, rgba) = ReadPng(Oriented(orientation, bigEndian).ConvertToPng());

				new Size(width, height).AssertEqual(DisplayedSize(orientation), $"orientation {orientation}");
				AssertLayout(rgba, width, height, _layouts[orientation], 8, $"orientation {orientation}");
			}
		}
	}

	[TestMethod]
	public void GetImageSize_IsTheDisplayedSize()
	{
		for (ushort orientation = 1; orientation <= 8; orientation++)
			Oriented(orientation).GetImageSize().AssertEqual(DisplayedSize(orientation), $"orientation {orientation}");
	}

	[TestMethod]
	public void ResizeImage_KeepsThePictureUpright()
	{
		// Stored 32x16, displayed 16x32.
		var jpeg = Oriented(6);

		var resized = jpeg.ResizeImage(8, 16);
		resized.GetImageSize().AssertEqual(new Size(8, 16));
		AssertStoredUpright(resized, new Size(8, 16));

		var rgba = ReadPng(resized.ConvertToPng(), 8, 16);
		AssertLayout(rgba, 8, 16, _layouts[6], 24, "resized");
	}

	[TestMethod]
	public void ResizeImage_FitsTheDisplayedSizeIntoTheBounds()
	{
		var jpeg = Oriented(6);

		// The displayed 16x32 already fits: the source comes back untouched, orientation included.
		jpeg.ResizeImage(16, 32).AssertEqual(jpeg);

		// It does not fit 32x16, though the stored raster does.
		jpeg.ResizeImage(32, 16).GetImageSize().AssertEqual(new Size(8, 16));
	}

	[TestMethod]
	public void AddTextWatermark_KeepsThePictureUpright()
	{
		// A text too faint to move the color of a quadrant still takes the picture through the decoder and the encoder.
		var marked = Oriented(8).AddTextWatermark("I", new TestFont().MapLetterI().Build(), fontSize: 6, opacity: 1, margin: 0);

		(marked[0] == 0xFF && marked[1] == 0xD8).AssertTrue("the result is not a JPEG");
		marked.GetImageSize().AssertEqual(new Size(16, 32));
		AssertStoredUpright(marked, new Size(16, 32));

		var (width, height, rgba) = ReadPng(marked.ConvertToPng());

		new Size(width, height).AssertEqual(new Size(16, 32));
		AssertLayout(rgba, width, height, _layouts[8], 8, "watermarked");
	}

	[TestMethod]
	public void AddTextWatermark_ThatDrawsNothing_ReturnsThePictureWithItsOrientation()
	{
		var jpeg = Oriented(8);

		jpeg.AddTextWatermark("text", opacity: 0, margin: 0).AssertEqual(jpeg);
	}

	[TestMethod]
	public void ExifThatCannotBeRead_LeavesThePictureAsStored()
	{
		byte[][] blocks =
		[
			// An orientation outside 1..8.
			ExifOrientation(0),
			ExifOrientation(9),
			// The TIFF header cut short, a wrong byte order mark, a first IFD beyond the block.
			ExifOrientation(6)[..12],
			[.. "Exif\0\0XX"u8, .. ExifOrientation(6)[8..]],
			[.. ExifOrientation(6)[..10], 0x7F, 0xFF, 0xFF, 0xFF, .. ExifOrientation(6)[14..]],
			// Not an Exif block at all.
			[.. "http://ns.adobe.com/xap/1.0/\0"u8, 1, 2, 3],
		];

		foreach (var block in blocks)
		{
			var jpeg = InsertJpegSegment(_quadrants, _exifMarker, block);

			jpeg.GetImageSize().AssertEqual(new Size(32, 16));
			AssertLayout(ReadPng(jpeg.ConvertToPng(), 32, 16), 32, 16, _layouts[1], 8, "unreadable Exif");
		}
	}

	private static byte[] Oriented(ushort orientation, bool bigEndian = true)
		=> InsertJpegSegment(_quadrants, _exifMarker, ExifOrientation(orientation, bigEndian));

	private static Size DisplayedSize(ushort orientation)
		=> orientation >= 5 ? new(16, 32) : new(32, 16);

	// The picture carries no orientation: the size its frame header stores is the size it is shown with.
	private static void AssertStoredUpright(byte[] jpeg, Size shown)
	{
		RemoveJpegSegments(jpeg, _exifMarker).AssertEqual(jpeg, "the picture still has an Exif block");

		// A frame header holds the sample precision, then the height and the width.
		var frame = FindJpegFrame(jpeg);

		new Size(BinaryPrimitives.ReadUInt16BigEndian(jpeg.AsSpan(frame + 3)), BinaryPrimitives.ReadUInt16BigEndian(jpeg.AsSpan(frame + 1))).AssertEqual(shown, "the size the frame header stores");
	}

	private static void AssertLayout(byte[] rgba, int width, int height, string layout, int tolerance, string what)
	{
		(int x, int y)[] centers = [(width / 4, height / 4), (width * 3 / 4, height / 4), (width / 4, height * 3 / 4), (width * 3 / 4, height * 3 / 4)];

		for (var i = 0; i < 4; i++)
		{
			var (r, g, b) = layout[i] switch
			{
				'R' => (220, 40, 41),
				'G' => (40, 179, 60),
				'B' => (41, 60, 199),
				_ => (231, 210, 41),
			};

			var actual = Pixel(rgba, width, centers[i].x, centers[i].y);

			var near = Math.Abs(actual.r - r) <= tolerance && Math.Abs(actual.g - g) <= tolerance && Math.Abs(actual.b - b) <= tolerance && actual.a == 255;
			near.AssertTrue($"{what}: quadrant {i} should be '{layout[i]}' ({r},{g},{b}), it is {actual}");
		}
	}
}
