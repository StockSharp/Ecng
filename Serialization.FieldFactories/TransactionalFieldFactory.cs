namespace Ecng.Serialization
{
	#region Using Directives

	using System;

	using Ecng.Common;
	using Ecng.Reflection;
	using Ecng.Transactions;

	#endregion

	[Serializable]
	public class TransactionalFieldFactory<T> : FieldFactory<Transactional<T>, T>
	{
		public TransactionalFieldFactory(Field field, int order)
			: base(field, order)
		{
		}

		#region FieldFactory<Transactional<T>, T> Members

		protected override Transactional<T> OnCreateInstance(ISerializer serializer, T source)
		{
			return new Transactional<T>(source);
		}

		protected override T OnCreateSource(ISerializer serializer, Transactional<T> instance)
		{
			return instance.Value;
		}

		#endregion
	}

	[AttributeUsage(ReflectionHelper.Members | ReflectionHelper.Types)]
	public sealed class TransactionalAttribute : ReflectionFieldFactoryAttribute
	{
		#region ReflectionFieldFactoryAttribute Members

		protected override Type GetFactoryType(Field field)
		{
			return typeof(TransactionalFieldFactory<>).Make(field.Type.GetGenericTypeArg(typeof(Transactional<>), 0));
		}

		#endregion
	}
}