namespace Ecng.Markdown;

/// <summary>
/// Fenced or indented code block.
/// </summary>
/// <param name="text">Code text.</param>
/// <param name="language">Language the fence declared, empty when none.</param>
public sealed class MdCodeBlock(string text, string language) : MdBlock
{
	/// <summary>
	/// Code text.
	/// </summary>
	public string Text { get; } = text;

	/// <summary>
	/// Language the fence declared.
	/// </summary>
	public string Language { get; } = language;
}
