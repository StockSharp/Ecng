namespace Ecng.Tests.Data;

using Ecng.Data.Sql;

[TestClass]
public class QueryTests : BaseTestClass
{
	private static ISqlDialect GetDialect(string dialectName) => dialectName switch
	{
		"SqlServer" => SqlServerDialect.Instance,
		"SQLite" => SQLiteDialect.Instance,
		"PostgreSql" => PostgreSqlDialect.Instance,
		_ => throw new ArgumentException($"Unknown dialect: {dialectName}")
	};

	private static string Q(string dialectName, string id) => dialectName switch
	{
		"SqlServer" => $"[{id}]",
		"SQLite" or "PostgreSql" => $"\"{id}\"",
		_ => id
	};

	private static readonly ISqlDialect _ss = SqlServerDialect.Instance;

	#region SELECT chain

	[TestMethod]
	public void Select_All_From_Table()
	{
		var sql = new Query()
			.Select().All("t")
			.NewLine()
			.From().Table("Users", "t")
			.Render(_ss);

		sql.AssertContains("select [t].*");
		sql.AssertContains("from [Users] t");
	}

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	[DataRow("PostgreSql")]
	public void Select_All_From_Table_AllDialects(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var sql = new Query()
			.Select().All("t")
			.NewLine()
			.From().Table("Users", "t")
			.Render(dialect);

		sql.AssertContains($"select {Q(dialectName, "t")}.*");
		sql.AssertContains($"from {Q(dialectName, "Users")} t");
	}

	[TestMethod]
	public void Select_Exact_Columns()
	{
		var sql = new Query()
			.Select().Exact("t", "Id", "Name", "Email")
			.NewLine()
			.From().Table("Users", "t")
			.Render(_ss);

		sql.AssertContains("select t.[Id], t.[Name], t.[Email]");
		sql.AssertContains("from [Users] t");
	}

	[TestMethod]
	public void Select_Distinct()
	{
		var sql = new Query()
			.Select().Distinct().All("t")
			.NewLine()
			.From().Table("Users", "t")
			.Render(_ss);

		sql.AssertContains("select distinct [t].*");
	}

	[TestMethod]
	public void Select_Top()
	{
		var sql = new Query()
			.Select().Top(10).All("t")
			.NewLine()
			.From().Table("Users", "t")
			.Render(_ss);

		sql.AssertContains("select top 10 [t].*");
	}

	#endregion

	#region SELECT with WHERE

	[TestMethod]
	public void Select_Where_Equals_SingleColumn()
	{
		var sql = new Query()
			.Select().All("t")
			.NewLine()
			.From().Table("Users", "t")
			.NewLine()
			.Where().Raw(" ").Equals("t", "Id")
			.Render(_ss);

		sql.AssertContains("where t.[Id] = @Id");
	}

	[TestMethod]
	public void Select_Where_Equals_MultipleColumns()
	{
		var sql = new Query()
			.Select().All("t")
			.NewLine()
			.From().Table("Users", "t")
			.NewLine()
			.Where().Raw(" ").Equals("t", "Id", "Status")
			.Render(_ss);

		sql.AssertContains("where t.[Id] = @Id and t.[Status] = @Status");
	}

	[TestMethod]
	public void Select_Where_And_Or()
	{
		var sql = new Query()
			.Select().All("t")
			.NewLine()
			.From().Table("Users", "t")
			.NewLine()
			.Where().Raw(" ")
			.Column("t", "Status").Equal().Param("Status")
			.And()
			.OpenBracket()
			.Column("t", "Age").More().Param("MinAge")
			.Or()
			.Column("t", "Role").Equal().Param("Role")
			.CloseBracket()
			.Render(_ss);

		sql.AssertContains("where [t].[Status] = @Status and ([t].[Age] > @MinAge or [t].[Role] = @Role)");
	}

	#endregion

	#region INSERT chain

