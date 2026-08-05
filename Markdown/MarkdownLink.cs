namespace Ecng.Markdown;

/// <summary>
/// A reference resolved to something the reader can be pointed at.
/// </summary>
/// <remarks>
/// A plain class rather than a tuple because this travels to clients that render the text themselves, and a
/// value tuple has no JSON shape worth speaking of - its parts are fields, and a tuple cannot be a JSON key
/// at all.
/// </remarks>
public class MarkdownLink
{
	/// <summary>Where the reference points.</summary>
	public string Url { get; set; }

	/// <summary>How it reads in the reader's language.</summary>
	public string Name { get; set; }

	/// <summary>Longer text, shown as a tooltip where there is room for one.</summary>
	public string Description { get; set; }
}
