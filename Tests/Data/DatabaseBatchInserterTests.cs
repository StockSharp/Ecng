namespace Ecng.Tests.Data;

using System.Data.Common;

using Ecng.Data;

using LinqToDB;
using LinqToDB.DataProvider.SqlServer;

using Microsoft.Data.SqlClient;

[TestClass]
[TestCategory("Integration")]
public class DatabaseBatchInserterTests : BaseTestClass
{
	private const string TestTableName = "ecng_batch_test";
	private const string DynamicTestTableName = "ecng_batch_dynamic_test";
	private const string AdoTestTableName = "ecng_batch_ado_test";
	private const string AdoDynamicTestTableName = "ecng_batch_ado_dynamic_test";

	private DatabaseConnectionPair GetSqlServerConnection()
	{
		return new DatabaseConnectionPair
		{
			Provider = ProviderName.SqlServer2017 + "." + SqlServerProvider.MicrosoftDataSqlClient,
			ConnectionString = GetSecret("DB_CONNECTION_STRING"),
		};
	}

	private static void DropTable(DatabaseConnectionPair connection, string tableName)
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
	}

	[TestMethod]
	public async Task InsertAsync_SingleItem_Success()
	{
		var provider = new Linq2dbBatchInserterProvider();
		var connection = GetSqlServerConnection();

		DropTable(connection, TestTableName);

		using var inserter = provider.Create<TestEntity>(
			connection,
			TestTableName,
			b => ConfigureMapping(b, TestTableName));

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
	public async Task BulkCopyAsync_MultipleItems_Success()
	{
		var provider = new Linq2dbBatchInserterProvider();
		var connection = GetSqlServerConnection();

		DropTable(connection, TestTableName);

		using var inserter = provider.Create<TestEntity>(
			connection,
			TestTableName,
			b => ConfigureMapping(b, TestTableName));

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
	public async Task BulkCopyAsync_LargeDataset_Success()
	{
		var provider = new Linq2dbBatchInserterProvider();
		var connection = GetSqlServerConnection();

		DropTable(connection, TestTableName);

		using var inserter = provider.Create<TestEntity>(
			connection,
			TestTableName,
			b => ConfigureMapping(b, TestTableName));

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
	public async Task Create_WithDynamicProperties_Success()
	{
		var provider = new Linq2dbBatchInserterProvider();
		var connection = GetSqlServerConnection();

		DropTable(connection, DynamicTestTableName);

		using var inserter = provider.Create<DynamicTestEntity>(
			connection,
			DynamicTestTableName,
			b => ConfigureDynamicMapping(b, DynamicTestTableName));

		var entity = new DynamicTestEntity
		{
			Id = 1,
			Name = "Dynamic Test",
		};
		entity.SetDynamic("CustomField", "CustomValue");

		await inserter.InsertAsync(entity, CancellationToken);
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

	#region ADO.NET Provider Tests

	private static AdoBatchInserterProvider GetAdoProvider()
		=> new(connStr => new SqlConnection(connStr));

	[TestMethod]
	public async Task Ado_InsertAsync_SingleItem_Success()
	{
		var provider = GetAdoProvider();
		var connection = GetSqlServerConnection();

		DropTable(connection, AdoTestTableName);

		using var inserter = provider.Create<TestEntity>(
			connection,
			AdoTestTableName,
			b => ConfigureMapping(b, AdoTestTableName));

		var entity = new TestEntity
		{
			Id = 1,
			Name = "ADO Test Item",
			Value = 123.45m,
			CreatedAt = DateTime.UtcNow,
		};

		await inserter.InsertAsync(entity, CancellationToken);
	}

	[TestMethod]
	public async Task Ado_BulkCopyAsync_MultipleItems_Success()
	{
		var provider = GetAdoProvider();
		var connection = GetSqlServerConnection();

		DropTable(connection, AdoTestTableName);

		using var inserter = provider.Create<TestEntity>(
			connection,
			AdoTestTableName,
			b => ConfigureMapping(b, AdoTestTableName));

		var entities = Enumerable.Range(1, 100)
			.Select(i => new TestEntity
			{
				Id = i,
				Name = $"ADO Item {i}",
				Value = i * 1.5m,
				CreatedAt = DateTime.UtcNow.AddMinutes(-i),
			})
			.ToList();

		await inserter.BulkCopyAsync(entities, CancellationToken);
	}

	[TestMethod]
	public async Task Ado_BulkCopyAsync_LargeDataset_Success()
	{
		var provider = GetAdoProvider();
		var connection = GetSqlServerConnection();

		DropTable(connection, AdoTestTableName);

		using var inserter = provider.Create<TestEntity>(
			connection,
			AdoTestTableName,
			b => ConfigureMapping(b, AdoTestTableName));

		var entities = Enumerable.Range(1, 10000)
			.Select(i => new TestEntity
			{
				Id = i,
				Name = $"ADO Bulk Item {i}",
				Value = i * 0.01m,
				CreatedAt = DateTime.UtcNow,
			})
			.ToList();

		await inserter.BulkCopyAsync(entities, CancellationToken);
	}

	[TestMethod]
	public async Task Ado_Create_WithDynamicProperties_Success()
	{
		var provider = GetAdoProvider();
		var connection = GetSqlServerConnection();

		DropTable(connection, AdoDynamicTestTableName);

		using var inserter = provider.Create<DynamicTestEntity>(
			connection,
			AdoDynamicTestTableName,
			b => ConfigureDynamicMapping(b, AdoDynamicTestTableName));

		var entity = new DynamicTestEntity
		{
			Id = 1,
			Name = "ADO Dynamic Test",
		};
		entity.SetDynamic("CustomField", "CustomValue");

		await inserter.InsertAsync(entity, CancellationToken);
	}

	[TestMethod]
	public void Ado_Create_NullConnection_ThrowsArgumentNullException()
	{
		var provider = GetAdoProvider();

		Throws<ArgumentNullException>(() =>
			provider.Create<TestEntity>(null, "table", _ => { }));
	}

	[TestMethod]
	public void Ado_Create_EmptyTableName_ThrowsArgumentNullException()
	{
		var provider = GetAdoProvider();
		var connection = GetSqlServerConnection();

		Throws<ArgumentNullException>(() =>
			provider.Create<TestEntity>(connection, "", _ => { }));
	}

	[TestMethod]
	public void Ado_Create_NullConfigureMapping_ThrowsArgumentNullException()
	{
		var provider = GetAdoProvider();
		var connection = GetSqlServerConnection();

		Throws<ArgumentNullException>(() =>
			provider.Create<TestEntity>(connection, "table", null));
	}

	#endregion

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
		private readonly Dictionary<string, object> _dynamicProperties = new();

		public int Id { get; set; }
		public string Name { get; set; }

		public object GetDynamic(string name)
			=> _dynamicProperties.TryGetValue(name, out var value) ? value : null;

		public void SetDynamic(string name, object value)
			=> _dynamicProperties[name] = value;
	}
}
