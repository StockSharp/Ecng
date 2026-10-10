namespace Ecng.Tests.Drawing;

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

using Ecng.Drawing;

/// <summary>
/// Regression contract written first: new behavior must fail on the previous branch,
/// then pass after codec and font changes. Fixtures are independently generated
/// with Pillow, not with Ecng's own encoder.
/// </summary>
[TestClass]
public class ImageExtendedTddTests : BaseTestClass
{
	// RGB 16x16: Pillow baseline 8-bit, Q92. Input is deliberately not a PNG.
	private static readonly byte[] _baseline = Convert.FromBase64String(
		"/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAMCAgICAgMCAgIDAwMDBAYEBAQEBAgGBgUGCQgKCgkICQkKDA8MCgsOCwkJDRENDg8QEBEQCgwSExIQEw8QEBD/2wBDAQMDAwQDBAgEBAgQCwkLEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBD/wAARCAAQABADAREAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDov/r9/wA+f5nv0Ffh7atd+fX723u03pOa1m/chof0J/X9f15s+aec98/5/wA+1fq2qfW9/JO6X3KSX/btGPebP4B/r+v61P0y5z3z/n/PtX8pap9b38k7pfcpJf8AbtGPebP6N/r+v61Pmj/63b8uP5Dv1NfqiStZeXT7klu03rCD1m/fnofwF/X9f15I/9k=");

	// Same source pixels as _baseline, encoded as progressive JPEG by Pillow/libjpeg.
	private static readonly byte[] _progressive = Convert.FromBase64String(
		"/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAMCAgICAgMCAgIDAwMDBAYEBAQEBAgGBgUGCQgKCgkICQkKDA8MCgsOCwkJDRENDg8QEBEQCgwSExIQEw8QEBD/2wBDAQMDAwQDBAgEBAgQCwkLEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBD/wgARCAAQABADAREAAhEBAxEB/8QAFgABAQEAAAAAAAAAAAAAAAAABwUI/8QAFQEBAQAAAAAAAAAAAAAAAAAABwb/2gAMAwEAAhADEAAAAaMOhGlWAaZKEYzqgH//xAAUEAEAAAAAAAAAAAAAAAAAAAAg/9oACAEBAAEFAh//xAAUEQEAAAAAAAAAAAAAAAAAAAAg/9oACAEDAQE/AR//xAAcEQABAwUAAAAAAAAAAAAAAADwAAFhAjFBocH/2gAIAQIBAT8BDr5syCdUNKCdUNKDjZu6/8QAFBABAAAAAAAAAAAAAAAAAAAAIP/aAAgBAQAGPwIf/8QAFBABAAAAAAAAAAAAAAAAAAAAIP/aAAgBAQABPyEf/9oADAMBAAIAAwAAABAB7//EABgRAQEAAwAAAAAAAAAAAAAAAPAAASFh/9oACAEDAQE/EB3MNw3DmL//xAAcEQEAAQUBAQAAAAAAAAAAAAABEQAhQWGRMdH/2gAIAQIBAT8QUiXeeq+otju8dXHMzoZDgD6HVxzM6GQ4A+h0BEGscA9Rbvd46//EAB4QAAAFBQEAAAAAAAAAAAAAAAABESFBMWGRsfDh/9oACAEBAAE/EPZy+zmhB1le6wdZXusPIw2impj/2Q==");

