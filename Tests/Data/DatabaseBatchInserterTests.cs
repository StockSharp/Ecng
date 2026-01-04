namespace Ecng.Tests.Data;

using System.Data.Common;

using Ecng.Data;

using LinqToDB.Data;

using Microsoft.Data.SqlClient;

[TestClass]
[TestCategory("Integration")]
public class DatabaseBatchInserterTests : BaseTestClass
{
	private const string _testTableName = "ecng_batch_test";
	private const string _dynamicTestTableName = "ecng_batch_dynamic_test";

	private static DatabaseConnectionPair GetSqlServerConnectionPair()
	{
		return new()
		{
			Provider = DatabaseProviderRegistry.SqlServer,
			ConnectionString = GetSecret("DB_CONNECTION_STRING"),
		};
	}

	private static Linq2dbBatchInserterProvider CreateLinq2dbProvider() => new();
	private static AdoBatchInserterProvider CreateAdoProvider() => new(pair => new SqlConnection(pair.ConnectionString));

	private static Task RunTestWithLinq2db(Func<Linq2dbBatchInserterProvider, DataConnection, Task> test)
	{
		var provider = CreateLinq2dbProvider();
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());
		return test(provider, connection);
	}

	private static Task RunTestWithAdo(Func<AdoBatchInserterProvider, DbConnection, Task> test)
	{
		var provider = CreateAdoProvider();
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());
		return test(provider, connection);
	}

	[TestMethod]
	public async Task InsertAsync_SingleItem_Linq2db_Success()
	{
		await RunTestWithLinq2db(async (provider, connection) =>
		{
			provider.DropTable(connection, _testTableName);

			using var inserter = provider.Create<TestEntity>(
				connection,
				_testTableName,
				b => ConfigureMapping(b, _testTableName));

			var entity = new TestEntity
			{
				Id = 1,
				Name = "Test Item",
				Value = 123.45m,
				CreatedAt = DateTime.UtcNow,
			};

			await inserter.InsertAsync(entity, CancellationToken);
		});
	}

	[TestMethod]
	public async Task InsertAsync_SingleItem_Ado_Success()
	{
		await RunTestWithAdo(async (provider, connection) =>
		{
			provider.DropTable(connection, _testTableName);

			using var inserter = provider.Create<TestEntity>(
				connection,
				_testTableName,
				b => ConfigureMapping(b, _testTableName));

			var entity = new TestEntity
			{
				Id = 1,
				Name = "Test Item",
				Value = 123.45m,
				CreatedAt = DateTime.UtcNow,
			};

			await inserter.InsertAsync(entity, CancellationToken);
		});
	}

	[TestMethod]
	public async Task BulkCopyAsync_MultipleItems_Linq2db_Success()
	{
		await RunTestWithLinq2db(async (provider, connection) =>
		{
			provider.DropTable(connection, _testTableName);

			using var inserter = provider.Create<TestEntity>(
				connection,
				_testTableName,
				b => ConfigureMapping(b, _testTableName));

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
		});
	}

	[TestMethod]
	public async Task BulkCopyAsync_MultipleItems_Ado_Success()
	{
		await RunTestWithAdo(async (provider, connection) =>
		{
			provider.DropTable(connection, _testTableName);

			using var inserter = provider.Create<TestEntity>(
				connection,
				_testTableName,
				b => ConfigureMapping(b, _testTableName));

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
		});
	}

	[TestMethod]
	public async Task BulkCopyAsync_LargeDataset_Linq2db_Success()
	{
		await RunTestWithLinq2db(async (provider, connection) =>
		{
			provider.DropTable(connection, _testTableName);

			using var inserter = provider.Create<TestEntity>(
				connection,
				_testTableName,
				b => ConfigureMapping(b, _testTableName));

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
		});
	}

	[TestMethod]
	public async Task BulkCopyAsync_LargeDataset_Ado_Success()
	{
		await RunTestWithAdo(async (provider, connection) =>
		{
			provider.DropTable(connection, _testTableName);

			using var inserter = provider.Create<TestEntity>(
				connection,
				_testTableName,
				b => ConfigureMapping(b, _testTableName));

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
		});
	}

	[TestMethod]
	public async Task Create_WithDynamicProperties_Linq2db_Success()
	{
		await RunTestWithLinq2db(async (provider, connection) =>
		{
			provider.DropTable(connection, _dynamicTestTableName);

			using var inserter = provider.Create<DynamicTestEntity>(
				connection,
				_dynamicTestTableName,
				b => ConfigureDynamicMapping(b, _dynamicTestTableName));

			var entity = new DynamicTestEntity
			{
				Id = 1,
				Name = "Dynamic Test",
			};
			entity.SetDynamic("CustomField", "CustomValue");

			await inserter.InsertAsync(entity, CancellationToken);
		});
	}

	[TestMethod]
	public async Task Create_WithDynamicProperties_Ado_Success()
	{
		await RunTestWithAdo(async (provider, connection) =>
		{
			provider.DropTable(connection, _dynamicTestTableName);

			using var inserter = provider.Create<DynamicTestEntity>(
				connection,
				_dynamicTestTableName,
				b => ConfigureDynamicMapping(b, _dynamicTestTableName));

			var entity = new DynamicTestEntity
			{
				Id = 1,
				Name = "Dynamic Test",
			};
			entity.SetDynamic("CustomField", "CustomValue");

			await inserter.InsertAsync(entity, CancellationToken);
		});
	}

	[TestMethod]
	public void Create_NullConnection_Linq2db_ThrowsArgumentNullException()
	{
		var provider = CreateLinq2dbProvider();

		Throws<ArgumentNullException>(() =>
			provider.Create<TestEntity>(null, "table", _ => { }));
	}

	[TestMethod]
	public void Create_NullConnection_Ado_ThrowsArgumentNullException()
	{
		var provider = CreateAdoProvider();

		Throws<ArgumentNullException>(() =>
			provider.Create<TestEntity>(null, "table", _ => { }));
	}

	[TestMethod]
	public async Task Create_EmptyTableName_Linq2db_ThrowsArgumentNullException()
	{
		await RunTestWithLinq2db((provider, connection) =>
		{
			Throws<ArgumentNullException>(() =>
				provider.Create<TestEntity>(connection, "", _ => { }));
			return Task.CompletedTask;
		});
	}

	[TestMethod]
	public async Task Create_EmptyTableName_Ado_ThrowsArgumentNullException()
	{
		await RunTestWithAdo((provider, connection) =>
		{
			Throws<ArgumentNullException>(() =>
				provider.Create<TestEntity>(connection, "", _ => { }));
			return Task.CompletedTask;
		});
	}

	[TestMethod]
	public async Task Create_NullConfigureMapping_Linq2db_ThrowsArgumentNullException()
	{
		await RunTestWithLinq2db((provider, connection) =>
		{
			Throws<ArgumentNullException>(() =>
				provider.Create<TestEntity>(connection, "table", null));
			return Task.CompletedTask;
		});
	}

	[TestMethod]
	public async Task Create_NullConfigureMapping_Ado_ThrowsArgumentNullException()
	{
		await RunTestWithAdo((provider, connection) =>
		{
			Throws<ArgumentNullException>(() =>
				provider.Create<TestEntity>(connection, "table", null));
			return Task.CompletedTask;
		});
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

	public class TestEntity
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public decimal Value { get; set; }
		public DateTime CreatedAt { get; set; }
	}

	public class DynamicTestEntity
	{
		private readonly Dictionary<string, object> _dynamicProperties = [];

		public int Id { get; set; }
		public string Name { get; set; }

		public object GetDynamic(string name)
			=> _dynamicProperties.TryGetValue(name, out var value) ? value : null;

		public void SetDynamic(string name, object value)
			=> _dynamicProperties[name] = value;
	}
}
