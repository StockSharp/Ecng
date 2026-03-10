namespace Ecng.Tests.ComponentModel;

using Ecng.ComponentModel;

[TestClass]
public class ComparisonOperatorTests : BaseTestClass
{
	// ─── Like(value, pattern, operator) ───

	[TestMethod]
	public void Like_Null_Contains()
	{
		"hello world".Like("world", null).AssertTrue();
		"hello world".Like("xyz", null).AssertFalse();
	}

	[TestMethod]
	public void Like_In_Contains()
	{
		"hello world".Like("world", ComparisonOperator.In).AssertTrue();
		"hello world".Like("xyz", ComparisonOperator.In).AssertFalse();
	}

	[TestMethod]
	public void Like_Greater_StartsWith()
	{
		"hello world".Like("hello", ComparisonOperator.Greater).AssertTrue();
		"hello world".Like("world", ComparisonOperator.Greater).AssertFalse();
	}

	[TestMethod]
	public void Like_GreaterOrEqual_StartsWith()
	{
		"hello world".Like("hello", ComparisonOperator.GreaterOrEqual).AssertTrue();
		"hello world".Like("world", ComparisonOperator.GreaterOrEqual).AssertFalse();
	}

	[TestMethod]
	public void Like_Less_EndsWith()
	{
		"hello world".Like("world", ComparisonOperator.Less).AssertTrue();
		"hello world".Like("hello", ComparisonOperator.Less).AssertFalse();
	}

	[TestMethod]
	public void Like_LessOrEqual_EndsWith()
	{
		"hello world".Like("world", ComparisonOperator.LessOrEqual).AssertTrue();
		"hello world".Like("hello", ComparisonOperator.LessOrEqual).AssertFalse();
	}

	[TestMethod]
	public void Like_Equal_Exact()
	{
		"hello".Like("hello", ComparisonOperator.Equal).AssertTrue();
		"hello".Like("HELLO", ComparisonOperator.Equal).AssertTrue();
		"hello".Like("hell", ComparisonOperator.Equal).AssertFalse();
	}

	[TestMethod]
	public void Like_NotEqual_NotContains()
	{
		"hello world".Like("xyz", ComparisonOperator.NotEqual).AssertTrue();
		"hello world".Like("world", ComparisonOperator.NotEqual).AssertFalse();
	}

	[TestMethod]
	public void Like_Like_ShouldContain()
	{
		// Like operator should work like Contains (same as In)
		"hello world".Like("world", ComparisonOperator.Like).AssertTrue();
		"hello world".Like("xyz", ComparisonOperator.Like).AssertFalse();
	}

	[TestMethod]
	public void Like_Any_AlwaysTrue()
	{
		// Any operator means any value matches
		"hello".Like("xyz", ComparisonOperator.Any).AssertTrue();
		"".Like("xyz", ComparisonOperator.Any).AssertTrue();
	}

	[TestMethod]
	public void Like_CaseInsensitive()
	{
		"Hello World".Like("hello", ComparisonOperator.In).AssertTrue();
		"Hello World".Like("HELLO", ComparisonOperator.Greater).AssertTrue();
		"Hello World".Like("WORLD", ComparisonOperator.Less).AssertTrue();
	}

	[TestMethod]
	public void Like_EmptyPattern_ReturnsTrue()
	{
		"anything".Like("", null).AssertTrue();
		"anything".Like(null, null).AssertTrue();
	}

	[TestMethod]
	public void Like_NullValue_Throws()
	{
		Throws<ArgumentNullException>(() => ((string)null).Like("test", null));
	}

	// ─── ToExpression(pattern, operator) ───

	[TestMethod]
	public void ToExpression_Null_Contains()
	{
		"test".ToExpression(null).AssertEqual("%test%");
	}

	[TestMethod]
	public void ToExpression_In_Contains()
	{
		"test".ToExpression(ComparisonOperator.In).AssertEqual("%test%");
	}

	[TestMethod]
	public void ToExpression_Like_Contains()
	{
		// Like operator wraps with % (same as In - contains)
		"test".ToExpression(ComparisonOperator.Like).AssertEqual("%test%");
	}

	[TestMethod]
	public void ToExpression_Greater_StartsWith()
	{
		"test".ToExpression(ComparisonOperator.Greater).AssertEqual("test%");
	}

	[TestMethod]
	public void ToExpression_GreaterOrEqual_StartsWith()
	{
		"test".ToExpression(ComparisonOperator.GreaterOrEqual).AssertEqual("test%");
	}

	[TestMethod]
	public void ToExpression_Less_EndsWith()
	{
		"test".ToExpression(ComparisonOperator.Less).AssertEqual("%test");
	}

	[TestMethod]
	public void ToExpression_LessOrEqual_EndsWith()
	{
		"test".ToExpression(ComparisonOperator.LessOrEqual).AssertEqual("%test");
	}

	[TestMethod]
	public void ToExpression_Equal_Exact()
	{
		"test".ToExpression(ComparisonOperator.Equal).AssertEqual("test");
	}

	[TestMethod]
	public void ToExpression_NotEqual_ThrowsNotSupported()
	{
		Throws<NotSupportedException>(() => "test".ToExpression(ComparisonOperator.NotEqual));
	}

	[TestMethod]
	public void ToExpression_Any_MatchAll()
	{
		// Any means match everything - should produce '%' wildcard
		"test".ToExpression(ComparisonOperator.Any).AssertEqual("%");
	}

	[TestMethod]
	public void ToExpression_Empty_Throws()
	{
		Throws<ArgumentNullException>(() => "".ToExpression(null));
	}

	[TestMethod]
	public void ToExpression_Default_SameAsNull()
	{
		"test".ToExpression(default).AssertEqual("test".ToExpression(null));
	}
}
