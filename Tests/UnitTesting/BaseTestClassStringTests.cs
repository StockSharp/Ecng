namespace Ecng.Tests;

using System;
using System.Text.RegularExpressions;

using Ecng.UnitTesting;

/// <summary>
/// The string part of the <see cref="BaseTestClass"/> assertion surface: substring, prefix,
/// suffix, regex and emptiness checks. Each test method drives its helper in both directions -
/// an input it must accept and an input it must reject - so that emptying the helper body
/// cannot leave the method green. The two separation tests - whitespace and null - are the
/// deliberate exception: they hold a single input fixed and record which helpers take it and
/// which refuse it, so a helper appears there in one direction only and gets its other
/// direction from its own dedicated test.
/// </summary>
[TestClass]
public class BaseTestClassStringTests : BaseTestClass
{
	private const string _sentence = "The quick brown fox";

	[TestMethod]
	public void Contains_AcceptsPresentSubstringAndRejectsAbsentOne()
	{
		Contains("quick", _sentence);
		Contains("The", _sentence);
		Contains("fox", _sentence);
		Contains(_sentence, _sentence);

		Throws<AssertFailedException>(() => Contains("slow", _sentence));

		// The letters are all present but not adjacent, so the search is for a run, not a set.
		Throws<AssertFailedException>(() => Contains("quickbrown", _sentence));
	}

	[TestMethod]
	public void Contains_ArgumentOrderIsSubstringThenValue()
	{
		// The signature is Contains(substring, value) - needle first, haystack second, the
		// opposite of the reading order most callers assume. The same two strings swapped ask
		// whether the short word contains the long sentence, which must fail.
		Contains("quick", _sentence);
		Throws<AssertFailedException>(() => Contains(_sentence, "quick"));
	}

	[TestMethod]
	public void Contains_IsCaseSensitive()
	{
		Contains("The", _sentence);
		Throws<AssertFailedException>(() => Contains("the", _sentence));
		Throws<AssertFailedException>(() => Contains("QUICK", _sentence));
	}

	[TestMethod]
	public void Contains_EmptySubstringIsPresentEverywhereButEmptyValueHoldsNothingElse()
	{
		// The empty needle is found even in the one haystack where nothing else can be.
		Contains(string.Empty, _sentence);
		Contains(string.Empty, string.Empty);
		Throws<AssertFailedException>(() => Contains("a", string.Empty));
	}

	[TestMethod]
	public void Contains_MatchesNeedleLiterally()
	{
		// The needle is text to find, not a pattern: its metacharacters stand for themselves.
		Contains("a+b", "x a+b y");
		Contains(".", "a.c");
		Contains("[a-z]", "col [a-z] end");
		Contains("50%", "off 50% today");

		// Read as a regex these would all match; read literally none of them do.
		Throws<AssertFailedException>(() => Contains("a+b", "aab"));
		Throws<AssertFailedException>(() => Contains(".", "abc"));
		Throws<AssertFailedException>(() => Contains("[a-z]", "q"));
	}

	[TestMethod]
	public void Contains_RejectsNulls()
	{
		// The non-null pair passes, which is what pins null as the reason for the failures below
		// rather than something about these particular strings.
		Contains("quick", _sentence);

		Throws<AssertFailedException>(() => Contains(null, _sentence));
		Throws<AssertFailedException>(() => Contains("quick", null));
		Throws<AssertFailedException>(() => Contains(null, null));
	}

	[TestMethod]
	public void DoesNotContain_AcceptsAbsentSubstringAndRejectsPresentOne()
	{
		DoesNotContain("slow", _sentence);
		DoesNotContain("a", string.Empty);

		Throws<AssertFailedException>(() => DoesNotContain("quick", _sentence));
		Throws<AssertFailedException>(() => DoesNotContain(_sentence, _sentence));

		// An empty needle sits inside every string, so the negative check can never hold.
		Throws<AssertFailedException>(() => DoesNotContain(string.Empty, _sentence));
	}

	[TestMethod]
	public void DoesNotContain_IsCaseSensitive()
	{
		// Differing only in case counts as absent - and the same word in the sentence's own case
		// is present, which is what makes the accepted call above a case check and not a typo.
		DoesNotContain("QUICK", _sentence);
		Throws<AssertFailedException>(() => DoesNotContain("quick", _sentence));
	}

