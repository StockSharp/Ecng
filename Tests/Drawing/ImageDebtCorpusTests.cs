namespace Ecng.Tests.Drawing;

#if NET10_0
using System.Diagnostics;
using System.Drawing;
using System.Globalization;

using Ecng.Drawing;

using StbImageSharp;

using static ImageDebtReproductionTests;

/// <summary>
/// Opt-in checks of private site files against an independent decoder. The corpus stays outside Git.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("DrawingCorpus")]
[DoNotParallelize]
public class ImageDebtCorpusTests : BaseTestClass
{
	public static IEnumerable<object[]> CorpusCases()
	{
		var directory = Environment.GetEnvironmentVariable("ECNG_DRAWING_CORPUS_PATH");
		if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
		{
			yield return ["", ImageOperations.Size, 0];
			yield break;
		}
		var files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
			.Where(path => new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".ico", ".tiff", ".avif" }
				.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
			.OrderBy(path => path, StringComparer.Ordinal).ToArray();
		if (files.Length == 0)
			yield return ["", ImageOperations.Size, 0];
		foreach (var file in files)
		{
			yield return [file, ImageOperations.Size, 0];
			yield return [file, ImageOperations.Resize, 100];
			yield return [file, ImageOperations.Resize, 600];
			yield return [file, ImageOperations.Convert, 0];
			yield return [file, ImageOperations.Watermark, 0];
		}
	}

	[TestMethod]
	[DynamicData(nameof(CorpusCases))]
	public async Task SiteImages_EachPublicOperationIsIndependentlyChecked(string path, ImageOperations operation, int bounds)
	{
		IsFalse(string.IsNullOrEmpty(path),
			"Set ECNG_DRAWING_CORPUS_PATH to the private site corpus directory.");
		var data = await File.ReadAllBytesAsync(path, CancellationToken);
		using var reference = new ImageSharpCodecReference(TestContext);
		var sourceSize = reference.GetSize(data);
		AreEqual(sourceSize, data.GetImageSize());
		var format = data.GetImageFormat();
		if (operation == ImageOperations.Size)
		{
			IsTrue(data.TryGetImageSize(out var size));
			AreEqual(sourceSize, size);
			Record(path, format, operation, bounds, data.Length, 0, 0, "Header", 0, 0);
			return;
		}

		byte[] Process() => operation switch
		{
			ImageOperations.Resize => data.ResizeImage(bounds, bounds),
			ImageOperations.Convert => data.ConvertToPng(),
			ImageOperations.Watermark => data.AddTextWatermark("StockSharp", fontSize: sourceSize.Width / 30f * 72 / 96,
				opacity: 75, margin: sourceSize.Width / 100),
			_ => throw new ArgumentOutOfRangeException(nameof(operation)),
		};
		var start = Stopwatch.GetTimestamp();
		if (format == ImageFormats.Gif && Path.GetFileNameWithoutExtension(path) is "112228" or "112230" or "155349" or "155401")
		{
			var error = ThrowsExactly<InvalidDataException>(() => Process());
			IsTrue(error.Message.Contains("100000000", StringComparison.Ordinal), error.Message);
			Record(path, format, operation, bounds, data.Length, 0, Stopwatch.GetElapsedTime(start).TotalMilliseconds, "AnimationLimit", 0, 0);
			return;
		}
		var current = Process();
		var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
		var decoded = reference.Decode(current, out var currentSize);
		AreEqual(currentSize, current.GetImageSize());
		var divisor = operation == ImageOperations.Resize ? Math.Max(Math.Max(sourceSize.Width, sourceSize.Height), bounds) : 1;
		var numerator = operation == ImageOperations.Resize ? bounds : 1;
		AreEqual(new Size((int)Math.Max(1, (long)sourceSize.Width * numerator / divisor),
			(int)Math.Max(1, (long)sourceSize.Height * numerator / divisor)), currentSize);
		var expectedFormat = operation == ImageOperations.Convert || format is ImageFormats.Tiff or ImageFormats.Avif ? ImageFormats.Png : format;
		AreEqual(expectedFormat, current.GetImageFormat());
		if (operation == ImageOperations.Convert)
		{
			// BI_RGB32 has a reserved fourth byte, not alpha. Both reference libraries interpret
			// this site's unused bytes as transparency; request RGB and use the specified opaque alpha.
			var isRgb32Bmp = format == ImageFormats.Bmp && BitConverter.ToUInt32(data, 14) == 40
				&& BitConverter.ToUInt16(data, 28) == 32 && BitConverter.ToUInt32(data, 30) == 0;
			var expected = isRgb32Bmp ? new byte[decoded.Length] : reference.Decode(data, out _);
			if (isRgb32Bmp)
			{
				var rgb = ImageResult.FromMemory(data, ColorComponents.RedGreenBlue).Data;
				AreEqual(expected.Length / 4 * 3, rgb.Length);
				for (var pixel = 0; pixel < rgb.Length / 3; pixel++)
				{
					rgb.AsSpan(pixel * 3, 3).CopyTo(expected.AsSpan(pixel * 4, 3));
					expected[pixel * 4 + 3] = 255;
				}
			}
			AreEqual(expected.Length, decoded.Length);
			var tolerance = format is ImageFormats.Jpeg or ImageFormats.Webp ? 2 : 0;
			for (var offset = 0; offset < expected.Length; offset += 4)
			{
				if (expected[offset + 3] != decoded[offset + 3])
					Fail($"Alpha at pixel {offset / 4}: {expected[offset + 3]} vs {decoded[offset + 3]}.");
				if (expected[offset + 3] == 0)
					continue;
				for (var channel = 0; channel < 3; channel++)
					if (Math.Abs(expected[offset + channel] - decoded[offset + channel]) > tolerance)
						Fail($"Pixel {offset / 4}, channel {channel}: {expected[offset + channel]} vs {decoded[offset + channel]}.");
			}
		}
		var equivalentPngBytes = 0;
		var independentWebpBytes = 0;
		if (format == ImageFormats.Webp && operation == ImageOperations.Resize && currentSize != sourceSize)
		{
			var png = current.ConvertToPng();
			var pngPixels = reference.Decode(png, out var pngSize);
			AreEqual(currentSize, pngSize);
			IsTrue(decoded.AsSpan().SequenceEqual(pngPixels), "Lossless WebP and its PNG must contain the same RGBA pixels.");
			IsTrue(current.Length <= png.Length, $"Lossless WebP {current.Length} bytes exceeds equivalent PNG {png.Length} bytes.");
			equivalentPngBytes = png.Length;
			independentWebpBytes = reference.Processor(reference.Encoder(ImageFormats.Webp, 90))(png).Length;
		}
		Record(path, format, operation, bounds, data.Length, current.Length, elapsed, "Decoded", equivalentPngBytes, independentWebpBytes);
	}

