namespace Ecng.Tests.Data;

using Ecng.Data;

using LinqToDB;

[TestClass]
[TestCategory("Integration")]
public class DatabaseBatchInserterTests : BaseTestClass
{
	private const string TestTableName = "ecng_batch_test";

	private DatabaseConnectionPair GetSqlServerConnection()
	{
		return new DatabaseConnectionPair
		{
			Provider = ProviderName.SqlServer + ".MS",
			ConnectionString = GetSecret("DB_CONNECTION_STRING"),
		};
	}

	private static string GetUniqueTableName()
		=> $"{TestTableName}_{Guid.NewGuid():N}";

	[TestMethod]
	public async Task InsertAsync_SingleItem_Success()
	{
		var provider = new Linq2dbBatchInserterProvider();
		var connection = GetSqlServerConnection();
		var tableName = GetUniqueTableName();

		try
		{
			using var inserter = provider.Create<TestEntity>(
				connection,
				tableName,
				b => ConfigureMapping(b, tableName));

			var entity = new TestEntity
			{
				Id = 1,
				Name = "Test Item",
				Value = 123.45m,
				CreatedAt = DateTime.UtcNow,
			};

			await inserter.InsertAsync(entity, CancellationToken);
		}
		finally
		{
			await DropTableAsync(connection, tableName);
		}
	}

	[TestMethod]
	public async Task BulkCopyAsync_MultipleItems_Success()
	{
		var provider = new Linq2dbBatchInserterProvider();
		var connection = GetSqlServerConnection();
		var tableName = GetUniqueTableName();

		try
		{
			using var inserter = provider.Create<TestEntity>(
				connection,
				tableName,
				b => ConfigureMapping(b, tableName));

			var entities = Enumerable.Range(1, 100)
				.Select(i => new TestEntity
				{
					Id = i,
					Name = $"Item {i}",
					Value = i * 1.5m,
					CreatedAt = DateTime.UtcNow.AddMinutes(-i),
				})
				.ToList();

			await inserter.BulkCopyAsync(entities, CancellationToken);
		}
		finally
		{
			await DropTableAsync(connection, tableName);
		}
	}

	[TestMethod]
	public async Task BulkCopyAsync_LargeDataset_Success()
	{
		var provider = new Linq2dbBatchInserterProvider();
		var connection = GetSqlServerConnection();
		var tableName = GetUniqueTableName();

		try
		{
			using var inserter = provider.Create<TestEntity>(
				connection,
				tableName,
				b => ConfigureMapping(b, tableName));

			var entities = Enumerable.Range(1, 10000)
				.Select(i => new TestEntity
				{
					Id = i,
					Name = $"Bulk Item {i}",
					Value = i * 0.01m,
					CreatedAt = DateTime.UtcNow,
				})
				.ToList();

			await inserter.BulkCopyAsync(entities, CancellationToken);
		}
		finally
		{
			await DropTableAsync(connection, tableName);
		}
	}

	[TestMethod]
	public async Task Create_WithDynamicProperties_Success()
	{
		var provider = new Linq2dbBatchInserterProvider();
		var connection = GetSqlServerConnection();
		var tableName = GetUniqueTableName();

		try
		{
			using var inserter = provider.Create<DynamicTestEntity>(
				connection,
				tableName,
				b => ConfigureDynamicMapping(b, tableName));

			var entity = new DynamicTestEntity
			{
				Id = 1,
				Name = "Dynamic Test",
			};
			entity.SetDynamic("CustomField", "CustomValue");

			await inserter.InsertAsync(entity, CancellationToken);
		}
		finally
		{
			await DropTableAsync(connection, tableName);
		}
	}

	[TestMethod]
	public void Create_NullConnection_ThrowsArgumentNullException()
	{
		var provider = new Linq2dbBatchInserterProvider();

		Throws<ArgumentNullException>(() =>
			provider.Create<TestEntity>(null, "table", _ => { }));
	}

	[TestMethod]
	public void Create_EmptyTableName_ThrowsArgumentNullException()
	{
		var provider = new Linq2dbBatchInserterProvider();
		var connection = GetSqlServerConnection();

		Throws<ArgumentNullException>(() =>
			provider.Create<TestEntity>(connection, "", _ => { }));
	}

	[TestMethod]
	public void Create_NullConfigureMapping_ThrowsArgumentNullException()
	{
		var provider = new Linq2dbBatchInserterProvider();
		var connection = GetSqlServerConnection();

		Throws<ArgumentNullException>(() =>
			provider.Create<TestEntity>(connection, "table", null));
	}

	private static void ConfigureMapping(IDatabaseMappingBuilder<TestEntity> builder, string tableName)
	{
		builder
			.HasTableName(tableName)
			.IsColumnRequired()
			.Property(e => e.Id).HasColumnName("id").HasDataType(DatabaseDataType.Int)
			.Property(e => e.Name).HasColumnName("name").HasLength(100).HasDataType(DatabaseDataType.NVarChar)
			.Property(e => e.Value).HasColumnName("value").HasScale(2).HasDataType(DatabaseDataType.Decimal)
			.Property(e => e.CreatedAt).HasColumnName("created_at").HasDataType(DatabaseDataType.DateTime);
	}

	private static void ConfigureDynamicMapping(IDatabaseMappingBuilder<DynamicTestEntity> builder, string tableName)
	{
		builder
			.HasTableName(tableName)
			.IsColumnRequired()
			.Property(e => e.Id).HasColumnName("id").HasDataType(DatabaseDataType.Int)
			.Property(e => e.Name).HasColumnName("name").HasLength(100).HasDataType(DatabaseDataType.NVarChar)
			.DynamicProperty("CustomField").HasColumnName("custom_field").HasLength(200).HasDataType(DatabaseDataType.NVarChar)
			.DynamicPropertyAccessors(
				(e, name, def) => e.GetDynamic(name) ?? def,
				(e, name, value) => e.SetDynamic(name, value));
	}

	private static Task DropTableAsync(DatabaseConnectionPair connection, string tableName)
	{
		try
		{
			using var db = connection.CreateConnection();
			using var cmd = db.CreateCommand();
			cmd.CommandText = $"IF OBJECT_ID('{tableName}', 'U') IS NOT NULL DROP TABLE [{tableName}]";
			cmd.ExecuteNonQuery();
		}
		catch
		{
			// Ignore cleanup errors
		}

		return Task.CompletedTask;
	}

	public class TestEntity
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public decimal Value { get; set; }
		public DateTime CreatedAt { get; set; }
	}

	public class DynamicTestEntity
	{
		private readonly Dictionary<string, object> _dynamicProperties = new();

		public int Id { get; set; }
		public string Name { get; set; }

		public object GetDynamic(string name)
			=> _dynamicProperties.TryGetValue(name, out var value) ? value : null;

		public void SetDynamic(string name, object value)
			=> _dynamicProperties[name] = value;
	}
}