	[TestMethod]
	public void DoesNotContain_ArgumentOrderIsSubstringThenValue()
	{
		// Same order as Contains. The rejecting call is what pins the orientation: had the helper
		// read its arguments the other way it would have looked for the sentence inside "brown",
		// found nothing and stayed green.
		Throws<AssertFailedException>(() => DoesNotContain("brown", _sentence));
		DoesNotContain(_sentence, "brown");
	}

	[TestMethod]
	public void DoesNotContain_MatchesNeedleLiterally()
	{
		// "a+b" as a regex would match "aab"; taken literally it does not appear there.
		DoesNotContain("a+b", "aab");
		DoesNotContain(".", "abc");
		Throws<AssertFailedException>(() => DoesNotContain("a+b", "x a+b y"));
	}

	[TestMethod]
	public void DoesNotContain_RejectsNulls()
	{
		DoesNotContain("slow", _sentence);

		Throws<AssertFailedException>(() => DoesNotContain(null, _sentence));
		Throws<AssertFailedException>(() => DoesNotContain("quick", null));
	}

	[TestMethod]
	public void StartsWith_AcceptsPrefixAndRejectsNonPrefix()
	{
		StartsWith("The", _sentence);
		StartsWith("T", _sentence);
		StartsWith(_sentence, _sentence);

		// Present but not at the start, and absent entirely.
		Throws<AssertFailedException>(() => StartsWith("quick", _sentence));
		Throws<AssertFailedException>(() => StartsWith("fox", _sentence));
		Throws<AssertFailedException>(() => StartsWith("slow", _sentence));
	}

	[TestMethod]
	public void StartsWith_ArgumentOrderIsSubstringThenValue()
	{
		// StartsWith(substring, value): the prefix comes first, the string being tested second.
		StartsWith("The", _sentence);
		Throws<AssertFailedException>(() => StartsWith(_sentence, "The"));
	}

	[TestMethod]
	public void StartsWith_IsCaseSensitive()
	{
		StartsWith("The", _sentence);
		Throws<AssertFailedException>(() => StartsWith("the", _sentence));
	}

	[TestMethod]
	public void StartsWith_MatchesNeedleLiterally()
	{
		StartsWith("a+b", "a+b tail");
		Throws<AssertFailedException>(() => StartsWith("a+b", "aab tail"));

		// A regex anchor is just another character to a literal prefix check.
		Throws<AssertFailedException>(() => StartsWith("^The", _sentence));
	}

	[TestMethod]
	public void StartsWith_EmptyPrefixMatchesAnythingButEmptyValueTakesOnlyEmptyPrefix()
	{
		StartsWith(string.Empty, _sentence);
		StartsWith(string.Empty, string.Empty);
		Throws<AssertFailedException>(() => StartsWith("a", string.Empty));
	}

	[TestMethod]
	public void StartsWith_RejectsNulls()
	{
		StartsWith("The", _sentence);

		Throws<AssertFailedException>(() => StartsWith(null, _sentence));
		Throws<AssertFailedException>(() => StartsWith("The", null));
	}

	[TestMethod]
	public void EndsWith_AcceptsSuffixAndRejectsNonSuffix()
	{
		EndsWith("fox", _sentence);
		EndsWith("x", _sentence);
		EndsWith(_sentence, _sentence);

		Throws<AssertFailedException>(() => EndsWith("quick", _sentence));
		Throws<AssertFailedException>(() => EndsWith("The", _sentence));
		Throws<AssertFailedException>(() => EndsWith("slow", _sentence));
	}

	[TestMethod]
	public void EndsWith_ArgumentOrderIsSubstringThenValue()
	{
		// EndsWith(substring, value): the suffix comes first.
		EndsWith("fox", _sentence);
		Throws<AssertFailedException>(() => EndsWith(_sentence, "fox"));
	}

	[TestMethod]
	public void EndsWith_IsCaseSensitive()
	{
		EndsWith("fox", _sentence);
		Throws<AssertFailedException>(() => EndsWith("FOX", _sentence));
	}