	private static readonly byte[] _cmyk = Convert.FromBase64String(
		"/9j/7gAOQWRvYmUAZAAAAAAA/9sAQwADAgICAgIDAgICAwMDAwQGBAQEBAQIBgYFBgkICgoJCAkJCgwPDAoLDgsJCQ0RDQ4PEBAREAoMEhMSEBMPEBAQ/8AAFAgAEAAQBEMRAE0RAFkRAEsRAP/EAB8AAAEFAQEBAQEBAAAAAAAAAAABAgMEBQYHCAkKC//EALUQAAIBAwMCBAMFBQQEAAABfQECAwAEEQUSITFBBhNRYQcicRQygZGhCCNCscEVUtHwJDNicoIJChYXGBkaJSYnKCkqNDU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6g4SFhoeIiYqSk5SVlpeYmZqio6Slpqeoqaqys7S1tre4ubrCw8TFxsfIycrS09TV1tfY2drh4uPk5ebn6Onq8fLz9PX29/j5+v/aAA4EQwBNAFkASwAAPwD7brKr5bZlKs7uhUrK7M85ZSpO2VmlXllLfLLMvzStiGHCgmv1Tr8BKKd+8WT/AJarKsn+xHIJET/viORE/wC2dpH6yGiv37r9CqP3iyf8tVlWT/YjkEiJ/wB8RyIn/bO0j9ZDRX4CUU1VUqqIiFSsSKqQFlKk7olWJuWUt80ULfNK2ZpsKAKK/9k=");

	private static readonly byte[] _grayProgressive = Convert.FromBase64String(
		"/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAUDBAQEAwUEBAQFBQUGBwwIBwcHBw8LCwkMEQ8SEhEPERETFhwXExQaFRERGCEYGh0dHx8fExciJCIeJBweHx7/wgALCAAQABABAREA/8QAFgABAQEAAAAAAAAAAAAAAAAABgQH/9oACAEBAAAAAaiWuEf/xAAUEAEAAAAAAAAAAAAAAAAAAAAg/9oACAEBAAEFAh//xAAUEAEAAAAAAAAAAAAAAAAAAAAg/9oACAEBAAY/Ah//xAAUEAEAAAAAAAAAAAAAAAAAAAAg/9oACAEBAAE/IR//2gAIAQEAAAAQH//EABcQAQEBAQAAAAAAAAAAAAAAAPAAIRD/2gAIAQEAAT8QhkM5/9k=");

	[TestMethod]
	public void ResizeJpeg_PreservesJpegFormatAndCanBeDecoded()
	{
		var output = _baseline.ResizeImage(8, 8);
		(output.Length > 4 && output[0] == 0xFF && output[1] == 0xD8 && output[^2] == 0xFF && output[^1] == 0xD9).AssertTrue();
		output.GetImageSize().Width.AssertEqual(8);
		output.GetImageSize().Height.AssertEqual(8);
		var pixels = output.ConvertToPng();
		pixels.GetPngSize().Width.AssertEqual(8);
	}

	[TestMethod]
	public void ResizeJpeg_WhenAlreadySmall_PreservesOriginalBytes()
	{
		var output = _baseline.ResizeImage(64, 64);
		_baseline.SequenceEqual(output).AssertTrue();
	}

	[TestMethod]
	public void ProgressiveJpeg_DecodesFullColorImage()
	{
		var png = _progressive.ConvertToPng();
		png.GetPngSize().Width.AssertEqual(16);
		png.GetPngSize().Height.AssertEqual(16);
		_progressive.ResizeImage(8, 8).GetImageSize().Width.AssertEqual(8);
	}

	[TestMethod]
	public void ProgressiveGrayJpeg_DecodesAndResizes()
	{
		var png = _grayProgressive.ConvertToPng();
		png.GetPngSize().Width.AssertEqual(16);
		_grayProgressive.ResizeImage(8, 8).GetImageSize().Width.AssertEqual(8);
	}

	[TestMethod]
	public void AdobeCmykJpeg_DecodesWithNonGrayColors()
	{
		var png = _cmyk.ConvertToPng();
		png.GetPngSize().Width.AssertEqual(16);
		png.GetPngSize().Height.AssertEqual(16);
		_cmyk.ResizeImage(8, 8).GetImageSize().Width.AssertEqual(8);
	}


