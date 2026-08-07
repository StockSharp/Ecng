namespace Ecng.Markdown;

/// <summary>
/// Image.
/// </summary>
/// <param name="url">Address the image is loaded from.</param>
/// <param name="alt">Text shown while the image is unavailable.</param>
public sealed class MdImage(string url, string alt) : MdInline
{
	/// <summary>
	/// Address the image is loaded from.
	/// </summary>
	public string Url { get; } = url;

	/// <summary>
	/// Text shown while the image is unavailable.
	/// </summary>
	public string Alt { get; } = alt;
}
