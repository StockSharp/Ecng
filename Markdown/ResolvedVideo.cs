namespace Ecng.Markdown;

/// <summary>
/// A video reference resolved to either an address to play or the reason there is nothing to play.
/// </summary>
/// <remarks>
/// A class with settable properties, like the other resolved references: this travels to clients that render
/// the text themselves, and a read-only struct comes back from JSON empty - there is nothing for the
/// deserializer to assign to, and it does not reach for the constructor on its own.
/// </remarks>
public class ResolvedVideo
{
	/// <summary>
	/// Initializes a new instance.
	/// </summary>
	public ResolvedVideo()
	{
	}

	/// <summary>
	/// Initializes a new instance.
	/// </summary>
	/// <param name="url">Address the video is played from. Empty when the video is not playable.</param>
	/// <param name="unavailableText">
	/// Why the video cannot be played, in the reader's language. Only the caller knows that language, so the
	/// text is resolved rather than composed here. Empty renders nothing at all.
	/// </param>
	public ResolvedVideo(string url, string unavailableText)
	{
		Url = url;
		UnavailableText = unavailableText;
	}

	/// <summary>
	/// Address the video is played from.
	/// </summary>
	public string Url { get; set; }

	/// <summary>
	/// Why the video cannot be played, already localized.
	/// </summary>
	public string UnavailableText { get; set; }

	/// <summary>
	/// Whether there is an address to play.
	/// </summary>
	public bool IsPlayable => !Url.IsEmpty();
}
