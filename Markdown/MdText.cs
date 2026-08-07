namespace Ecng.Markdown;

/// <summary>
/// Plain run of text.
/// </summary>
/// <param name="text">Text content.</param>
public sealed class MdText(string text) : MdInline
{
	/// <summary>
	/// Text content.
	/// </summary>
	public string Text { get; } = text;
}
