namespace Ecng.Tests.Data;

using Ecng.Common;
using Ecng.Data;

[TestClass]
public class FilterConditionTests
{
	[TestMethod]
	public void Constructor_SetsProperties()
	{
		var filter = new FilterCondition("Column1", ComparisonOperator.Equal, 42);

		filter.Column.AssertEqual("Column1");
		filter.Operator.AssertEqual(ComparisonOperator.Equal);
		filter.Value.AssertEqual(42);
	}

	[TestMethod]
	public void Constructor_AllOperators()
	{
		var operators = new[]
		{
			ComparisonOperator.Equal,
			ComparisonOperator.NotEqual,
			ComparisonOperator.Greater,
			ComparisonOperator.GreaterOrEqual,
			ComparisonOperator.Less,
			ComparisonOperator.LessOrEqual,
			ComparisonOperator.In,
		};

		foreach (var op in operators)
		{
			var filter = new FilterCondition("Col", op, "value");
			filter.Operator.AssertEqual(op);
		}
	}

	[TestMethod]
	public void Constructor_NullValue()
	{
		var filter = new FilterCondition("Column1", ComparisonOperator.Equal, null);

		filter.Column.AssertEqual("Column1");
		filter.Operator.AssertEqual(ComparisonOperator.Equal);
		IsNull(filter.Value);
	}

	[TestMethod]
	public void Constructor_ArrayValue_ForInOperator()
	{
		var values = new[] { 1, 2, 3 };
		var filter = new FilterCondition("Id", ComparisonOperator.In, values);

		filter.Column.AssertEqual("Id");
		filter.Operator.AssertEqual(ComparisonOperator.In);
		filter.Value.AssertEqual(values);
	}
}

[TestClass]
public class OrderByConditionTests
{
	[TestMethod]
	public void Constructor_Ascending()
	{
		var orderBy = new OrderByCondition("Column1");

		orderBy.Column.AssertEqual("Column1");
		orderBy.Descending.AssertEqual(false);
	}

	[TestMethod]
	public void Constructor_Descending()
	{
		var orderBy = new OrderByCondition("Column1", descending: true);

		orderBy.Column.AssertEqual("Column1");
		orderBy.Descending.AssertEqual(true);
	}

	[TestMethod]
	public void Constructor_ExplicitAscending()
	{
		var orderBy = new OrderByCondition("Column1", descending: false);

		orderBy.Column.AssertEqual("Column1");
		orderBy.Descending.AssertEqual(false);
	}
}
