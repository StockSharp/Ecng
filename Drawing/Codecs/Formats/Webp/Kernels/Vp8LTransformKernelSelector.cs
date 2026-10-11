namespace Ecng.Drawing.Codecs.Formats.Webp.Kernels;

internal static class Vp8LTransformKernelSelector
{
	internal static IVp8LTransformKernel Instance { get; } = new ScalarVp8LTransformKernel();
}
