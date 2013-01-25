namespace Ecng.Test.Data
{
	#region Using Directives

	using System;

	using Ecng.ComponentModel;
	using Ecng.Data;
	using Ecng.Serialization;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	#endregion

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public class BaseValidateEntity
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
	}

	public class DataRangeEntity : BaseValidateEntity
	{
		#region Value

		[Ecng.ComponentModel.Range(MinValue = "01/01/1900", MaxValue = "01/01/2000")]
		[Validation]
		private DateTime _value;

		public DateTime Value
		{
			get { return _value; }
			set { _value = value; }
		}

		#endregion
	}

	public class TextLengthRangeEntity : BaseValidateEntity
	{
		#region Value

		[String(1, 10)]
		[Validation]
		private string _value;

		public string Value
		{
			get { return _value; }
			set { _value = value; }
		}

		#endregion
	}

	public class NonNegativeEntity : BaseValidateEntity
	{
		#region Value

		[Ecng.ComponentModel.Range(MinValue = 0)]
		[Validation]
		private int _value;

		public int Value
		{
			get { return _value; }
			set { _value = value; }
		}

		#endregion
	}

	public class NotNullEntity : BaseValidateEntity
	{
		#region Value

		[NotNull]
		[Validation]
		private string _value;

		public string Value
		{
			get { return _value; }
			set { _value = value; }
		}

		#endregion
	}

	public class NotNullableEntity : BaseValidateEntity
	{
		#region Value

		[NotNull]
		[Validation]
		private int? _value;

		public int? Value
		{
			get { return _value; }
			set { _value = value; }
		}

		#endregion
	}

	public class ValueRangeEntity : BaseValidateEntity
	{
		#region Value

		[Ecng.ComponentModel.Range(MinValue = 10L, MaxValue = 20L)]
		[Validation]
		private long _value;

		public long Value
		{
			get { return _value; }
			set { _value = value; }
		}

		#endregion
	}

	public class RegexRangeEntity : BaseValidateEntity
	{
		#region Value

		[String(Regex = @"\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*")]
		[Validation]
		private string _value;

		public string Value
		{
			get { return _value; }
			set { _value = value; }
		}

		#endregion
	}

	[TestClass]
	public class ValidationTest
	{
		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void DataRange()
		{
			using (Database db = Config.CreateDatabase())
			{
				DataRangeEntity ranger = new DataRangeEntity();
				ranger.Value = new DateTime(1800, 10, 20);
				db.Create(ranger);
			}
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void NonNegative()
		{
			using (Database db = Config.CreateDatabase())
			{
				NonNegativeEntity ranger = new NonNegativeEntity();
				ranger.Value = -1;
				db.Create(ranger);
			}
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void NotNull()
		{
			using (Database db = Config.CreateDatabase())
			{
				NotNullEntity ranger = new NotNullEntity();
				db.Create(ranger);
			}
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException), "Value '' of field '_value' of schema 'Ecng.Test.Data.NotNullableEntity' cannot process validating.")]
		public void NotNullable()
		{
			using (Database db = Config.CreateDatabase())
			{
				NotNullableEntity ranger = new NotNullableEntity();
				db.Create(ranger);
			}
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException), "Value '1000' of field '_value' of schema 'Ecng.Test.Data.ValueRangeEntity' cannot process validating.")]
		public void ValueRange()
		{
			using (Database db = Config.CreateDatabase())
			{
				ValueRangeEntity ranger = new ValueRangeEntity();
				ranger.Value = 1000;
				db.Create(ranger);
			}
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException), "Value '1  2 3 4 5 6 7 8 9 0' of field '_value' of schema 'Ecng.Test.Data.TextLengthRangeEntity' cannot process validating.")]
		public void TextLengthRange()
		{
			using (Database db = Config.CreateDatabase())
			{
				TextLengthRangeEntity ranger = new TextLengthRangeEntity();
				ranger.Value = "1  2 3 4 5 6 7 8 9 0";
				db.Create(ranger);
			}
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException), "Value 'wrong email address' of field '_value' of schema 'Ecng.Test.Data.RegexRangeEntity' cannot process validating.")]
		public void RegexRange()
		{
			//try
			//{
			using (Database db = Config.CreateDatabase())
			{
				RegexRangeEntity ranger = new RegexRangeEntity();
				ranger.Value = "wrong email address";
				db.Create(ranger);
			}
			//}
			//catch (ArgumentException ex)
			//{
			//    if (ex.Message != "value")
			//        throw;
			//}
		}
	}
}