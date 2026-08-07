namespace Ecng.Markdown;

using System.Collections.Generic;

/// <summary>
/// The whole rendered document.
/// </summary>
/// <param name="blocks">Top-level blocks.</param>
public sealed class MdDocument(IReadOnlyList<MdBlock> blocks)
{
	/// <summary>
	/// Top-level blocks.
	/// </summary>
	public IReadOnlyList<MdBlock> Blocks { get; } = blocks;
}
