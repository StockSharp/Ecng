namespace Ecng.Serialization
{
	#region Using Directives

	using System;

	using Ecng.Common;

	#endregion

	public class PrimitiveFieldFactory<I, S> : FieldFactory<I, S>
	{
		public PrimitiveFieldFactory(Field field, int order)
			: base(field, order)
		{
		}

		protected internal override I OnCreateInstance(ISerializer serializer, S source)
		{
			return source.To<I>();
		}

		protected internal override S OnCreateSource(ISerializer serializer, I instance)
		{
			return instance.To<S>();
		}

		public override FieldFactory Clone()
		{
			return new PrimitiveFieldFactory<I, S>(Field, Order);
		}
	}

	public class PrimitiveAttribute : ReflectionFieldFactoryAttribute
	{
		#region ReflectionFieldFactoryAttribute Members

		protected override Type GetFactoryType(Field field)
		{
			return typeof(PrimitiveFieldFactory<,>).Make(field.Type, field.Type);
		}

		#endregion
	}
}