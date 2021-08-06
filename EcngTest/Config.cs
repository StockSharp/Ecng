namespace Ecng.Test
{
	#region Using Directives

	using Ecng.Serialization;

	#endregion

	static class Config
	{
		public static T Create<T>()
		{
			//CreateProxy<T>();
			return GetSchema<T>().GetFactory<T>().CreateEntity(null, new SerializationItemCollection());
		}

		public static Schema GetSchema<T>()
		{
			return SchemaManager.GetSchema<T>();
		}

		//public static void CreateProxy<T>()
		//{
		//	if (typeof(T).IsAbstract && !ReflectionHelper.ProxyTypes.ContainsKey(typeof(T)))
		//		ReflectionHelper.ProxyTypes.Add(typeof(T), MetaExtension.Create(typeof(T)));
		//}
	}
}