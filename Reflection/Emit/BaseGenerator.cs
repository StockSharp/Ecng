namespace Ecng.Reflection.Emit
{
	using System;

	public abstract class BaseGenerator<T>
		where T : class
	{
		protected BaseGenerator(T builder)
		{
			Builder = builder ?? throw new ArgumentNullException(nameof(builder));
			Attributes = new AttributeGeneratorList(Builder);
		}

		public T Builder { get; }
		public AttributeGeneratorList Attributes { get; }
	}
}