	[TestMethod]
	public void EndsWith_MatchesNeedleLiterally()
	{
		EndsWith("a+b", "head a+b");
		Throws<AssertFailedException>(() => EndsWith("a+b", "head aab"));
		Throws<AssertFailedException>(() => EndsWith("fox$", _sentence));
	}

	[TestMethod]
	public void EndsWith_EmptySuffixMatchesAnythingButEmptyValueTakesOnlyEmptySuffix()
	{
		EndsWith(string.Empty, _sentence);
		EndsWith(string.Empty, string.Empty);
		Throws<AssertFailedException>(() => EndsWith("a", string.Empty));
	}

	[TestMethod]
	public void EndsWith_RejectsNulls()
	{
		EndsWith("fox", _sentence);

		Throws<AssertFailedException>(() => EndsWith(null, _sentence));
		Throws<AssertFailedException>(() => EndsWith("fox", null));
	}

	[TestMethod]
	public void MatchesRegex_AcceptsMatchAndRejectsNonMatch()
	{
		MatchesRegex(new Regex("^The .+ fox$"), _sentence);
		MatchesRegex(new Regex(@"^\d{3}$"), "123");

		Throws<AssertFailedException>(() => MatchesRegex(new Regex("^The .+ dog$"), _sentence));
		Throws<AssertFailedException>(() => MatchesRegex(new Regex(@"^\d+$"), "12a"));
	}

	[TestMethod]
	public void MatchesRegex_MatchesAnywhereWhenUnanchored()
	{
		// Regex.IsMatch semantics: a partial hit is a match, the pattern need not span the value.
		// The anchored twin of the same pattern must fail, or the accepted call would prove
		// nothing beyond "quick appears somewhere".
		MatchesRegex(new Regex("quick"), _sentence);
		Throws<AssertFailedException>(() => MatchesRegex(new Regex("^quick$"), _sentence));

		MatchesRegex(new Regex("b+"), "abbbc");
		Throws<AssertFailedException>(() => MatchesRegex(new Regex("^b+$"), "abbbc"));
	}

	[TestMethod]
	public void MatchesRegex_TreatsPatternAsRegexNotLiteral()
	{
		// The opposite of Contains: here the metacharacters are live.
		MatchesRegex(new Regex("a+b"), "aab");
		Throws<AssertFailedException>(() => MatchesRegex(new Regex("a+b"), "a+b"));
	}

	[TestMethod]
	public void MatchesRegex_HonoursRegexOptions()
	{
		Throws<AssertFailedException>(() => MatchesRegex(new Regex("^the"), _sentence));
		MatchesRegex(new Regex("^the", RegexOptions.IgnoreCase), _sentence);
	}

	[TestMethod]
	public void MatchesRegex_EmptyPatternMatchesAnything()
	{
		// The empty pattern even matches the empty string, where a one-character pattern cannot.
		MatchesRegex(new Regex(string.Empty), _sentence);
		MatchesRegex(new Regex(string.Empty), string.Empty);
		Throws<AssertFailedException>(() => MatchesRegex(new Regex("x"), string.Empty));
	}

	[TestMethod]
	public void MatchesRegex_RejectsNulls()
	{
		MatchesRegex(new Regex("quick"), _sentence);

		Throws<AssertFailedException>(() => MatchesRegex(null, _sentence));
		Throws<AssertFailedException>(() => MatchesRegex(new Regex("quick"), null));
	}

	[TestMethod]
	public void DoesNotMatch_AcceptsNonMatchAndRejectsMatch()
	{
		DoesNotMatch(new Regex("^The .+ dog$"), _sentence);
		DoesNotMatch(new Regex(@"^\d+$"), "12a");
		DoesNotMatch(new Regex("^the"), _sentence);

		Throws<AssertFailedException>(() => DoesNotMatch(new Regex("^The .+ fox$"), _sentence));

		// Unanchored: a partial hit counts as a match here too.
		Throws<AssertFailedException>(() => DoesNotMatch(new Regex("quick"), _sentence));

		// The empty pattern matches every string, so the negative check can never hold.
		Throws<AssertFailedException>(() => DoesNotMatch(new Regex(string.Empty), _sentence));
	}

