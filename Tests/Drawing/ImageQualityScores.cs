namespace Ecng.Tests.Drawing;

internal sealed record ImageQualityScores(double RgbMae, double RgbRmse, double RgbPsnr, double LumaSsim, double AlphaMae, double MaxRgbError);
