namespace Ecng.Reflection.Emit
{
	using System;

	public abstract class BaseGenerator<T>
		where T : class
	{
		protected BaseGenerator(T builder)
		{
			if (builder == null)
				throw new ArgumentNullException("builder");

			Builder = builder;
			Attributes = new AttributeGeneratorList(Builder);
		}

		public T Builder { get; private set; }
		public AttributeGeneratorList Attributes { get; private set; }
	}
}