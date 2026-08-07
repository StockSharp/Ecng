namespace Ecng.Markdown;

using System.Collections.Generic;

/// <summary>
/// Hyperlink.
/// </summary>
/// <param name="url">Target address.</param>
/// <param name="title">Tooltip text.</param>
/// <param name="children">Link content.</param>
public sealed class MdLink(string url, string title, IReadOnlyList<MdInline> children) : MdInline
{
	/// <summary>
	/// Target address.
	/// </summary>
	public string Url { get; } = url;

	/// <summary>
	/// Tooltip text.
	/// </summary>
	public string Title { get; } = title;

	/// <summary>
	/// Link content.
	/// </summary>
	public IReadOnlyList<MdInline> Children { get; } = children;
}
