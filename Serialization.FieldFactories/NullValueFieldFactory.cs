namespace Ecng.Serialization
{
	using Ecng.Common;

	public class NullValueFieldFactory<T> : FieldFactory<T, T>
		where T : class
	{
		public NullValueFieldFactory(Field field, int order, T nullValue)
			: base(field, order)
		{
			NullValue = nullValue;
		}

		public T NullValue { get; private set; }

		protected override T OnCreateInstance(ISerializer serializer, T source)
		{
			return source ?? NullValue;
		}

		protected override T OnCreateSource(ISerializer serializer, T instance)
		{
			return object.ReferenceEquals(instance, NullValue) ? null : instance;
		}
	}

	public abstract class NullValueAttribute : FieldFactoryAttribute
	{
		protected NullValueAttribute(object nullValue)
		{
			NullValue = nullValue;
		}

		public object NullValue { get; private set; }

		public override FieldFactory CreateFactory(Field field)
		{
			return typeof(NullValueFieldFactory<>).Make(field.Type).CreateInstance<FieldFactory>(field, Order, NullValue);
		}
	}
}