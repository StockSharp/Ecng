namespace Ecng.Serialization.Configuration
{
	using System.Configuration;

	public class SerializationSection : ConfigurationSection
	{
		[ConfigurationProperty("fieldFactories", IsDefaultCollection = false)]
		[ConfigurationCollection(typeof(ConfigFieldFactoryCollection), AddItemName = "add", ClearItemsName = "clear", RemoveItemName = "remove")]
		public ConfigFieldFactoryCollection FieldFactories
		{
			get
			{
				return (ConfigFieldFactoryCollection)base["fieldFactories"];
			}
		}
	}
}