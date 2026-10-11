#if NET10_0
namespace Ecng.Tests.Drawing;

using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.Loader;

using Ecng.Drawing;

using static ImageTestHelper;
using static ImageDebtReproductionTests;

/// <summary>
/// Opt-in measurements against the site's compiled ImageSharp implementation. Timing is reported,
/// not asserted: the debt log has no accepted performance threshold or committed corpus.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("Performance")]
[DoNotParallelize]
public class ImageDebtPerformanceTests : BaseTestClass
{
	[TestMethod]
	[DataRow("png.ico")]
	[DataRow("dib.ico")]
	[DataRow("black.tiff")]
	[DataRow("lossy.webp")]
	[DataRow("lossless.webp")]
	[DataRow("alpha.webp")]
	public async Task MissingFormatFixtures_AreReadableByThePreviousSiteDecoder(string name)
	{
		var data = name.EndsWith(".ico", StringComparison.Ordinal) ? Icon(name == "png.ico") : Fixture(name);
		using var reference = new SiteImageReference(TestContext);
		var png = await reference.Process(data, ImageOperations.Convert, 0, CancellationToken);
		var decoded = StbImageSharp.ImageResult.FromMemory(png, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
		AreEqual(32, decoded.Width);
		AreEqual(24, decoded.Height);
		AssertPixels(Rgba(32, 24, (_, _) => (0, 0, 0, name == "alpha.webp" ? (byte)64 : (byte)255)), decoded.Data, 32);
	}

	[TestMethod]
	[DataRow("png")]
	[DataRow("jpeg")]
	public async Task LargeFixtures_AreCompleteImagesAcceptedByThePreviousSiteDecoder(string format)
	{
		var data = format == "png" ? LargePng() : Fixture("large-48mp.jpg");
		using var reference = new SiteImageReference(TestContext);
		AreEqual(new Size(8000, 6000), await reference.GetSize(data, CancellationToken));
	}

	[TestMethod]
	[DataRow("png", ImageOperations.Resize, 100)]
	[DataRow("jpeg", ImageOperations.Resize, 100)]
	[DataRow("png", ImageOperations.Resize, 600)]
	[DataRow("jpeg", ImageOperations.Resize, 600)]
	[DataRow("png", ImageOperations.Convert, 0)]
	[DataRow("jpeg", ImageOperations.Convert, 0)]
	[DataRow("png", ImageOperations.Watermark, 0)]
	[DataRow("jpeg", ImageOperations.Watermark, 0)]
	public async Task ImageOperations_ReportSerialWarmMedianAgainstImageSharp(string format, ImageOperations operation, int bounds)
	{
		const int width = 1366;
		const int height = 768;
		var pixels = Rgba(width, height, (x, y) => ((byte)(x / 7), (byte)(y / 3), (byte)((x + y) / 11), 255));
		var png = CreatePng(width, height, pixels);
		var data = png;
		if (format == "jpeg")
		{
			var image = new RasterImage(width, height);
			pixels.CopyTo(image.Pixels, 0);
			data = JpegEncoder.Encode(image, 90, []);
		}

		using var reference = new SiteImageReference(TestContext);
		Task<byte[]> Current()
			=> Task.FromResult(operation switch
			{
				ImageOperations.Resize => data.ResizeImage(bounds, bounds),
				ImageOperations.Convert => data.ConvertToPng(),
				ImageOperations.Watermark => data.AddTextWatermark("StockSharp", fontSize: width / 30f * 72 / 96,
					opacity: 75, margin: width / 100),
				_ => throw new ArgumentOutOfRangeException(nameof(operation)),
			});
		Task<byte[]> Previous() => reference.Process(data, operation, bounds, CancellationToken);

		// Warm both implementations, then alternate order to reduce first-run and thermal bias.
		Validate(await Current());
		Validate(await Previous());
		var current = new double[3];
		var previous = new double[3];
		for (var sample = 0; sample < current.Length; sample++)
		{
			CancellationToken.ThrowIfCancellationRequested();
			if (sample % 2 == 0)
			{
				current[sample] = await Measure(Current);
				previous[sample] = await Measure(Previous);
			}
			else
			{
				previous[sample] = await Measure(Previous);
				current[sample] = await Measure(Current);
			}
		}
		Array.Sort(current);
		Array.Sort(previous);
		TestContext.WriteLine($"Synthetic {format}, {width}x{height}, {operation}({bounds}): " +
			$"ImageSharp {previous[1]:F3} ms; Ecng {current[1]:F3} ms; ratio {current[1] / previous[1]:F2}. " +
			"Serial warm medians of 3 samples; input is not the site's corpus.");

		async Task<double> Measure(Func<Task<byte[]>> action)
		{
			var watch = Stopwatch.StartNew();
			var result = await action();
			watch.Stop();
			Validate(result);
			return watch.Elapsed.TotalMilliseconds;
		}

		void Validate(byte[] result)
		{
			var size = result.GetImageSize();
			if (operation == ImageOperations.Resize)
			{
				IsTrue(size.Width > 0 && size.Width <= bounds && size.Height > 0 && size.Height <= bounds);
				IsTrue(Math.Abs((double)size.Width / size.Height - (double)width / height) < 0.04);
			}
			else
			{
				AreEqual(width, size.Width);
				AreEqual(height, size.Height);
			}
		}
	}
}

/// <summary>
/// Uses an existing compiled site assembly, without adding ImageSharp dependencies to Ecng or
/// replacing its own font/image packages. The selected assembly must still use ImageSharp.
/// </summary>
internal sealed class SiteImageReference : IDisposable
{
	private readonly string _directory;
	private readonly Type _extensions;

