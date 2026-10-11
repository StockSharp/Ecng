#if NET10_0
namespace Ecng.Tests.Drawing;

using Ecng.Drawing;

using StbImageSharp;

using static ImageDebtReproductionTests;

/// <summary>
/// Opt-in differential checks of an externally supplied site corpus. File bodies must be obtained
/// through the administrative API, not the download endpoint which increments site counters.
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
			yield return ["", ImageOperations.Size];
			yield break;
		}
		var files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
			.Where(path => Path.GetExtension(path).Equals(".gif", StringComparison.OrdinalIgnoreCase)
				|| Path.GetExtension(path).Equals(".bmp", StringComparison.OrdinalIgnoreCase))
			.OrderBy(path => path, StringComparer.Ordinal).ToArray();
		if (files.Length == 0)
			yield return ["", ImageOperations.Size];
		foreach (var file in files)
			foreach (var operation in Enum.GetValues<ImageOperations>())
				yield return [file, operation];
	}

	[TestMethod]
	[DynamicData(nameof(CorpusCases))]
	public async Task SiteGifAndBmp_EachPublicOperationMatchesReferenceDimensions(string path, ImageOperations operation)
	{
		IsFalse(string.IsNullOrEmpty(path),
			"Set ECNG_DRAWING_CORPUS_PATH to the administrative API export containing site GIF/BMP bodies.");
		var data = await File.ReadAllBytesAsync(path, CancellationToken);
		using var reference = new SiteImageReference(TestContext);
		if (operation == ImageOperations.Size)
		{
			AreEqual(await reference.GetSize(data, CancellationToken), data.GetImageSize());
			return;
		}

		var current = operation switch
		{
			ImageOperations.Resize => data.ResizeImage(100, 100),
			ImageOperations.Convert => data.ConvertToPng(),
			ImageOperations.Watermark => data.AddTextWatermark("StockSharp", fontSize: data.GetImageSize().Width / 30f * 72 / 96,
				opacity: 75, margin: data.GetImageSize().Width / 100),
			_ => throw new ArgumentOutOfRangeException(nameof(operation)),
		};
		var previous = await reference.Process(data, operation, 100, CancellationToken);
		var currentSize = current.GetImageSize();
		var previousSize = previous.GetImageSize();
		if (operation == ImageOperations.Resize)
		{
			IsTrue(currentSize.Width > 0 && currentSize.Width <= 100 && currentSize.Height > 0 && currentSize.Height <= 100);
			if (previousSize.Width <= 100 && previousSize.Height <= 100)
			{
				IsTrue(Math.Abs(previousSize.Width - currentSize.Width) <= 1);
				IsTrue(Math.Abs(previousSize.Height - currentSize.Height) <= 1);
			}
			else
				TestContext.WriteLine($"The old site resize exceeds the requested bounds: {previousSize}.");
		}
		else
			AreEqual(previousSize, currentSize);
		if (operation == ImageOperations.Convert)
		{
			// The old site writes only the first GIF frame to PNG. Ecng preserves animation as APNG;
			// compare the first displayed frame without treating that intentional difference as a bug.
			var expected = ImageResult.FromMemory(previous, ColorComponents.RedGreenBlueAlpha);
			var actual = ImageResult.FromMemory(current, ColorComponents.RedGreenBlueAlpha);
			AreEqual(expected.Data.Length, actual.Data.Length);
			for (var offset = 0; offset < expected.Data.Length; offset += 4)
			{
				AreEqual(expected.Data[offset + 3], actual.Data[offset + 3], $"Alpha at pixel {offset / 4}.");
				if (expected.Data[offset + 3] == 0)
					continue;
				for (var channel = 0; channel < 3; channel++)
					AreEqual(expected.Data[offset + channel], actual.Data[offset + channel], $"Pixel {offset / 4}, channel {channel}.");
			}
		}
		TestContext.WriteLine($"{Path.GetFileName(path)}: {operation}, " +
			$"Ecng {current.Length} bytes, ImageSharp {previous.Length} bytes, size {current.GetImageSize()}.");
	}
}
#endif
