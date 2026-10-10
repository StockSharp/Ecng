namespace Ecng.Tests.Drawing;

using System.Buffers.Binary;
using System.Drawing;

using Ecng.Drawing;

using static ImageTestHelper;

/// <summary>
/// What a picture says of its colors - an ICC profile, or the chunks a PNG has in its place - goes into
/// the picture made of it, read back here from the segments and the chunks themselves.
/// </summary>
[TestClass]
public class ColorProfileTests : BaseTestClass
{
	private const byte _profileSegment = 0xE2;
	private const byte _exifSegment = 0xE1;

	// The most of a profile one segment of a JPEG holds.
	private const int _segmentSize = 65519;

	// The largest profile that is carried over.
	private const int _limit = 4 * 1024 * 1024;

	// 16x16 baseline JPEG files written by libjpeg-turbo: three components, one, and four.
	private static readonly byte[] _rgbJpeg = Convert.FromBase64String(
		"/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAMCAgMCAgMDAwMEAwMEBQgFBQQEBQoHBwYIDAoMDAsKCwsNDhIQDQ4RDgsLEBYQERMUFRUVDA8XGBYUGBIUFRT/2wBDAQMEBAUEBQkFBQkUDQsNFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBT/wAARCAAQABADASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwBPD8Oo6Hq+ua/qd7GbyMmGyto32pEVXb8uOygqgx32e9M06PUfDPhK/wBdvrpJNVv28q3hLcknJZuc4HzcZ6B1/u1T8OaDp3iLxXc6nf6nINBsYyVJbb5xXkY/3mYnPYFD2q1Y6Lp/iCLWdX1S8I0/TixSL7oJIzt9hg8jsGf+7XRiKjq1ZTrXb93mfLrrqoLra/S9ktEtOY3qXhNpqyXteb972+GC9Vv/AJaH/9k=");

	private static readonly byte[] _grayJpeg = Convert.FromBase64String(
		"/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAMCAgMCAgMDAwMEAwMEBQgFBQQEBQoHBwYIDAoMDAsKCwsNDhIQDQ4RDgsLEBYQERMUFRUVDA8XGBYUGBIUFRT/wAALCAAQABABAREA/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/9oACAEBAAA/AE8Pw6joer65r+p3sZvIyYbK2jfakRVdvy47KCqDHfZ70zTo9R8M+Er/AF2+ukk1W/byreEtySclm5zgfNxnoHX+7VPw5oOneIvFdzqd/qcg0GxjJUltvnFeRj/eZic9gUParVjoun+IItZ1fVLwjT9OLFIvugkjO32GDyOwZ/7tf//Z");

	private static readonly byte[] _cmykJpeg = Convert.FromBase64String(
		"/9j/7gAOQWRvYmUAZAAAAAAA/9sAQwADAgIDAgIDAwMDBAMDBAUIBQUEBAUKBwcGCAwKDAwLCgsLDQ4SEA0OEQ4LCxAWEBETFBUVFQwPFxgWFBgSFBUU/8AAFAgAEAAQBEMRAE0RAFkRAEsRAP/EAB8AAAEFAQEBAQEBAAAAAAAAAAABAgMEBQYHCAkKC//EALUQAAIBAwMCBAMFBQQEAAABfQECAwAEEQUSITFBBhNRYQcicRQygZGhCCNCscEVUtHwJDNicoIJChYXGBkaJSYnKCkqNDU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6g4SFhoeIiYqSk5SVlpeYmZqio6Slpqeoqaqys7S1tre4ubrCw8TFxsfIycrS09TV1tfY2drh4uPk5ebn6Onq8fLz9PX29/j5+v/aAA4EQwBNAFkASwAAPwDf8MeD/Gdp8UfGGfFtgJbKF3Q7lWO2jSFlAUZ64YjjoBgdax/Csy+CrXUvGmuIr6t/x7WVrEchZiANsftGuxFPd/LPrW94m8Wa1478T69LqmoagNIOqy2fhzQ7PUX0y0t7O0fyZb24njG9IfMdYlWMb5DhE6mv1TrG8K6b4vtPhz4iI8S27wT3CxtAZQst0yBnkZyD8i5yP9kEDqavQSyeBfC1x4x1fbN4n1ceVp8P9wv1cZ6AB+PaRP7lYX2WSMt51p5M0LGOWPcZhE4OGG6TzXA3Do8jlS2HVM0VD4e8BeCz8ZvFdxL42uzZzW0yGZ3IDlE5CrnsWXPYdKz/AAjpsPjbV11rVW2+E9Ej3xKTgXBzwQfWRycHspjbtUOq2+patbaVd3N2xjEsYnmVhHJJ5CmVFG37gEtzM2QOGKMOUGCsTwx4B8C23wk8eW0vjO6W5hupGfEjbxk5IJzxjHI9a3Y7b/hPfEd34t15UHh7R4yYbZzsjkcjIX2GGwfQSS/3KsSadcWdy8FxaQFrcAolrbAzwxbFypORgAc7AQqDjJOTRX//2Q==");

