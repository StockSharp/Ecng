namespace Ecng.Drawing.Codecs.Jpeg;


internal static class JpegMarkerHelper
{
	public static bool IsRestartMarker(this JpegMarkers marker)
	{
		return JpegMarkers.DefineRestart0 <= marker && marker <= JpegMarkers.DefineRestart7;
	}
}