	[TestMethod]
	public void Watermark_OnEverySupportedJpegMode_ChangesPixelsAndPreservesOutsideRegion()
	{
		var path = Path.Combine(Path.GetTempPath(), $"ecng-jpeg-watermark-{Guid.NewGuid():N}.ttf");
		File.WriteAllBytes(path, ImagePixelGoldenTests.BuildTestTrueType());
		try
		{
			foreach (var source in new[] { _baseline, _progressive, _grayProgressive, _cmyk })
			{
				var input = (byte[])source.Clone();
				var reference = ReadPngRgba8(source.ConvertToPng(), 16, 16);
				var marked = source.AddTextWatermark("I", fontSize: 12,
					opacity: 255, margin: 1, fontFilePath: path);
				(marked.Length > 8 && marked[0] == 137 && marked[1] == 80).AssertTrue();
				var actual = ReadPngRgba8(marked, 16, 16);
				actual.Length.AssertEqual(reference.Length);
				var differences = 0;
				for (var i = 0; i < actual.Length; i++)
					if (actual[i] != reference[i])
						differences++;

				(differences > 0).AssertTrue();
				for (var i = 0; i < 4; i++)
					actual[i].AssertEqual(reference[i]); // top-left unaffected
				source.SequenceEqual(input).AssertTrue(); // input never mutated
			}
		}
		finally { File.Delete(path); }
	}

	[TestMethod]
	public void Watermark_UnknownFamilyFallbackWithoutPrivateTtf()
	{
		var png = _baseline.ConvertToPng();
		var output = png.AddTextWatermark("StockSharp", fontSize: 7, margin: 1,
			fontFamily: "__a_missing_font_family_6bf471__");
		output.GetPngSize().Width.AssertEqual(16);
		output.GetPngSize().Height.AssertEqual(16);
	}


	[TestMethod]
	public void JpegMustRejectMissingEndOfImageNotSilentlyReturnPixels()
	{
		ThrowsExactly<InvalidDataException>(() => _baseline[..^2].ConvertToPng());
		ThrowsExactly<InvalidDataException>(() => _cmyk[..^2].ConvertToPng());
	}

	[TestMethod]
	public void Fuzz_MalformedJpegMustFailCleanly_NotCrashWithUnexpectedRuntimeErrors()
	{
		var seed = new Random(809123);
		var originals = new[] { _baseline, _progressive, _cmyk, _grayProgressive };
		foreach (var source in originals)
		{
			for (var iteration = 0; iteration < 80; iteration++)
			{
				var corrupted = (byte[])source.Clone();
				var index = seed.Next(2, corrupted.Length - 2);
				corrupted[index] ^= (byte)(1 << seed.Next(8));
				try
				{
					var png = corrupted.ConvertToPng();
					var size = png.GetPngSize();
					size.Width.AssertEqual(16);
					size.Height.AssertEqual(16);
				}
				catch (InvalidDataException) { }
				catch (NotSupportedException) { }
				catch (IOException) { }
			}
		}
	}

	[TestMethod]
	public void InvalidJpeg_TruncatedProgressiveDoesNotReturnValidPixels()
	{
		foreach (var length in new[] { 4, 16, 48, _progressive.Length / 2 })
		{
			var cut = _progressive[..length];
			try
			{
				cut.ConvertToPng();
				Assert.Fail($"Truncated progressive JPEG of {length} bytes decoded successfully.");
			}
			catch (InvalidDataException) { }
			catch (NotSupportedException) { }
		}
	}

