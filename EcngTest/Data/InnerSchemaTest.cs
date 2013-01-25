namespace Ecng.Test.Data
{
	#region Using Directives

	using Ecng.Data;
	using Ecng.Reflection.Aspects;
	using Ecng.Serialization;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	#endregion

	[EntityExtension]
	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public abstract class InnerSchemaValue
	{
		#region Value

		[DefaultImp]
		public abstract int Value { get; set; }

		#endregion
	}

	public abstract class InnerSchemaEntity : FieldFactoryEntity<InnerSchemaValue>
	{
		#region Value

		[InnerSchema]
		[DefaultImp]
		public override abstract InnerSchemaValue Value { get; set; }

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public struct InnerSchemaValue2
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

	[Entity("InnerSchemaEntity")]
	public abstract class InnerSchemaEntity2 : FieldFactoryEntity<InnerSchemaValue2>
	{
		#region Value

		[InnerSchema]
		[DefaultImp]
		public override abstract InnerSchemaValue2 Value { get; set; }

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public struct InnerSchemaValue3
	{
		#region InnerSchemaValue3.ctor()

		public InnerSchemaValue3(int myVar, int value2)
		{
			_myVar = myVar;
			_value2 = value2;
		}

		#endregion

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

		[Field("Value2")]
		private int _value2;

		public int Value2
		{
			get { return _value2; }
			set { _value2 = value; }
		}

		#endregion
	}

	public abstract class InnerSchemaEntity3 : FieldFactoryEntity<InnerSchemaValue3>
	{
		#region Value

		[InnerSchema]
		[NameOverride("Value", "FirstValue")]
		[NameOverride("Value2", "SecondValue")]
		[DefaultImp]
		public override abstract InnerSchemaValue3 Value { get; set; }

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public struct InnerSchemaValue4
	{
		#region Value

		private InnerSchemaValue3 _myVar;

		public InnerSchemaValue3 Value
		{
			get { return _myVar; }
			set { _myVar = value; }
		}

		#endregion
	}

	public abstract class InnerSchemaEntity4 : FieldFactoryEntity<InnerSchemaValue4>
	{
		#region Value

		[InnerSchema]
		[NameOverride("Value", "FirstValue")]
		[NameOverride("Value2", "SecondValue")]
		[DefaultImp]
		public override abstract InnerSchemaValue4 Value { get; set; }

		#endregion

		#region Value2

		[InnerSchema]
		[NameOverride("Value", "FirstValue2")]
		[NameOverride("Value2", "SecondValue2")]
		[DefaultImp]
		public abstract InnerSchemaValue4 Value2 { get; set; }

		#endregion
	}

	[TestClass]
	public class InnerSchemaTest
	{
		[TestMethod]
		public void SimpleAbs()
		{
			using (Database db = Config.CreateDatabase())
			{
				InnerSchemaEntity entity = Config.Create<InnerSchemaEntity>();
				entity.Value = Config.Create<InnerSchemaValue>();
				entity.Value.Value = 10;
				db.Create(entity);

				db.ClearCache();
				entity = db.Read<InnerSchemaEntity>(entity.Id);
				Assert.AreEqual(10, entity.Value.Value);

				entity.Value.Value = 5;
				db.Update(entity);
				Assert.AreEqual(5, entity.Value.Value);

				db.Delete(entity);
			}
		}

		[TestMethod]
		public void Simple()
		{
			using (Database db = Config.CreateDatabase())
			{
				InnerSchemaEntity2 entity = Config.Create<InnerSchemaEntity2>();
				InnerSchemaValue2 value = new InnerSchemaValue2();

				value.Value = 10;
				entity.Value = value;
				
				db.Create(entity);

				db.ClearCache();
				entity = db.Read<InnerSchemaEntity2>(entity.Id);
				Assert.AreEqual(10, entity.Value.Value);

				value.Value = 5;
				entity.Value = value;
				db.Update(entity);
				Assert.AreEqual(5, entity.Value.Value);

				db.Delete(entity);
			}
		}

		[TestMethod]
		public void Complex()
		{
			using (Database db = Config.CreateDatabase())
			{
				InnerSchemaEntity3 entity = Config.Create<InnerSchemaEntity3>();
				InnerSchemaValue3 value = new InnerSchemaValue3();

				value.Value = 10;
				value.Value2 = 5;
				entity.Value = value;
				db.Create(entity);

				db.ClearCache();
				entity = db.Read<InnerSchemaEntity3>(entity.Id);
				Assert.AreEqual(10, entity.Value.Value);
				Assert.AreEqual(5, entity.Value.Value2);

				value.Value = 5;
				value.Value2 = 10;
				entity.Value = value;
				db.Update(entity);
				Assert.AreEqual(5, entity.Value.Value);
				Assert.AreEqual(10, entity.Value.Value2);

				db.Delete(entity);
			}
		}

		[TestMethod]
		public void Complex2()
		{
			using (Database db = Config.CreateDatabase())
			{
				InnerSchemaEntity4 entity = Config.Create<InnerSchemaEntity4>();
				InnerSchemaValue4 value = new InnerSchemaValue4();
				value.Value = new InnerSchemaValue3(10, 5);
				InnerSchemaValue4 value2 = new InnerSchemaValue4();
				value2.Value = new InnerSchemaValue3(7, 3);

				entity.Value = value;
				entity.Value2 = value2;
				db.Create(entity);

				db.ClearCache();
				entity = db.Read<InnerSchemaEntity4>(entity.Id);
				Assert.AreEqual(10, entity.Value.Value.Value);
				Assert.AreEqual(5, entity.Value.Value.Value2);
				Assert.AreEqual(7, entity.Value2.Value.Value);
				Assert.AreEqual(3, entity.Value2.Value.Value2);

				value.Value = new InnerSchemaValue3(5, 10);
				entity.Value = value;
				value2.Value = new InnerSchemaValue3(3, 7);
				entity.Value2 = value2;
				db.Update(entity);
				Assert.AreEqual(5, entity.Value.Value.Value);
				Assert.AreEqual(10, entity.Value.Value.Value2);
				Assert.AreEqual(3, entity.Value2.Value.Value);
				Assert.AreEqual(7, entity.Value2.Value.Value2);

				db.Delete(entity);
			}
		}
	}
}