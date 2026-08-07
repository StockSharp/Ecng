namespace Ecng.Markdown;

/// <summary>
/// Inline code fragment.
/// </summary>
/// <param name="text">Code text.</param>
public sealed class MdCodeSpan(string text) : MdInline
{
	/// <summary>
	/// Code text.
	/// </summary>
	public string Text { get; } = text;
}
