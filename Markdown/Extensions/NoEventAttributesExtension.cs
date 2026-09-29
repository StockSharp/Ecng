namespace Ecng.Markdown.Extensions;

using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

/// <summary>
/// Removes event-handler properties ("on*") that generic attributes put on any node, so untrusted markdown
/// cannot attach script through "{onclick=...}". Runs on every document the pipeline parses, including the
/// inner content a styled inline renders on its own.
/// </summary>
public class NoEventAttributesExtension : IMarkdownExtension
{
	/// <inheritdoc />
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		ArgumentNullException.ThrowIfNull(pipeline);

		pipeline.DocumentProcessed -= Strip;
		pipeline.DocumentProcessed += Strip;
	}

	/// <inheritdoc />
	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
	}

	private static void Strip(MarkdownDocument document)
	{
		foreach (var node in document.Descendants())
		{
			if (node.TryGetAttributes()?.Properties is { Count: > 0 } properties)
				properties.RemoveAll(p => p.Key.StartsWithIgnoreCase("on"));
		}
	}
}
