namespace Ecng.Test.Web.UI
{
	using System;
	using System.Collections;
	using System.Web.UI;
	using System.Web.UI.WebControls;

	using Ecng.Data;
	using Ecng.Reflection.Aspects;
	using Ecng.Serialization;
	using Ecng.Web.UI;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public class Root : RootObject<Database>
	{
		#region Root.ctor()

		public Root()
			: base("Root", Config.CreateDatabase())
		{
			for (int i = 0; i < ChildCount; i++)
			{
				var e = Config.Create<TestEntity>();
				e.Id = i + 5;
				e.Name = "John Smith";
				_childs.Add(e);
			}

			Instance = this;

			_childs = new TestEntityList(Database);
		}

		#endregion

		public static Root Instance;

		public const int ChildCount = 10;

		#region Childs

		private readonly TestEntityList _childs;

		[RelationMany(typeof(TestEntityList))]
		public TestEntityList Childs => _childs;

		#endregion

		public override void Initialize()
		{
			//_childs.Database = Database;
		}
	}

	[EntityExtension]
	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public abstract class TestEntity
	{
		#region Id

		[DefaultImp]
		[Identity]
		public abstract long Id { get; set; }

		#endregion

		#region Name

		[DefaultImp]
		public abstract string Name { get; set; }

		#endregion
	}

	public class TestEntityList : RelationManyList<TestEntity>
	{
		public TestEntityList(Database database)
			: base(database)
		{
		}
	}

	/// <summary>
	/// Summary description for ObjectModelDataSourceTest
	/// </summary>
	[TestClass]
	public class ObjectModelDataSourceTest
	{
		[TestMethod]
		public void Select()
		{
			GetView().Select(new DataSourceSelectArguments("Name", 2, 5), delegate//(IEnumerable data)
			{
				//Assert.AreEqual(5, ((IList)data).Count);
			});
		}

		[TestMethod]
		public void SelectSingle()
		{
			DataSourceView view = GetView("Instance.Childs[@Id]", out var source);
			source.PathParameters.Add(new Parameter("Id", TypeCode.Int64, "5"));
			view.Select(new DataSourceSelectArguments(), delegate(IEnumerable data)
			{
				Assert.AreEqual(1, ((IList)data).Count);
				Assert.AreEqual("John Smith", ((TestEntity)((IList)data)[0]).Name);
			});
		}

		[TestMethod]
		public void Insert()
		{
			var values = new Hashtable
			{
				{ "Id", 10L },
				{ "Name", "John Smith" }
			};
			GetView().Insert(values, delegate//(int affectedRecords, Exception ex)
			{
				return true;
			});

			//Assert.AreEqual(Root.ChildCount + 1, GetChilds().Count);
		}

		[TestMethod]
		public void Delete()
		{
			var keys = new Hashtable
			{
				{ "Id", 10L }
			};
			GetView().Delete(keys, null, delegate//(int affectedRecords, Exception ex)
			{
				return true;
			});

			//Assert.AreEqual(Root.ChildCount - 1, GetChilds().Count);
		}

		[TestMethod]
		public void Update()
		{
			var keys = new Hashtable
			{
				{ "Id", 10L }
			};

			var values = new Hashtable
			{
				{ "Name", "Mark Twain" }
			};
			GetView().Update(keys, values, null, delegate//(int affectedRecords, Exception ex)
			{
				return true;
			});

			Assert.AreEqual("Mark Twain", GetChilds()[10L].Name);
		}

		private static DataSourceView GetView()
		{
			return GetView("Instance.Childs", out var source);
		}

		private static DataSourceView GetView(string path, out ObjectModelDataSource source)
		{
			source = new ObjectModelDataSource
			{
				RootType = typeof(Root).AssemblyQualifiedName,
				Path = path
			};
			return ((IDataSource)source).GetView("DefaultView");
		}

		private static TestEntityList GetChilds()
		{
			return Root.Instance.Childs;
		}
	}
}