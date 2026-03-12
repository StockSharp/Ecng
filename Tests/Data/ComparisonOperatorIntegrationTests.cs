namespace Ecng.Tests.Data;

using Ecng.Data;
using Ecng.Data.Sql;

using Microsoft.Data.Sqlite;

/// <summary>
/// Integration tests for ComparisonOperator with a real SQLite database.
/// Tests filtering via SelectAsync, UpdateAsync, and DeleteAsync on int, string, decimal, and null columns.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("Database")]
[DoNotParallelize]
public class ComparisonOperatorIntegrationTests : BaseTestClass
{
	private const string ProviderName = DatabaseProviderRegistry.SQLite;
	private const string TableName = "Ecng_CompOpTest";

	private static SqliteConnection _keepAlive;
	private static IDatabaseConnection _connection;
	private static IDatabaseTable _table;

	[ClassInitialize]
	public static void ClassInit(TestContext context)
	{
		SQLiteDialect.Register(SqliteFactory.Instance);

		// Keep-alive connection to prevent in-memory DB from being destroyed
		_keepAlive = new SqliteConnection("Data Source=Ecng_CompOpTest;Mode=Memory;Cache=Shared");
		_keepAlive.Open();

		var pair = new DatabaseConnectionPair
		{
			Provider = ProviderName,
			ConnectionString = "Data Source=Ecng_CompOpTest;Mode=Memory;Cache=Shared",
		};

		_connection = AdoDatabaseProvider.Instance.CreateConnection(pair);
	}

	[ClassCleanup]
	public static void ClassCleanup()
	{
		(_connection as IDisposable)?.Dispose();
		_keepAlive?.Dispose();
	}

	[TestInitialize]
	public async Task TestInit()
	{
		_table = AdoDatabaseProvider.Instance.GetTable(_connection, TableName);
		await _table.DropAsync(CancellationToken);
		await CreateIntStringDecimalTable();
		await SeedData();
	}

	[TestCleanup]
	public async Task TestClean()
	{
		await _table.DropAsync(CancellationToken);
	}

	/// <summary>
	/// Creates table with Id (int), Name (string), Price (decimal), Note (string nullable).
	/// </summary>
	private async Task CreateIntStringDecimalTable()
	{
		using var cmd = _keepAlive.CreateCommand();
		cmd.CommandText = $"""
			CREATE TABLE IF NOT EXISTS "{TableName}" (
				"Id" INTEGER PRIMARY KEY,
				"Name" TEXT NOT NULL,
				"Price" REAL NOT NULL,
				"Note" TEXT
			)
			""";
		await cmd.ExecuteNonQueryAsync(CancellationToken);
	}

	/// <summary>
	/// Seeds 8 rows covering a range of int, string, decimal, and null values.
	/// </summary>
	private async Task SeedData()
	{
		// Id=1, Name=Apple,     Price=1.50,   Note=Fruit
		// Id=2, Name=Banana,    Price=2.00,   Note=Fruit
		// Id=3, Name=Cherry,    Price=3.75,   Note=Berry
		// Id=4, Name=Date,      Price=5.00,   Note=NULL
		// Id=5, Name=Elderberry,Price=10.25,  Note=Berry
		// Id=6, Name=Fig,       Price=2.00,   Note=NULL
		// Id=7, Name=Grape,     Price=4.50,   Note=Fruit
		// Id=8, Name=Honeydew,  Price=8.00,   Note=Melon
		var rows = new (int id, string name, decimal price, string note)[]
		{
			(1, "Apple",      1.50m,  "Fruit"),
			(2, "Banana",     2.00m,  "Fruit"),
			(3, "Cherry",     3.75m,  "Berry"),
			(4, "Date",       5.00m,  null),
			(5, "Elderberry", 10.25m, "Berry"),
			(6, "Fig",        2.00m,  null),
			(7, "Grape",      4.50m,  "Fruit"),
			(8, "Honeydew",   8.00m,  "Melon"),
		};

		foreach (var (id, name, price, note) in rows)
		{
			var values = new Dictionary<string, object>
			{
				["Id"] = id,
				["Name"] = name,
				["Price"] = price,
				["Note"] = note,
			};
			await _table.InsertAsync(values, CancellationToken);
		}
	}

