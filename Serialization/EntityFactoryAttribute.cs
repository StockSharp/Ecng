namespace Ecng.Serialization
{
	#region Using Directives

	using System;

	using Ecng.Common;
    using Ecng.Reflection;

	#endregion

    [AttributeUsage(ReflectionHelper.Types)]
	public class EntityFactoryAttribute : FactoryAttribute
	{
		public EntityFactoryAttribute(Type factoryType)
			: base(factoryType)
		{
		}
	}
}