namespace Ecng.Markdown;

/// <summary>
/// Text carrying the author's own styling, from the :[text]{color=...} directive.
/// </summary>
/// <param name="text">Text content.</param>
/// <param name="color">Colour as the author wrote it, empty when not set.</param>
/// <param name="fontSize">Font size as the author wrote it, empty when not set.</param>
/// <param name="fontFamily">Font family as the author wrote it, empty when not set.</param>
public sealed class MdStyledText(string text, string color, string fontSize, string fontFamily) : MdInline
{
	/// <summary>
	/// Text content.
	/// </summary>
	public string Text { get; } = text;

	/// <summary>
	/// Colour as the author wrote it.
	/// </summary>
	public string Color { get; } = color;

	/// <summary>
	/// Font size as the author wrote it.
	/// </summary>
	public string FontSize { get; } = fontSize;

	/// <summary>
	/// Font family as the author wrote it.
	/// </summary>
	public string FontFamily { get; } = fontFamily;
}