	private async Task<List<IDictionary<string, object>>> Select(params FilterCondition[] filters)
		=> (await _table.SelectAsync(filters, null, null, null, CancellationToken)).ToList();

	private static FilterCondition F(string col, ComparisonOperator op, object val) => new(col, op, val);

	private static List<int> Ids(List<IDictionary<string, object>> rows)
		=> rows.Select(r => Convert.ToInt32(r["Id"])).OrderBy(x => x).ToList();

	private static List<string> Names(List<IDictionary<string, object>> rows)
		=> rows.Select(r => r["Name"].ToString()).OrderBy(x => x).ToList();

	// ─── Int: Equal ───

	[TestMethod]
	public async Task Int_Equal()
	{
		var rows = await Select(F("Id", ComparisonOperator.Equal, 3));
		rows.Count.AssertEqual(1);
		Names(rows).First().AssertEqual("Cherry");
	}

	[TestMethod]
	public async Task Int_Equal_NoMatch()
	{
		var rows = await Select(F("Id", ComparisonOperator.Equal, 99));
		rows.Count.AssertEqual(0);
	}

	// ─── Int: NotEqual ───

	[TestMethod]
	public async Task Int_NotEqual()
	{
		var rows = await Select(F("Id", ComparisonOperator.NotEqual, 1));
		rows.Count.AssertEqual(7);
		Ids(rows).Contains(1).AssertFalse();
	}

	// ─── Int: Greater ───

	[TestMethod]
	public async Task Int_Greater()
	{
		// Id > 6 => 7, 8
		var rows = await Select(F("Id", ComparisonOperator.Greater, 6));
		Ids(rows).AssertEqual([7, 8]);
	}

	[TestMethod]
	public async Task Int_Greater_NoMatch()
	{
		var rows = await Select(F("Id", ComparisonOperator.Greater, 100));
		rows.Count.AssertEqual(0);
	}

	// ─── Int: GreaterOrEqual ───

	[TestMethod]
	public async Task Int_GreaterOrEqual()
	{
		// Id >= 7 => 7, 8
		var rows = await Select(F("Id", ComparisonOperator.GreaterOrEqual, 7));
		Ids(rows).AssertEqual([7, 8]);
	}

	[TestMethod]
	public async Task Int_GreaterOrEqual_BoundaryValue()
	{
		// Id >= 1 => all 8 rows
		var rows = await Select(F("Id", ComparisonOperator.GreaterOrEqual, 1));
		rows.Count.AssertEqual(8);
	}

	// ─── Int: Less ───

	[TestMethod]
	public async Task Int_Less()
	{
		// Id < 3 => 1, 2
		var rows = await Select(F("Id", ComparisonOperator.Less, 3));
		Ids(rows).AssertEqual([1, 2]);
	}

	[TestMethod]
	public async Task Int_Less_NoMatch()
	{
		var rows = await Select(F("Id", ComparisonOperator.Less, 0));
		rows.Count.AssertEqual(0);
	}

	// ─── Int: LessOrEqual ───

	[TestMethod]
	public async Task Int_LessOrEqual()
	{
		// Id <= 2 => 1, 2
		var rows = await Select(F("Id", ComparisonOperator.LessOrEqual, 2));
		Ids(rows).AssertEqual([1, 2]);
	}

	[TestMethod]
	public async Task Int_LessOrEqual_BoundaryValue()
	{
		// Id <= 8 => all 8 rows
		var rows = await Select(F("Id", ComparisonOperator.LessOrEqual, 8));
		rows.Count.AssertEqual(8);
	}

	// ─── Int: In ───

	[TestMethod]
	public async Task Int_In()
	{
		var rows = await Select(F("Id", ComparisonOperator.In, new[] { 2, 5, 7 }));
		Ids(rows).AssertEqual([2, 5, 7]);
	}