	public SiteImageReference(TestContext context)
	{
		_directory = context.Properties["ImageSharpReferencePath"] as string;
		if (string.IsNullOrEmpty(_directory))
			throw new InvalidOperationException("Set ImageSharpReferencePath to the directory of a site build that still uses ImageSharp.");
		AssemblyLoadContext.Default.Resolving += Resolve;
		try
		{
			var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(_directory, "StockSharp.Web.Api.Data.dll"));
			_extensions = assembly.GetType("StockSharp.Web.Api.Data.Extensions.ImageExtensions", throwOnError: true);
			if (_extensions.GetMethod("GetSizeAsync", BindingFlags.Public | BindingFlags.Static) is null)
				throw new InvalidOperationException("The supplied site assembly has already migrated away from ImageSharp.");
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	public async Task<byte[]> Process(byte[] data, ImageOperations operation, int bounds, CancellationToken cancellationToken)
	{
		var (name, arguments) = operation switch
		{
			ImageOperations.Resize => ("ShrinkImage", new object[] { data, bounds, bounds, cancellationToken }),
			ImageOperations.Convert => ("ToPng", new object[] { data, cancellationToken }),
			ImageOperations.Watermark => ("WatermarkImage", new object[] { data, null, "StockSharp", cancellationToken }),
			_ => throw new ArgumentOutOfRangeException(nameof(operation)),
		};
		var method = _extensions.GetMethod(name, BindingFlags.Public | BindingFlags.Static)
			?? throw new MissingMethodException(_extensions.FullName, name);
		return await (ValueTask<byte[]>)method.Invoke(null, arguments);
	}

	public async Task<Size> GetSize(byte[] data, CancellationToken cancellationToken)
	{
		var method = _extensions.GetMethod("GetSizeAsync", BindingFlags.Public | BindingFlags.Static)
			?? throw new MissingMethodException(_extensions.FullName, "GetSizeAsync");
		var (width, height) = await (ValueTask<(int, int)>)method.Invoke(null, [data, cancellationToken]);
		return new Size(width, height);
	}

	private Assembly Resolve(AssemblyLoadContext context, AssemblyName name)
	{
		var path = Path.Combine(_directory, name.Name + ".dll");
		return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
	}

	public void Dispose() => AssemblyLoadContext.Default.Resolving -= Resolve;
}
#endif
