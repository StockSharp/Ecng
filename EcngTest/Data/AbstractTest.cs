namespace Ecng.Test.Data
{
	#region Using Directives

	using System;

	using Ecng.Data;
	using Ecng.Reflection.Aspects;
	using Ecng.Serialization;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	#endregion

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	[EntityExtension]
	public abstract class AbstractEntity
	{
		#region Id

		[Identity]
		[Field("Id", ReadOnly = true)]
		[DefaultImp]
		public abstract long Id { get; set; }

		#endregion

		#region Value

		[DefaultImp]
		public abstract string Value { get; set; }

		#endregion
	}

	[Entity("AbstractEntity")]
	public abstract class DerivedAbstractEntity : AbstractEntity
	{
	}

	[Entity("AbstractEntity")]
	public class DerivedEntity : DerivedAbstractEntity
	{
		#region Id

		[Identity]
		[Field("Id", ReadOnly = true)]
		private long _id;

		public override long Id
		{
			get { return _id; }
			set { _id = value; }
		}

		#endregion

		#region Value

		[Field("Value")]
		private string _value;

		public override string Value
		{
			get { return _value; }
			set { _value = value; }
		}

		#endregion
	}

	[EntityExtension]
	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public abstract class Value
	{
		#region FirstValue

		[DefaultImp]
		public abstract int FirstValue { get; set; }

		#endregion

		#region SecondValue

		[Field("SecondValue")]
		private int _secondValue;

		public int SecondValue
		{
			get { return _secondValue; }
			set { _secondValue = value; }
		}

		#endregion
	}

	public class DeriveValue : Value
	{
		#region FirstValue

		private int _firstValue;

		public override int FirstValue
		{
			get { return _firstValue; }
			set { _firstValue = value; }
		}

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Properties, VisibleScopes.Public)]
	[Entity("AbstractEntity")]
	public class Entity
	{
		#region Id

		private long _id;

		[Identity]
		[Field("Id", ReadOnly = true)]
		public long Id
		{
			get { return _id; }
			set { _id = value; }
		}

		#endregion

		#region Value

		private string _myVar;

		public string Value
		{
			get { return _myVar; }
			set { _myVar = value; }
		}

		#endregion
	}

	[EntityExtension]
	public abstract class AbstractDerivedEntity : Entity
	{
	}

	[Entity("AbstractEntity2")]
	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public class AbstractFieldTypeEntity
	{
		#region Id

		[Identity]
		[Field("Id", ReadOnly = true)]
		private long _id;

		public long Id
		{
			get { return _id; }
			set { _id = value; }
		}

		#endregion

		#region Value

		private Value _value;

		[InnerSchema]
		public Value Value
		{
			get { return _value; }
			set { _value = value; }
		}

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	[EntityExtension]
	[Entity("InterfaceEntity")]
	public interface IInterfaceEntity
	{
		#region Id

		[Identity]
		[Field("Id", ReadOnly = true)]
		[DefaultImp]
		long Id { get; set; }

		#endregion

		#region Value

		[DefaultImp]
		string Value { get; set; }

		#endregion
	}

	[TestClass]
	public class AbstractTest
	{
		[TestMethod]
		public void AbstractEntity()
		{
			nTest<AbstractEntity>();
		}

		[TestMethod]
		public void DerivedAbstractEntity()
		{
			nTest<DerivedAbstractEntity>();
		}

		[TestMethod]
		public void DerivedEntity()
		{
			nTest<DerivedEntity>();
		}

		[TestMethod]
		public void InterfaceEntity()
		{
			nInterfaceTest<IInterfaceEntity>();
		}

		[TestMethod]
		public void AbstractDerivedEntity()
		{
			using (Database db = Config.CreateDatabase())
			{
				AbstractDerivedEntity entity = Config.Create<AbstractDerivedEntity>();
				entity.Value = "John Smith";
				db.Create(entity);
				Console.WriteLine("Entity id: {0}", entity.Id);

				db.Delete(entity);
			}
		}

		[TestMethod]
		public void AbstractFieldTypeEntity()
		{
			using (Database db = Config.CreateDatabase())
			{
				AbstractFieldTypeEntity entity = new AbstractFieldTypeEntity();
				entity.Value = new DeriveValue();
				entity.Value.FirstValue = 10;
				entity.Value.SecondValue = 11;
				db.Create(entity);
				Console.WriteLine("Entity id: {0}", entity.Id);

				db.ClearCache();
				entity = db.Read<AbstractFieldTypeEntity>(entity.Id);
				Console.WriteLine("Entity value: {0}-{1}", entity.Value.FirstValue, entity.Value.SecondValue);

				db.Delete(entity);
			}
		}

		private static void nTest<T>()
			where T : AbstractEntity
		{
			using (Database db = Config.CreateDatabase())
			{
				T entity = Config.Create<T>();
				entity.Value = "John Smith";
				db.Create(entity);
				Console.WriteLine("Entity id: {0}", entity.Id);

				db.Delete(entity);
			}
		}

		private static void nInterfaceTest<T>()
			where T : IInterfaceEntity
		{
			using (Database db = Config.CreateDatabase())
			{
				T entity = Config.Create<T>();
				entity.Value = "John Smith";
				db.Create(entity);
				Console.WriteLine("Entity id: {0}", entity.Id);

				db.Delete(entity);
			}
		}
	}
}