namespace Ecng.Tests.Markdown;

using Ecng.Markdown;



[TestClass]
public class MarkdownProjectorTests : BaseTestClass
{
	private static readonly Md2HtmlFormatter _formatter = new();

	private static MdDocument Project(string text, ResolvedMarkdownData data = null)
	{
		var parsed = _formatter.Parse(text, false);
		return MarkdownProjector.Project(parsed, data ?? new());
	}

	private static MdParagraph SingleParagraph(MdDocument doc)
	{
		doc.Blocks.Count.AssertEqual(1);
		return (MdParagraph)doc.Blocks[0];
	}

	[TestMethod]
	public void Emphasis_KeepsItsNestedText()
	{
		var paragraph = SingleParagraph(Project("plain **bold** text"));

		var emphasis = paragraph.Children.OfType<MdEmphasis>().Single();
		emphasis.Kind.AssertEqual(MdEmphasisKinds.Bold);
		((MdText)emphasis.Children.Single()).Text.AssertEqual("bold");
	}

	[TestMethod]
	public void Link_CarriesItsAddressSeparatelyFromItsText()
	{
		// A host binds the address to a command and the text to the visible run, so the two must not arrive
		// glued together the way they are in HTML.
		var link = SingleParagraph(Project("see [the docs](https://stocksharp.com/doc/)"))
			.Children.OfType<MdLink>().Single();

		link.Url.AssertEqual("https://stocksharp.com/doc/");
		((MdText)link.Children.Single()).Text.AssertEqual("the docs");
	}

	[TestMethod]
	public void DiagramReference_ArrivesAsItsResolvedAddress()
	{
		// The whole point of rendering natively: the desktop gets the schema's address and draws it with the
		// diagram control, instead of being handed a host div meant for a browser script.
		var data = new ResolvedMarkdownData
		{
			Diagrams = new() { ["122179"] = "https://stocksharp.com/file/122179/schema.json" },
		};

		var diagram = Project("@diagram(122179)", data).Blocks.OfType<MdDiagram>().Single();

		diagram.Source.AssertEqual("https://stocksharp.com/file/122179/schema.json");
	}

	[TestMethod]
	public void Video_NotReady_CarriesTheReasonInsteadOfAnAddress()
	{
		var data = new ResolvedMarkdownData
		{
			Videos = new() { [42] = new(string.Empty, "Video is being processed") },
		};

		var video = SingleParagraph(Project("@vss(42)", data)).Children.OfType<MdVideo>().Single();

		video.IsPlayable.AssertFalse();
		video.UnavailableText.AssertEqual("Video is being processed");
	}

	[TestMethod]
	public void RoleGatedFragment_IsAbsentForAReaderWithoutTheRole()
	{
		// Gating has to happen here, not in each host: a fragment the reader may not see must never reach a
		// control at all, and getting that wrong in one host only would leak it.
		var data = new ResolvedMarkdownData { Roles = new() { [1] = false } };

		var doc = Project("@role(1){secret}", data);

		doc.Blocks.OfType<MdParagraph>()
			.SelectMany(p => p.Children.OfType<MdText>())
			.Any(t => t.Text.Contains("secret"))
			.AssertFalse();
	}

	[TestMethod]
	public void Spoiler_StaysSomethingTheReaderOpens()
	{
		// Projected as a container it would lose the fold and spill its contents onto the page - which is the
		// one thing an author writing a spoiler asked not to happen.
		var spoiler = Project(":::spoiler Details\nhidden text\n:::").Blocks.OfType<MdSpoiler>().Single();

		spoiler.Title.AssertEqual("Details");
		spoiler.Children.OfType<MdParagraph>()
			.SelectMany(p => p.Children.OfType<MdText>())
			.Any(t => t.Text.Contains("hidden text"))
			.AssertTrue();
	}

	[TestMethod]
	public void Embed_ReachesTheReaderAsAnAddress()
	{
		// A desktop host embeds no web pages, but dropping the block would erase what the author put there
		// and leave nothing to explain the gap.
		var embed = Project("::iframe{url=https://stocksharp.com/live/ width=640 height=360}")
			.Blocks.OfType<MdEmbed>().Single();

		embed.Url.AssertEqual("https://stocksharp.com/live/");
		embed.Width.AssertEqual(640);
		embed.Height.AssertEqual(360);
	}

	[TestMethod]
	public void Table_KeepsItsGrid()
	{
		var table = Project("| a | b |\n|---|---|\n| 1 | 2 |").Blocks.OfType<MdTable>().Single();

		table.HasHeader.AssertTrue();
		table.Rows.Count.AssertEqual(2);
		table.Rows[1].Count.AssertEqual(2);
	}

	[TestMethod]
	public void CodeBlock_KeepsItsLanguage()
	{
		var code = Project("```csharp\nvar x = 1;\n```").Blocks.OfType<MdCodeBlock>().Single();

		code.Language.AssertEqual("csharp");
		code.Text.Contains("var x = 1;").AssertTrue();
	}

	// The ":::" sections are what a product page is made of. Read as plain containers they collapse into a
	// run of paragraphs - which is exactly what a card showed before: "867 | ready-made strategies" as text.