	[TestMethod]
	public void DoesNotMatch_TreatsPatternAsRegexNotLiteral()
	{
		DoesNotMatch(new Regex("a+b"), "a+b");
		Throws<AssertFailedException>(() => DoesNotMatch(new Regex("a+b"), "aab"));
	}

	[TestMethod]
	public void DoesNotMatch_RejectsNulls()
	{
		DoesNotMatch(new Regex("slow"), _sentence);

		Throws<AssertFailedException>(() => DoesNotMatch(null, _sentence));
		Throws<AssertFailedException>(() => DoesNotMatch(new Regex("quick"), null));
	}

	[TestMethod]
	public void IsEmpty_AcceptsEmptyStringAndRejectsAnyCharacter()
	{
		IsEmpty(string.Empty);

		Throws<AssertFailedException>(() => IsEmpty("a"));
		Throws<AssertFailedException>(() => IsEmpty(_sentence));

		// Whitespace is characters, so an empty check must reject it.
		Throws<AssertFailedException>(() => IsEmpty(" "));
	}

	[TestMethod]
	public void IsNotEmpty_AcceptsAnyCharacterAndRejectsEmptyAndNull()
	{
		IsNotEmpty("a");
		IsNotEmpty(_sentence);

		// A whitespace-only string has characters, so it is not empty.
		IsNotEmpty(" ");

		Throws<AssertFailedException>(() => IsNotEmpty(string.Empty));
		Throws<AssertFailedException>(() => IsNotEmpty((string)null));
	}

	[TestMethod]
	public void IsNullOrEmpty_AcceptsNullAndEmptyAndRejectsAnyCharacter()
	{
		IsNullOrEmpty(null);
		IsNullOrEmpty(string.Empty);

		Throws<AssertFailedException>(() => IsNullOrEmpty("a"));
		Throws<AssertFailedException>(() => IsNullOrEmpty(" "));
		Throws<AssertFailedException>(() => IsNullOrEmpty("\t"));
	}

	[TestMethod]
	public void IsNotNullOrEmpty_AcceptsAnyCharacterAndRejectsNullAndEmpty()
	{
		IsNotNullOrEmpty("a");
		IsNotNullOrEmpty(_sentence);
		IsNotNullOrEmpty(" ");

		Throws<AssertFailedException>(() => IsNotNullOrEmpty(null));
		Throws<AssertFailedException>(() => IsNotNullOrEmpty(string.Empty));
	}

	[TestMethod]
	public void IsNullOrWhiteSpace_AcceptsNullEmptyAndWhitespaceAndRejectsVisibleCharacters()
	{
		IsNullOrWhiteSpace(null);
		IsNullOrWhiteSpace(string.Empty);
		IsNullOrWhiteSpace(" ");
		IsNullOrWhiteSpace("\t\r\n ");

		Throws<AssertFailedException>(() => IsNullOrWhiteSpace("a"));

		// One visible character among the spaces is enough.
		Throws<AssertFailedException>(() => IsNullOrWhiteSpace("  a  "));
	}

	[TestMethod]
	public void IsNotNullOrWhiteSpace_AcceptsVisibleCharactersAndRejectsNullEmptyAndWhitespace()
	{
		IsNotNullOrWhiteSpace("a");
		IsNotNullOrWhiteSpace(_sentence);
		IsNotNullOrWhiteSpace("  a  ");

		Throws<AssertFailedException>(() => IsNotNullOrWhiteSpace(null));
		Throws<AssertFailedException>(() => IsNotNullOrWhiteSpace(string.Empty));
		Throws<AssertFailedException>(() => IsNotNullOrWhiteSpace(" "));
		Throws<AssertFailedException>(() => IsNotNullOrWhiteSpace("\t\r\n "));
	}

