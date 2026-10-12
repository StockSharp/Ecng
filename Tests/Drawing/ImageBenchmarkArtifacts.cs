namespace Ecng.Tests.Drawing;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

internal static class ImageBenchmarkArtifacts
{
	private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true, NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals };

	public static string DirectoryFor(TestContext context)
	{
		var directory = context.Properties["ImageBenchmarkOutputPath"] as string ?? Path.Combine(context.ResultsDirectory, "image-benchmark");
		Directory.CreateDirectory(directory);
		return directory;
	}

	public static void Write(TestContext context, ImageBenchmarkRow row, byte[] source, byte[] current, byte[] reference)
	{
		var directory = DirectoryFor(context);
		var name = row.Case;
		File.WriteAllBytes(Path.Combine(directory, name + "-input." + Extension(row.InputFormat)), source);
		File.WriteAllBytes(Path.Combine(directory, name + "-ecng." + Extension(row.OutputFormat)), current);
		File.WriteAllBytes(Path.Combine(directory, name + "-reference." + Extension(row.OutputFormat)), reference);
		var json = Path.Combine(directory, name + ".json");
		File.WriteAllText(json, JsonSerializer.Serialize(row, _jsonOptions));
		context.AddResultFile(json);
		var csv = Path.Combine(directory, "measurements.csv");
		if (!File.Exists(csv))
			File.WriteAllText(csv, "case,pattern,input,operation,output,quality,width,height,input_bytes,ecng_bytes,reference_bytes,ecng_ms,reference_ms,ecng_p90_ms,reference_p90_ms,ecng_allocated_bytes,reference_allocated_bytes,ecng_psnr,reference_psnr,ecng_ssim,reference_ssim,ecng_mae,reference_mae,ecng_alpha_mae,reference_alpha_mae\n");
		File.AppendAllText(csv, string.Join(",", new object[]
		{
			row.Case, row.Pattern, row.InputFormat, row.Operation, row.OutputFormat, row.Quality, row.Width, row.Height,
			row.InputBytes, row.EcngBytes, row.ReferenceBytes, row.EcngMedianMs, row.ReferenceMedianMs, row.EcngP90Ms, row.ReferenceP90Ms,
			row.EcngAllocatedBytes, row.ReferenceAllocatedBytes, row.EcngQuality.RgbPsnr, row.ReferenceQuality.RgbPsnr,
			row.EcngQuality.LumaSsim, row.ReferenceQuality.LumaSsim, row.EcngQuality.RgbMae, row.ReferenceQuality.RgbMae,
			row.EcngQuality.AlphaMae, row.ReferenceQuality.AlphaMae,
		}.Select(value => Convert.ToString(value, CultureInfo.InvariantCulture))) + "\n");
		context.WriteLine($"{name}: Ecng {row.EcngBytes} bytes / {row.EcngMedianMs:F3} ms / {row.EcngQuality.RgbPsnr:F2} dB / SSIM {row.EcngQuality.LumaSsim:F6}; " +
			$"ImageSharp {row.ReferenceBytes} bytes / {row.ReferenceMedianMs:F3} ms / {row.ReferenceQuality.RgbPsnr:F2} dB / SSIM {row.ReferenceQuality.LumaSsim:F6}.");
	}

	private static string Extension(string format) => format.ToLowerInvariant() switch { "jpeg" => "jpg", "webp-lossy" or "webp-lossless" => "webp", var value => value };
}
