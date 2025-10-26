namespace Ecng.Tests.Common;

using System.Globalization;

[TestClass]
public class StringTests
{
	[TestMethod]
	public void ReplaceIgnoreCase()
	{
		"".ReplaceIgnoreCase("", "").AssertEqual("");
		"1".ReplaceIgnoreCase("1", "2").AssertEqual("2");
		"1".ReplaceIgnoreCase("2", "1").AssertEqual("1");
		"".ReplaceIgnoreCase("1", "2").AssertEqual("");
		"1".ReplaceIgnoreCase("11", "22").AssertEqual("1");
		"1".ReplaceIgnoreCase("", "22").AssertEqual("1");
		"".ReplaceIgnoreCase("", "22").AssertEqual("22");
		((string)null).ReplaceIgnoreCase("", "22").AssertEqual(null);

		"AA ffgg GGG".ReplaceIgnoreCase("g", "k").AssertEqual("AA ffkk kkk");
		"AA ffgg GGG".ReplaceIgnoreCase("g", "").AssertEqual("AA ff ");

		"AbABabab".ReplaceIgnoreCase("ab", "kg").AssertEqual("kgkgkgkg");
		"AbABaba".ReplaceIgnoreCase("ab", "kg").AssertEqual("kgkgkga");

		"_".ReplaceIgnoreCase("_", "/").AssertEqual("/");
		"__".ReplaceIgnoreCase("_", "/").AssertEqual("//");
		"___".ReplaceIgnoreCase("__", "/").AssertEqual("/_");
		"___S".ReplaceIgnoreCase("__", "/").AssertEqual("/_S");
		"S___S".ReplaceIgnoreCase("__", "/").AssertEqual("S/_S");
		"S___".ReplaceIgnoreCase("__", "/").AssertEqual("S/_");
	}