	[TestMethod]
	public void Jpeg_KeepsItsProfileWhenItIsMadeSmaller()
	{
		var profile = Profile("RGB ", 3144);

		var result = WithProfile(_rgbJpeg, profile).ResizeImage(8, 8);

		result.GetImageSize().AssertEqual(new Size(8, 8));
		ReadProfile(result).AssertEqual(profile);
	}

	[TestMethod]
	public void Jpeg_ProfileInSeveralSegments_IsPutTogetherByTheirNumbers()
	{
		// Three segments of 60000 bytes at most, written in the order 2, 3, 1.
		var profile = Profile("RGB ", 150_000);
		var source = WithProfile(_rgbJpeg, profile, 60_000, [2, 3, 1]);

		var result = source.ResizeImage(8, 8);

		ReadProfile(result).AssertEqual(profile);

		// Written again, it is split where a segment is full.
		ReadJpegSegments(result, _profileSegment).Select(segment => segment.Length - 14).ToArray().AssertEqual([_segmentSize, _segmentSize, 150_000 - 2 * _segmentSize]);
	}

	[TestMethod]
	public void Jpeg_ProfileBehindFillBytesAndMarkersThatStandAlone_IsFound()
	{
		var profile = Profile("RGB ", 600);
		var jpeg = WithProfile(_rgbJpeg, profile);

		// Bytes of all ones may pad a marker, and a marker such as TEM has no segment after it.
		byte[] padded = [.. jpeg[..2], 0xFF, 0x01, 0xFF, 0xFF, 0xFF, .. jpeg[2..]];

		ReadProfile(padded.ResizeImage(8, 8)).AssertEqual(profile);
	}

	[TestMethod]
	public void Jpeg_ProfileGoesIntoThePngMadeOfIt()
	{
		var profile = Profile("RGB ", 150_000);

		var png = WithProfile(_rgbJpeg, profile).ConvertToPng();
		var chunks = ReadChunks(png);

		// It is said of the samples before the samples.
		chunks.Select(chunk => chunk.tag).ToArray().AssertEqual(["IHDR", "iCCP", "IDAT", "IEND"]);

		var (name, unpacked) = ReadProfileChunk(chunks[1].data);
		(name.Length is >= 1 and <= 79).AssertTrue($"profile name '{name}'");
		unpacked.AssertEqual(profile);
	}

	[TestMethod]
	public void Png_ChunksThatSayWhatItsColorsAre_GoIntoTheResult()
	{
		var rgba = Rgba(8, 8, (x, y) => ((byte)(x * 30), (byte)(y * 30), 128, 255));
		var profile = ("iCCP", ProfileChunk("Display", Profile("RGB ", 600)));
		var gamma = ("gAMA", new byte[] { 0, 0, 0xB1, 0x8F });
		var primaries = ("cHRM", Enumerable.Range(0, 32).Select(i => (byte)(i * 7)).ToArray());
		var standard = ("sRGB", new byte[] { 1 });

		// A profile with the gamma and the primaries that stand for it where a profile is not read.
		// Whatever order they came in, they are written primaries first.
		var result = ReadChunks(WithChunks(CreatePng(8, 8, rgba), profile, gamma, primaries).ResizeImage(4, 4));

		result.Select(chunk => chunk.tag).ToArray().AssertEqual(["IHDR", "cHRM", "gAMA", "iCCP", "IDAT", "IEND"]);
		result[1].data.AssertEqual(primaries.Item2);
		result[2].data.AssertEqual(gamma.Item2);
		result[3].data.AssertEqual(profile.Item2);

		// The standard color space named instead of a profile.
		result = ReadChunks(WithChunks(CreatePng(8, 8, rgba), standard).ResizeImage(4, 4));

		result.Select(chunk => chunk.tag).ToArray().AssertEqual(["IHDR", "sRGB", "IDAT", "IEND"]);
		result[1].data.AssertEqual(standard.Item2);

		// A file has one or the other: of both the profile is kept.
		result = ReadChunks(WithChunks(CreatePng(8, 8, rgba), standard, profile).ConvertToPng());

		result.Select(chunk => chunk.tag).ToArray().AssertEqual(["IHDR", "iCCP", "IDAT", "IEND"]);
	}

