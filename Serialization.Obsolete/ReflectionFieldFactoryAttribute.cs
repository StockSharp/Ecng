namespace Ecng.Serialization
{
	#region Using Directives

	using System;
	using System.Linq;

	using Ecng.Common;

	#endregion

	public abstract class ReflectionFieldFactoryAttribute : FieldFactoryAttribute
	{
		#region FieldFactoryAttribute Members

		public override FieldFactory CreateFactory(Field field)
		{
			var factoryType = GetFactoryType(field);

			if (!typeof(FieldFactory).IsAssignableFrom(factoryType))
				throw new InvalidOperationException($"Type {factoryType} is not derived from {typeof(FieldFactory)}.");

			return factoryType.CreateInstanceArgs<FieldFactory>(new object[] { field, Order }.Concat(GetArgs(field)).ToArray());
		}

		#endregion

		protected abstract Type GetFactoryType(Field field);

		protected virtual object[] GetArgs(Field field)
		{
			return Array.Empty<object>();
		}
	}

	public abstract class ReflectionImplFieldFactoryAttribute : ReflectionFieldFactoryAttribute
	{
		private readonly Type _factoryType;

		protected ReflectionImplFieldFactoryAttribute(Type factoryType)
		{
			_factoryType = factoryType ?? throw new ArgumentNullException(nameof(factoryType));
		}

		protected override Type GetFactoryType(Field field)
		{
			return _factoryType;
		}
	}
}