	[TestMethod]
	public async Task Int_In_SingleValue()
	{
		var rows = await Select(F("Id", ComparisonOperator.In, new[] { 4 }));
		rows.Count.AssertEqual(1);
		Names(rows).First().AssertEqual("Date");
	}

	[TestMethod]
	public async Task Int_In_EmptyList()
	{
		// Empty IN list should return no rows (1 = 0)
		var rows = await Select(F("Id", ComparisonOperator.In, Array.Empty<int>()));
		rows.Count.AssertEqual(0);
	}

	// ─── String: Equal ───

	[TestMethod]
	public async Task String_Equal()
	{
		var rows = await Select(F("Name", ComparisonOperator.Equal, "Cherry"));
		rows.Count.AssertEqual(1);
		Ids(rows).First().AssertEqual(3);
	}

	[TestMethod]
	public async Task String_Equal_NoMatch()
	{
		var rows = await Select(F("Name", ComparisonOperator.Equal, "Pineapple"));
		rows.Count.AssertEqual(0);
	}

	// ─── String: NotEqual ───

	[TestMethod]
	public async Task String_NotEqual()
	{
		var rows = await Select(F("Name", ComparisonOperator.NotEqual, "Apple"));
		rows.Count.AssertEqual(7);
		Names(rows).Contains("Apple").AssertFalse();
	}

	// ─── String: Greater / Less ───

	[TestMethod]
	public async Task String_Greater()
	{
		// Name > "G" => Grape, Honeydew (alphabetical ordering)
		var rows = await Select(F("Name", ComparisonOperator.Greater, "G"));
		Ids(rows).AssertEqual([7, 8]);
	}

	[TestMethod]
	public async Task String_LessOrEqual()
	{
		// Name <= "Cherry" => Apple, Banana, Cherry
		var rows = await Select(F("Name", ComparisonOperator.LessOrEqual, "Cherry"));
		Ids(rows).AssertEqual([1, 2, 3]);
	}

	// ─── String: Like ───

	[TestMethod]
	public async Task String_Like_Contains()
	{
		// Name LIKE '%err%' => Cherry, Elderberry
		var rows = await Select(F("Name", ComparisonOperator.Like, "%err%"));
		Ids(rows).AssertEqual([3, 5]);
	}

	[TestMethod]
	public async Task String_Like_StartsWith()
	{
		// Name LIKE 'E%' => Elderberry
		var rows = await Select(F("Name", ComparisonOperator.Like, "E%"));
		rows.Count.AssertEqual(1);
		Names(rows).First().AssertEqual("Elderberry");
	}

	[TestMethod]
	public async Task String_Like_EndsWith()
	{
		// Name LIKE '%ry' => Cherry, Elderberry
		var rows = await Select(F("Name", ComparisonOperator.Like, "%ry"));
		Ids(rows).AssertEqual([3, 5]);
	}

	[TestMethod]
	public async Task String_Like_ExactMatch()
	{
		// Name LIKE 'Fig' => Fig
		var rows = await Select(F("Name", ComparisonOperator.Like, "Fig"));
		rows.Count.AssertEqual(1);
		Names(rows).First().AssertEqual("Fig");
	}

	[TestMethod]
	public async Task String_Like_NoMatch()
	{
		var rows = await Select(F("Name", ComparisonOperator.Like, "%xyz%"));
		rows.Count.AssertEqual(0);
	}

	// ─── String: In ───

	[TestMethod]
	public async Task String_In()
	{
		var rows = await Select(F("Name", ComparisonOperator.In, new[] { "Apple", "Fig", "Honeydew" }));
		Ids(rows).AssertEqual([1, 6, 8]);
	}

	[TestMethod]
	public async Task String_In_NoMatch()
	{
		var rows = await Select(F("Name", ComparisonOperator.In, new[] { "Pineapple", "Kiwi" }));
		rows.Count.AssertEqual(0);
	}

