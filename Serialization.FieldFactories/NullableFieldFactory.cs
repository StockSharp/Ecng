namespace Ecng.Serialization
{
	#region Using Directives

	using System;

	using Ecng.Common;

	#endregion

	[Serializable]
	public class NullableFieldFactory<T> : FieldFactory<T?, T>
		where T : struct
	{
		#region NullableFieldFactory.ctor()

		public NullableFieldFactory(Field field, int order)
			: base(field, order)
		{
		}

		#endregion

		#region FieldFactory<T?, T> Members


		protected override T? OnCreateInstance(ISerializer serializer, T source)
		{
			return source;
		}

		protected override T OnCreateSource(ISerializer serializer, T? instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance", "For field {0} in schema {1} value is empty.".Put(Field.Name, Field.Schema.Name));

			return instance.Value;
		}

		#endregion
	}

	public sealed class NullableAttribute : FieldFactoryAttribute
	{
		#region FieldFactoryAttribute Members

		public override FieldFactory CreateFactory(Field field)
		{
			return typeof(NullableFieldFactory<>).Make(field.Type.GetUnderlyingType()).CreateInstance<FieldFactory>(field, Order);
		}

		#endregion
	}
}