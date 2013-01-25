namespace Ecng.Configuration
{
	#region Using Directives

	using System;
	using System.Configuration;

	using Ecng.Common;
	//using Ecng.Reflection;

	#endregion

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = true)]
	public class ConfigSectionAttribute : FactoryAttribute
	{
		#region ConfigSectionAttribute.ctor()

		public ConfigSectionAttribute(Type sectionType)
			: base(sectionType)
		{
			if (!typeof(ConfigurationSection).IsAssignableFrom(sectionType))
				throw new ArgumentException("sectionType");
		}

		#endregion

		#region SectionType

		public Type SectionType
		{
			get { return FactoryType; }
		}

		#endregion
	}
}