	// These reference RGBA buffers were decoded by Pillow/libjpeg *before*
	// implementing the managed progressive/CMYK decoder. They are zlib
	// compressed to keep the test source small, not generated from Ecng.
	private const string ProgressiveReference =
		"eNotUWtP2gAU/b5FJ32wGY2gRqdQqrAMmR/1ryhGZ0Dk1VKcJvtzW+ZGHzwLFNiW/Y2zey9+aHLb3nPueYTVGr61txA0TYRWGT/sDNzGAUL7Dq6dR9z5BL15A/3hABr9U6sVmk3oLQN67Q7fHxIIK6fwrSaeW/sYNHLo2SX8aiXhWsfQGte0l4P6eAilfg+9nobmGFDtW+Hgu4FtwW9m4dXp3blE0CYN9R10GyW5y1itckW4lNzWKkXh4HmpeQ9+I4OBc4tOPQ+3XEDfeULXTi01N6rQ22nErTTUmiO4eMMQL88tE/16Fn7rgvQWSI9BuCL86jk6zp7s8C5jGMsczMWc2ktW/VYZXtuQu73WFYKvO3ArCQRWcamXtLLm5ZwSL+yJvY1If4ey8hq7hH2Ed39GTxL+Uw19a5kzZyV+KTvOULCUKWfr2yfoNI8kqz7tsGa/eYmBncVPergj6Yr1kxfWzHe5U+42iArwwxv48wP4ExNu/x7+gjqJDPiDCt5Mc1iPjqCNi1iLdhCfG4iHJcTGR1CjE3gj0jDIwf1zCHdYofc0vMhEZ/wZ/jSNd1Py/LcGtZ+EMjhDPPoCdbxNXNvYmFBus/eC9brXNKfQWWTg9YrC4c0zclfpJaD924Y+u0Is+AiVvsdnZcTCQ9HsjUjzPI1gQreHDtzfJnkyxEtstgt1eA4tovzHBpRJgeYLvB1msTp78cu7hGEsczCXcBK3FqXwbv4EpZvHWpjH+py6CDNYme5hY1wSjaKVNIt28sBexBN50ykrJUxCW5hyVx1Rn+Mc9MgiPaZkxFkJB2XHGTKWM+VslckxdZCk/RLWww9YjfahTZvY7J3i9SIhHUlXixcvpFm6pE65W2Wax+b0TrJaiTLYpMz57qv5FjaGNfwHwppe4A==";
	private const string CmykReference =
		"eNpdkc2tgzAQhJtIC4kCxiwRVSXBWIELdXJ2L++N7Q8JcRhpMDs/Xu9b+Ns3EzphEl7wen7bGmEROhCEXnCF79vAfBR6wQsfYRQObSt4YcYj+8bis+vfrvPaIfMFv0ZYyfHkGtlvuKfzwHzWPvFaC6/aiM7w8fSY6Hl0Nu4w0aEvM3X22iGS/6D/sbcf98idvuiMztcOjp551uHT8T2Tf2QZPWa0bdlt7XzH59hngPdkOXzC6R2X8rZ7ypBP6oC0SdrkCr+JV2TewvUu6S4YWmUm7SLNeOTzWHzqvDqnQejgju+VHE+ukf2G657py2zO/uHxwueBNqIzfDw9JvKz5uDaY/oI2XssM3X22iGS/+S+BtfetLPaaUJndL52cGgbOizcw+jfnrKMHjPatuy2asdT53yHiN9AlsMnnN5xKW9bs8JpbxO8nv8DdY5e9Q==";
	private const string GrayReference =
		"eNqNkUkKhUAQQ0/oPKJeSc/kPKIeKZ/XIPyV7aIgi+RVurqua9UvE4ah8jxXFEUKgsDoJEnk+76yLHvNMvgdx1GapirLUq7rGlZVVYZhy+MlG8ex0exmHm3L05m9+D3PU1EUhkcPun3pj5cMWRiwYMK25f+7Ppq3wEPb8tyIW+GlB3vJctMv/fkjGOzGT2dYaP52miYdx6F5njWOo9HrumoYBu37rqZpXgd/27batk3XdanrOsO679swbHm8ZJdlMZrdzKNteTqzF3/f9zrP0/DoQbcv/fGSIQsDFkzYtvx/10fzFnhoW54bcSu89GAvWW76pT9/BIPd+OkMC83f2vI/WPWoIw==";


