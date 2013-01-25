namespace Ecng.Test
{
	#region Using Directives

	using Ecng.Data;
	using Ecng.Reflection;
	using Ecng.Reflection.Aspects;
	using Ecng.Serialization;

	#endregion

	static class Config
	{
		#region ConnectionString

		private const string _connectionString = @"Data Source=cbdb2;Initial Catalog=EcngTestDB;Integrated Security=True;User ID=ecngtest;Password=ecngtest;";

		public static string ConnectionString
		{
			get { return _connectionString; }
		}

		#endregion

		public static Database CreateDatabase()
		{
			return new Database("Customer Database", Config.ConnectionString);
		}

		public static T Create<T>()
		{
			CreateProxy<T>();
			return GetSchema<T>().GetFactory<T>().CreateEntity(null, new SerializationItemCollection());
		}

		public static Schema GetSchema<T>()
		{
			return SchemaManager.GetSchema<T>();
		}

		public static void CreateProxy<T>()
		{
			if (typeof(T).IsAbstract && !ReflectionHelper.ProxyTypes.ContainsKey(typeof(T)))
				ReflectionHelper.ProxyTypes.Add(typeof(T), MetaExtension.Create(typeof(T)));
		}
	}
}