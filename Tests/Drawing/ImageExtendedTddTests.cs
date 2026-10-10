namespace Ecng.Tests.Drawing;

using System;
using System.IO;
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
		"/9j/7gAOQWRvYmUAZAAAAAAA/9sAQwADAgICAgIDAgICAwMDAwQGBAQEBAQIBgYFBgkICgoJCAkJCgwPDAoLDgsJCQ0RDQ4PEBAREAoMEhMSEBMPEBAQ/8AAFAgAEAAQBEMRAE0RAFkRAP/EAB8AAAEFAQEBAQEBAAAAAAAAAAABAgMEBQYHCAkKC//EALUQAAIBAwMCBAMFBQQEAAABfQECAwAEEQUSITFBBhNRYQcicRQygZGhCCNCscEVUtHwJDNicoIJChYXGBkaJSYnKCkqNDU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6g4SFhoeIiYqSk5SVlpeYmZqio6Slpqeoqaqys7S1tre4ubrCw8TFxsfIycrS09TV1tfY2drh4uPk5ebn6Onq8fLz9PX29/j5+v/aAA4EQwBNAFkASwAAPwD7brKr5bZlKs7uhUrK7M85ZSpO2VmlXllLfLLMvzStiGHCgmv1Tr8BKKd+8WT/AJarKsn+xHIJET/viORE/wC2dpH6yGiv37r9CqP3iyf8tVlWT/YjkEiJ/wB8RyIn/bO0j9ZDRX4CUU1VUqqIiFSsSKqQFlKk7olWJuWUt80ULfNK2ZpsKAKK/9k=");

	private static readonly byte[] _grayProgressive = Convert.FromBase64String(
		"/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAUDBAQEAwUEBAQFBQUGBwwIBwcHBw8LCwkMEQ8SEhEPERETFhwXExQaFRERGCEYGh0dHx8fExciJCIeJBweHx7/wgALCAAQABABAREA/8QAFgABAQEAAAAAAAAAAAAAAAAABgQH/9oACAEBAAAAAaiWuEf/xAAUEAEAAAAAAAAAAAAAAAAAAAAg/9oACAEBAAEFAh//xAAUEAEAAAAAAAAAAAAAAAAAAAAg/9oACAEBAAY/Ah//xAAUEAEAAAAAAAAAAAAAAAAAAAAg/9oACAEBAAE/IR//2gAIAQEAAAAQH//EABcQAQEBAQAAAAAAAAAAAAAAAPAAIRD/2gAIAQEAAT8QhkM5/9k=");

	[TestMethod]
	public void Red_ResizeJpeg_PreservesJpegFormatAndCanBeDecoded()
	{
		var output = _baseline.ResizeImage(8, 8);
		(output.Length > 4 && output[0] == 0xFF && output[1] == 0xD8 && output[^2] == 0xFF && output[^1] == 0xD9).AssertTrue();
		output.GetImageSize().Width.AssertEqual(8);
		output.GetImageSize().Height.AssertEqual(8);
		var pixels = output.ConvertToPng();
		pixels.GetPngSize().Width.AssertEqual(8);
	}

	[TestMethod]
	public void Red_ResizeJpeg_WhenAlreadySmall_PreservesOriginalBytes()
	{
		var output = _baseline.ResizeImage(64, 64);
		_baseline.SequenceEqual(output).AssertTrue();
	}

	[TestMethod]
	public void Red_ProgressiveJpeg_DecodesFullColorImage()
	{
		var png = _progressive.ConvertToPng();
		png.GetPngSize().Width.AssertEqual(16);
		png.GetPngSize().Height.AssertEqual(16);
		_progressive.ResizeImage(8, 8).GetImageSize().Width.AssertEqual(8);
	}

	[TestMethod]
	public void Red_ProgressiveGrayJpeg_DecodesAndResizes()
	{
		var png = _grayProgressive.ConvertToPng();
		png.GetPngSize().Width.AssertEqual(16);
		_grayProgressive.ResizeImage(8, 8).GetImageSize().Width.AssertEqual(8);
	}

	[TestMethod]
	public void Red_AdobeCmykJpeg_DecodesWithNonGrayColors()
	{
		var png = _cmyk.ConvertToPng();
		png.GetPngSize().Width.AssertEqual(16);
		png.GetPngSize().Height.AssertEqual(16);
		_cmyk.ResizeImage(8, 8).GetImageSize().Width.AssertEqual(8);
	}

	[TestMethod]
	public void Red_Watermark_UnknownFamilyFallbackWithoutPrivateTtf()
	{
		var png = _baseline.ConvertToPng();
		var output = png.AddTextWatermark("StockSharp", fontSize: 7, margin: 1,
			fontFamily: "__a_missing_font_family_6bf471__");
		output.GetPngSize().Width.AssertEqual(16);
		output.GetPngSize().Height.AssertEqual(16);
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
}
