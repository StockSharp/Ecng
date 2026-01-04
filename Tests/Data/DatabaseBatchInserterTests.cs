namespace Ecng.Tests.Data;

using Ecng.Data;

using LinqToDB;
using LinqToDB.DataProvider.SqlServer;

using Microsoft.Data.SqlClient;

public enum BatchInserterProviderType
{
	Linq2db,
	Ado,
}

[TestClass]
[TestCategory("Integration")]
public class DatabaseBatchInserterTests : BaseTestClass
{
	private const string _testTableName = "ecng_batch_test";
	private const string _dynamicTestTableName = "ecng_batch_dynamic_test";

	private static DatabaseConnectionPair GetSqlServerConnection()
	{
		return new()
		{
			Provider = ProviderName.SqlServer2017 + "." + SqlServerProvider.MicrosoftDataSqlClient,
			ConnectionString = GetSecret("DB_CONNECTION_STRING"),
		};
	}

	private static IDatabaseBatchInserterProvider GetProvider(BatchInserterProviderType providerType)
		=> providerType switch
		{
			BatchInserterProviderType.Linq2db => new Linq2dbBatchInserterProvider(),
			BatchInserterProviderType.Ado => new AdoBatchInserterProvider(connStr => new SqlConnection(connStr)),
			_ => throw new ArgumentOutOfRangeException(nameof(providerType)),
		};

	private static void DropTable(DatabaseConnectionPair connection, string tableName)
	{
		using var db = connection.CreateConnection();
		using var cmd = db.CreateCommand();
		cmd.CommandText = $"IF OBJECT_ID('{tableName}', 'U') IS NOT NULL DROP TABLE [{tableName}]";
		cmd.ExecuteNonQuery();
	}

	[TestMethod]
	[DataRow(BatchInserterProviderType.Linq2db)]
	[DataRow(BatchInserterProviderType.Ado)]
	public async Task InsertAsync_SingleItem_Success(BatchInserterProviderType providerType)
	{
		var provider = GetProvider(providerType);
		var connection = GetSqlServerConnection();

		DropTable(connection, _testTableName);

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
	[DataRow(BatchInserterProviderType.Linq2db)]
	[DataRow(BatchInserterProviderType.Ado)]
	public async Task BulkCopyAsync_MultipleItems_Success(BatchInserterProviderType providerType)
	{
		var provider = GetProvider(providerType);
		var connection = GetSqlServerConnection();

		DropTable(connection, _testTableName);

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
	[DataRow(BatchInserterProviderType.Linq2db)]
	[DataRow(BatchInserterProviderType.Ado)]
	public async Task BulkCopyAsync_LargeDataset_Success(BatchInserterProviderType providerType)
	{
		var provider = GetProvider(providerType);
		var connection = GetSqlServerConnection();

		DropTable(connection, _testTableName);

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
	}

	[TestMethod]
	[DataRow(BatchInserterProviderType.Linq2db)]
	[DataRow(BatchInserterProviderType.Ado)]
	public async Task Create_WithDynamicProperties_Success(BatchInserterProviderType providerType)
	{
		var provider = GetProvider(providerType);
		var connection = GetSqlServerConnection();

		DropTable(connection, _dynamicTestTableName);

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
	[DataRow(BatchInserterProviderType.Linq2db)]
	[DataRow(BatchInserterProviderType.Ado)]
	public void Create_NullConnection_ThrowsArgumentNullException(BatchInserterProviderType providerType)
	{
		var provider = GetProvider(providerType);

		Throws<ArgumentNullException>(() =>
			provider.Create<TestEntity>(null, "table", _ => { }));
	}

	[TestMethod]
	[DataRow(BatchInserterProviderType.Linq2db)]
	[DataRow(BatchInserterProviderType.Ado)]
	public void Create_EmptyTableName_ThrowsArgumentNullException(BatchInserterProviderType providerType)
	{
		var provider = GetProvider(providerType);
		var connection = GetSqlServerConnection();

		Throws<ArgumentNullException>(() =>
			provider.Create<TestEntity>(connection, "", _ => { }));
	}

	[TestMethod]
	[DataRow(BatchInserterProviderType.Linq2db)]
	[DataRow(BatchInserterProviderType.Ado)]
	public void Create_NullConfigureMapping_ThrowsArgumentNullException(BatchInserterProviderType providerType)
	{
		var provider = GetProvider(providerType);
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
