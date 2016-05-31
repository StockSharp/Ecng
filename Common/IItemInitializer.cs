namespace Ecng.Common
{
	using System;

	public interface IItemInitializer
	{
		object Create();
	}

	public class ItemInitializerAttribute : Attribute
	{
		public Type ItemInitializerTypeator { get; }

		public ItemInitializerAttribute(Type itemInitializerType)
		{
			var valueSourceInterface = itemInitializerType.GetInterface(typeof(IItemInitializer).FullName);

			if (valueSourceInterface == null)
				throw new ArgumentException($"Type {itemInitializerType} must implement the {nameof(IItemInitializer)} interface.", nameof(itemInitializerType));

			ItemInitializerTypeator = itemInitializerType;
		}
	}
}