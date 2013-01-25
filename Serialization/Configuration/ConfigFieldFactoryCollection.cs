namespace Ecng.Serialization.Configuration
{
	using System.Configuration;

	public class ConfigFieldFactoryCollection : ConfigurationElementCollection
	{
		public void Add(ConfigFieldFactory elem)
		{
			BaseAdd(elem);
		}

		protected override void BaseAdd(ConfigurationElement element)
		{
			BaseAdd(element, false);
		}

		public override ConfigurationElementCollectionType CollectionType
		{
			get
			{
				return ConfigurationElementCollectionType.AddRemoveClearMap;
			}
		}

		protected override ConfigurationElement CreateNewElement()
		{
			return new ConfigFieldFactory();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			var factory = (ConfigFieldFactory)element;
			return factory.EntityType + "-" + factory.FieldName;
		}

		public ConfigFieldFactory this[int index]
		{
			get
			{
				return (ConfigFieldFactory)BaseGet(index);
			}
			set
			{
				if (BaseGet(index) != null)
				{
					BaseRemoveAt(index);
				}

				BaseAdd(index, value);
			}
		}

		public void Remove(ConfigFieldFactory factory)
		{
			if (BaseIndexOf(factory) >= 0)
				BaseRemove(factory.EntityType + "-" + factory.FieldName);
		}

		public void RemoveAt(int index)
		{
			BaseRemoveAt(index);
		}

		public void Remove(string name)
		{
			BaseRemove(name);
		}

		public void Clear()
		{
			BaseClear();
		}
	}
}