	[TestMethod]
	public void Insert_Into_Values()
	{
		var columns = new[] { "Name", "Value" };

		var sql = new Query()
			.Insert().Into("Users", columns).Values(columns)
			.Render(_ss);

		sql.AssertContains("insert into [Users]");
		sql.AssertContains("([Name], [Value])");
		sql.AssertContains("values");
		sql.AssertContains("(@Name, @Value)");
	}

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	[DataRow("PostgreSql")]
	public void Insert_Into_Values_AllDialects(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var columns = new[] { "Name", "Value" };

		var sql = new Query()
			.Insert().Into("Users", columns).Values(columns)
			.Render(dialect);

		sql.AssertContains($"insert into {Q(dialectName, "Users")}");
		sql.AssertContains($"{Q(dialectName, "Name")}, {Q(dialectName, "Value")}");
		sql.AssertContains("@Name, @Value");
	}

	#endregion

	#region UPDATE chain with SET — RemoveLast bug regression

	[TestMethod]
	public void Update_Set_BasicColumns()
	{
		var columns = new[] { "Name", "Value" };

		var sql = new Query()
			.Update("t").Set("t", columns)
			.From().Table("Users", "t")
			.NewLine()
			.Where().Raw(" ").Equals("t", "Id")
			.Render(_ss);

		sql.AssertContains("update [t]");
		sql.AssertContains("set");
		sql.AssertContains("t.[Name] = @Name");
		sql.AssertContains("t.[Value] = @Value");
		sql.AssertContains("where t.[Id] = @Id");
	}

	/// <summary>
	/// CRITICAL regression test: the SET clause uses RemoveLast(1 + Environment.NewLine.Length)
	/// to strip trailing comma + newline. If the newline size assumption is wrong (e.g. \r\n vs \n),
	/// the parameter name gets truncated. This test verifies that long column names like
	/// "NullableValue" and "LongColumnNameHere" survive the SET rendering intact.
	/// </summary>
	[TestMethod]
	public void Update_Set_LongColumnNames_NotTruncated()
	{
		var columns = new[] { "Id", "NullableValue", "LongColumnNameHere", "X" };

		var sql = new Query()
			.Update("t").Set("t", columns)
			.From().Table("Users", "t")
			.NewLine()
			.Where().Raw(" ").Equals("t", "Id")
			.Render(_ss);

		// Verify each column parameter is fully present, not truncated
		sql.AssertContains("t.[NullableValue] = @NullableValue", "NullableValue parameter must not be truncated");
		sql.AssertContains("t.[LongColumnNameHere] = @LongColumnNameHere", "LongColumnNameHere parameter must not be truncated");
		sql.AssertContains("t.[X] = @X", "Short column X must not be truncated");
		sql.AssertContains("t.[Id] = @Id", "Id column must not be truncated");
	}

	/// <summary>
	/// Tests that the SET clause does not leave trailing commas after RemoveLast.
	/// The last column's line should not end with a comma.
	/// </summary>
	[TestMethod]
	public void Update_Set_NoTrailingComma()
	{
		var columns = new[] { "Alpha", "Beta" };

		var sql = new Query()
			.Update("t").Set("t", columns)
			.From().Table("Users", "t")
			.Render(_ss);

		// After the last SET assignment there should be no comma before the next keyword
		// The rendered output should transition from "@Beta" to newline and "from"
		sql.AssertContains("@Beta");
		sql.AssertContains("from [Users] t");

		// Verify no ",from" or ", from" which would indicate trailing comma not removed
		sql.Contains(",from").AssertFalse("Should not have trailing comma before FROM");
	}

	/// <summary>
	/// Test SET with a single column — edge case for RemoveLast.
	/// </summary>
	[TestMethod]
	public void Update_Set_SingleColumn()
	{
		var columns = new[] { "Name" };

		var sql = new Query()
			.Update("t").Set("t", columns)
			.From().Table("Users", "t")
			.Render(_ss);

		sql.AssertContains("t.[Name] = @Name");
		sql.AssertContains("from [Users] t");
	}

