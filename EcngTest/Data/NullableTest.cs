namespace Ecng.Test.Data
{
	#region Using Directives

	using Ecng.Data;
	using Ecng.Reflection.Aspects;
	using Ecng.Serialization;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	#endregion

	public abstract class NullableEntity : FieldFactoryEntity<int?>
	{
		#region Value

		[Nullable]
		[DefaultImp]
		public override abstract int? Value { get; set; }

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public struct NullableValue
	{
		#region Value

		private int _myVar;

		public int Value
		{
			get { return _myVar; }
			set { _myVar = value; }
		}

		#endregion
	}

	[Entity("NullableEntity")]
	public abstract class NullableValueEntity : FieldFactoryEntity<NullableValue?>
	{
		#region Value

		[Nullable(Order = 0)]
		[InnerSchema(Order = 1)]
		[DefaultImp]
		public override abstract NullableValue? Value { get; set; }

		#endregion
	}

	[TestClass]
	public class NullableTest
	{
		[TestMethod]
		public void Simple()
		{
			using (Database db = Config.CreateDatabase())
			{
				var entity = Config.Create<NullableEntity>();
				db.Create(entity);

				db.ClearCache();
				entity = db.Read<NullableEntity>(entity.Id);
				Assert.AreEqual(null, entity.Value);

				entity.Value = 10;
				db.Update(entity);

				db.ClearCache();
				entity = db.Read<NullableEntity>(entity.Id);
				Assert.AreEqual(10, entity.Value);

				db.Delete(entity);
			}
		}

		[TestMethod]
		public void Complex()
		{
			using (Database db = Config.CreateDatabase())
			{
				var entity = Config.Create<NullableValueEntity>();
				db.Create(entity);

				db.ClearCache();
				entity = db.Read<NullableValueEntity>(entity.Id);
				Assert.AreEqual(null, entity.Value);

				entity.Value = new NullableValue();
				db.Update(entity);

				db.ClearCache();
				entity = db.Read<NullableValueEntity>(entity.Id);
				Assert.AreEqual(0, entity.Value.Value);

				db.Delete(entity);
			}
		}
	}
}