	[TestMethod]
	public void ReplaceIgnoreCaseError()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => ((string)null).ReplaceIgnoreCase(null, "22"));
	}

	[TestMethod]
	public void ReplaceIgnoreCaseError2()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => "".ReplaceIgnoreCase(null, "22"));
	}

	[TestMethod]
	public void ReplaceIgnoreCaseError3()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => "".ReplaceIgnoreCase("", null));
	}

	[TestMethod]
	public void ReplaceIgnoreCaseError4()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => "11".ReplaceIgnoreCase("11", null));
	}

	[TestMethod]
	public void LangCode()
	{
		var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

		foreach (var culture in cultures)
		{
			culture.Name.GetLangCode().AssertEqual(culture.TwoLetterISOLanguageName);
		}
	}

	private class SmartFormatSubObj
	{
		public int PropSync { get; } = 1;
		public ValueTask<int> PropAsync(CancellationToken token) => new(2);
		public Task<int> PropAsync2(CancellationToken token) => 3.FromResult();
	}

	private class SmartFormatObj
	{
		public int PropSync { get; } = 1;
		public ValueTask<int> PropAsync(CancellationToken token) => new(2);
		public Task<int> PropAsync2(CancellationToken token) => 3.FromResult();
		public SmartFormatSubObj ComplexProp => new();
		public ValueTask<SmartFormatSubObj> ComplexPropAsync(CancellationToken token) => new(ComplexProp);
	}

	[TestMethod]
	public async Task SmartFormatAsync()
	{
		var template = @"{PropSync} <> {PropAsync} <> {PropAsync2}";
		var res = await template.PutExAsync([new SmartFormatObj()], default);
		res.AssertEqual("1 <> 2 <> 3");
	}

	[TestMethod]
	public async Task SmartFormatComplexAsync()
	{
		var template = @"{ComplexProp.PropAsync} == {ComplexPropAsync.PropAsync}";
		var res = await template.PutExAsync([new SmartFormatObj()], default);
		res.AssertEqual("2 == 2");
	}

	[TestMethod]
	public async Task SmartFormatComplex2Async()
	{
		var template = @"{ComplexProp.PropAsync2} == {ComplexPropAsync.PropAsync2}";
		var res = await template.PutExAsync([new SmartFormatObj()], default);
		res.AssertEqual("3 == 3");
	}

	[TestMethod]
	public void Truncate()
	{
		var str = "1234567890";
		str.Truncate(int.MaxValue).AssertEqual("1234567890");
		str.Truncate(10).AssertEqual("1234567890");
		str.Truncate(2).AssertEqual("12...");
		str.Truncate(0).AssertEqual("...");
	}

	[TestMethod]
	public void EmptyChecks()
	{
		((string)null).IsEmpty().AssertTrue();
		"".IsEmpty().AssertTrue();
		"a".IsEmpty().AssertFalse();
		((string)null).IsEmptyOrWhiteSpace().AssertTrue();
		"  ".IsEmptyOrWhiteSpace().AssertTrue();
		"a".IsEmptyOrWhiteSpace().AssertFalse();
	}

	[TestMethod]
	public void PutTest()
	{
		"{0}-{1}".Put(1, "a").AssertEqual("1-a");
	}

	[TestMethod]
	public void SplitByComma()
	{
		"a,b,c".SplitByComma().AssertEqual(["a", "b", "c"]);
	}

	[TestMethod]
	public void WhiteSpaceAndRemove()
	{
		"a b\tc".ReplaceWhiteSpaces('_').AssertEqual("a_b_c");
		"a b c".ReplaceWhiteSpaces().AssertEqual("a b c");
		"a b".RemoveSpaces().AssertEqual("ab");
		"hello world".Remove("WORLD", true).AssertEqual("hello ");
	}

	[TestMethod]
	public void NumberChecks()
	{
		"10".IsNumber(false).AssertTrue();
		"10.5".IsNumber(true).AssertTrue();
		"10a".IsNumber(false).AssertFalse();
		"1.0".IsNumberOnly(true).AssertTrue();
		"a".IsNumberOnly(false).AssertFalse();
		'5'.IsDigit().AssertTrue();
	}

	[TestMethod]
	public void ContainsAndIndex()
	{
		"Hello".ContainsIgnoreCase("he").AssertTrue();
		"Hello".StartsWithIgnoreCase("he").AssertTrue();
		"Hello".EndsWithIgnoreCase("LO").AssertTrue();
		"hello".IndexOfIgnoreCase("L").AssertEqual(2);
		"hello".LastIndexOfIgnoreCase("L").AssertEqual(3);
	}

	[TestMethod]
	public void Reverse()
	{
		"abc".Reverse().AssertEqual("cba");
	}

	[TestMethod]
	public void Reduce()
	{
		"abcdef".Reduce(6, "").AssertEqual("abcdef");
		"abcdef".Reduce(5, "").AssertEqual("abcde");

		"abcdef".Reduce(6, "...").AssertEqual("abc...");
		"abcdef".Reduce(3, "...").AssertEqual("...");

		"".Reduce(0, "").AssertEqual("");

		Assert.ThrowsExactly<ArgumentNullException>(() => ((string)null).Reduce(0, "..."));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => "abcdef".Reduce(0, "..."));
	}

	[TestMethod]
	public void SplitBySepAndVariants()
	{
		"a|b|c".SplitBySep("|").SequenceEqual(["a", "b", "c"]).AssertTrue();
		"a\rb\rc".SplitByR().SequenceEqual(["a", "b", "c"]).AssertTrue();
		"a\nb\nc".SplitByN().SequenceEqual(["a", "b", "c"]).AssertTrue();
		"a\r\nb\r\nc".SplitByRN().SequenceEqual(["a", "b", "c"]).AssertTrue();
		"a.b.c".SplitByDot().SequenceEqual(["a", "b", "c"]).AssertTrue();
		"a;b;c".SplitByDotComma().SequenceEqual(["a", "b", "c"]).AssertTrue();
		"a:b:c".SplitByColon().SequenceEqual(["a", "b", "c"]).AssertTrue();
		"a b c".SplitBySpace().SequenceEqual(["a", "b", "c"]).AssertTrue();
		"a=b=c".SplitByEqual().SequenceEqual(["a", "b", "c"]).AssertTrue();
		"a\tb\tc".SplitByTab().SequenceEqual(["a", "b", "c"]).AssertTrue();
		"a@b@c".SplitByAt().SequenceEqual(["a", "b", "c"]).AssertTrue();
	}

	[TestMethod]
	public void JoinVariants()
	{
		var arr = new string[] { "a", "b", "c" };
		arr.JoinAt().AssertEqual("a@b@c");
		arr.JoinTab().AssertEqual("a\tb\tc");
		arr.JoinComma().AssertEqual("a,b,c");
		arr.JoinDotComma().AssertEqual("a;b;c");
		arr.JoinDot().AssertEqual("a.b.c");
		arr.JoinCommaSpace().AssertEqual("a, b, c");
		arr.JoinSpace().AssertEqual("a b c");
		arr.JoinPipe().AssertEqual("a|b|c");
		arr.JoinColon().AssertEqual("a:b:c");
		arr.JoinEqual().AssertEqual("a=b=c");
		arr.JoinAnd().AssertEqual("a&b&c");
		arr.JoinN().AssertEqual("a\nb\nc");
		arr.JoinRN().AssertEqual("a\r\nb\r\nc");
		arr.JoinNL().AssertEqual(string.Join(Environment.NewLine, arr));
		arr.Join("-").AssertEqual("a-b-c");
	}

	[TestMethod]
	public void RemoveTrailingZeros_String()
	{
		"10,00".RemoveTrailingZeros(",").AssertEqual("10");
		"0,00".RemoveTrailingZeros(",").AssertEqual("0");
		"10.5000".RemoveTrailingZeros(".").AssertEqual("10.5");
	}

	[TestMethod]
	public void RemoveMultipleWhitespace_Works()
	{
		"a   b\t\tc".RemoveMultipleWhitespace().AssertEqual("a b c");
	}

	[TestMethod]
	public void Nl2Br_Works()
	{
		"a\nb\r\nc".Nl2Br().AssertEqual("a<br />b<br />c");
	}

	[TestMethod]
	public void ToTitleCase_Works()
	{
		"hello world".ToTitleCase().AssertEqual(CultureInfo.CurrentCulture.TextInfo.ToTitleCase("hello world"));
	}

	[TestMethod]
	public void Times_Works()
	{
		"a".Times(3).AssertEqual("aaa");
		"a".Times(3, ",").AssertEqual("a,a,a");
	}

	[TestMethod]
	public void ToLatin_And_LightScreening()
	{
		// TODO
		//"тест".ToLatin().AssertEqual("test");
		"a b.c#?:".LightScreening().AssertEqual("a-bc");
	}

	[TestMethod]
	public void ComparePaths_Works()
	{
		var p1 = Path.GetFullPath(".");
		var p2 = Path.GetFullPath("./");

		if (!p1.ComparePaths(p2))
			p1.AssertEqual(p2);
	}

	[TestMethod]
	public void Like_Works()
	{
		"abc".Like("a_c").AssertTrue();
		"abc".Like("a%", true).AssertTrue();
		"abc".Like("a%z").AssertFalse();
	}

	[TestMethod]
	public void IsValidEmailAddress_And_Url()
	{
		"test@example.com".IsValidEmailAddress().AssertTrue();
		"notanemail".IsValidEmailAddress().AssertFalse();
		"https://example.com".IsValidUrl().AssertTrue();
		"not a url".IsValidUrl().AssertFalse();
	}

	[TestMethod]
	public void Digest_And_Base64()
	{
		var bytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
		bytes.Digest().AssertEqual("DEADBEEF");
		bytes.Digest(2).AssertEqual("DEAD");
		var str = "Hello";
		System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(str)))).AssertEqual(str); // roundtrip check
		Convert.ToBase64String(bytes).AssertEqual(bytes.Base64());
	}

	[TestMethod]
	public void EncodingHelpers()
	{
		var str = "Hello";
		str.ASCII().SequenceEqual(System.Text.Encoding.ASCII.GetBytes(str)).AssertTrue();
		str.UTF8().SequenceEqual(System.Text.Encoding.UTF8.GetBytes(str)).AssertTrue();
		str.Unicode().SequenceEqual(System.Text.Encoding.Unicode.GetBytes(str)).AssertTrue();
		str.Cyrillic().SequenceEqual(System.Text.Encoding.GetEncoding(1251).GetBytes(str)).AssertTrue();
	}

	[TestMethod]
	public void ToBitString_And_ToByteArray()
	{
		var bytes = new byte[] { 0b10101010, 0b01010101 };
		var bitStr = bytes.ToBitString();
		bitStr.ToByteArray().SequenceEqual(bytes).AssertTrue();
	}

	[TestMethod]
	public void GetDeterministicHashCode_And_TryToLong()
	{
		var s = "test";
		s.GetDeterministicHashCode().AssertEqual(StringHelper.GetDeterministicHashCode(s));
		"12345".TryToLong().AssertEqual(12345L);
		"notanumber".TryToLong().AssertNull();
	}

	[TestMethod]
	public void FastIndexOf_Works()
	{
		"abcdef".FastIndexOf("cd").AssertEqual(2);
		"abcdef".FastIndexOf("gh").AssertEqual(-1);
	}

	[TestMethod]
	public void RemoveLast_And_IsEmpty_StringBuilder()
	{
		var sb = new System.Text.StringBuilder("abc");
		sb.RemoveLast(1);
		sb.ToString().AssertEqual("ab");
		sb.IsEmpty().AssertFalse();
		sb.Clear();
		sb.IsEmpty().AssertTrue();
	}

	[TestMethod]
	public void GetAndClear_StringBuilder()
	{
		var sb = new System.Text.StringBuilder("abc");
		sb.GetAndClear().AssertEqual("abc");
		sb.Length.AssertEqual(0);
	}

	[TestMethod]
	public void Intern_Works()
	{
		var s1 = string.Intern("abc");
		var s2 = "abc".Intern();
		ReferenceEquals(s1, s2).AssertTrue();
	}
}