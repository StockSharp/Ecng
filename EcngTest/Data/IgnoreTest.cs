namespace Ecng.Test.Data
{
	using Ecng.Reflection.Aspects;
	using Ecng.Serialization;
	using IgnoreAttribute = Ecng.Serialization.IgnoreAttribute;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public class IgnoreEntity
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

		[IgnoreAttribute]
		private int? _value;

		public int? Value
		{
			get { return _value; }
			set { _value = value; }
		}

		#endregion

		#region RealValue

		[Field("Value")]
		private int _realValue;

		public int RealValue
		{
			get { return _realValue; }
			set { _realValue = value; }
		}

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	[EntityExtension]
	[Entity("IgnoreEntity")]
	public abstract class AbsIgnoreEntity
	{
		#region Id

		[Identity]
		[Field("Id", ReadOnly = true)]
		[DefaultImp]
		public abstract long Id { get; set; }

		#endregion

		#region Value

		[Ignore]
		[DefaultImp]
		public abstract int? Value { get; set; }

		#endregion

		#region RealValue

		[Field("Value")]
		[DefaultImp]
		public abstract int RealValue { get; set; }

		#endregion
	}

	[EntityExtension]
	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public abstract class IgnoreValue
	{
		#region Value

		[DefaultImp]
		[Nullable]
		public abstract int? Value { get; set; }

		#endregion

		#region RealValue

		[DefaultImp]
		[Field("Value")]
		public abstract int RealValue { get; set; }

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	[EntityExtension]
	public abstract class AbsComplexIgnoreEntity
	{
		#region Id

		[Identity]
		[Field("Id", ReadOnly = true)]
		[DefaultImp]
		public abstract long Id { get; set; }

		#endregion

		#region Value

		[Ignore(FieldName = "Value")]
		[DefaultImp]
		public abstract IgnoreValue Value { get; set; }

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	[EntityExtension]
	[Entity("IgnoreEntity")]
	public abstract class BaseIgnoreEntity
	{
		#region Id

		[Identity]
		[Field("Id", ReadOnly = true)]
		[DefaultImp]
		public abstract long Id { get; set; }

		#endregion

		#region Value

		[DefaultImp]
		[Nullable]
		public abstract int? Value { get; set; }

		#endregion

		#region RealValue

		[Field("Value")]
		[DefaultImp]
		public abstract int RealValue { get; set; }

		#endregion
	}

	[Ignore(FieldName = "Value")]
	public abstract class DerivedIgnoreEntity : BaseIgnoreEntity
	{
	}

	public abstract class DerivedIgnoreEntity2 : BaseIgnoreEntity
	{
		[Ignore]
		[DefaultImp]
		public override abstract int? Value { get; set; }
	}

	[TestClass]
	public class IgnoreTest
	{
		[TestMethod]
		public void Simple()
		{
			using (var db = Config.CreateDatabase())
			{
				var entity = new IgnoreEntity { Value = 10, RealValue = 5 };
				db.Create(entity);

				db.ClearCache();
				entity = db.Read<IgnoreEntity>(entity.Id);

				Assert.IsNull(entity.Value);
				Assert.AreEqual(5, entity.RealValue);

				db.Delete(entity);
			}
		}

		[TestMethod]
		public void AbstractSimple()
		{
			using (var db = Config.CreateDatabase())
			{
				var entity = Config.Create<AbsIgnoreEntity>();
				entity.Value = 10;
				entity.RealValue = 5;

				db.Create(entity);

				db.ClearCache();
				entity = db.Read<AbsIgnoreEntity>(entity.Id);

				Assert.IsNull(entity.Value);
				Assert.AreEqual(5, entity.RealValue);

				db.Delete(entity);
			}
		}

		[TestMethod]
		public void AbstractComplex()
		{
			using (var db = Config.CreateDatabase())
			{
				var entity = Config.Create<AbsComplexIgnoreEntity>();
				entity.Value = Config.Create<IgnoreValue>();
				entity.Value.Value = 10;

				db.Create(entity);

				db.ClearCache();
				entity = db.Read<AbsComplexIgnoreEntity>(entity.Id);

				Assert.IsNull(entity.Value.Value);

				db.Delete(entity);
			}
		}

		[TestMethod]
		public void DerivedType()
		{
			using (var db = Config.CreateDatabase())
			{
				var entity = Config.Create<DerivedIgnoreEntity>();
				entity.Value = 10;

				db.Create(entity);

				db.ClearCache();
				entity = db.Read<DerivedIgnoreEntity>(entity.Id);

				Assert.IsNull(entity.Value);

				db.Delete(entity);
			}
		}

		[TestMethod]
		public void DerivedField()
		{
			using (var db = Config.CreateDatabase())
			{
				var entity = Config.Create<DerivedIgnoreEntity2>();
				entity.Value = 10;

				db.Create(entity);

				db.ClearCache();
				entity = db.Read<DerivedIgnoreEntity2>(entity.Id);

				Assert.IsNull(entity.Value);

				db.Delete(entity);
			}
		}
	}
}