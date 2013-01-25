namespace Ecng.Test.Data
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Transactions;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	#endregion

	public abstract class PageLoadEntity : FieldFactoryEntity<int>
	{
	}

	public class PageLoadEntityList : RelationManyList<PageLoadEntity>
	{
		#region PageLoadEntityList.ctor()

		public PageLoadEntityList(IStorage storage)
			: base(storage)
		{
		}

		public PageLoadEntityList(IStorage storage, PageLoadRoot root)
			: this(storage)
		{
		}

		#endregion
	}

	public abstract class AbsPageLoadEntityList : RelationManyList<PageLoadEntity>
	{
		protected AbsPageLoadEntityList(IStorage storage)
			: base(storage)
		{
		}
	}

	public abstract class PageLoadRoot
	{
		#region SimpleList

		private PageLoadEntityList _simpleList;

		[RelationMany(typeof(PageLoadEntityList))]
		public PageLoadEntityList SimpleList
		{
			get { return _simpleList; }
		}

		#endregion

		#region AbsList

		private AbsPageLoadEntityList _absList;

		[RelationMany(typeof(AbsPageLoadEntityList))]
		public AbsPageLoadEntityList AbsList
		{
			get { return _absList; }
		}

		#endregion

		#region InterfaceList

		private IList<PageLoadEntity> _interfaceList;

		[RelationMany(typeof(IList<PageLoadEntity>))]
		public IList<PageLoadEntity> InterfaceList
		{
			get { return _interfaceList; }
		}

		#endregion
	}

	[TestClass]
	public class PageLoadTest
	{
		//[TestMethod]
		//public void Offline()
		//{
		//    PageLoadEntityList list = new PageLoadEntityList();
		//    ListMethodsTest(list);
		//}

		[TestMethod]
		public void Online()
		{
			var list = new PageLoadEntityList(Config.CreateDatabase());
			ListMethodsTest(list);
		}

		//[TestMethod]
		//public void TransactedOffline()
		//{
		//    PageLoadEntityList list = new PageLoadEntityList();
		//    TransactedTest(list, false);
		//    TransactedTest(list, true);
		//}

		[TestMethod]
		public void TransactedOnline()
		{
			var list = new PageLoadEntityList(Config.CreateDatabase());
			TransactedTest(list, false);
			TransactedTest(list, true);
		}

		[TestMethod]
		public void SimpleList()
		{
			var entity = Config.Create<PageLoadRoot>();
			ListMethodsTest(entity.SimpleList);
		}

		[TestMethod]
		public void AbsList()
		{
			var entity = Config.Create<PageLoadRoot>();
			ListMethodsTest(entity.AbsList);
		}

		[TestMethod]
		public void InterfaceList()
		{
			var entity = Config.Create<PageLoadRoot>();
			ListMethodsTest(entity.InterfaceList.To<RelationManyList<PageLoadEntity>>());
		}

		private static void TransactedTest<T>(RelationManyList<T> list, bool commit)
		{
			var factory = SchemaManager.GetSchema<T>().GetFactory<T>();
			T entity = factory.CreateEntity(null, null);

			using (var scope = new TransactionScope())
			{
				list.Add(entity);

				if (commit)
					scope.Complete();
			}

			if (commit)
			{
				Assert.AreEqual(1, list.Count);
				Assert.IsTrue(list.Contains(entity));
			}
			else
			{
				Assert.AreEqual(0, list.Count);
				Assert.IsFalse(list.Contains(entity));
			}
		}

		private static void ListMethodsTest<T>(RelationManyList<T> list)
		{
			var entity = Config.Create<T>();

			list.AddRange(new[] { Config.Create<T>(), Config.Create<T>() });
			list.Add(entity);
			Assert.AreEqual(3, list.Count);

			Assert.IsTrue(list.Contains(entity));

			var array = new T[list.Count];
			list.CopyTo(array, 0);
			Assert.AreEqual(list.Count, array.Length);

			Func<T, bool> func = arg => arg.Equals(entity);

			Assert.IsTrue(list.Any(func));
			Assert.AreEqual(1, list.Where(func).Count());
			Assert.AreEqual(entity, list.First(func));
			//Assert.AreEqual(2, list.FindFirstIndex(predicate));
			Assert.AreEqual(entity, list.Last(func));
			//Assert.AreEqual(2, list.FindLastIndex(predicate));
			Assert.AreEqual(2, list.IndexOf(entity));

			list.Insert(2, Config.Create<T>());
			Assert.AreEqual(4, list.GetRange(0, list.Count).Count());

			Assert.IsTrue(list.Remove(entity));

			list.RemoveAt(0);

			Assert.AreEqual(2, list.Count);
			Assert.AreEqual(2, list.ToArray().Length);

			list.Update(entity);

			list.Clear();
			Assert.AreEqual(0, list.Count);
		}
	}
}