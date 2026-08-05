namespace Ecng.Tests.Markdown;

using System.Text.Json;

using Ecng.Markdown;

/// <summary>
/// The resolved references travel to clients that render markdown themselves, so their shape has to survive
/// the trip. This was learnt the hard way: the type was built for a caller in the same process, where a
/// (type, id) tuple key and value-tuple payloads are perfectly reasonable and JSON cannot express either.
/// </summary>
[TestClass]
public class ResolvedMarkdownDataSerializationTests : BaseTestClass
{
	// What the API client formats bodies with.
	private static readonly JsonSerializerOptions _options = new();

	private static ResolvedMarkdownData RoundTrip(ResolvedMarkdownData data)
		=> JsonSerializer.Deserialize<ResolvedMarkdownData>(JsonSerializer.Serialize(data, _options), _options);

	private static ResolvedMarkdownData CreateFull() => new()
	{
		Entities =
		{
			["product"] = new() { [9] = new() { Url = "/store/designer/", Name = "S#.Designer", Description = "Strategy designer" } },
			["topic"] = new() { [789] = new() { Url = "/forum/start/", Name = "Getting Started", Description = "Beginner guide" } },
		},
		Files = { [122179] = new() { Url = "/file/122179/file.png", Name = "File", Description = "A file" } },
		Roles = { [1] = true, [2] = false },
		Videos =
		{
			[42] = new("/video/42", string.Empty),
			[7] = new(string.Empty, "Video is being processed"),
		},
		Diagrams = { ["122179"] = "https://stocksharp.com/file/122179/schema.json" },
		Counters = { [SiteCounters.Connectors] = "93", [SiteCounters.Users] = "22 431" },
	};

	[TestMethod]
	public void EveryResolvedReferenceSurvivesTheTrip()
	{
		var data = RoundTrip(CreateFull());

		data.Entities["product"][9].Name.AssertEqual("S#.Designer");
		data.Entities["product"][9].Url.AssertEqual("/store/designer/");
		data.Entities["topic"][789].Description.AssertEqual("Beginner guide");

		data.Files[122179].Url.AssertEqual("/file/122179/file.png");
		data.Diagrams["122179"].AssertEqual("https://stocksharp.com/file/122179/schema.json");
		data.Counters[SiteCounters.Connectors].AssertEqual("93");

		data.Roles[1].AssertTrue();
		data.Roles[2].AssertFalse();
	}

	[TestMethod]
	public void AVideoKeepsWhetherThereIsAnythingToPlay()
	{
		// The struct carries the whole answer: an address, or why there is none. Losing either half would
		// leave a client showing an empty player or a silent gap.
		var data = RoundTrip(CreateFull());

		data.Videos[42].IsPlayable.AssertTrue();
		data.Videos[42].Url.AssertEqual("/video/42");

		data.Videos[7].IsPlayable.AssertFalse();
		data.Videos[7].UnavailableText.AssertEqual("Video is being processed");
	}

	[TestMethod]
	public void AnEmptyResultIsStillReadable()
	{
		// A text quoting nothing resolves to nothing, and that has to deserialize into usable collections
		// rather than nulls a renderer would trip over.
		var data = RoundTrip(new());

		data.Entities.Count.AssertEqual(0);
		data.Files.Count.AssertEqual(0);
		data.Videos.Count.AssertEqual(0);
		data.Diagrams.Count.AssertEqual(0);
		data.Counters.Count.AssertEqual(0);
		data.Roles.Count.AssertEqual(0);
	}
}
