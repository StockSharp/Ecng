namespace Ecng.Tests.Drawing;

#if NET10_0
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

using Ecng.Drawing;

using static ImageTestHelper;

[TestClass]
[TestCategory("Integration")]
[TestCategory("DrawingBenchmark")]
[DoNotParallelize]
public class ImageCodecBaselineBenchmarkTests : BaseTestClass
{
	[TestMethod]
	[DataRow("bands", 513, 385)]
	[DataRow("bands", 1025, 769)]
	[DataRow("bands", 2049, 769)]
	[DataRow("noise", 513, 385)]
	[DataRow("noise", 1025, 769)]
	[DataRow("noise", 2049, 769)]
	public void EncoderBoundarySizes_PreserveThePreviousEncodedBytes(string pattern, int width, int height)
	{
		var baselinePath = TestContext.Properties["EcngDrawingBaselinePath"] as string;
		IsTrue(!string.IsNullOrEmpty(baselinePath) && File.Exists(baselinePath),
			"Set EcngDrawingBaselinePath to a preserved Ecng.Drawing assembly for the current framework.");
		var context = new AssemblyLoadContext("JPEG encoder baseline", isCollectible: true);
		try
		{
			var assembly = context.LoadFromAssemblyPath(Path.GetFullPath(baselinePath));
			var rasterType = assembly.GetType("Ecng.Drawing.RasterImage", throwOnError: true);
			var encoderType = assembly.GetType("Ecng.Drawing.JpegEncoder", throwOnError: true);
			var previous = Activator.CreateInstance(rasterType, width, height);
			var image = new RasterImage(width, height);
			SyntheticImageCorpus.Pixels(pattern, width, height).CopyTo(image.Pixels, 0);
			image.Pixels.CopyTo((byte[])rasterType.GetProperty(nameof(RasterImage.Pixels)).GetValue(previous), 0);
			var method = encoderType.GetMethod(nameof(JpegEncoder.Encode), BindingFlags.Public | BindingFlags.Static);
			var expected = (byte[])method.Invoke(null, [previous, 100, Array.Empty<byte>()]);
			var actual = JpegEncoder.Encode(image, 100, []);
			IsTrue(expected.AsSpan().SequenceEqual(actual), "Optimizing the JPEG encoder changed its encoded bytes.");
			var decoderType = assembly.GetType("Ecng.Drawing.JpegCodec", throwOnError: true);
			var decode = decoderType.GetMethod(nameof(JpegCodec.Decode), BindingFlags.Public | BindingFlags.Static,
				binder: null, [typeof(byte[])], modifiers: null);
			var previousImage = decode.Invoke(null, [expected]);
			var previousPixels = (byte[])rasterType.GetProperty(nameof(RasterImage.Pixels)).GetValue(previousImage);
			AssertPixels(previousPixels, JpegCodec.Decode(actual).Pixels, width);
			TestContext.WriteLine($"{pattern} {width}x{height}: {actual.Length} encoded bytes and all decoded pixels identical to the preserved assembly.");
		}
		finally
		{
			context.Unload();
		}
	}

	[TestMethod]
	[DataRow("bands", "png", 512, ImageFormats.Jpeg)]
	[DataRow("texture", "png", 512, ImageFormats.Jpeg)]
	[DataRow("texture", "jpeg", 600, ImageFormats.Jpeg)]
	[DataRow("lines", "jpeg", 100, ImageFormats.Png)]
	public void PreviousAndCurrentCodecs_ReportAlternatingTimesInTheSameProcess(string pattern, string input, int bounds, ImageFormats output)
	{
		var baselinePath = TestContext.Properties["EcngDrawingBaselinePath"] as string;
		IsTrue(!string.IsNullOrEmpty(baselinePath) && File.Exists(baselinePath),
			"Set EcngDrawingBaselinePath to a preserved Ecng.Drawing assembly for the current framework.");
		var context = new AssemblyLoadContext("Drawing baseline", isCollectible: true);
		try
		{
			var assembly = context.LoadFromAssemblyPath(Path.GetFullPath(baselinePath));
			var format = assembly.GetType("Ecng.Drawing.ImageFormats", throwOnError: true);
			var helper = assembly.GetType("Ecng.Drawing.ImageHelper", throwOnError: true);
			var method = helper.GetMethod(nameof(ImageHelper.ResizeImage), BindingFlags.Public | BindingFlags.Static,
				binder: null, [typeof(byte[]), typeof(int), typeof(int), format, typeof(int)], modifiers: null);
			IsNotNull(method);
			using var reference = new ImageSharpCodecReference(TestContext);
			var width = input == "png" ? 512 : 960;
			var height = input == "png" ? 384 : 640;
			var pixels = SyntheticImageCorpus.Pixels(pattern, width, height);
			var png = CreatePng(width, height, pixels);
			var source = input == "png" ? png : reference.Processor(reference.Encoder(ImageFormats.Jpeg, 90))(png);
			object[] arguments = [source, bounds, bounds, Enum.ToObject(format, (int)output), 90];
			byte[] Before() => (byte[])method.Invoke(null, arguments);
			byte[] After() => source.ResizeImage(bounds, bounds, output, 90);
			for (var warmup = 0; warmup < 32; warmup++)
			{
				Before();
				After();
			}
			var beforeTimes = new double[9];
			var afterTimes = new double[9];
			for (var sample = 0; sample < beforeTimes.Length; sample++)
			{
				CancellationToken.ThrowIfCancellationRequested();
				if (sample % 2 == 0)
				{
					beforeTimes[sample] = Measure(Before);
					afterTimes[sample] = Measure(After);
				}
				else
				{
					afterTimes[sample] = Measure(After);
					beforeTimes[sample] = Measure(Before);
				}
			}
			var beforePixels = reference.Decode(Before(), out var beforeSize);
			var afterPixels = reference.Decode(After(), out var afterSize);
			AreEqual(beforeSize, afterSize);
			AssertPixels(beforePixels, afterPixels, beforeSize.Width);
			var before = beforeTimes.Order().ElementAt(4);
			var after = afterTimes.Order().ElementAt(4);
			TestContext.WriteLine($"{pattern}-{input}-{bounds}-{output}: before {before:F4} ms; after {after:F4} ms; speedup {before / after:F3}x; decoded pixels identical.");
		}
		finally
		{
			context.Unload();
		}
	}

	private static double Measure(Func<byte[]> action)
	{
		var start = Stopwatch.GetTimestamp();
		action();
		return Stopwatch.GetElapsedTime(start).TotalMilliseconds;
	}
}
#endif
