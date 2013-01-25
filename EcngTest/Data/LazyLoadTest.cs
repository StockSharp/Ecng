namespace Ecng.Test.Data
{
	#region Using Directives

	using System.Drawing;

	using Ecng.Data;
	using Ecng.Reflection.Aspects;
	using Ecng.Serialization;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	#endregion

	public abstract class LazyLoadEntity : FieldFactoryEntity<string>
	{
		#region Value

		[LazyLoad]
		[Wrapper(typeof(LazyLoadObject<>))]
		public override abstract string Value { get; set; }

		#endregion
	}

	public abstract class LazyLoadEntity2 : FieldFactoryEntity<string>
	{
		#region Value

		private readonly LazyLoadObject<string> _value;// = new LazyLoadObject<string>();

		[LazyLoad]
		public override string Value
		{
			get { return _value.Value; }
			set { _value.Value = value; }
		}

		#endregion
	}

	public abstract class LazyLoadValue : FieldFactoryEntity<string>
	{
		#region Value

		[LazyLoad]
		[Wrapper(typeof(LazyLoadObject<>))]
		public override abstract string Value { get; set; }

		#endregion
	}

	public abstract class LazyLoadValueEntity : FieldFactoryEntity<LazyLoadValue>
	{
		#region Value

		[LazyLoad]
		[Wrapper(typeof(LazyLoadObject<>))]
		public override abstract LazyLoadValue Value { get; set; }

		#endregion
	}

	public struct InnerLazyLoadValue
	{
		#region Value

		private string _value;

		public string Value
		{
			get { return _value; }
			set { _value = value; }
		}

		#endregion
	}

	public abstract class InnerLazyLoadEntity : FieldFactoryEntity<InnerLazyLoadValue>
	{
		#region Value

		[LazyLoad]
		[Wrapper(typeof(LazyLoadObject<>))]
		public override abstract InnerLazyLoadValue Value { get; set; }

		#endregion
	}

	public abstract class InnerLazyLoadValue2 : FieldFactoryEntity<string>
	{
		#region Value

		[LazyLoad]
		[Wrapper(typeof(LazyLoadObject<>))]
		public override abstract string Value { get; set; }

		#endregion
	}

	public abstract class InnerLazyLoadEntity2 : FieldFactoryEntity<InnerLazyLoadValue2>
	{
		#region Value

		[LazyLoad]
		[Wrapper(typeof(LazyLoadObject<>))]
		public override abstract InnerLazyLoadValue2 Value { get; set; }

		#endregion
	}

	public abstract class InnerLazyLoadEntity3 : FieldFactoryEntity<InnerLazyLoadEntity2>
	{
		#region Value

		[LazyLoad]
		[Wrapper(typeof(LazyLoadObject<>))]
		public override abstract InnerLazyLoadEntity2 Value { get; set; }

		#endregion
	}

	public abstract class InnerLazyLoadEntity4 : FieldFactoryEntity<InnerLazyLoadEntity3>
	{
		#region Value

		[LazyLoad]
		[Wrapper(typeof(LazyLoadObject<>))]
		public override abstract InnerLazyLoadEntity3 Value { get; set; }

		#endregion
	}

	public abstract class PictureLazyLoadEntity : FieldFactoryEntity<Bitmap>
	{
		#region Value

		[Serializer(Order = 0)]
		[LazyLoad(Order = 1)]
		[Wrapper(typeof(LazyLoadObject<>))]
		public override abstract Bitmap Value { get; set; }

		#endregion
	}

	[TestClass]
	public class LazyLoadTest
	{
		[TestMethod]
		public void AbsSimple()
		{
			nTest<LazyLoadEntity, string>("John Smith", "Mark Twain");
		}

		[TestMethod]
		public void Simple()
		{
			nTest<LazyLoadEntity2, string>("John Smith", "Mark Twain");
		}

		[TestMethod]
		public void AbsComplex()
		{
			var value = Config.Create<LazyLoadValue>();
			value.Value = "John Smith";

			var newValue = Config.Create<LazyLoadValue>();
			newValue.Value = "Mark Twain";

			nTest<LazyLoadValueEntity, LazyLoadValue>(value, newValue);
		}

		[TestMethod]
		public void Complex()
		{
			var value = new InnerLazyLoadValue { Value = "John Smith" };
			var newValue = new InnerLazyLoadValue { Value = "Mark Twain" };
			nTest<InnerLazyLoadEntity, InnerLazyLoadValue>(value, newValue);
		}

		[TestMethod]
		public void AbsComplex2()
		{
			var value = Config.Create<InnerLazyLoadValue2>();
			value.Value = "John Smith";

			var newValue = Config.Create<InnerLazyLoadValue2>();
			newValue.Value = "Mark Twain";

			nTest<InnerLazyLoadEntity2, InnerLazyLoadValue2>(value, newValue);
		}

		[TestMethod]
		public void AbsComplex3()
		{
			var value = Config.Create<InnerLazyLoadEntity3>();
			var newValue = Config.Create<InnerLazyLoadEntity3>();

			nTest<InnerLazyLoadEntity4, InnerLazyLoadEntity3>(value, newValue);
		}

		[TestMethod]
		public void Picture()
		{
			nTest<PictureLazyLoadEntity, Bitmap>(Properties.Resources.LazyLoadTestImage, Properties.Resources.LazyLoadTestImage2);
		}

		private static void nTest<T, V>(V value, V newValue)
			where T : FieldFactoryEntity<V>
		{
			using (Database db = Config.CreateDatabase())
			{
				var entity = Config.Create<T>();
				entity.Value = value;
				db.Create(entity);

				db.ClearCache();
				entity = db.Read<T>(entity.Id);
				Assert.AreEqual(value, entity.Value);

				entity.Value = newValue;
				db.Update(entity);

				db.ClearCache();
				entity = db.Read<T>(entity.Id);
				Assert.AreEqual(newValue, entity.Value);

				db.Delete(entity);
			}
		}
	}
}