	[TestMethod]
	public void JpegResizer_CompareEveryPixelAgainstIndependentPillowBilinearReference()
	{
		// Reference generated by decoding the original 16x16 JPEG with libjpeg,
		// then center-aligned bilinear averaging to 8x8. JPEG compression on
		// output is lossy, so bounded differences are expected.
		const string reference =
			"eNotjUESgjAQBP8AyWcAU3DRE+BrTIRPmsAJgvqTcXblkKrNdu9M9DWWMCCFC9LTwfo7zNTABMfXK1u8Q/QtVs5mbmBDRTaqk8LJ5hvioz7vxr9DV3JX/oUtU6eZ6rBDutJG9m4QM7P2HvbTodgq2INdh1OWcoVXHtUpNnZ8ryj2FiX/shMmjrg2D8os94azZIqjOeySTLkTr8w1fqf9l7U=";

		using var referenceCompressed = new MemoryStream(Convert.FromBase64String(reference));
		using var referenceZlib = new ZLibStream(referenceCompressed, CompressionMode.Decompress);
		using var expectedBuffer = new MemoryStream();
		referenceZlib.CopyTo(expectedBuffer);
		var expected = expectedBuffer.ToArray();

		var result = _baseline.ResizeImage(8, 8);
		(result.Length > 2 && result[0] == 0xFF && result[1] == 0xD8).AssertTrue();
		var actual = ReadPngRgba8(result.ConvertToPng(), 8, 8);
		expected.Length.AssertEqual(8 * 8 * 4);
		actual.Length.AssertEqual(expected.Length);

		for (var i = 0; i < actual.Length; i++)
		{
			var difference = Math.Abs(actual[i] - expected[i]);
			if (difference > 25)
				Assert.Fail($"JPEG resize at ({i/4%8},{i/4/8}), channel {"RGBA"[i%4]}: " +
					$"wanted {expected[i]}, got {actual[i]}, tolerance 25.");
		}
	}


	// Independently created Pillow/libjpeg fixture: four-component JPEG with
	// Adobe APP14 transform=2 (YCCK). This is NOT an Adobe CMYK transform=0 file.
	private static readonly byte[] _ycckFixture = Convert.FromBase64String(
		"/9j/7gAOQWRvYmUAZAAAAAAC/9sAQwADAgIDAgIDAwMDBAMDBAUIBQUEBAUKBwcGCAwKDAwLCgsLDQ4SEA0OEQ4LCxAWEBETFBUVFQwPFxgWFBgSFBUU/8AAFAgACwAPBEMRAE0RAFkRAEsRAP/EAB8AAAEFAQEBAQEBAAAAAAAAAAABAgMEBQYHCAkKC//EALUQAAIBAwMCBAMFBQQEAAABfQECAwAEEQUSITFBBhNRYQcicRQygZGhCCNCscEVUtHwJDNicoIJChYXGBkaJSYnKCkqNDU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6g4SFhoeIiYqSk5SVlpeYmZqio6Slpqeoqaqys7S1tre4ubrCw8TFxsfIycrS09TV1tfY2drh4uPk5ebn6Onq8fLz9PX29/j5+v/aAA4EQwBNAFkASwAAPwD2D9q34gf8fn7z1716b4u/bS+//p//AI/TPF/7Y3ir4lareaZ4Q0zWPFWpRQtcyWeiWct5MkQZVMhSNWIUM6DdjGWA7in6x+0N8Ufj3cyR+AvDWseIrd5pbY6hDH5djHKkfmNHJdSFYY22FTtdwTuQDJZQfEP+Cf3gaHxV8XPFXxV1OO4+z+FEXT9IdoZFhkvblXWZ1lDBWaKDKtGQ3F4jHaQhPi3ir9tL5z/p/f8Av1BYfs5/FfxzPez/ABC8U6f8MbSNmSGHfHq15M4MZVtkMwiWIhpBuM28NHjy8MGqBv2ZP9Jvrj4u/E7+No7fT/BMvvGVle5uYf8ArqpiEP8AcbzOq0z9q2+n/wBM/et3r5x/Y28NWHxp/ax8E+DPGaTaz4b1L7f9rsvtMkHmeXYXEqfPEyuMPGh4YZxg8EivpjxfbWnw10q80zwhpuneFdNlma5ks9EsYbOF5SqqZCkaqCxVEG7GcKB2FexfGDxPqegaVHpmmXP9nabZQrbWtnaRrFDBEihUjRFACqqgAKAAAABXrf7H2iWfh/8AYw8KX+nxNb3euajqOoahJ5jN5863klsHwSQv7m2hXC4HyZxkkn9Vz+yr8HPB+i2+kWHww8K3Fpbltkmq6TDqFwdzljvnuFklfljjcxwMAYAAHxx8XfHOv+e//E0n+96j1r8+Pjb498Q/aX/4mtx9/wBR6/Sv/9k=");