	[TestMethod]
	public void WhitespaceSeparatesTheEmptyAndWhiteSpaceHelpers()
	{
		// A whitespace-only string is one of the two inputs that split the six emptiness helpers -
		// null, in the test below, is the other - and the split is the point: empty is about
		// length, whitespace is about content.
		const string ws = " \t\r\n";

		Throws<AssertFailedException>(() => IsEmpty(ws));
		IsNotEmpty(ws);

		Throws<AssertFailedException>(() => IsNullOrEmpty(ws));
		IsNotNullOrEmpty(ws);

		IsNullOrWhiteSpace(ws);
		Throws<AssertFailedException>(() => IsNotNullOrWhiteSpace(ws));
	}

	[TestMethod]
	public void NullSeparatesTheEmptyAndNullOrEmptyHelpers()
	{
		// Null is the other dividing input: the IsEmpty pair rejects it outright, the
		// IsNullOrEmpty and IsNullOrWhiteSpace pairs fold it in with the empty string.
		Throws<AssertFailedException>(() => IsEmpty((string)null));
		Throws<AssertFailedException>(() => IsNotEmpty((string)null));

		IsNullOrEmpty(null);
		Throws<AssertFailedException>(() => IsNotNullOrEmpty(null));

		IsNullOrWhiteSpace(null);
		Throws<AssertFailedException>(() => IsNotNullOrWhiteSpace(null));
	}

	[TestMethod]
	public void SubstringAndRegexHelpers_KeepCustomMessage()
	{
		// Passing a message must not disturb the check itself - the same calls on good input
		// still succeed, so the failures below come from the inputs and not from the overload.
		Contains("quick", _sentence, "contains text");
		DoesNotContain("slow", _sentence, "does-not-contain text");
		StartsWith("The", _sentence, "prefix text");
		EndsWith("fox", _sentence, "suffix text");
		MatchesRegex(new Regex("^The"), _sentence, "regex text");
		DoesNotMatch(new Regex("^dog"), _sentence, "negated regex text");

		KeepsMessage(() => Contains("slow", _sentence, "contains text"), "contains text");
		KeepsMessage(() => DoesNotContain("quick", _sentence, "does-not-contain text"), "does-not-contain text");
		KeepsMessage(() => StartsWith("fox", _sentence, "prefix text"), "prefix text");
		KeepsMessage(() => EndsWith("The", _sentence, "suffix text"), "suffix text");
		KeepsMessage(() => MatchesRegex(new Regex("^dog"), _sentence, "regex text"), "regex text");
		KeepsMessage(() => DoesNotMatch(new Regex("quick"), _sentence, "negated regex text"), "negated regex text");
	}

	[TestMethod]
	public void EmptinessHelpers_KeepCustomMessage()
	{
		IsEmpty(string.Empty, "empty text");
		IsNotEmpty("a", "non-empty text");
		IsNullOrEmpty(null, "null-or-empty text");
		IsNotNullOrEmpty("a", "non-null-or-empty text");
		IsNullOrWhiteSpace(" ", "whitespace text");
		IsNotNullOrWhiteSpace("a", "non-whitespace text");

		KeepsMessage(() => IsEmpty("a", "empty text"), "empty text");
		KeepsMessage(() => IsNotEmpty(string.Empty, "non-empty text"), "non-empty text");
		KeepsMessage(() => IsNullOrEmpty("a", "null-or-empty text"), "null-or-empty text");
		KeepsMessage(() => IsNotNullOrEmpty(null, "non-null-or-empty text"), "non-null-or-empty text");
		KeepsMessage(() => IsNullOrWhiteSpace("a", "whitespace text"), "whitespace text");
		KeepsMessage(() => IsNotNullOrWhiteSpace(" ", "non-whitespace text"), "non-whitespace text");

		// The null input takes a different branch inside IsEmpty/IsNotEmpty than a wrong-length
		// one, and that branch builds its own default message.
		KeepsMessage(() => IsEmpty((string)null, "empty null text"), "empty null text");
		KeepsMessage(() => IsNotEmpty((string)null, "non-empty null text"), "non-empty null text");
	}

	private static void KeepsMessage(Action failing, string expected)
	{
		var ex = Throws<AssertFailedException>(failing);

		// Raw string search rather than the Contains helper: checking Contains's own failure
		// message with Contains would make the assertion depend on what it is testing.
		IsTrue(ex.Message.Contains(expected), $"Message did not carry the custom text: {ex.Message}");
	}
}