	[TestMethod]
	public void Png_ColorChunkOfALengthItsKindDoesNotHave_IsLeftBehind()
	{
		var png = CreatePng(8, 8, new byte[8 * 8 * 4]);

		// Gamma is four bytes, the primaries thirty-two, the standard color space one.
		var result = WithChunks(png, ("gAMA", new byte[5]), ("cHRM", new byte[31]), ("sRGB", new byte[2])).ResizeImage(4, 4);

		Tags(result).AssertEqual(["IHDR", "IDAT", "IEND"]);
	}

	[TestMethod]
	public void Png_OfSeveralProfiles_TheFirstIsTheOneLookedAt()
	{
		var png = CreatePng(8, 8, new byte[8 * 8 * 4]);
		var gray = ("iCCP", ProfileChunk("Gray", Profile("GRAY", 600)));
		var rgb = ("iCCP", ProfileChunk("Display", Profile("RGB ", 600)));

		// A file is to have one. The first is not for RGB, so none is carried.
		Tags(WithChunks(png, gray, rgb).ResizeImage(4, 4)).AssertEqual(["IHDR", "IDAT", "IEND"]);
		ReadChunks(WithChunks(png, rgb, gray).ResizeImage(4, 4)).Single(chunk => chunk.tag == "iCCP").data.AssertEqual(rgb.Item2);

		// Two hundred chunks of four kilobytes that unpack to four megabytes each: the others are not unpacked.
		var large = new byte[_limit];
		BinaryPrimitives.WriteInt32BigEndian(large, large.Length);
		"GRAY"u8.CopyTo(large.AsSpan(16));
		"acsp"u8.CopyTo(large.AsSpan(36));

		var many = WithChunks(png, Enumerable.Repeat(("iCCP", ProfileChunk("Large", large)), 200).ToArray());
		(many.Length < 2_000_000).AssertTrue($"the file is {many.Length} bytes");

		var before = GC.GetAllocatedBytesForCurrentThread();
		var result = many.ResizeImage(4, 4);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

		Tags(result).AssertEqual(["IHDR", "IDAT", "IEND"]);
		(allocated < 4 * _limit).AssertTrue($"{allocated} bytes were allocated");
	}

	[TestMethod]
	public void Profile_ForSamplesThePictureNoLongerHas_IsLeftBehind()
	{
		// What is written is RGB whatever was read: a profile for four inks or for gray says nothing of it.
		ReadProfile(WithProfile(_cmykJpeg, Profile("CMYK", 3000)).ResizeImage(8, 8)).AssertNull();
		ReadProfile(WithProfile(_grayJpeg, Profile("GRAY", 600)).ResizeImage(8, 8)).AssertNull();
		Tags(WithProfile(_cmykJpeg, Profile("CMYK", 3000)).ConvertToPng()).AssertEqual(["IHDR", "IDAT", "IEND"]);

		var gray = WithChunks(CreatePng(8, 8, new byte[8 * 8 * 4]), ("iCCP", ProfileChunk("Gray", Profile("GRAY", 600))));
		Tags(gray.ResizeImage(4, 4)).AssertEqual(["IHDR", "IDAT", "IEND"]);

		// The same pictures keep a profile that is for RGB.
		ReadProfile(WithProfile(_cmykJpeg, Profile("RGB ", 600)).ResizeImage(8, 8)).AssertNotNull();
		ReadProfile(WithProfile(_grayJpeg, Profile("RGB ", 600)).ResizeImage(8, 8)).AssertNotNull();
	}

