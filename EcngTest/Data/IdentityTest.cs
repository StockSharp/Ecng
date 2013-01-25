namespace Ecng.Test.Data
{
	#region Using Directives

	using System;
	using System.Configuration;

	using Ecng.Common;
	using Ecng.Serialization;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	#endregion

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public class IdEntity
	{
		#region Id

		[Identity]
		[Field("Id")]
		private long _id;

		public long Id
		{
			get { return _id; }
			set { _id = value; }
		}

		#endregion

		#region Value

		[Field("Value")]
		private string _value;

		public string Value
		{
			get { return _value; }
			set { _value = value; }
		}

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public class ReadOnlyIdEntity
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

		[Field("Value")]
		private string _value;

		public string Value
		{
			get { return _value; }
			set { _value = value; }
		}

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public struct Id
	{
		#region Id.ctor()

		public Id(int firstValue, string secondValue)
		{
			_firstValue = firstValue;
			_secondValue = secondValue;
		}

		#endregion

		#region FirstValue

		[Field("FirstValue")]
		private int _firstValue;

		public int FirstValue
		{
			get { return _firstValue; }
			set { _firstValue = value; }
		}

		#endregion

		#region SecondValue

		[Field("SecondValue")]
		private string _secondValue;

		public string SecondValue
		{
			get { return _secondValue; }
			set { _secondValue = value; }
		}

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public class ComplexIdEntity
	{
		#region Id

		[Identity]
		[InnerSchema]
		[Field("Id")]
		private Id _id;

		public Id Id
		{
			get { return _id; }
			set { _id = value; }
		}

		#endregion

		#region Value

		[Field("Value")]
		private string _value;

		public string Value
		{
			get { return _value; }
			set { _value = value; }
		}

		#endregion
	}

	/*
	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public class ReadOnlyComplexIdEntity
	{
		#region Id

		private Id _id;

		[Identity(ReadOnly = true)]
		[InnerSchema]
		public Id Id
		{
			get { return _id; }
			set { _id = value; }
		}

		#endregion

		#region Value

		private string _value;

		public string Value
		{
			get { return _value; }
			set { _value = value; }
		}

		#endregion
	}
	*/

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public class ValidationId
	{
		#region ValidationId.ctor()

		public ValidationId()
		{
		}

		public ValidationId(int firstValue, int secondValue)
		{
			_firstValue = firstValue;
			_secondValue = secondValue;
		}

		#endregion

		#region FirstValue

		[Field("FirstValue")]
		private int _firstValue;

		public int FirstValue
		{
			get { return _firstValue; }
			set { _firstValue = value; }
		}

		#endregion

		#region SecondValue

		[Field("SecondValue")]
		private int _secondValue;

		[IntegerValidator(MinValue = 10000, MaxValue = int.MaxValue)]
		public int SecondValue
		{
			get { return _secondValue; }
			set { _secondValue = value; }
		}

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	[Entity("ComplexIdEntity")]
	public class ValidationIdEntity
	{
		#region Id

		[Identity]
		[InnerSchema]
		[Field("Id")]
		private ValidationId _id;
		
		public ValidationId Id
		{
			get { return _id; }
			set { _id = value; }
		}

		#endregion

		#region Value

		[Field("Value")]
		private string _value;

		public string Value
		{
			get { return _value; }
			set { _value = value; }
		}

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public class GuidIdEntity
	{
		#region Id

		[Identity]
		[Field("Id")]
		private Guid _id;

		public Guid Id
		{
			get { return _id; }
			set { _id = value; }
		}

		#endregion
	}

	[TestClass]
	public class IdentityTest
	{
		[TestMethod]
		public void SimpleIdentity()
		{
			using (var db = Config.CreateDatabase())
			{
				var entity = new IdEntity { Id = DateTime.UtcNow.Ticks };
				db.Create(entity);
				db.Update(entity);

				db.ClearCache();
				entity = db.Read<IdEntity>(entity.Id);
				Console.WriteLine("Entity id: {0}", entity.Id);

				db.Delete(entity);
			}
		}

		[TestMethod]
		public void ReadOnlyIdentity()
		{
			using (var db = Config.CreateDatabase())
			{
				var entity = new ReadOnlyIdEntity { Value = "John Smith" };
				db.Create(entity);
				Console.WriteLine("Entity id: {0}", entity.Id);

				db.Delete(entity);
			}
		}

		[TestMethod]
		public void GuidIdentity()
		{
			using (var db = Config.CreateDatabase())
			{
				var entity = new GuidIdEntity { Id = Guid.NewGuid() };
				db.Create(entity);
				Console.WriteLine("Entity id: {0}", entity.Id);

				db.Delete(entity);
			}
		}

		[TestMethod]
		public void ComplexIdentity()
		{
			using (var db = Config.CreateDatabase())
			{
				db.DeleteAll(typeof(ComplexIdEntity).GetSchema());

				var entity = new ComplexIdEntity { Id = new Id(1, "1") };
				db.Create(entity);

				//entity.Id = new Id(entity.Id.FirstValue, "1000");
				entity.Value = "John Smith";
				db.Update(entity);

				db.ClearCache();
				entity = db.Read<ComplexIdEntity>(entity.Id);
				Console.WriteLine("Entity id: {0}-{1}", entity.Id.FirstValue, entity.Id.SecondValue);

				db.Delete(entity);
			}
		}

		/*
		[TestMethod]
		public void ReadOnlyComplexIdentity()
		{
			using (Database db = Config.CreateDatabase())
			{
				ReadOnlyComplexIdEntity entity = new ReadOnlyComplexIdEntity();
				entity.Value = "John Smith";
				db.Create(entity);
				Console.WriteLine("Entity id: {0}-{1}", entity.Id.FirstValue, entity.Id.SecondValue);

				db.Delete(entity);
			}
		}
		*/

		[TestMethod]
		public void ComplexIdentityWithFactory()
		{
			using (var db = Config.CreateDatabase())
			{
				var entity = new ValidationIdEntity { Id = new ValidationId(1, ((int)DateTime.UtcNow.Ticks).Abs()) };
				db.Create(entity);
				db.Update(entity);

				db.ClearCache();
				entity = db.Read<ValidationIdEntity>(entity.Id);
				Console.WriteLine("Entity id: {0}-{1}", entity.Id.FirstValue, entity.Id.SecondValue);

				db.Delete(entity);
			}
		}
	}
}