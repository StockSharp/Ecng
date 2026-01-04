#if NET10_0_OR_GREATER
namespace Ecng.Tests.Data;

using Ecng.Data;

using Microsoft.Data.SqlClient;

[TestClass]
[TestCategory("Integration")]
[DoNotParallelize]
public class DatabaseBatchInserterTests : BaseTestClass
{
	private const string _testTableName = "ecng_batch_test";
	private const string _dynamicTestTableName = "ecng_batch_dynamic_test";

	[ClassInitialize]
	public static void ClassInit(TestContext context)
	{
		// Register SQL Server provider for ADO.NET tests
		DatabaseProviderRegistry.Register(DatabaseProviderRegistry.SqlServer, SqlClientFactory.Instance);
	}

	private static DatabaseConnectionPair GetSqlServerConnectionPair()
	{
		return new()
		{
			Provider = DatabaseProviderRegistry.SqlServer,
			ConnectionString = GetSecret("DB_CONNECTION_STRING"),
		};
	}

	private static IDatabaseBatchInserterProvider CreateProvider(string providerName)
	{
		return providerName switch
		{
			nameof(Linq2dbBatchInserterProvider) => new Linq2dbBatchInserterProvider(),
			nameof(AdoBatchInserterProvider) => new AdoBatchInserterProvider(),
			_ => throw new InvalidOperationException($"Unknown provider: {providerName}"),
		};
	}

	[TestMethod]
	[DataRow(nameof(Linq2dbBatchInserterProvider))]
	[DataRow(nameof(AdoBatchInserterProvider))]
	public async Task InsertAsync_SingleItem_Success(string providerName)
	{
		var provider = CreateProvider(providerName);
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());

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
	}

	[TestMethod]
	[DataRow(nameof(Linq2dbBatchInserterProvider))]
	[DataRow(nameof(AdoBatchInserterProvider))]
	public async Task BulkCopyAsync_MultipleItems_Success(string providerName)
	{
		var provider = CreateProvider(providerName);
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());

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
	}

	[TestMethod]
	[DataRow(nameof(Linq2dbBatchInserterProvider))]
	[DataRow(nameof(AdoBatchInserterProvider))]
	public async Task BulkCopyAsync_LargeDataset_Success(string providerName)
	{
		var provider = CreateProvider(providerName);
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());

		provider.DropTable(connection, _testTableName);

		using var inserter = provider.Create<TestEntity>(
			connection,
			_testTableName,
			b => ConfigureMapping(b, _testTableName));

		var entities = Enumerable.Range(1, 100)
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

	[TestMethod]
	[DataRow(nameof(Linq2dbBatchInserterProvider))]
	[DataRow(nameof(AdoBatchInserterProvider))]
	public async Task Create_WithDynamicProperties_Success(string providerName)
	{
		var provider = CreateProvider(providerName);
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());

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
	}

	[TestMethod]
	[DataRow(nameof(Linq2dbBatchInserterProvider))]
	[DataRow(nameof(AdoBatchInserterProvider))]
	public void Create_NullConnection_ThrowsArgumentNullException(string providerName)
	{
		var provider = CreateProvider(providerName);
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());

		Throws<ArgumentNullException>(() =>
			provider.Create<TestEntity>(null, "table", _ => { }));
	}

	[TestMethod]
	[DataRow(nameof(Linq2dbBatchInserterProvider))]
	[DataRow(nameof(AdoBatchInserterProvider))]
	public async Task Create_EmptyTableName_ThrowsArgumentNullException(string providerName)
	{
		var provider = CreateProvider(providerName);
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());

		Throws<ArgumentNullException>(() =>
				provider.Create<TestEntity>(connection, "", _ => { }));
	}

	[TestMethod]
	[DataRow(nameof(Linq2dbBatchInserterProvider))]
	[DataRow(nameof(AdoBatchInserterProvider))]
	public async Task Create_NullConfigureMapping_ThrowsArgumentNullException(string providerName)
	{
		var provider = CreateProvider(providerName);
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());

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
#endif