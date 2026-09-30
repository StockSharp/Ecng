namespace Ecng.Tests.Markdown;

using Ecng.Markdown.Extensions;

/// <summary>
/// The counters the tests' host registers: which counters exist is the host's business, and these are the
/// ones the tests speak of.
/// </summary>
static class TestSiteCounters
{
	public const string Indicators = "Indicators";
	public const string Connectors = "Connectors";
	public const string Users = "Users";
	public const string Strategies = "Strategies";
	public const string Apps = "Apps";

	/// <summary>
	/// Registers the counters; a repeated registration changes nothing.
	/// </summary>
	public static void Register()
	{
		SiteCounterParser.Register("indicator", Indicators);
		SiteCounterParser.Register("connector", Connectors);
		SiteCounterParser.Register("user", Users);
		SiteCounterParser.Register("strategy", Strategies);
		SiteCounterParser.Register("app", Apps);
	}
}
