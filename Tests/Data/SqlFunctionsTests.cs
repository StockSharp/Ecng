#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using Ecng.Data.Sql;

/// <summary>
/// The SQL functions a query is written with have to mean the same thing when the query runs over a list held
/// in memory instead of the database -- otherwise turning caching on for a table changes what its queries
/// return, which is not a decision a caching flag is allowed to make.
/// </summary>
[TestClass]
public class SqlFunctionsTests : BaseTestClass
{
	[TestMethod]
	public void Like_PercentStandsForAnyRun()
	{
		"Binance".Like("%nan%").AssertTrue();
		"Binance".Like("Bin%").AssertTrue();
		"Binance".Like("%ance").AssertTrue();
		"Binance".Like("%xyz%").AssertFalse();
	}

	[TestMethod]
	public void Like_UnderscoreStandsForOneCharacter()
	{
		"Binance".Like("Bi_ance").AssertTrue();
		"Binance".Like("Bi_nce").AssertFalse();
	}

	[TestMethod]
	public void Like_IsAnchored()
	{
		// LIKE compares the whole value, unlike a substring search.
		"Binance".Like("nan").AssertFalse();
		"Binance".Like("Binance").AssertTrue();
	}

	[TestMethod]
	public void Like_IgnoresCase()
	{
		// The database compares under a case-insensitive collation; memory has to agree with it.
		"Binance".Like("%NAN%").AssertTrue();
		"BINANCE".Like("bin%").AssertTrue();
	}

	[TestMethod]
	public void Like_BracketsAreACharacterClass()
	{
		"Binance".Like("[BC]inance").AssertTrue();
		"Binance".Like("[^BC]inance").AssertFalse();
		"Binance".Like("[A-C]inance").AssertTrue();
	}

	[TestMethod]
	public void Like_NullNeverMatches()
	{
		// NULL LIKE anything is unknown, and a row a predicate is unsure about is not returned.
		((string)null).Like("%a%").AssertFalse();
		"Binance".Like(null).AssertFalse();
	}

	[TestMethod]
	public void Like_PatternCharactersInTheValueAreJustCharacters()
	{
		"100%".Like("100%").AssertTrue();
		"100$".Like("100%").AssertTrue();
	}

	[TestMethod]
	public void LikeEscaped_EscapedMetacharacterIsText()
	{
		// The pattern a search box builds escapes what the visitor typed, so "100%" must not match "100 rub".
		"100%".LikeEscaped("100!%".Replace('!', SqlLike.EscapeChar)).AssertTrue();
		"100 rub".LikeEscaped("100!%".Replace('!', SqlLike.EscapeChar)).AssertFalse();
	}

	[TestMethod]
	public void LikeEscaped_ContainsPatternMatchesTypedTextLiterally()
	{
		// Exactly what SqlLike.ToLikeContains builds for a visitor's search term.
		var pattern = "50%".ToLikeContains();

		"discount 50% today".LikeEscaped(pattern).AssertTrue();
		"discount 50 today".LikeEscaped(pattern).AssertFalse();
	}

	[TestMethod]
	public void LikeEscaped_EscapedBracketIsText()
	{
		var pattern = "[beta]".ToLikeContains();

		"release [beta] build".LikeEscaped(pattern).AssertTrue();
		"release b build".LikeEscaped(pattern).AssertFalse();
	}

	[TestMethod]
	public void IsNull_AnswersWithTheFallbackWhenThereIsNoValue()
	{
		((string)null).IsNull("none").AssertEqual("none");
		"value".IsNull("none").AssertEqual("value");
	}

	[TestMethod]
	public void IfNull_AnswersWithTheFallbackWhenThereIsNoValue()
	{
		((string)null).IfNull("none").AssertEqual("none");
		"value".IfNull("none").AssertEqual("value");
	}
}

#endif