	[TestMethod]
	public void Profile_ThatIsNotOne_IsLeftBehind()
	{
		var tooShort = Profile("RGB ", 127);

		var wrongLength = Profile("RGB ", 600);
		wrongLength[3]++;

		var unsigned = Profile("RGB ", 600);
		unsigned[36] = (byte)'x';

		foreach (var profile in new[] { tooShort, wrongLength, unsigned })
		{
			ReadProfile(WithProfile(_rgbJpeg, profile).ResizeImage(8, 8)).AssertNull();

			var png = WithChunks(CreatePng(8, 8, new byte[8 * 8 * 4]), ("iCCP", ProfileChunk("Broken", profile)));
			Tags(png.ResizeImage(4, 4)).AssertEqual(["IHDR", "IDAT", "IEND"]);
		}

		// A chunk that is not what an iCCP chunk is: no name, an unknown compression method, data that is not deflated.
		var content = ProfileChunk("Display", Profile("RGB ", 600));
		byte[][] broken = [content[7..], [.. content[..8], 1, .. content[9..]], [.. content[..9], .. new byte[40]]];

		foreach (var chunk in broken)
			Tags(WithChunks(CreatePng(8, 8, new byte[8 * 8 * 4]), ("iCCP", chunk)).ResizeImage(4, 4)).AssertEqual(["IHDR", "IDAT", "IEND"]);
	}

	[TestMethod]
	public void Jpeg_ProfileWhoseSegmentsDoNotMakeAWhole_IsLeftBehind()
	{
		var profile = Profile("RGB ", 900);
		byte[] Part(int number, int count) => ProfilePart(profile, number, count, 300);

		var cases = new (string what, byte[][] parts)[]
		{
			("a part is missing", [Part(1, 3), Part(3, 3)]),
			("a part comes twice", [Part(1, 3), Part(2, 3), Part(2, 3), Part(3, 3)]),
			("the parts disagree on how many there are", [Part(1, 3), Part(2, 3), Part(3, 4)]),
			("a part has no number", [Part(0, 3), Part(1, 3), Part(2, 3), Part(3, 3)]),
			("a part is numbered beyond the last", [Part(1, 2), Part(2, 2), Part(3, 2)]),
			("a part is cut short", [Part(1, 1)[..13]]),

			// The first of two holds nothing, then comes again with the whole profile; the second never comes.
			("an empty part comes twice", [ProfilePart([], 1, 2, 300), ProfilePart(profile, 1, 2, 900)]),
		};

		foreach (var (what, parts) in cases)
		{
			var jpeg = _rgbJpeg;

			foreach (var part in Enumerable.Reverse(parts))
				jpeg = InsertJpegSegment(jpeg, _profileSegment, part);

			ReadProfile(jpeg.ResizeImage(8, 8)).AssertNull(what);
		}
	}

	[TestMethod]
	public void Profile_LargerThanIsCarried_IsLeftBehind()
	{
		// As large as is carried, and a byte larger.
		ReadProfile(WithProfile(_rgbJpeg, Profile("RGB ", _limit)).ResizeImage(8, 8)).AssertNotNull();
		ReadProfile(WithProfile(_rgbJpeg, Profile("RGB ", _limit + 1)).ResizeImage(8, 8)).AssertNull();

		// In a PNG a profile is packed: a small chunk may unpack to any size. Thirty megabytes here.
		var huge = new byte[30_000_000];
		Profile("RGB ", 600).CopyTo(huge, 0);
		BinaryPrimitives.WriteInt32BigEndian(huge, huge.Length);

		var png = WithChunks(CreatePng(8, 8, new byte[8 * 8 * 4]), ("iCCP", ProfileChunk("Huge", huge)));
		(png.Length < 100_000).AssertTrue($"the file is {png.Length} bytes");

		var before = GC.GetAllocatedBytesForCurrentThread();
		var result = png.ResizeImage(4, 4);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

		Tags(result).AssertEqual(["IHDR", "IDAT", "IEND"]);
		(allocated < 3 * _limit).AssertTrue($"{allocated} bytes were allocated");
	}

