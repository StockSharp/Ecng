namespace Ecng.Test.Data
{
	#region Using Directives

	using Ecng.Data;
	using Ecng.Serialization;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	#endregion

	public abstract class BaseFieldTestEntity : FieldFactoryEntity<string>
	{
		#region Value

		public override abstract string Value { get; set; }

		#endregion
	}

	public abstract class RenameFieldTestEntity : FieldFactoryEntity<string>
	{
		[Field("NewValue")]
		public override abstract string Value
		{
			get;
			set;
		}
	}

	public abstract class MakeReadOnlyFieldTestEntity : FieldFactoryEntity<string>
	{
		[Field("Value", ReadOnly = true)]
		public override abstract string Value
		{
			get;
			set;
		}
	}

	public abstract class BaseFieldTestValue
	{
		#region Value

		public abstract string Value { get; set; }

		#endregion
	}

	public abstract class RenameFieldTestValue : BaseFieldTestValue
	{
		[Field("NewValue")]
		public override abstract string Value
		{
			get;
			set;
		}
	}

	public abstract class MakeReadOnlyFieldTestValue : BaseFieldTestValue
	{
		[Field("Value", ReadOnly = true)]
		public override abstract string Value
		{
			get;
			set;
		}
	}

	public abstract class ComplexRenameFieldTestEntity : FieldFactoryEntity<RenameFieldTestValue>
	{
	}

	public abstract class ComplexMakeReadOnlyFieldTestEntity : FieldFactoryEntity<MakeReadOnlyFieldTestValue>
	{
	}

	[TestClass]
	public class InheritanceFieldTest
	{
		public void RenameField()
		{
			nTest<RenameFieldTestEntity, string>("John Smith");
		}

		public void MakeReadOnlyField()
		{
			nTest<MakeReadOnlyFieldTestEntity, string>("John Smith");
		}

		public void ComplexRenameField()
		{
			nTest<ComplexRenameFieldTestEntity, RenameFieldTestValue>();
		}

		public void ComplexMakeReadOnlyField()
		{
			nTest<ComplexMakeReadOnlyFieldTestEntity, MakeReadOnlyFieldTestValue>();
		}

		private void nTest<T, V>()
			where T : FieldFactoryEntity<V>
			where V : BaseFieldTestValue
		{
			V value = Config.Create<V>();
			value.Value = "John Smith";
			nTest<T, V>(value);
		}

		private void nTest<T, V>(V value)
			where T : FieldFactoryEntity<V>
		{
			using (Database db = Config.CreateDatabase())
			{
				T entity = Config.Create<T>();
				entity.Value = value;
				db.Create(entity);

				db.ClearCache();
				entity = db.Read<T>(entity.Id);
				Assert.AreEqual(value, entity.Value);

				db.Delete(entity);
			}
		}
	}
}