	// ─── Decimal: operators ───

	[TestMethod]
	public async Task Decimal_Equal()
	{
		// Price = 2.00 => Banana(2), Fig(6)
		var rows = await Select(F("Price", ComparisonOperator.Equal, 2.00));
		Ids(rows).AssertEqual([2, 6]);
	}

	[TestMethod]
	public async Task Decimal_NotEqual()
	{
		// Price <> 2.00 => all except Banana and Fig (6 rows)
		var rows = await Select(F("Price", ComparisonOperator.NotEqual, 2.00));
		rows.Count.AssertEqual(6);
		Ids(rows).Contains(2).AssertFalse();
		Ids(rows).Contains(6).AssertFalse();
	}

	[TestMethod]
	public async Task Decimal_Greater()
	{
		// Price > 5.00 => Elderberry(10.25), Honeydew(8.00)
		var rows = await Select(F("Price", ComparisonOperator.Greater, 5.00));
		Ids(rows).AssertEqual([5, 8]);
	}

	[TestMethod]
	public async Task Decimal_GreaterOrEqual()
	{
		// Price >= 5.00 => Date(5.00), Elderberry(10.25), Honeydew(8.00)
		var rows = await Select(F("Price", ComparisonOperator.GreaterOrEqual, 5.00));
		Ids(rows).AssertEqual([4, 5, 8]);
	}

	[TestMethod]
	public async Task Decimal_Less()
	{
		// Price < 2.00 => Apple(1.50)
		var rows = await Select(F("Price", ComparisonOperator.Less, 2.00));
		rows.Count.AssertEqual(1);
		Names(rows).First().AssertEqual("Apple");
	}

	[TestMethod]
	public async Task Decimal_LessOrEqual()
	{
		// Price <= 2.00 => Apple(1.50), Banana(2.00), Fig(2.00)
		var rows = await Select(F("Price", ComparisonOperator.LessOrEqual, 2.00));
		Ids(rows).AssertEqual([1, 2, 6]);
	}

	[TestMethod]
	public async Task Decimal_Equal_Precision()
	{
		// Price = 3.75 => Cherry
		var rows = await Select(F("Price", ComparisonOperator.Equal, 3.75));
		rows.Count.AssertEqual(1);
		Names(rows).First().AssertEqual("Cherry");
	}

	// ─── NULL handling ───

	[TestMethod]
	public async Task Null_Equal_IsNull()
	{
		// Note IS NULL => Date(4), Fig(6)
		var rows = await Select(F("Note", ComparisonOperator.Equal, null));
		Ids(rows).AssertEqual([4, 6]);
	}

	[TestMethod]
	public async Task Null_NotEqual_IsNotNull()
	{
		// Note IS NOT NULL => Apple(1), Banana(2), Cherry(3), Elderberry(5), Grape(7), Honeydew(8)
		var rows = await Select(F("Note", ComparisonOperator.NotEqual, null));
		Ids(rows).AssertEqual([1, 2, 3, 5, 7, 8]);
	}

	// ─── Any operator ───

	[TestMethod]
	public async Task Any_ReturnsAllRows()
	{
		// Any with any value should return all rows (1 = 1)
		var rows = await Select(F("Id", ComparisonOperator.Any, 0));
		rows.Count.AssertEqual(8);
	}

	// ─── Multiple filters (AND) ───

	[TestMethod]
	public async Task MultipleFilters_And()
	{
		// Id > 2 AND Id < 6 => 3, 4, 5
		var rows = await Select(
			F("Id", ComparisonOperator.Greater, 2),
			F("Id", ComparisonOperator.Less, 6));
		Ids(rows).AssertEqual([3, 4, 5]);
	}

	[TestMethod]
	public async Task MultipleFilters_StringAndInt()
	{
		// Note = 'Fruit' AND Id > 2 => Grape(7)
		var rows = await Select(
			F("Note", ComparisonOperator.Equal, "Fruit"),
			F("Id", ComparisonOperator.Greater, 2));
		Ids(rows).AssertEqual([7]);
	}

