namespace Ecng.Tests.Data;

using Ecng.Data;
using Ecng.Data.Sql;

/// <summary>
/// Tests SQL generation for ComparisonOperator via Query.CreateBuildCondition.
/// </summary>
[TestClass]
public class ComparisonOperatorSqlTests : BaseTestClass
{
	private static readonly ISqlDialect _dialect = SQLiteDialect.Instance;

	private static string BuildCondition(ComparisonOperator op, string paramName = "p0")
		=> Query.CreateBuildCondition("Col", op, paramName).Render(_dialect);

	[TestMethod]
	public void Sql_Equal()
		=> BuildCondition(ComparisonOperator.Equal).AssertEqual("\"Col\" = @p0");

	[TestMethod]
	public void Sql_NotEqual()
		=> BuildCondition(ComparisonOperator.NotEqual).AssertEqual("\"Col\" <> @p0");

	[TestMethod]
	public void Sql_Greater()
		=> BuildCondition(ComparisonOperator.Greater).AssertEqual("\"Col\" > @p0");

	[TestMethod]
	public void Sql_GreaterOrEqual()
		=> BuildCondition(ComparisonOperator.GreaterOrEqual).AssertEqual("\"Col\" >= @p0");

	[TestMethod]
	public void Sql_Less()
		=> BuildCondition(ComparisonOperator.Less).AssertEqual("\"Col\" < @p0");

	[TestMethod]
	public void Sql_LessOrEqual()
		=> BuildCondition(ComparisonOperator.LessOrEqual).AssertEqual("\"Col\" <= @p0");

	[TestMethod]
	public void Sql_Like()
		=> BuildCondition(ComparisonOperator.Like).AssertEqual("\"Col\" LIKE @p0");

	[TestMethod]
	public void Sql_Any_Tautology()
		=> BuildCondition(ComparisonOperator.Any).AssertEqual("1 = 1");

	[TestMethod]
	public void Sql_In_SingleParam()
		=> BuildCondition(ComparisonOperator.In).AssertEqual("\"Col\" IN (@p0)");

	[TestMethod]
	public void Sql_NullParam_Equal_IsNull()
		=> Query.CreateBuildCondition("Col", ComparisonOperator.Equal, null).Render(_dialect)
			.AssertEqual("\"Col\" IS NULL");

	[TestMethod]
	public void Sql_NullParam_NotEqual_IsNotNull()
		=> Query.CreateBuildCondition("Col", ComparisonOperator.NotEqual, null).Render(_dialect)
			.AssertEqual("\"Col\" IS NOT NULL");

	[TestMethod]
	public void Sql_NullParam_Greater_Throws()
		=> Throws<ArgumentOutOfRangeException>(
			() => Query.CreateBuildCondition("Col", ComparisonOperator.Greater, null).Render(_dialect));

	[TestMethod]
	public void Sql_InCondition_MultipleParams()
		=> Query.CreateBuildInCondition("Col", ["p0", "p1", "p2"]).Render(_dialect)
			.AssertEqual("\"Col\" IN (@p0, @p1, @p2)");

	[TestMethod]
	public void Sql_InCondition_EmptyParams()
		=> Query.CreateBuildInCondition("Col", Array.Empty<string>()).Render(_dialect)
			.AssertEqual("1 = 0");

	[TestMethod]
	public void Sql_InCondition_SingleParam()
		=> Query.CreateBuildInCondition("Col", ["p0"]).Render(_dialect)
			.AssertEqual("\"Col\" IN (@p0)");
}