	[TestMethod]
	public void Jpeg_ProfileOfMoreSegmentsThanAByteNumbers_IsRefusedByTheEncoder()
	{
		var image = new RasterImage(8, 8);

		// A profile is split into segments that number themselves in a byte.
		ThrowsExactly<ArgumentOutOfRangeException>(() => JpegEncoder.Encode(image, 90, new byte[255 * _segmentSize + 1]));
		ReadJpegSegments(JpegEncoder.Encode(image, 90, new byte[255 * _segmentSize]), _profileSegment).Count.AssertEqual(255);
	}

	[TestMethod]
	public void AnimatedPng_KeepsItsProfile_AndSoDoTheFramesTakenOutOfIt()
	{
		var gif = new GifBuilder(8, 8, GifBuilder.GrayPalette(4))
			.Frame(0, 0, 8, 8, Enumerable.Repeat((byte)1, 64).ToArray(), delay: 5)
			.Frame(0, 0, 8, 8, Enumerable.Repeat((byte)2, 64).ToArray(), delay: 5)
			.Build();

		var profile = ("iCCP", ProfileChunk("Display", Profile("RGB ", 600)));
		var apng = WithChunks(gif.ConvertToPng(), profile);

		var smaller = ReadChunks(apng.ResizeImage(4, 4));

		smaller.Single(chunk => chunk.tag == "iCCP").data.AssertEqual(profile.Item2);
		smaller.Count(chunk => chunk.tag == "fcTL").AssertEqual(2);

		foreach (var (png, _) in apng.GetAnimationFrames())
			ReadChunks(png).Single(chunk => chunk.tag == "iCCP").data.AssertEqual(profile.Item2);

		// A GIF says nothing of its colors, and neither do the pictures made of it.
		Tags(gif.GetGifFrames()[0].png).AssertEqual(["IHDR", "IDAT", "IEND"]);
		ReadChunks(gif.ConvertToPng()).Any(chunk => chunk.tag is "iCCP" or "sRGB" or "gAMA" or "cHRM").AssertFalse();
	}

	[TestMethod]
	public void Watermark_LeavesTheProfileWhereItIs()
	{
		var font = new TestFont().MapLetterI().Build();

		var chunk = ("iCCP", ProfileChunk("Display", Profile("RGB ", 600)));
		var png = WithChunks(CreatePng(64, 48, BlackRgba(64, 48)), chunk);

		var marked = png.AddTextWatermark("I", font, fontSize: 18, opacity: 255, margin: 8);

		ReadChunks(marked).Single(read => read.tag == "iCCP").data.AssertEqual(chunk.Item2);
		AssertPixels(WhiteBoxes(BlackRgba(64, 48), 64, 255, (34.4, 16, 53.6, 35.2)), ReadPng(marked, 64, 48), 64);

		// A watermarked JPEG is a JPEG, and keeps its segments.
		var profile = Profile("RGB ", 150_000);
		var jpeg = WithProfile(_rgbJpeg, profile);

		marked = jpeg.AddTextWatermark("I", font, fontSize: 5, opacity: 255, margin: 1);

		(marked[0] == 0xFF && marked[1] == 0xD8).AssertTrue("the result is not a JPEG");
		marked.SequenceEqual(jpeg).AssertFalse("the text is not drawn");
		ReadProfile(marked).AssertEqual(profile);
	}

	[TestMethod]
	public void PictureThatComesBackAsItCame_IsNotWrittenAgain()
	{
		// A profile for four inks and an Exif block: neither would be in a picture written again.
		var jpeg = InsertJpegSegment(WithProfile(_cmykJpeg, Profile("CMYK", 3000)), _exifSegment, ExifOrientation(1));

		jpeg.ResizeImage(16, 16).AssertEqual(jpeg);
		jpeg.AddTextWatermark("text", opacity: 0, margin: 0).AssertEqual(jpeg);

		var png = WithChunks(CreatePng(8, 8, new byte[8 * 8 * 4]), ("iCCP", ProfileChunk("Gray", Profile("GRAY", 600))), ("tEXt", "Comment\0as it came"u8.ToArray()));

		png.ResizeImage(8, 8).AssertEqual(png);
		png.AddTextWatermark("text", opacity: 0, margin: 0).AssertEqual(png);
	}

