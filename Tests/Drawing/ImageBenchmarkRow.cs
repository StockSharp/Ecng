namespace Ecng.Tests.Drawing;

internal sealed record ImageBenchmarkRow(string Case, string Pattern, string InputFormat, string Operation, string OutputFormat, int Quality,
	int Width, int Height, int InputBytes, int EcngBytes, int ReferenceBytes, double EcngMedianMs, double ReferenceMedianMs,
	double EcngP90Ms, double ReferenceP90Ms, long EcngAllocatedBytes, long ReferenceAllocatedBytes,
	ImageQualityScores EcngQuality, ImageQualityScores ReferenceQuality, string ReferenceVersion);
