namespace Ecng.Serialization
{
	#region Using Directives

	using System;

	using Ecng.Common;
    using Ecng.Reflection;

	#endregion

    [AttributeUsage(ReflectionHelper.Members)]
	public class FieldAccessorAttribute : FactoryAttribute
	{
		#region FieldAccessorAttribute.ctor()

		public FieldAccessorAttribute(Type factoryType)
			: base(factoryType)
		{
		}

		#endregion
	}
}