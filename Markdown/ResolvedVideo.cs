namespace Ecng.Markdown;

/// <summary>
/// A video reference resolved to either an address to play or the reason there is nothing to play.
/// </summary>
/// <param name="url">Address the video is played from. Empty when the video is not playable.</param>
/// <param name="unavailableText">
/// Why the video cannot be played, in the reader's language. Only the caller knows that language, so the
/// text is resolved rather than composed here. Empty renders nothing at all.
/// </param>
public readonly struct ResolvedVideo(string url, string unavailableText)
{
	/// <summary>
	/// Address the video is played from.
	/// </summary>
	public string Url { get; } = url;

	/// <summary>
	/// Why the video cannot be played, already localized.
	/// </summary>
	public string UnavailableText { get; } = unavailableText;

	/// <summary>
	/// Whether there is an address to play.
	/// </summary>
	public bool IsPlayable => !Url.IsEmpty();
}