	/// <summary>
	/// Test SET with columns of varying lengths to catch off-by-one in RemoveLast.
	/// </summary>
	[TestMethod]
	public void Update_Set_VaryingLengthColumns()
	{
		var columns = new[] { "A", "BB", "CCC", "DDDD", "EEEEE", "VeryLongColumnNameThatShouldSurvive" };

		var sql = new Query()
			.Update("t").Set("t", columns)
			.From().Table("Users", "t")
			.Render(_ss);

		foreach (var col in columns)
		{
			sql.AssertContains($"t.[{col}] = @{col}", $"Column '{col}' must appear fully in SET clause");
		}
	}

	#endregion

	#region DELETE chain

	[TestMethod]
	public void Delete_From_Where()
	{
		var sql = new Query()
			.Delete().Raw(" t")
			.NewLine()
			.From().Table("Users", "t")
			.NewLine()
			.Where().Raw(" ").Equals("t", "Id")
			.Render(_ss);

		sql.AssertContains("delete t");
		sql.AssertContains("from [Users] t");
		sql.AssertContains("where t.[Id] = @Id");
	}

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("SQLite")]
	[DataRow("PostgreSql")]
	public void Delete_From_Where_AllDialects(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var sql = new Query()
			.Delete().Raw(" t")
			.NewLine()
			.From().Table("Users", "t")
			.NewLine()
			.Where().Raw(" ").Equals("t", "Id")
			.Render(dialect);

		sql.AssertContains("delete t");
		sql.AssertContains($"from {Q(dialectName, "Users")} t");
		sql.AssertContains($"where t.{Q(dialectName, "Id")} = @Id");
	}

	#endregion

	#region JOIN

	[TestMethod]
	public void InnerJoin_On()
	{
		var sql = new Query()
			.Select().All("t")
			.NewLine()
			.From().Table("Orders", "t")
			.NewLine()
			.InnerJoin().Table("Users", "u")
			.On().Column("t", "UserId").Equal().Column("u", "Id")
			.Render(_ss);

		sql.AssertContains("select [t].*");
		sql.AssertContains("from [Orders] t");
		sql.AssertContains("inner join [Users] u on [t].[UserId] = [u].[Id]");
	}

	[TestMethod]
	public void LeftJoin_On()
	{
		var sql = new Query()
			.Select().All("o")
			.NewLine()
			.From().Table("Orders", "o")
			.NewLine()
			.LeftJoin().Table("Users", "u")
			.On().Column("o", "UserId").Equal().Column("u", "Id")
			.Render(_ss);

		sql.AssertContains("left join [Users] u on [o].[UserId] = [u].[Id]");
	}

	#endregion

	#region Aggregates

	[TestMethod]
	public void Aggregate_Count_Star()
	{
		var sql = new Query()
			.Select().Count().OpenBracket().Star().CloseBracket()
			.NewLine()
			.From().Table("Users", "t")
			.Render(_ss);

		sql.AssertContains("select count(*)");
		sql.AssertContains("from [Users] t");
	}

	[TestMethod]
	public void Aggregate_Count_Column()
	{
		var sql = new Query()
			.Select().Count().OpenBracket().Column("t", "Id").CloseBracket()
			.NewLine()
			.From().Table("Users", "t")
			.Render(_ss);

		sql.AssertContains("select count([t].[Id])");
	}

	[TestMethod]
	public void Aggregate_Sum()
	{
		var sql = new Query()
			.Select().Sum().OpenBracket().Column("t", "Amount").CloseBracket()
			.NewLine()
			.From().Table("Orders", "t")
			.Render(_ss);

		sql.AssertContains("select sum([t].[Amount])");
	}

	[TestMethod]
	public void Aggregate_Avg()
	{
		var sql = new Query()
			.Select().Avg().OpenBracket().Column("t", "Price").CloseBracket()
			.NewLine()
			.From().Table("Orders", "t")
			.Render(_ss);

		sql.AssertContains("select avg([t].[Price])");
	}

	[TestMethod]
	public void Aggregate_Min()
	{
		var sql = new Query()
			.Select().Min().OpenBracket().Column("t", "CreatedAt").CloseBracket()
			.NewLine()
			.From().Table("Events", "t")
			.Render(_ss);

		sql.AssertContains("select min([t].[CreatedAt])");
	}

	[TestMethod]
	public void Aggregate_Max()
	{
		var sql = new Query()
			.Select().Max().OpenBracket().Column("t", "Score").CloseBracket()
			.NewLine()
			.From().Table("Events", "t")
			.Render(_ss);

		sql.AssertContains("select max([t].[Score])");
	}

	#endregion

	#region Pagination

	[TestMethod]
	[DataRow("SqlServer", "offset @skip rows", "fetch next @take rows only")]
	[DataRow("SQLite", "OFFSET @skip", "LIMIT @take")]
	[DataRow("PostgreSql", "OFFSET @skip", "LIMIT @take")]
	public void Pagination_Skip_Take(string dialectName, string expectedSkip, string expectedTake)
	{
		var dialect = GetDialect(dialectName);

		var sql = new Query()
			.Select().All("t")
			.NewLine()
			.From().Table("Users", "t")
			.NewLine()
			.OrderBy().Column("t", "Id").Asc()
			.NewLine()
			.Skip("skip").Take("take")
			.Render(dialect);

		sql.AssertContains($"order by {Q(dialectName, "t")}.{Q(dialectName, "Id")} asc");
		sql.Contains(expectedSkip).AssertTrue($"Should contain skip clause '{expectedSkip}', got: {sql}");
		sql.Contains(expectedTake).AssertTrue($"Should contain take clause '{expectedTake}', got: {sql}");
	}

	[TestMethod]
	public void OrderBy_Desc()
	{
		var sql = new Query()
			.Select().All("t")
			.NewLine()
			.From().Table("Users", "t")
			.NewLine()
			.OrderBy().Column("t", "Name").Desc()
			.Render(_ss);

		sql.AssertContains("order by [t].[Name] desc");
	}

	#endregion

	#region CASE / WHEN / THEN / ELSE / END

	[TestMethod]
	public void Case_When_Then_Else_End()
	{
		var sql = new Query()
			.Select()
			.Case().Raw(" ")
			.When().Column("t", "Status").Equal().Param("Active")
			.Then().Raw("1")
			.Raw(" ")
			.Else().Raw("0 ")
			.End()
			.NewLine()
			.From().Table("Users", "t")
			.Render(_ss);

		sql.AssertContains("case when [t].[Status] = @Active then 1 else 0 end");
	}

	[TestMethod]
	public void Case_Multiple_When()
	{
		var sql = new Query()
			.Case().Raw(" ")
			.When().Column("t", "Type").Equal().Param("A").Then().Raw("1")
			.Raw(" ")
			.When().Column("t", "Type").Equal().Param("B").Then().Raw("2")
			.Raw(" ")
			.Else().Raw("0 ")
			.End()
			.Render(_ss);

		sql.AssertContains("case when [t].[Type] = @A then 1 when [t].[Type] = @B then 2 else 0 end");
	}

	#endregion

	#region CopyTo

	[TestMethod]
	public void CopyTo_ProducesSameOutput()
	{
		var original = new Query()
			.Select().All("t")
			.NewLine()
			.From().Table("Users", "t")
			.NewLine()
			.Where().Raw(" ").Equals("t", "Id");

		var copy = new Query();
		original.CopyTo(copy);

		var sql1 = original.Render(_ss);
		var sql2 = copy.Render(_ss);

		sql2.AssertEqual(sql1);
	}

	[TestMethod]
	public void CopyTo_OriginalModification_DoesNotAffectCopy()
	{
		var original = new Query()
			.Select().All("t")
			.From().Table("Users", "t");

		var copy = new Query();
		original.CopyTo(copy);

		var sqlBefore = copy.Render(_ss);

		// Modify the original after copy
		original.Where().Raw(" ").Equals("t", "Id");

		var sqlAfter = copy.Render(_ss);

		// Copy should remain unchanged
		sqlAfter.AssertEqual(sqlBefore);
	}

	#endregion

	#region Union / UnionAll

	[TestMethod]
	public void Union_TwoQueries()
	{
		var sql = new Query()
			.Select().All("t")
			.NewLine()
			.From().Table("Users", "t")
			.NewLine()
			.Union()
			.NewLine()
			.Select().All("t")
			.NewLine()
			.From().Table("Admins", "t")
			.Render(_ss);

		sql.AssertContains("from [Users] t");
		sql.AssertContains("union");
		sql.AssertContains("from [Admins] t");
	}

	[TestMethod]
	public void UnionAll_TwoQueries()
	{
		var sql = new Query()
			.Select().All("t")
			.NewLine()
			.From().Table("Users", "t")
			.NewLine()
			.UnionAll()
			.NewLine()
			.Select().All("t")
			.NewLine()
			.From().Table("Admins", "t")
			.Render(_ss);

		sql.AssertContains("union all");
		sql.AssertContains("from [Users] t");
		sql.AssertContains("from [Admins] t");
	}

	#endregion

	#region String functions

	[TestMethod]
	public void StringFunc_Upper()
	{
		var sql = new Query()
			.Upper().OpenBracket().Column("t", "Name").CloseBracket()
			.Render(_ss);

		sql.AssertEqual("Upper([t].[Name])");
	}

	[TestMethod]
	public void StringFunc_Lower()
	{
		var sql = new Query()
			.Lower().OpenBracket().Column("t", "Name").CloseBracket()
			.Render(_ss);

		sql.AssertEqual("Lower([t].[Name])");
	}

	[TestMethod]
	public void StringFunc_Len()
	{
		var sql = new Query()
			.Len().OpenBracket().Column("t", "Name").CloseBracket()
			.Render(_ss);

		sql.AssertEqual("len([t].[Name])");
	}

	[TestMethod]
	public void StringFunc_SubString()
	{
		var sql = new Query()
			.SubString().OpenBracket().Column("t", "Name").Comma().Raw("1").Comma().Raw("5").CloseBracket()
			.Render(_ss);

		sql.AssertEqual("SubString([t].[Name], 1, 5)");
	}

	[TestMethod]
	public void StringFunc_Replace()
	{
		var sql = new Query()
			.Replace().OpenBracket().Column("t", "Name").Comma().Param("Old").Comma().Param("New").CloseBracket()
			.Render(_ss);

		sql.AssertEqual("replace([t].[Name], @Old, @New)");
	}

	#endregion

	#region Null handling

	[TestMethod]
	public void IsNull_ColumnCheck()
	{
		var sql = new Query()
			.IsNull("Status")
			.Render(_ss);

		sql.AssertEqual("[Status] is null");
	}

	[TestMethod]
	public void IsParamNull_Check()
	{
		var sql = new Query()
			.IsParamNull("Status")
			.Render(_ss);

		sql.AssertEqual("@Status is null");
	}

	[TestMethod]
	public void NullIf_Function()
	{
		var sql = new Query()
			.NullIf().OpenBracket().Column("t", "Value").Comma().Raw("0").CloseBracket()
			.Render(_ss);

		sql.AssertEqual("nullif([t].[Value], 0)");
	}

	[TestMethod]
	public void IsNull_Function()
	{
		var sql = new Query()
			.IsNull().OpenBracket().Column("t", "Value").Comma().Raw("0").CloseBracket()
			.Render(_ss);

		sql.AssertEqual("isnull([t].[Value], 0)");
	}

	[TestMethod]
	public void Column_Is_Null_Keyword()
	{
		var sql = new Query()
			.Column("t", "Value").Is().Null()
			.Render(_ss);

		sql.AssertEqual("[t].[Value] is null");
	}

	[TestMethod]
	public void Column_IsNot_Null_Keyword()
	{
		var sql = new Query()
			.Column("t", "Value").IsNot().Null()
			.Render(_ss);

		sql.AssertEqual("[t].[Value] is not null");
	}

	#endregion

	#region BatchQuery

	[TestMethod]
	public void BatchQuery_MultipleQueries_RendersWithNewlines()
	{
		var q1 = new Query()
			.Select().All("t")
			.NewLine()
			.From().Table("Users", "t");

		var q2 = new Query()
			.Select().All("t")
			.NewLine()
			.From().Table("Orders", "t");

		var batch = new BatchQuery();
		batch.Queries.Add(q1);
		batch.Queries.Add(q2);

		var sql = batch.Render(_ss);

		sql.AssertContains("from [Users] t");
		sql.AssertContains("from [Orders] t");

		// Both queries should be present and separated
		var usersIdx = sql.IndexOf("[Users]", StringComparison.Ordinal);
		var ordersIdx = sql.IndexOf("[Orders]", StringComparison.Ordinal);
		(usersIdx < ordersIdx).AssertTrue("First query should come before second query");
	}

	[TestMethod]
	public void BatchQuery_Empty_RendersEmpty()
	{
		var batch = new BatchQuery();
		var sql = batch.Render(_ss);

		sql.AssertEqual(string.Empty);
	}

	[TestMethod]
	public void BatchQuery_SingleQuery()
	{
		var q = new Query()
			.Delete().Raw(" ")
			.From().Table("TempData", "t");

		var batch = new BatchQuery();
		batch.Queries.Add(q);

		var sql = batch.Render(_ss);

		sql.AssertContains("delete from [TempData] t");
	}

	#endregion

	#region Miscellaneous operators and keywords

	[TestMethod]
	public void Like_ColumnCondition()
	{
		var sql = new Query()
			.Like("Name")
			.Render(_ss);

		sql.AssertEqual("[Name] like @Name");
	}

	[TestMethod]
	public void Like_Keyword()
	{
		var sql = new Query()
			.Column("t", "Name").Like().Param("Pattern")
			.Render(_ss);

		sql.AssertEqual("[t].[Name] like @Pattern");
	}

	[TestMethod]
	public void Between_Clause()
	{
		var sql = new Query()
			.Column("t", "CreatedAt").Between("@Low", "@High")
			.Render(_ss);

		sql.AssertEqual("[t].[CreatedAt] between @Low and @High");
	}

	[TestMethod]
	public void In_Clause()
	{
		var sql = new Query()
			.Column("t", "Status").In()
			.OpenBracket().Param("s1").Comma().Param("s2").Comma().Param("s3").CloseBracket()
			.Render(_ss);

		sql.AssertEqual("[t].[Status] in (@s1, @s2, @s3)");
	}

	[TestMethod]
	public void GroupBy_Having()
	{
		var sql = new Query()
			.Select().Column("t", "Status").Comma().Count().OpenBracket().Star().CloseBracket()
			.As().Raw("cnt")
			.NewLine()
			.From().Table("Users", "t")
			.NewLine()
			.GroupBy().Column("t", "Status")
			.NewLine()
			.Having().Raw(" ").Count().OpenBracket().Star().CloseBracket().More().Raw("5")
			.Render(_ss);

		sql.AssertContains("group by [t].[Status]");
		sql.AssertContains("having count(*) > 5");
	}

	[TestMethod]
	public void As_Alias()
	{
		var sql = new Query()
			.Column("t", "FirstName").As().Raw("fn")
			.Render(_ss);

		sql.AssertEqual("[t].[FirstName] as fn");
	}

	[TestMethod]
	public void Comparison_Operators()
	{
		new Query().Column("t", "A").Less().Param("v").Render(_ss)
			.AssertEqual("[t].[A] < @v");

		new Query().Column("t", "A").LessOrEqual().Param("v").Render(_ss)
			.AssertEqual("[t].[A] <= @v");

		new Query().Column("t", "A").More().Param("v").Render(_ss)
			.AssertEqual("[t].[A] > @v");

		new Query().Column("t", "A").MoreOrEqual().Param("v").Render(_ss)
			.AssertEqual("[t].[A] >= @v");

		new Query().Column("t", "A").NotEqual().Param("v").Render(_ss)
			.AssertEqual("[t].[A] <> @v");
	}

	[TestMethod]
	public void Bitwise_Operators()
	{
		new Query().Column("t", "Flags").BitwiseAnd().Param("mask").Render(_ss)
			.AssertEqual("[t].[Flags] & @mask");

		new Query().Column("t", "Flags").BitwiseOr().Param("mask").Render(_ss)
			.AssertEqual("[t].[Flags] | @mask");
	}

	[TestMethod]
	public void Exists_Subquery()
	{
		var sql = new Query()
			.Exists().OpenBracket()
			.Select().Star()
			.NewLine()
			.From().Table("Orders", "o")
			.NewLine()
			.Where().Raw(" ").Column("o", "UserId").Equal().Column("t", "Id")
			.CloseBracket()
			.Render(_ss);

		sql.AssertContains("exists(select *");
		sql.AssertContains("from [Orders] o");
		sql.AssertContains("where [o].[UserId] = [t].[Id])");
	}

	[TestMethod]
	public void Param_Renders_WithPrefix()
	{
		var sql = new Query().Param("MyValue").Render(_ss);
		sql.AssertEqual("@MyValue");
	}

	[TestMethod]
	public void Raw_Renders_Literally()
	{
		var sql = new Query().Raw("CUSTOM SQL FRAGMENT").Render(_ss);
		sql.AssertEqual("CUSTOM SQL FRAGMENT");
	}

	[TestMethod]
	public void Identity_SqlServer()
	{
		var sql = new Query().Identity("Id").Render(_ss);
		sql.AssertEqual("scope_identity() as Id");
	}

	[TestMethod]
	[DataRow("SqlServer", "scope_identity() as Id")]
	[DataRow("SQLite", "last_insert_rowid() as Id")]
	[DataRow("PostgreSql", "Id")]
	public void Identity_AllDialects(string dialectName, string expected)
	{
		var dialect = GetDialect(dialectName);
		var sql = new Query().Identity("Id").Render(dialect);
		sql.AssertEqual(expected);
	}

	#endregion

	#region Dialect-specific: Now, UtcNow, NewId

	[TestMethod]
	[DataRow("SqlServer", "getDate()")]
	[DataRow("SQLite", "datetime('now', 'localtime')")]
	[DataRow("PostgreSql", "now()")]
	public void Now_DialectSpecific(string dialectName, string expected)
	{
		var dialect = GetDialect(dialectName);
		var sql = new Query().Now().Render(dialect);
		sql.AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "getUtcDate()")]
	[DataRow("SQLite", "datetime('now')")]
	[DataRow("PostgreSql", "now() AT TIME ZONE 'UTC'")]
	public void UtcNow_DialectSpecific(string dialectName, string expected)
	{
		var dialect = GetDialect(dialectName);
		var sql = new Query().UtcNow().Render(dialect);
		sql.AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "newId()")]
	[DataRow("SQLite", "lower(hex(randomblob(16)))")]
	[DataRow("PostgreSql", "gen_random_uuid()")]
	public void NewId_DialectSpecific(string dialectName, string expected)
	{
		var dialect = GetDialect(dialectName);
		var sql = new Query().NewId().Render(dialect);
		sql.AssertEqual(expected);
	}

	#endregion

	#region Action count and AddAction

	[TestMethod]
	public void Actions_Count_Matches_ChainLength()
	{
		var q = new Query()
			.Select()   // 1
			.All("t")   // 2
			.From()     // 3
			.Table("Users", "t"); // 4

		q.Actions.Count.AssertEqual(4);
	}

	[TestMethod]
	public void AddAction_Null_Throws()
	{
		var q = new Query();
		ThrowsExactly<ArgumentNullException>(() => q.AddAction(null));
	}

	#endregion
}
