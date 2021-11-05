namespace Ecng.Serialization
{
	using System.Reflection;

	using Ecng.Reflection;

	public class FastInvokerFieldAccessor<TEntity, TField> : FieldAccessor<TEntity>
	{
		#region Private Fields

		private readonly FastInvoker<TEntity, VoidType, TField> _getInvoker;
		private readonly FastInvoker<TEntity, TField, VoidType> _setInvoker;

		#endregion

		#region FieldAccessor.ctor()

		public FastInvokerFieldAccessor(Field field)
			: base(field)
		{
			_getInvoker = CreateGetInvoker(field);
			_setInvoker = CreateSetInvoker(field);
		}

		#endregion

		public override object GetValue(TEntity entity)
		{
			return _getInvoker.GetValue(entity);
		}

		public override TEntity SetValue(TEntity entity, object value)
		{
			return _setInvoker.SetValue(entity, (TField)value);
		}

		private static FastInvoker<TEntity, VoidType, TField> CreateGetInvoker(Field field)
		{
			if (field.Member is FieldInfo)
				return FastInvoker<TEntity, VoidType, TField>.Create((FieldInfo)field.Member, true);
			else
				return FastInvoker<TEntity, VoidType, TField>.Create((PropertyInfo)field.Member, true);
		}

		private static FastInvoker<TEntity, TField, VoidType> CreateSetInvoker(Field field)
		{
			if (field.Member is FieldInfo)
				return FastInvoker<TEntity, TField, VoidType>.Create((FieldInfo)field.Member, false);
			else
				return FastInvoker<TEntity, TField, VoidType>.Create((PropertyInfo)field.Member, false);
		}
	}
}