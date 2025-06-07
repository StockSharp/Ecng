namespace Ecng.Tests.Common;

using System.Globalization;
using System.Threading;

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
	public void ReverseReduce()
	{
		"abc".Reverse().AssertEqual("cba");
		"abcdef".Reduce(5, "...").AssertEqual("ab...");
	}
}