	private const string YcckLibjpegRgba =
		"eJwV0V1I01EYx/HHub9ubnNzbrb958uWsjnzbYHzBdEEa6XMNjHFauBqqMnCpIgioggqqZCwG6GL8iIIichACoqoMIPywpsIFELqQqibvI76drz48JwHnh+Hcx65Icgt5aYyo+z000KpqlWXhbZLwv6LDhKTOsfGKhlMhRkejmDPUXOjyogyoaSFvHHBlRFqTggt6QJiRz309ZcR76miL1bDoc5aopEQdlHz3cpBZb+g9QpBdd7TrRHtcnKgXaerzU9npJKuxhCRqgB+3YdV5bSdbJ1gUNwRIVAvhOqttIbdNNeW0RQspzngp7G0goBbx2l1YNJM3BedxxLEpKucV6goK6DBV0yD7qHO6yPq8RN26ui2YuwmG6Lu2Z7dZkH2Iol2ZCiBXpBHqc1Gtc1N5Q5LCUGrF7tmwWI08y/zi+3sNpsTm+xbX0eiMWUAaR3DZ3BQYXCy21iC1+jElVuIlmPkT8cPtjq28GxsULO2Rmx5meOLi4hvGCk/hVSfxitFuMSOWfL5a3nHb9ca8uYbthdf0V+v0rz0lvjCUwbn55H8NGIbQxwTWIuy5IvGd3nOT/mEzK0qXyic/Uj43iuaZp7Qe/sBQ1fukrl6Xb05Ta7hJDZjCreWRKbeI5MryJnPyIUVPNlnhKYeEc3OER+9w0DmGqmRs6TG7WguweFWf6vqriK1o74PSPIluUeW8PQ/JJKcoyUxTc954fA5oX/ESjxpwWwWChWHSbDnCxZN+A/l6hnb";

	[TestMethod]
	public void YcckJpeg_AdobeTransform2_DecodesPixelForPixelAgainstLibjpeg()
	{
		var png = _ycckFixture.ConvertToPng();
		var actual = ReadPngRgba8(png, 15, 11);
		using var compressed = new MemoryStream(Convert.FromBase64String(YcckLibjpegRgba));
		using var zlib = new ZLibStream(compressed, CompressionMode.Decompress);
		using var reference = new MemoryStream();
		zlib.CopyTo(reference);
		var expected = reference.ToArray();
		actual.Length.AssertEqual(expected.Length);
		var count = 0;
		var peak = 0;
		for (var i = 0; i < actual.Length; i++)
		{
			var difference = Math.Abs(actual[i] - expected[i]);
			if (difference > peak) peak = difference;
			if (difference > 4) count++;
		}
		if (count != 0)
			Assert.Fail($"YCCK: {count} RGBA channels deviate > 4, peak={peak}; " +
				$"at (0,0) actual=({actual[0]},{actual[1]},{actual[2]}) vs " +
				$"reference=({expected[0]},{expected[1]},{expected[2]}).");
	}

