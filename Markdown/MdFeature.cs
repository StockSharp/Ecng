namespace Ecng.Markdown;

using System.Collections.Generic;

/// <summary>
/// A two-column section: one visual beside the text about it.
/// </summary>
/// <param name="media">The visual half, empty when the section had none.</param>
/// <param name="text">Everything else.</param>
/// <param name="isMediaRight">Whether the visual belongs on the right.</param>
/// <param name="isAlt">Whether the section is the tinted variant.</param>
/// <remarks>
/// Which half is the visual is decided here rather than by each host: the rule is a reading of the source -
/// the first block that is a standalone image, a diagram or an embed - and two hosts applying it separately
/// would eventually disagree about a section that has both a picture and a screenshot.
/// </remarks>
public sealed class MdFeature(
	IReadOnlyList<MdBlock> media,
	IReadOnlyList<MdBlock> text,
	bool isMediaRight,
	bool isAlt) : MdBlock
{
	/// <summary>
	/// The visual half, empty when the section had none.
	/// </summary>
	public IReadOnlyList<MdBlock> Media { get; } = media;

	/// <summary>
	/// Everything else.
	/// </summary>
	public IReadOnlyList<MdBlock> Text { get; } = text;

	/// <summary>
	/// Whether the visual belongs on the right.
	/// </summary>
	public bool IsMediaRight { get; } = isMediaRight;

	/// <summary>
	/// Whether the section is the tinted variant.
	/// </summary>
	public bool IsAlt { get; } = isAlt;
}
