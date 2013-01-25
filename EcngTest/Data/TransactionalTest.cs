namespace Ecng.Test.Data
{
	#region Using Directives

	using System.Transactions;

	using Ecng.Data;
	using Ecng.Reflection.Aspects;
	using Ecng.Serialization;
	using Ecng.Transactions;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	#endregion

	public abstract class TransactEntity : FieldFactoryEntity<int>
	{
		#region Value

		[Transactional]
		[Wrapper(typeof(Transactional<>))]
		public override abstract int Value { get; set; }

		#endregion
	}

	[Entity("TransactEntity")]
	public abstract class TransactEntity2 : FieldFactoryEntity<int>
	{
		#region Value

		[Transactional]
		[Field("Value")]
		private Transactional<int> _value = new Transactional<int>();

		public override int Value
		{
			get { return _value.Value; }
			set { _value.Value = value; }
		}

		#endregion
	}

	public abstract class TransactNullEntity : FieldFactoryEntity<string>
	{
		#region Value

		[Transactional]
		[Wrapper(typeof(Transactional<>))]
		public override abstract string Value { get; set; }

		#endregion
	}

	[Entity("TransactEntity")]
	public abstract class TransactLazyLoadEntity : FieldFactoryEntity<int>
	{
		#region Value

		[Transactional(Order = 0)]
		[LazyLoad(Order = 1)]
		[Wrapper(typeof(Transactional<>))]
		public override abstract int Value { get; set; }

		#endregion
	}

	[Entity("TransactEntity")]
	public abstract class TransactLazyLoadEntity2 : FieldFactoryEntity<int>
	{
		#region Value

		[Transactional(Order = 0)]
		[LazyLoad(Order = 1)]
		[Field("Value")]
		private Transactional<LazyLoadObject<int>> _value = new Transactional<LazyLoadObject<int>>();

		public override int Value
		{
			get { return _value.Value.Value; }
			set { _value.Value.Value = value; }
		}

		#endregion
	}

	[Entity("TransactEntity")]
	public abstract class LazyLoadTransactEntity : FieldFactoryEntity<int>
	{
		#region Value

		[LazyLoad(Order = 0)]
		[Transactional(Order = 1)]
		[Wrapper(typeof(Transactional<>))]
		public override abstract int Value { get; set; }

		#endregion
	}

	[Entity("TransactEntity")]
	public abstract class LazyLoadTransactEntity2 : FieldFactoryEntity<int>
	{
		#region Value

		[LazyLoad(Order = 0)]
		[Transactional(Order = 1)]
		[Field("Value")]
		private LazyLoadObject<Transactional<int>> _value;// = new LazyLoadObject<Transactional<int>>(null);

		public override int Value
		{
			get { return _value.Value.Value; }
			set { _value.Value.Value = value; }
		}

		#endregion
	}

	public struct TransactValue
	{
		#region Value1

		[Field("Value1")]
		private int _myVar;

		public int Value1
		{
			get { return _myVar; }
			set { _myVar = value; }
		}

		#endregion

		#region Value2

		[Field("Value2")]
		private int _myVar2;

		public int Value2
		{
			get { return _myVar2; }
			set { _myVar2 = value; }
		}

		#endregion
	}

	public abstract class ComplexTransactEntity : FieldFactoryEntity<TransactValue>
	{
		#region Value

		[InnerSchema(Order = 0)]
		[Transactional(Order = 1)]
		[Wrapper(typeof(Transactional<>))]
		public override abstract TransactValue Value { get; set; }

		#endregion
	}

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	[EntityExtension]
	public abstract class AbsTransactValue
	{
		#region Value1

		[DefaultImp]
		public abstract int Value1 { get; set; }

		#endregion

		#region Value2

		[DefaultImp]
		public abstract int Value2 { get; set; }

		#endregion
	}

	[Entity("ComplexTransactEntity")]
	public abstract class ComplexTransactEntity2 : FieldFactoryEntity<AbsTransactValue>
	{
		#region Value

		[InnerSchema(Order = 0)]
		[Transactional(Order = 1)]
		[Wrapper(typeof(Transactional<>))]
		public override abstract AbsTransactValue Value { get; set; }

		#endregion
	}

	[TestClass]
	public class TransactionalTest
	{
		[TestMethod]
		public void AbsSimple()
		{
			nTest<TransactEntity, int>(1);
		}

		[TestMethod]
		public void Simple()
		{
			nTest<TransactEntity2, int>(1);
		}

		[TestMethod]
		public void Null()
		{
			nTest<TransactNullEntity, string>("Transacted Value");
		}

		[TestMethod]
		public void AbsTransactLazyLoad()
		{
			nTest<TransactLazyLoadEntity, int>(1);
		}

		[TestMethod]
		public void TransactLazyLoad()
		{
			nTest<TransactLazyLoadEntity2, int>(1);
		}

		[TestMethod]
		public void AbsLazyLoadTransact()
		{
			nTest<LazyLoadTransactEntity, int>(1);
		}

		[TestMethod]
		public void LazyLoadTransact()
		{
			nTest<LazyLoadTransactEntity2, int>(1);
		}

		[TestMethod]
		public void Complex()
		{
			TransactValue v = new TransactValue();
			v.Value1 = 10;
			v.Value2 = 20;
			nTest<ComplexTransactEntity, TransactValue>(v);
		}

		[TestMethod]
		public void AbsComplex()
		{
			AbsTransactValue v = Config.Create<AbsTransactValue>();
			v.Value1 = 10;
			v.Value2 = 20;
			nTest<ComplexTransactEntity2, AbsTransactValue>(v);
		}

		private static void nTest<T, V>(V value)
			where T : FieldFactoryEntity<V>
		{
			Assert.AreNotEqual(default(V), value);

			using (Database database = Config.CreateDatabase())
			{
				T entity = Config.Create<T>();

				database.Create(entity);

				using (new TransactionScope())
				{
					entity.Value = value;
					database.Update(entity);
				}

				database.ClearCache();
				entity = database.Read<T>(entity.Id);
				Assert.AreEqual(default(V), entity.Value);

				using (TransactionScope scope = new TransactionScope())
				{
					entity.Value = value;
					database.Update(entity);
					scope.Complete();
				}

				database.ClearCache();
				entity = database.Read<T>(entity.Id);
				Assert.AreEqual(value, entity.Value);

				database.Delete(entity);
			}
		}
	}
}