	private void Record(string path, ImageFormats format, ImageOperations operation, int bounds, int inputBytes, int outputBytes, double elapsed, string status, int equivalentPngBytes, int independentWebpBytes)
	{
		var directory = TestContext.Properties["ImageCorpusOutputPath"] as string ?? TestContext.ResultsDirectory;
		Directory.CreateDirectory(directory);
		var csv = Path.Combine(directory, "site-corpus.csv");
		if (!File.Exists(csv)) File.WriteAllText(csv, "file,format,operation,bounds,input_bytes,output_bytes,ecng_ms,status,equivalent_png_bytes,independent_lossless_webp_bytes\n");
		File.AppendAllText(csv, $"{Path.GetFileName(path)},{format},{operation},{bounds},{inputBytes},{outputBytes},{elapsed.ToString(CultureInfo.InvariantCulture)},{status},{equivalentPngBytes},{independentWebpBytes}\n");
		TestContext.WriteLine($"{Path.GetFileName(path)}: {operation} {bounds}, {status}, {outputBytes} bytes, {elapsed:F3} ms.");
	}

	public static IEnumerable<object[]> LargeAnimationCases()
	{
		foreach (var name in new[] { "112228.gif", "112230.gif", "155349.gif", "155401.gif" })
		{
			yield return [name, ImageOperations.Resize, 100];
			yield return [name, ImageOperations.Resize, 600];
			yield return [name, ImageOperations.Convert, 0];
			yield return [name, ImageOperations.Watermark, 0];
		}
	}

	[TestMethod]
	[DynamicData(nameof(LargeAnimationCases))]
	public async Task LargeSiteAnimations_ExplicitBudgetPreservesEveryFrame(string name, ImageOperations operation, int bounds)
	{
		var directory = Environment.GetEnvironmentVariable("ECNG_DRAWING_CORPUS_PATH");
		IsTrue(!string.IsNullOrEmpty(directory) && Directory.Exists(directory), "Set ECNG_DRAWING_CORPUS_PATH to the private site corpus.");
		var path = Directory.EnumerateFiles(directory, name, SearchOption.AllDirectories).Single();
		var data = await File.ReadAllBytesAsync(path, CancellationToken);
		var limits = new ImageProcessingLimits(25_000_000, 512, 160_000_000);
		var size = data.GetImageSize();
		var start = Stopwatch.GetTimestamp();
		var current = operation switch
		{
			ImageOperations.Resize => data.ResizeImage(bounds, bounds, limits),
			ImageOperations.Convert => data.ConvertToPng(limits),
			ImageOperations.Watermark => data.AddTextWatermark("StockSharp", limits, fontSize: size.Width / 30f * 72 / 96, opacity: 75, margin: size.Width / 100),
			_ => throw new ArgumentOutOfRangeException(nameof(operation)),
		};
		var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
		using var reference = new ImageSharpCodecReference(TestContext);
		AreEqual(reference.GetFrameCount(data), reference.GetFrameCount(current));
		AreEqual(current.GetImageSize(), reference.GetSize(current));
		if (operation != ImageOperations.Resize) AreEqual(size, current.GetImageSize());
		Record(path, ImageFormats.Gif, operation, bounds, data.Length, current.Length, elapsed, "ExplicitAnimationBudget", 0, 0);
	}
}
#endif
