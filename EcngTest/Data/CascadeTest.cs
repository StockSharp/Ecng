namespace Ecng.Test.Data
{
	using System;
	using System.Linq;

	using Ecng.Data;
	using Ecng.Serialization;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public class CascadeRoot : RootObject<Database>
	{
		#region CascadeRoot.ctor()

		public CascadeRoot(Database database)
			: base("Root", database)
		{
		}

		#endregion

		public ContainerEntityList Childs;

		public override void Initialize()
		{
			Childs = new ContainerEntityList(Database);
		}
	}

	public abstract class ContainerEntity : FieldFactoryEntity<int>
	{
		#region ContainerEntity.ctor()

		protected ContainerEntity()
		{
			Childs = new ChildContainerEntityList(null, this);
		}

		#endregion

		[RelationMany(typeof(ChildContainerEntityList))]
		public ChildContainerEntityList Childs;
	}

	public class ContainerEntityList : RelationManyList<ContainerEntity>
	{
		public ContainerEntityList(Database database)
			: base(database)
		{
		}
	}

	public abstract class ChildContainerEntity : FieldFactoryEntity<int>
	{
	}

	public class ChildContainerEntityList : RelationManyList<ChildContainerEntity>
	{
		#region ChildContainerEntityList.ctor()

		public ChildContainerEntityList(Database database, ContainerEntity entity)
			: base(database)
		{
		}

		#endregion
	}

	/// <summary>
	/// Summary description for CascadeTest
	/// </summary>
	[TestClass]
	public class CascadeTest
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
			Test(
			(database, entity) =>
				database.Create(entity),
			(database, entity) =>
				database.Delete(entity));
		}

		[TestMethod]
		public void Root()
		{
			CascadeRoot root = null;
			Test(
			(database, entity) =>
			{
				root = new CascadeRoot(database);
				root.Childs.Add(entity);
			},
			(database, entity) =>
				root.Childs.Clear());
		}

		private static void Test(Action<Database, ContainerEntity> create, Action<Database, ContainerEntity> delete)
		{
			using (var database = Config.CreateDatabase())
			{
				var entity = Config.Create<ContainerEntity>();
				entity.Childs.Add(Config.Create<ChildContainerEntity>());
				entity.Childs.Add(Config.Create<ChildContainerEntity>());
				entity.Childs.Add(Config.Create<ChildContainerEntity>());

				create(database, entity);
				database.ClearCache();

				var childIds = entity.Childs.Select(arg =>
				{
					Assert.AreNotEqual(0, arg.Id);
					return arg.Id;
				});

				foreach (var id in childIds)
					Assert.IsNotNull(database.Read<ChildContainerEntity>(id));

				entity = database.Read<ContainerEntity>(entity.Id);
				Assert.AreEqual(3, entity.Childs.Count);

				childIds = entity.Childs.Select(arg => arg.Id);

				delete(database, entity);
				database.ClearCache();

				foreach (var id in childIds)
					Assert.IsNull(database.Read<ChildContainerEntity>(id));
			}
		}
	}
}