	[TestMethod]
	public void Stats_SplitEachLineIntoItsFigureAndLabel()
	{
		var stats = Project(":::stats\n867 | ready-made strategies\n166 | indicators\n74 | connections\n:::")
			.Blocks.OfType<MdStats>().Single();

		stats.Items.Count.AssertEqual(3);
		stats.Items[0].Value.AssertEqual("867");
		stats.Items[0].Label.AssertEqual("ready-made strategies");
		stats.Items[2].Value.AssertEqual("74");
		stats.Items[2].Label.AssertEqual("connections");
	}

	[TestMethod]
	public void Stats_LineWithoutABar_IsAllValue()
	{
		var stats = Project(":::stats\nFree\n:::").Blocks.OfType<MdStats>().Single();

		stats.Items.Single().Value.AssertEqual("Free");
		stats.Items.Single().Label.AssertEqual(string.Empty);
	}

	[TestMethod]
	public void Cards_StartANewCardAtEveryHeading()
	{
		var cards = Project(":::cards\n### First\nabout the first\n### Second\nabout the second\n:::")
			.Blocks.OfType<MdCards>().Single();

		cards.Cards.Count.AssertEqual(2);
		((MdText)cards.Cards[0].Title.Single()).Text.AssertEqual("First");
		cards.Cards[1].Children.OfType<MdParagraph>().Count().AssertEqual(1);
	}

	[TestMethod]
	public void Steps_AreCardsThatMustBeReadInOrder()
	{
		var steps = Project(":::steps\n### Install\nrun it\n### Connect\npick a venue\n:::")
			.Blocks.OfType<MdSteps>().Single();

		steps.Steps.Count.AssertEqual(2);
		((MdText)steps.Steps[1].Title.Single()).Text.AssertEqual("Connect");
	}

	[TestMethod]
	public void Feature_PutsTheLoneImageOnOneSideAndTheWordsOnTheOther()
	{
		var feature = Project(":::feature-right\n![shot](https://stocksharp.com/a.png)\n\nWhat the picture shows.\n:::")
			.Blocks.OfType<MdFeature>().Single();

		feature.IsMediaRight.AssertTrue();
		feature.Media.OfType<MdParagraph>().Single().Children.OfType<MdImage>().Count().AssertEqual(1);
		feature.Text.OfType<MdParagraph>().Count().AssertEqual(1);
	}

	[TestMethod]
	public void Feature_ParagraphThatAlsoCarriesWords_IsText()
	{
		// Otherwise a sentence that happens to open with a picture would be hoisted out of the prose it
		// belongs to and shown on its own.
		var feature = Project(":::feature-left\n![shot](https://stocksharp.com/a.png) and some words\n:::")
			.Blocks.OfType<MdFeature>().Single();

		feature.Media.Count.AssertEqual(0);
		feature.Text.Count.AssertEqual(1);
	}

	[TestMethod]
	public void Cta_TurnsEveryLinkIntoAnAction()
	{
		var cta = Project(":::cta\n[Download](https://stocksharp.com/d/) [Docs](https://stocksharp.com/doc/)\n:::")
			.Blocks.OfType<MdCta>().Single();

		cta.Links.Count.AssertEqual(2);
		cta.Links[0].Url.AssertEqual("https://stocksharp.com/d/");
	}

	[TestMethod]
	public void Quote_LiftsTheSignatureOutOfWhatWasSaid()
	{
		var quote = Project(":::quote\nIt paid for itself in a week.\n\n— A trader\n:::")
			.Blocks.OfType<MdTestimonial>().Single();

		quote.Attribution.AssertEqual("A trader");
		quote.Children.OfType<MdParagraph>().Count().AssertEqual(1);
	}

	[TestMethod]
	public void Quote_LastLineWithoutADash_IsPartOfTheQuote()
	{
		var quote = Project(":::quote\nIt paid for itself in a week.\n\nAnd then some.\n:::")
			.Blocks.OfType<MdTestimonial>().Single();

		quote.Attribution.AssertEqual(string.Empty);
		quote.Children.OfType<MdParagraph>().Count().AssertEqual(2);
	}

	[TestMethod]
	public void Alignment_IsCarriedRatherThanDroppedWithItsWrapper()
	{
		var section = Project(":::center\nMiddle of the page.\n:::").Blocks.OfType<MdSection>().Single();

		section.Alignment.AssertEqual(MdAlignments.Center);
		section.Children.OfType<MdParagraph>().Count().AssertEqual(1);
	}

	[TestMethod]
	public void Split_KeepsEachLayerSeparate()
	{
		var split = Project(":::split\n![light](https://stocksharp.com/l.png)\n\n![dark](https://stocksharp.com/d.png)\n:::")
			.Blocks.OfType<MdSplit>().Single();

		split.Layers.Count.AssertEqual(2);
	}

	[TestMethod]
	public void UnknownSection_StillShowsWhatIsInside()
	{
		var document = Project(":::whatever\nStill readable.\n:::");

		document.Blocks.OfType<MdParagraph>().Count().AssertEqual(1);
	}
}
