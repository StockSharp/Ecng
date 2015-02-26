namespace Ecng.Test.Data
{
	#region Using Directives

	using Ecng.Data;
	using Ecng.Data.Providers;
	using Ecng.Data.Sql;
	using Ecng.Serialization;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	#endregion

	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public struct InnerQueryValue
	{
		#region Value

		[Field("Value")]
		private int _value;

		public int Value
		{
			get { return _value; }
			set { _value = value; }
		}

		#endregion
	}

	[Entity("QueryEntity")]
	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.NonPublic)]
	public class QueryTestEntity
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
		private int _value;

		public int Value
		{
			get { return _value; }
			set { _value = value; }
		}

		#endregion

		#region InnerQueryValue

		[InnerSchema]
		[NameOverride("Value", "Value2")]
		private InnerQueryValue _innerQueryValue;

		public InnerQueryValue InnerQueryValue
		{
			get { return _innerQueryValue; }
			set { _innerQueryValue = value; }
		}

		#endregion
	}

	[TestClass]
	public class QueryTest
	{
		[TestMethod]
		public void Count()
		{
			RunQueryType(SqlCommandTypes.Count);
		}

		[TestMethod]
		public void Create()
		{
			RunQueryType(SqlCommandTypes.Create);
		}

		[TestMethod]
		public void DeleteBy()
		{
			RunQueryType(SqlCommandTypes.DeleteBy);
		}

		[TestMethod]
		public void DeleteAll()
		{
			RunQueryType(SqlCommandTypes.DeleteAll);
		}

		[TestMethod]
		public void ReadBy()
		{
			RunQueryType(SqlCommandTypes.ReadBy);
		}

		[TestMethod]
		public void ReadAll()
		{
			RunQueryType(SqlCommandTypes.ReadAll);
		}

		[TestMethod]
		public void Update()
		{
			RunQueryType(SqlCommandTypes.UpdateBy);
		}

		private static void RunQueryType(SqlCommandTypes type)
		{
			var renderers = new SqlRenderer[]
			{
				//new FirebirdRenderer(),
				new JetRenderer(),
				new SqlServerRenderer(),
				//new PostgreSqlRenderer(),
				new MySqlRenderer()
			};

			foreach (var renderer in renderers)
				Query.Create(typeof(QueryTestEntity).GetSchema(), type, null, null).Render(renderer);
		}
	}
}