	[TestMethod]
	public void YcckJpeg_ResizePreservesJpeg_AndWatermarkPreservesImageSize()
	{
		_ycckFixture.GetImageSize().Width.AssertEqual(15);
		_ycckFixture.GetImageSize().Height.AssertEqual(11);
		var resized = _ycckFixture.ResizeImage(8, 8);
		(resized[0] == 0xFF && resized[1] == 0xD8).AssertTrue();
		resized.GetImageSize().Width.AssertEqual(8);
		var marked = _ycckFixture.AddTextWatermark("Hello",opacity:0,margin:0);
		marked.GetImageSize().Width.AssertEqual(15);
		marked.GetImageSize().Height.AssertEqual(11);
	}

	[TestMethod]
	public void ProgressiveJpeg_CompareAllRgbPixelsAgainstLibjpeg()
		=> CompareReference(_progressive, ProgressiveReference, 5);

	[TestMethod]
	public void AdobeCmykJpeg_CompareAllRgbPixelsAgainstLibjpeg()
		=> CompareReference(_cmyk, CmykReference, 5);

	[TestMethod]
	public void ProgressiveGrayJpeg_CompareAllRgbPixelsAgainstLibjpeg()
		=> CompareReference(_grayProgressive, GrayReference, 4);

	private static void CompareReference(byte[] jpeg, string reference, int tolerance)
	{
		using var compressed = new MemoryStream(Convert.FromBase64String(reference));
		using var zlib = new ZLibStream(compressed, CompressionMode.Decompress);
		using var expectedStream = new MemoryStream();
		zlib.CopyTo(expectedStream);
		var expected = expectedStream.ToArray();
		if (expected.Length != 1024)
			Assert.Fail("Independent RGB reference is invalid.");

		// Production PNG encoder is verified separately by strict independent
		// tests; decode its unfiltered RGBA8 IDAT bytes without JpegCodec.
		var png = jpeg.ConvertToPng();
		var actual = ReadPngRgba8(png);
		if (actual.Length != expected.Length)
			Assert.Fail($"Pixel buffer size mismatch: {actual.Length} != {expected.Length}");

		var mismatches = 0;
		var largestDifference = 0;
		var firstMismatch = -1;
		for (var i = 0; i < actual.Length; i++)
		{
			var diff = Math.Abs(actual[i] - expected[i]);
			largestDifference = Math.Max(largestDifference, diff);
			if (diff <= tolerance) continue;
			mismatches++;
			if (firstMismatch < 0) firstMismatch = i;
		}

		if (mismatches != 0)
			Assert.Fail($"{mismatches} channel mismatches (maximum difference {largestDifference}), " +
				$"first at ({firstMismatch / 4 % 16},{firstMismatch / 4 / 16}), " +
				$"channel {"RGBA"[firstMismatch % 4]}: actual={actual[firstMismatch]}, expected={expected[firstMismatch]}, " +
				$"per-channel tolerance={tolerance}");
	}

	private static byte[] ReadPngRgba8(byte[] png, int width = 16, int height = 16)
	{
		using var idat = new MemoryStream();
		for (var position = 8; position + 12 <= png.Length;)
		{
			var count = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(position, 4));
			if (count < 0 || count > png.Length - position - 12)
				throw new InvalidDataException("Corrupted output PNG.");
			if (png.AsSpan(position + 4, 4).SequenceEqual("IDAT"u8))
				idat.Write(png, position + 8, count);
			position += count + 12;
		}
		idat.Position = 0;
		using var output = new MemoryStream();
		using (var stream = new ZLibStream(idat, CompressionMode.Decompress))
			stream.CopyTo(output);

		var data = output.ToArray();
		var result = new byte[width * height * 4];
		if (data.Length != height * (1 + width * 4))
			throw new InvalidDataException("Unexpected output PNG dimension.");
		for (var y = 0; y < height; y++)
		{
			var stride = width * 4 + 1;
			if (data[y * stride] != 0)
				throw new InvalidDataException("Output PNG uses an unexpected scanline filter.");
			Array.Copy(data, y * stride + 1, result, y * width * 4, width * 4);
		}
		return result;
	}

}
