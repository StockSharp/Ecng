namespace Ecng.Markdown;

using System;

using Ecng.Common;

/// <summary>
/// Builds the host-neutral document from the text a client received.
/// </summary>
/// <remarks>
/// Parsing and projection belong together: the pipeline decides which extension nodes exist, so a host that
/// parsed with a pipeline of its own would be reading a different dialect from the one the text was written
/// in. Both renderers come through here for that reason.
/// </remarks>
public static class MarkdownDocumentFactory
{
	private static readonly Md2HtmlFormatter _formatter = new();

	/// <summary>
	/// Parses and projects a markdown text.
	/// </summary>
	/// <param name="text">Markdown source, already stripped by the server of anything the reader may not see.</param>
	/// <param name="data">References resolved by the server.</param>
	/// <returns>Document ready to render.</returns>
	public static MdDocument Create(string text, ResolvedMarkdownData data)
	{
		ArgumentNullException.ThrowIfNull(data);

		if (text.IsEmpty())
			return new([]);

		// Raw HTML is never honoured on a client: whatever trust the author had was already applied on the
		// server, and a desktop renderer has no way to execute markup anyway.
		return MarkdownProjector.Project(_formatter.Parse(text, false), data);
	}
}
