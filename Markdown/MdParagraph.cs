namespace Ecng.Markdown;

using System.Collections.Generic;

/// <summary>
/// Paragraph.
/// </summary>
/// <param name="children">Paragraph content.</param>
public sealed class MdParagraph(IReadOnlyList<MdInline> children) : MdBlock
{
	/// <summary>
	/// Paragraph content.
	/// </summary>
	public IReadOnlyList<MdInline> Children { get; } = children;
}
