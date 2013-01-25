namespace Ecng.Serialization
{
	using System;

	using Ecng.Collections;
	using Ecng.Common;

	public class VoidField : Field
	{
		private static readonly SynchronizedDictionary<Tuple<string, Type>, FieldFactory> _cachedFactories = new SynchronizedDictionary<Tuple<string, Type>, FieldFactory>();

		public VoidField(string name, Type type)
			: base(name, type)
		{
			Factory = _cachedFactories.SafeAdd(new Tuple<string, Type>(name, type),
				key => typeof(PrimitiveFieldFactory<,>)
						.Make(type, type)
						.CreateInstance<FieldFactory>(this, 0)).Clone();
		}
	}

	public class VoidField<T> : VoidField
	{
		public VoidField(string name)
			: base(name, typeof(T))
		{
		}
	}
}