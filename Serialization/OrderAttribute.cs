namespace Ecng.Serialization
{
	#region Using Directives

	using System;

	using Ecng.Reflection;

	#endregion

	[AttributeUsage(ReflectionHelper.Members)]
	public class OrderAttribute : Attribute
	{
		#region OrderAttribute.ctor()

		public OrderAttribute(int value)
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException("value");

			this.Value = value;
		}

		#endregion

		public int Value { get; private set; }
	}
}