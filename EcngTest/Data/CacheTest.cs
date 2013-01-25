namespace Ecng.Test.Data
{
	using System.Transactions;

	using Ecng.Serialization;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	public abstract class CacheEntity : FieldFactoryEntity<int>
	{ }


	/// <summary>
	/// Summary description for CacheTest
	/// </summary>
	[TestClass]
	public class CacheTest
	{
		#region Additional test attributes
		//
		// You can use the following additional attributes as you write your tests:
		//
		// Use ClassInitialize to run code before running the first test in the class
		// [ClassInitialize()]
		// public static void MyClassInitialize(TestContext testContext) { }
		//
		// Use ClassCleanup to run code after all tests in a class have run
		// [ClassCleanup()]
		// public static void MyClassCleanup() { }
		//
		// Use TestInitialize to run code before running each test 
		// [TestInitialize()]
		// public void MyTestInitialize() { }
		//
		// Use TestCleanup to run code after each test has run
		// [TestCleanup()]
		// public void MyTestCleanup() { }
		//
		#endregion

		[TestMethod]
		public void Simple()
		{
			using (var database = Config.CreateDatabase())
			{
				var entity = Config.Create<CacheEntity>();

				using (new TransactionScope())
				{
					database.Create(entity);
					database.Read<CacheEntity>(entity.Id).AssertNotNull();
				}

				database.ClearCache();
				Assert.IsNull(database.Read<CacheEntity>(entity.Id));

				using (var scope = new TransactionScope())
				{
					database.Create(entity);
					database.Read<CacheEntity>(entity.Id).AssertNotNull();
					scope.Complete();
				}

				database.ClearCache();
				database.Read<CacheEntity>(entity.Id).AssertNotNull();
			}
		}

		[TestMethod]
		public void Create()
		{
			using (var database = Config.CreateDatabase())
			{
				var entity = Config.Create<CacheEntity>();

				using (new TransactionScope())
				{
					database.Create(entity);
					database.Read<CacheEntity>(entity.Id).AssertNotNull();
				}

				Assert.IsNull(database.Read<CacheEntity>(entity.Id));

				using (var scope = new TransactionScope())
				{
					database.Create(entity);
					database.Read<CacheEntity>(entity.Id).AssertNotNull();
					scope.Complete();
				}

				database.Read<CacheEntity>(entity.Id).AssertNotNull();
			}
		}

		[TestMethod]
		public void Read()
		{
			using (var database = Config.CreateDatabase())
			{
				var entity = Config.Create<CacheEntity>();
				database.Create(entity);

				using (new TransactionScope())
					database.Read<CacheEntity>(entity.Id).AssertNotNull();

				database.Read<CacheEntity>(entity.Id).AssertNotNull();

				using (var scope = new TransactionScope())
				{
					database.Read<CacheEntity>(entity.Id).AssertNotNull();
					scope.Complete();
				}

				database.Read<CacheEntity>(entity.Id).AssertNotNull();
			}
		}

		[TestMethod]
		public void Update()
		{
			using (var database = Config.CreateDatabase())
			{
				var entity = Config.Create<CacheEntity>();
				entity.Value = 10;
				database.Create(entity);

				using (new TransactionScope())
				{
					entity.Value = 100;
					database.Update(entity);
				}

				Assert.AreEqual(10, database.Read<CacheEntity>(entity.Id).Value);

				using (var scope = new TransactionScope())
				{
					entity.Value = 100;
					database.Update(entity);
					scope.Complete();
				}

				Assert.AreEqual(100, database.Read<CacheEntity>(entity.Id).Value);
			}
		}

		[TestMethod]
		public void Delete()
		{
			using (var database = Config.CreateDatabase())
			{
				var entity = Config.Create<CacheEntity>();
				database.Create(entity);

				using (new TransactionScope())
				{
					database.Delete(entity);
					Assert.IsNull(database.Read<CacheEntity>(entity.Id));
				}

				database.Read<CacheEntity>(entity.Id).AssertNotNull();

				using (var scope = new TransactionScope())
				{
					database.Delete(entity);
					Assert.IsNull(database.Read<CacheEntity>(entity.Id));
					scope.Complete();
				}

				Assert.IsNull(database.Read<CacheEntity>(entity.Id));
			}
		}

		[TestMethod]
		public void DeleteAll()
		{
			using (var database = Config.CreateDatabase())
			{
				var entity = Config.Create<CacheEntity>();
				database.Create(entity);

				using (new TransactionScope())
				{
					database.DeleteAll(typeof(CacheEntity).GetSchema());
					Assert.IsNull(database.Read<CacheEntity>(entity.Id));
				}

				database.Read<CacheEntity>(entity.Id).AssertNotNull();

				using (var scope = new TransactionScope())
				{
					database.DeleteAll(typeof(CacheEntity).GetSchema());
					Assert.IsNull(database.Read<CacheEntity>(entity.Id));
					scope.Complete();
				}

				Assert.IsNull(database.Read<CacheEntity>(entity.Id));
			}
		}
	}
}
