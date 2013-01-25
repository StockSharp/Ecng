namespace Ecng.Serialization.Configuration
{
	using System.Configuration;

	public class ConfigFieldFactory : ConfigurationElement
	{
		[ConfigurationProperty("entityType", IsRequired = true)]
		public string EntityType
		{
			get { return (string)this["entityType"]; }
			set { this["entityType"] = value; }
		}

		[ConfigurationProperty("fieldName", IsRequired = true)]
		public string FieldName
		{
			get { return (string)this["fieldName"]; }
			set { this["fieldName"] = value; }
		}

		[ConfigurationProperty("fieldFactory", IsRequired = true)]
		public string FieldFactory
		{
			get { return (string)this["fieldFactory"]; }
			set { this["fieldFactory"] = value; }
		}
	}
}