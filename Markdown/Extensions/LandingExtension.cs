namespace Ecng.Markdown.Extensions;

using Markdig;
using Markdig.Renderers;

/// <summary>
/// The sections a landing page is laid out with: :::hero, :::index, :::pillar, :::tags and :::section. Not part of the
/// built-in set: a host whose stylesheet styles them adds it, and everywhere else such a fence stays a plain
/// container.
/// </summary>
public class LandingExtension : IMarkdownExtension
{
	/// <inheritdoc />
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		ArgumentNullException.ThrowIfNull(pipeline);
	}

	/// <inheritdoc />
	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is not HtmlRenderer htmlRenderer)
			return;

		var sections = htmlRenderer.ObjectRenderers.FindExact<SectionContainerRenderer>()
			?? throw new InvalidOperationException($"{nameof(SectionContainerRenderer)} is missing from the pipeline.");

		sections.Register(new HeroSection());
		sections.Register(new IndexSection());
		sections.Register(new PillarSection());
		sections.Register(new TagsSection());
		sections.Register(new LandingSection());
	}
}