	[TestMethod]
	public async Task MultipleFilters_EqualAndNotEqual()
	{
		// Note = 'Berry' AND Name <> 'Cherry' => Elderberry(5)
		var rows = await Select(
			F("Note", ComparisonOperator.Equal, "Berry"),
			F("Name", ComparisonOperator.NotEqual, "Cherry"));
		rows.Count.AssertEqual(1);
		Names(rows).First().AssertEqual("Elderberry");
	}

	// ─── Empty table ───

	[TestMethod]
	public async Task EmptyTable_AllOperators_ReturnEmpty()
	{
		// Delete all rows first
		await _table.DeleteAsync(null, CancellationToken);

		(await Select(F("Id", ComparisonOperator.Equal, 1))).Count.AssertEqual(0);
		(await Select(F("Id", ComparisonOperator.NotEqual, 1))).Count.AssertEqual(0);
		(await Select(F("Id", ComparisonOperator.Greater, 0))).Count.AssertEqual(0);
		(await Select(F("Id", ComparisonOperator.Less, 100))).Count.AssertEqual(0);
		(await Select(F("Id", ComparisonOperator.In, new[] { 1, 2 }))).Count.AssertEqual(0);
		(await Select(F("Name", ComparisonOperator.Like, "%a%"))).Count.AssertEqual(0);
		(await Select(F("Id", ComparisonOperator.Any, 0))).Count.AssertEqual(0);
	}

	// ─── Update with filter ───

	[TestMethod]
	public async Task Update_WithGreater()
	{
		// Update Price to 99 where Id > 6
		await _table.UpdateAsync(
			new Dictionary<string, object> { ["Price"] = 99.0 },
			[F("Id", ComparisonOperator.Greater, 6)],
			CancellationToken);

		var rows = await Select(F("Price", ComparisonOperator.Equal, 99.0));
		Ids(rows).AssertEqual([7, 8]);
	}

	[TestMethod]
	public async Task Update_WithNotEqual()
	{
		// Update Note to 'Updated' where Note <> 'Fruit'
		await _table.UpdateAsync(
			new Dictionary<string, object> { ["Note"] = "Updated" },
			[F("Note", ComparisonOperator.NotEqual, "Fruit")],
			CancellationToken);

		var rows = await Select(F("Note", ComparisonOperator.Equal, "Updated"));
		// Cherry(Berry), Elderberry(Berry), Honeydew(Melon) = 3 rows
		// Note: Date(NULL) and Fig(NULL) have NULL Note, which is NOT <> 'Fruit' in SQL
		Ids(rows).AssertEqual([3, 5, 8]);
	}

	// ─── Delete with filter ───

	[TestMethod]
	public async Task Delete_WithLessOrEqual()
	{
		var deleted = await _table.DeleteAsync(
			[F("Id", ComparisonOperator.LessOrEqual, 3)],
			CancellationToken);
		deleted.AssertEqual(3);

		var remaining = await Select();
		Ids(remaining).AssertEqual([4, 5, 6, 7, 8]);
	}

	[TestMethod]
	public async Task Delete_WithLike()
	{
		// Delete rows where Name LIKE '%erry%' => Cherry, Elderberry
		var deleted = await _table.DeleteAsync(
			[F("Name", ComparisonOperator.Like, "%erry%")],
			CancellationToken);
		deleted.AssertEqual(2);

		var remaining = await Select();
		remaining.Count.AssertEqual(6);
		Names(remaining).Contains("Cherry").AssertFalse();
		Names(remaining).Contains("Elderberry").AssertFalse();
	}

	[TestMethod]
	public async Task Delete_WithIn()
	{
		var deleted = await _table.DeleteAsync(
			[F("Id", ComparisonOperator.In, new[] { 1, 4, 8 })],
			CancellationToken);
		deleted.AssertEqual(3);

		var remaining = await Select();
		Ids(remaining).AssertEqual([2, 3, 5, 6, 7]);
	}
}
