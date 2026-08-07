namespace Ecng.Markdown;

using System.Collections.Generic;

/// <summary>
/// Content the reader opens on demand.
/// </summary>
/// <param name="title">Text on the closed spoiler.</param>
/// <param name="children">Hidden content.</param>
public sealed class MdSpoiler(string title, IReadOnlyList<MdBlock> children) : MdBlock
{
	/// <summary>
	/// Text on the closed spoiler.
	/// </summary>
	public string Title { get; } = title;

	/// <summary>
	/// Hidden content.
	/// </summary>
	public IReadOnlyList<MdBlock> Children { get; } = children;
}