	[TestMethod]
	public void Exif_IsLeftBehind()
	{
		var jpeg = InsertJpegSegment(WithProfile(_rgbJpeg, Profile("RGB ", 600)), _exifSegment, ExifOrientation(1));

		var result = jpeg.ResizeImage(8, 8);

		ReadJpegSegments(jpeg, _exifSegment).Count.AssertEqual(1);
		ReadJpegSegments(result, _exifSegment).Count.AssertEqual(0);
		ReadProfile(result).AssertNotNull();
	}

	[TestMethod]
	public void PictureWithoutAProfile_GetsNone()
	{
		ReadProfile(_rgbJpeg.ResizeImage(8, 8)).AssertNull();
		Tags(_rgbJpeg.ConvertToPng()).AssertEqual(["IHDR", "IDAT", "IEND"]);
		Tags(CreatePng(8, 8, new byte[8 * 8 * 4]).ResizeImage(4, 4)).AssertEqual(["IHDR", "IDAT", "IEND"]);
	}

	// A profile as far as the library looks into one: a header with the length of the profile, the color
	// space of the samples it is for and its signature.
	private static byte[] Profile(string space, int length)
	{
		var profile = new byte[length];

		for (var i = 0; i < length; i++)
			profile[i] = (byte)(i * 31 + 7);

		if (length >= 40)
		{
			BinaryPrimitives.WriteInt32BigEndian(profile, length);
			Encoding.ASCII.GetBytes(space).CopyTo(profile, 16);
			"acsp"u8.CopyTo(profile.AsSpan(36));
		}

		return profile;
	}

	// One APP2 segment of a profile: its signature, its number, the number of parts, the part.
	private static byte[] ProfilePart(byte[] profile, int number, int count, int size)
	{
		var from = Math.Clamp((number - 1) * size, 0, profile.Length);
		return [.. "ICC_PROFILE\0"u8, (byte)number, (byte)count, .. profile.AsSpan(from, Math.Min(size, profile.Length - from))];
	}

	// Puts a profile into a JPEG, in segments of the given size written in the given order of their numbers.
	private static byte[] WithProfile(byte[] jpeg, byte[] profile, int size = _segmentSize, int[] order = null)
	{
		var count = (profile.Length + size - 1) / size;

		// A segment that is inserted goes before the ones inserted earlier.
		foreach (var number in Enumerable.Reverse(order ?? [.. Enumerable.Range(1, count)]))
			jpeg = InsertJpegSegment(jpeg, _profileSegment, ProfilePart(profile, number, count, size));

		return jpeg;
	}

	// The profile of a JPEG, put together from its segments; null if it has none.
	private static byte[] ReadProfile(byte[] jpeg)
	{
		var parts = ReadJpegSegments(jpeg, _profileSegment);

		if (parts.Count == 0)
			return null;

		for (var i = 0; i < parts.Count; i++)
		{
			parts[i].AsSpan(0, 12).SequenceEqual("ICC_PROFILE\0"u8).AssertTrue("signature of a profile segment");
			parts[i][12].AssertEqual((byte)(i + 1), "number of a profile segment");
			parts[i][13].AssertEqual((byte)parts.Count, "number of profile segments");
		}

		return [.. parts.SelectMany(part => part.Skip(14))];
	}

	// The content of an iCCP chunk: the name of the profile, a zero, the compression method, the profile deflated.
	private static byte[] ProfileChunk(string name, byte[] profile)
		=> [.. Encoding.Latin1.GetBytes(name), 0, 0, .. Deflate(profile)];

	private static (string name, byte[] profile) ReadProfileChunk(byte[] content)
	{
		var end = Array.IndexOf(content, (byte)0);
		content[end + 1].AssertEqual((byte)0, "compression method of a profile");
		return (Encoding.Latin1.GetString(content, 0, end), Inflate(content[(end + 2)..]));
	}

	// Puts chunks into a PNG right after its header.
	private static byte[] WithChunks(byte[] png, params (string tag, byte[] data)[] chunks)
	{
		var all = ReadChunks(png).ToList();
		all.InsertRange(1, chunks);
		return WriteChunks(all);
	}

	private static string[] Tags(byte[] png)
		=> [.. ReadChunks(png).Select(chunk => chunk.tag)];
}
