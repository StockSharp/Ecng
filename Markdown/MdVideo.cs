namespace Ecng.Markdown;

using Ecng.Common;

/// <summary>
/// A video the author referenced, already resolved.
/// </summary>
/// <param name="url">Address the video plays from, empty when there is nothing to play.</param>
/// <param name="unavailableText">Why the video cannot be played, in the reader's language.</param>
public sealed class MdVideo(string url, string unavailableText) : MdInline
{
	/// <summary>
	/// Address the video plays from.
	/// </summary>
	public string Url { get; } = url;

	/// <summary>
	/// Why the video cannot be played.
	/// </summary>
	public string UnavailableText { get; } = unavailableText;

	/// <summary>
	/// Whether there is an address to play.
	/// </summary>
	public bool IsPlayable => !Url.IsEmpty();
}
