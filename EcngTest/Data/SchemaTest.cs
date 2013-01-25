namespace Ecng.Test.Data
{
	#region Using Directives

	using System;

	using Ecng.Data;
	using Ecng.Reflection.Aspects;
	using Ecng.Serialization;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	#endregion

	[EntityExtension]
	public abstract class BaseSchemaEntity
	{
		#region Value

		[NotImpException]
		public abstract int Value { get; set; }

		#endregion
	}

	//[DbSchema]
	//public abstract class DbSchemaEntity : BaseSchemaEntity
	//{
	//}

	//[DbSchema(Database = "Schema Database")]
	//public abstract class DbSchemaEntity2 : BaseSchemaEntity
	//{
	//}

	[BinarySchemaFactory]
	public abstract class FileSchemaEntity : BaseSchemaEntity
	{
	}

	[XmlSchemaFactory]
	public abstract class FileSchemaEntity2 : BaseSchemaEntity
	{
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public abstract class TypeSchemaEntity : BaseSchemaEntity
	{
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public abstract class TwoIdentityEntity
	{
		#region Id

		[Identity]
		public abstract long Id { get; set; }

		#endregion

		#region ValidationId

		[Identity]
		public abstract long Id2 { get; set; }

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public abstract class ReadOnlyEntity
	{
		#region Value

		[Field("Value", ReadOnly = true)]
		public abstract int Value { get; set; }

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public struct ReadOnlyValue2
	{
		#region Value

		[Field("Value")]
		private int _value;

		public int Value
		{
			get { return _value; }
			set { _value = value; }
		}

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public abstract class ReadOnlyEntity2
	{
		#region Value

		[Field("Value", ReadOnly = true)]
		public abstract ReadOnlyValue2 Value { get; set; }

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public struct ReadOnlyValue3
	{
		#region Value

		[Field("Value", ReadOnly = true)]
		private int _value;

		public int Value
		{
			get { return _value; }
			set { _value = value; }
		}

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public abstract class ReadOnlyEntity3
	{
		#region Value

		public abstract ReadOnlyValue3 Value { get; set; }

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public class EmptyEntityList : RelationManyList<EmptyEntity>
	{
		#region EmptyEntityList.ctor()

		public EmptyEntityList(Database database, EmptyEntity e)
			: base(database)
		{

		}

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public abstract class EmptyEntity
	{
		#region Entities

		[RelationMany(typeof(EmptyEntityList))]
		public abstract EmptyEntityList Entities { get; }

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public class DuplicateNamesEntity
	{
		#region Value

		[Field("Value")]
		private int _myVar;

		public int Value
		{
			get { return _myVar; }
			set { _myVar = value; }
		}

		#endregion

		#region Value2

		[Field("Value")]
		private int _value2;

		public int Value2
		{
			get { return _value2; }
			set { _value2 = value; }
		}

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public class DuplicateNamesValue
	{
		#region Value

		[Field("Value")]
		private int _myVar;

		public int Value
		{
			get { return _myVar; }
			set { _myVar = value; }
		}

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public class DuplicateNamesEntity2
	{
		#region Value

		[Field("Value")]
		private int _myVar;

		public int Value
		{
			get { return _myVar; }
			set { _myVar = value; }
		}

		#endregion

		#region Value2

		private DuplicateNamesValue _value2;

		[InnerSchema]
		public DuplicateNamesValue Value2
		{
			get { return _value2; }
			set { _value2 = value; }
		}

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public abstract class AbsDuplicateNamesEntity
	{
		#region Value

		[Field("FirstValue")]
		public abstract int Value { get; set; }

		#endregion

		#region Value2

		[InnerSchema]
		public abstract DuplicateNamesValue Value2 { get; set; }

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public struct InvalidRelationValue
	{
		#region Value

		[Field("Value")]
		private int _value;

		public int Value
		{
			get { return _value; }
			set { _value = value; }
		}

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public abstract class InvalidRelationEntity
	{
		#region Id

		[Identity]
		public abstract long Id { get; set; }

		#endregion

		#region Value

		[TestRelation]
		public abstract InvalidRelationValue Value { get; set; }

		#endregion
	}

	[TestClass]
	public class SchemaTest
	{
		//[TestMethod]
		//public void Db()
		//{
		//    TestSchema(typeof(DbSchemaEntity));
		//}

		//[TestMethod]
		//public void Db2()
		//{
		//    TestSchema(typeof(DbSchemaEntity2));
		//}

		[TestMethod]
		public void XmlFile()
		{
			TestSchema(typeof(FileSchemaEntity));
		}

		[TestMethod]
		public void BinaryFile()
		{
			TestSchema(typeof(FileSchemaEntity2));
		}

		[TestMethod]
		public void Type()
		{
			TestSchema(typeof(TypeSchemaEntity));
		}

		[TestMethod]
		public void TwoIdentity()
		{
			TestSchema(typeof(TwoIdentityEntity));
		}

		[TestMethod]
		public void ReadOnly()
		{
			TestSchema(typeof(ReadOnlyEntity));
		}

		[TestMethod]
		public void ReadOnly2()
		{
			TestSchema(typeof(ReadOnlyEntity2));
		}

		[TestMethod]
		public void ReadOnly3()
		{
			TestSchema(typeof(ReadOnlyEntity3));
		}

		[TestMethod]
		public void EmptyFields()
		{
			TestSchema(typeof(EmptyEntity));
		}

		[TestMethod]
		public void DuplicateNames()
		{
			TestSchema(typeof(DuplicateNamesEntity));
		}

		[TestMethod]
		public void DuplicateNames2()
		{
			TestSchema(typeof(DuplicateNamesEntity2));
		}

		[TestMethod]
		public void AbsDuplicateNames()
		{
			TestSchema(typeof(AbsDuplicateNamesEntity));
		}

		[TestMethod]
		public void InvalidRelationEntity()
		{
			TestSchema(typeof(InvalidRelationEntity));
		}

		private static void TestSchema(Type entityType)
		{
			var schema = entityType.GetSchema();
			Assert.AreEqual(1, schema.Fields.Count);
		}
	}
}