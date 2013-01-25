namespace Ecng.Test.Data
{
	using System;
	using System.Configuration;

	using Ecng.Data;
	using Ecng.Data.Sql;
	using Ecng.Reflection.Aspects;
	using Ecng.Serialization;
	using Ecng.Transactions;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[Serializable]
	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public struct SerializationValue
	{
		#region InnerField

		private int _innerField;

		public int InnerField
		{
			get { return _innerField; }
			set { _innerField = value; }
		}

		#endregion
	}

	public class TestRelationAttribute : RelationSingleAttribute
	{
		//protected override Database GetDatabase()
		//{
		//    return Config.CreateDatabase();
		//}
	}

	[EntityExtension]
	[Serializable]
	public abstract class SerializationEntity : FieldFactoryEntity<string>
	{
		[DefaultImp]
		public abstract int Field1 { get; set; }

		[LazyLoad]
		[Wrapper(typeof(LazyLoadObject<>))]
		public abstract int Field2 { get; set; }

		[InnerSchema(Order = 0)]
		[LazyLoad(Order = 1)]
		[Wrapper(typeof(LazyLoadObject<>))]
		public abstract SerializationValue Field3 { get; set; }

		[Transactional]
		[Wrapper(typeof(Transactional<>))]
		public abstract int Field4 { get; set; }

		[InnerSchema(Order = 0)]
		[Transactional(Order = 1)]
		[Wrapper(typeof(Transactional<>))]
		public abstract SerializationValue Field5 { get; set; }

		[InnerSchema]
		[DefaultImp]
		public abstract SerializationValue Field6 { get; set; }

		[InnerSchema(Order = 0)]
		[Transactional(Order = 1)]
		[Wrapper(typeof(Transactional<>))]
		public abstract SerializationValue Field7 { get; set; }

		[RelationMany(typeof(SerializationChildEntityList))]
		[DefaultImp]
		public abstract SerializationChildEntityList Field8 { get; }

		[FieldValue("Name1")]
		[FieldValue("Name2")]
		[FieldValues]
		[DefaultImp]
		public abstract FieldValue Field9 { get; set; }

		[Crypto(PublicKey = "/wSLfzApvDnYlBrGZV1zsichriJC+Eli1KgzdlIWAIQ=", KeyType = KeyTypes.Direct)]
		[DefaultImp]
		public abstract int Field10 { get; set; }

		[IntegerValidator(MinValue = 10, MaxValue = 20)]
		[DefaultImp]
		public abstract int Field11 { get; set; }

		[Nullable]
		[DefaultImp]
		public abstract int? Field12 { get; set; }

		[TestRelation]
		[DefaultImp]
		public abstract SerializationChildEntity Field13 { get; set; }

		[Serializer]
		[DefaultImp]
		public abstract SerializationValue[] Field14 { get; set; }
	}

	[Serializable]
	public abstract class SerializationChildEntity : FieldFactoryEntity<string>
	{
		[TestRelation]
		[DefaultImp]
		public abstract SerializationEntity Field1 { get; set; }
	}

	public class SerializationChildEntityList : RelationManyList<SerializationChildEntity>
	{
		private SerializationEntity _entity;

		#region SerializationChildEntityList.ctor()

		public SerializationChildEntityList(Database database, SerializationEntity entity)
			: base(database)
		{
			_entity = entity;
		}

		#endregion
	}

	/// <summary>
	/// Summary description for SerializationTest
	/// </summary>
	[TestClass]
	public class SerializationTest
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
		public void BinaryEntity()
		{
			nTestEntity<BinarySerializer<SerializationEntity>>();
		}

		[TestMethod]
		public void XmlEntity()
		{
			nTestEntity<XmlSerializer<SerializationEntity>>();
		}

		[TestMethod]
		public void BinarySchema()
		{
			nTest<Schema, BinarySerializer<Schema>>(SchemaManager.GetSchema<SerializationEntity>());
		}

		[TestMethod]
		public void XmlSchema()
		{
			nTest<Schema, XmlSerializer<Schema>>(SchemaManager.GetSchema<SerializationEntity>());
		}

		[TestMethod]
		public void BinaryQuery()
		{
			nTest<Query[], BinarySerializer<Query[]>>(CreateQueries());
		}

		[TestMethod]
		public void XmlQuery()
		{
			nTest<Query[], XmlSerializer<Query[]>>(CreateQueries());
		}

		[TestMethod]
		public void BinaryDatabase()
		{
			using (var database = CreateDatabase())
				nTest<Database, BinarySerializer<Database>>(database);
		}

		[TestMethod]
		public void XmlDatabase()
		{
			using (var database = CreateDatabase())
				nTest<Database, XmlSerializer<Database>>(database);
		}

		private static Query[] CreateQueries()
		{
			return new[]
			{
				Query.Create(SchemaManager.GetSchema<SerializationEntity>(), SqlCommandTypes.Create, null, null),
				Query.Create(SchemaManager.GetSchema<SerializationEntity>(), SqlCommandTypes.ReadBy, null, null),
				Query.Create(SchemaManager.GetSchema<SerializationEntity>(), SqlCommandTypes.ReadAll, null, null),
				Query.Create(SchemaManager.GetSchema<SerializationEntity>(), SqlCommandTypes.UpdateBy, null, null),
				Query.Create(SchemaManager.GetSchema<SerializationEntity>(), SqlCommandTypes.DeleteBy, null, null),
				Query.Create(SchemaManager.GetSchema<SerializationEntity>(), SqlCommandTypes.DeleteAll, null, null),
				Query.Create(SchemaManager.GetSchema<SerializationEntity>(), SqlCommandTypes.Count, null, null),
			};
		}

		private static Database CreateDatabase()
		{
			var database = Config.CreateDatabase();

			var entity = Config.Create<SerializationEntity>();
			database.Create(entity);
			database.Read<SerializationEntity>(entity.Id);
			database.ReadAll<SerializationEntity>();
			database.Update(entity);
			database.Delete(entity);
			database.DeleteAll(SchemaManager.GetSchema<SerializationEntity>());

			return database;
		}

		private static void nTestEntity<S>()
			where S : Serializer<SerializationEntity>, new()
		{
			var entity = Config.Create<SerializationEntity>();
			nTest<SerializationEntity, S>(entity);

			entity.Field8.Add(Config.Create<SerializationChildEntity>());
			entity.Field8.Add(Config.Create<SerializationChildEntity>());
			entity.Field8.Add(Config.Create<SerializationChildEntity>());
			nTest<SerializationEntity, S>(entity);
			nTest<SerializationChildEntityList, XmlSerializer<SerializationChildEntityList>>(entity.Field8);

			using (var database = Config.CreateDatabase())
			{
				database.Create(entity);
				database.ClearCache();

				entity = database.Read<SerializationEntity>(entity.Id);
				nTest<SerializationEntity, S>(entity);
			}
		}

		private static void nTest<T, S>(T graph)
			where S : Serializer<T>, new()
		{
			Assert.Equals(new S().Deserialize(new S().Serialize(graph)), graph);
		}
	}
}