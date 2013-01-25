namespace Ecng.Test.Data
{
	#region Using Directives

	using System;
	using System.Data.SqlClient;

	using Ecng.Common;
	using Ecng.Reflection.Aspects;
	using Ecng.Serialization;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	#endregion

	public abstract class ValuesEntity : FieldFactoryEntity<long>
	{
		#region Value

		[Value(1, '1')]
		[Value(2, '2')]
		[Values(typeof(char))]
		[DefaultImp]
		public override abstract long Value { get; set; }

		#endregion
	}

	[Entity("ValuesEntity")]
	public abstract class ValuesWithDefValueEntity : FieldFactoryEntity<long>
	{
		#region Value

		[Value(1, '1')]
		[Value(2, '2')]
		[Values(typeof(char), DefaultValue = 10)]
		[DefaultImp]
		public override abstract long Value { get; set; }

		#endregion
	}

	[Entity("ValuesEntity")]
	public abstract class ValuesSetEmptyEntity : FieldFactoryEntity<long>
	{
		#region Value

		[Values(typeof(long))]
		[DefaultImp]
		public override abstract long Value { get; set; }

		#endregion
	}

	[Entity("ValuesEntity")]
	public abstract class ValuesWithWrongDefValueEntity : FieldFactoryEntity<long>
	{
		#region Value

		[Value(1, '1')]
		[Value(2, '2')]
		[Values(typeof(char), DefaultValue = 2)]
		[DefaultImp]
		public override abstract long Value { get; set; }

		#endregion
	}

	public enum ValuesTestEnum
	{
		Value1,
		Value2,
	}

	[Entity("ValuesEntity")]
	public abstract class EnumValuesEntity : FieldFactoryEntity<ValuesTestEnum>
	{
		#region Value

		[Value(ValuesTestEnum.Value1, '1')]
		[Value(ValuesTestEnum.Value2, '2')]
		[Values(typeof(char))]
		[DefaultImp]
		public override abstract ValuesTestEnum Value { get; set; }

		#endregion
	}

	[Value(ValuesTestEnum2.Value1, '1')]
	[Value(ValuesTestEnum2.Value2, '2')]
	[Values(typeof(char), DefaultValue = ValuesTestEnum2.Default)]
	public enum ValuesTestEnum2
	{
		Value1,
		Value2,
		Default,
	}

	[Entity("ValuesEntity")]
	public abstract class EnumValuesEntity2 : FieldFactoryEntity<ValuesTestEnum2>
	{
		#region Value

		[DefaultImp]
		public override abstract ValuesTestEnum2 Value { get; set; }

		#endregion
	}

	[Values(typeof(char), DefaultValue = ValuesTestEnum3.Default)]
	public enum ValuesTestEnum3
	{
		[Value('1')]Value1,
		[Value('2')]Value2,
		Default,
	}

	[Entity("ValuesEntity")]
	public abstract class EnumValuesEntity3 : FieldFactoryEntity<ValuesTestEnum3>
	{
		#region Value

		[DefaultImp]
		public override abstract ValuesTestEnum3 Value { get; set; }

		#endregion
	}

	[Entity("ValuesEntity")]
	public abstract class OverrideEnumValuesEntity2 : FieldFactoryEntity<ValuesTestEnum2>
	{
		#region Value

		[Value(ValuesTestEnum.Value1, '2')]
		[Value(ValuesTestEnum.Value2, '1')]
		[DefaultImp]
		public override abstract ValuesTestEnum2 Value { get; set; }

		#endregion
	}

	[Value(ValuesTestEnum4.Value1, '1')]
	public enum ValuesTestEnum4
	{
		Value1,
		Value2,
	}

	[Entity("ValuesEntity")]
	public abstract class MergeEnumValuesEntity4 : FieldFactoryEntity<ValuesTestEnum4>
	{
		#region Value

		[Value(ValuesTestEnum4.Value2, '2')]
		[Values(typeof(char))]
		[DefaultImp]
		public override abstract ValuesTestEnum4 Value { get; set; }

		#endregion
	}

	public class DateValueAttribute : ValueAttribute
	{
		#region DateValueAttribute.ctor()

		public DateValueAttribute(string instanceValue, string sourceValue)
			: base(DateTime.Parse(instanceValue), sourceValue)
		{
		}

		#endregion
	}

	[Entity("ValuesEntity")]
	public abstract class DateValuesEntity : FieldFactoryEntity<DateTime>
	{
		#region Value

		[DateValue("1/1/1900", "1900")]
		[DateValue("1/1/2000", "2000")]
		[Values(typeof(string))]
		[DefaultImp]
		public override abstract DateTime Value { get; set; }

		#endregion
	}

	[Serializable]
	public struct FieldValue
	{
		#region FieldValue.ctor()

		public FieldValue(string name)
		{
			_name = name;
		}

		#endregion

		#region Name

		private string _name;

		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}

		#endregion

		public static readonly FieldValue Name1 = new FieldValue("Name1");
		public static readonly FieldValue Name2 = new FieldValue("Name2");
		public static readonly FieldValue Default = new FieldValue("Default");
	}

	public class FieldValueAttribute : ValueAttribute
	{
		#region FieldValueAttribute.ctor()

		public FieldValueAttribute(string sourceValue)
			: base(GetValue(sourceValue), sourceValue)
		{
		}

		#endregion

		private static FieldValue GetValue(string instanceValue)
		{
			if (instanceValue == "Name1")
				return FieldValue.Name1;
			else if (instanceValue == "Name2")
				return FieldValue.Name2;
			else if (instanceValue == "Default")
				return FieldValue.Default;
			else
				throw new ArgumentException();
		}
	}

	public class FieldValuesAttribute : ValuesAttribute
	{
		#region FieldValuesAttribute.ctor()

		public FieldValuesAttribute()
			: base(typeof(string))
		{
			DefaultValue = FieldValue.Default;
		}

		#endregion
	}

	[Entity("ValuesEntity")]
	public abstract class FieldValuesEntity : FieldFactoryEntity<FieldValue>
	{
		#region Value

		[FieldValue("Name1")]
		[FieldValue("Name2")]
		[FieldValues]
		[DefaultImp]
		public override abstract FieldValue Value { get; set; }

		#endregion
	}

	[TestClass]
	public class ValuesTest
	{
		[TestMethod]
		public void Simple()
		{
			nTest<ValuesEntity, long, char>(1, 2, '1', '2');
		}

		[TestMethod]
		//[ExpectedException(typeof(ArgumentException), "instance")]
		public void WithWrongValue()
		{
			try
			{
				nTest<ValuesEntity, long, char>(1, 10, '1', '2');
			}
			catch (ArgumentException ex)
			{
				if (ex.Message != "instance")
					throw;
			}
			
		}

		[TestMethod]
//        [ExpectedException(typeof(ArgumentException), @"Member 'Value' has empty value set.
//Parameter name: field")]
		public void WithEmptyValuesSet()
		{
			try
			{
				nTest<ValuesSetEmptyEntity, long, char>(1, 10, '1', '2');
			}
			catch (ArgumentException ex)
			{
				if (ex.Message != @"Member 'Value' has empty value set.
Parameter name: field")
					throw;
			}
		}

		[TestMethod]
		public void WithDefValue()
		{
			nTest<ValuesWithDefValueEntity, long, char>(10, 2, '0', '2');
		}

		[TestMethod]
//        [ExpectedException(typeof(ArgumentException), @"Member 'Value' has incorrect default value '2'.
//Parameter name: field")]
		public void WithDefWrongValue()
		{
			try
			{
				nTest<ValuesWithWrongDefValueEntity, long, char>(10, 2, '0', '2');
			}
			catch (ArgumentException ex)
			{
				if (ex.Message != @"Member 'Value' has incorrect default value '2'.
Parameter name: field")
					throw;
			}
		}

		[TestMethod]
		public void Enum()
		{
			nTest<EnumValuesEntity, ValuesTestEnum, char>(ValuesTestEnum.Value1, ValuesTestEnum.Value2, '1', '2');
		}

		[TestMethod]
		public void OverrideEnum()
		{
			nTest<OverrideEnumValuesEntity2, ValuesTestEnum2, char>(ValuesTestEnum2.Value1, ValuesTestEnum2.Value2, '2', '1');
		}

		[TestMethod]
		public void EnumWithDefValue()
		{
			nTest<EnumValuesEntity2, ValuesTestEnum2, char>(ValuesTestEnum2.Default, ValuesTestEnum2.Value2, '0', '2');
		}

		[TestMethod]
		public void EnumWithDefValue2()
		{
			nTest<EnumValuesEntity3, ValuesTestEnum3, char>(ValuesTestEnum3.Default, ValuesTestEnum3.Value2, '0', '2');
		}

		[TestMethod]
		public void MergeEnumWithDefValue()
		{
			nTest<MergeEnumValuesEntity4, ValuesTestEnum4, char>(ValuesTestEnum4.Value1, ValuesTestEnum4.Value2, '1', '2');
		}

		[TestMethod]
		public void Date()
		{
			nTest<DateValuesEntity, DateTime, string>(DateTime.Parse("1/1/1900"), DateTime.Parse("1/1/2000"), "1900", "2000");
		}

		[TestMethod]
		public void FieldValues()
		{
			nTest<FieldValuesEntity, FieldValue, string>(FieldValue.Default, FieldValue.Name1, "Some different value", "Name1");
		}

		private static void nTest<T, V, S>(V firstValue, V secondValue, S firstSourceValue, S secondSourceValue)
			where T : FieldFactoryEntity<V>
		{
			Config.CreateProxy<T>();

			using (var db = Config.CreateDatabase())
			{
				var schema = SchemaManager.GetSchema<T>();

				db.DeleteAll(schema);

				long id;

				using (var con = new SqlConnection(Config.ConnectionString))
				{
					con.Open();

					using (var cmd = new SqlCommand(@"	insert into " + schema.Name + @" (Value) values (@Value);
																select scope_identity()", con))
					{
						cmd.Parameters.AddWithValue("@Value", firstSourceValue);
						id = cmd.ExecuteScalar().To<long>();
					}
				}

				var entity = db.Read<T>(id);
				Assert.AreEqual(entity.Value, firstValue);

				entity.Value = secondValue;
				db.Update(entity);

				using (var con = new SqlConnection(Config.ConnectionString))
				{
					con.Open();

					using (var cmd = new SqlCommand(@"select count(*) from " + schema.Name + " where Value = @Value", con))
					{
						cmd.Parameters.AddWithValue("@Value", secondSourceValue);
						Assert.AreEqual(1, cmd.ExecuteScalar());
					}
				}

				db.Delete(entity);
			}
		}
	}
}