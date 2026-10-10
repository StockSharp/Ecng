namespace Ecng.Drawing;

using System;
using System.IO;

/// <summary>
/// The font the library carries for a machine that has none to offer: DejaVu Sans, cut down to the Latin,
/// Greek and Cyrillic letters, punctuation, currency signs, arrows and a few symbols.
/// </summary>
internal static class BuiltInFont
{
	/// <summary>
	/// The family name the font carries.
	/// </summary>
	public const string Family = "DejaVu Sans";

	/// <summary>
	/// The name the file of the font has where it is installed, without its extension.
	/// </summary>
	public const string FileName = "DejaVuSans";

	private const string _resource = "Ecng.Drawing.Fonts.DejaVuSans.ttf";

	// A server draws many watermarks: the resource is read once.
	private static readonly Lazy<byte[]> _data = new(Read);

	/// <summary>
	/// The bytes of the font file. They are shared by every caller and must only be read.
	/// </summary>
	public static byte[] Data => _data.Value;

	private static byte[] Read()
	{
		using var resource = typeof(BuiltInFont).Assembly.GetManifestResourceStream(_resource)
			?? throw new InvalidOperationException($"The assembly lacks its resource '{_resource}'.");
		using var buffer = new MemoryStream();

		resource.CopyTo(buffer);
		return buffer.ToArray();
	}
}
