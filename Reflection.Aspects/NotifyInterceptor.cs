namespace Ecng.Reflection.Aspects
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Reflection;

	using Ecng.Collections;

	#endregion

	public class NotifyInterceptor : Interceptor
	{
		#region Private Fields

		private static readonly Dictionary<Type, PropertyChangedEventHandler> _handlers = new Dictionary<Type, PropertyChangedEventHandler>();

		#endregion

		#region Interceptor Members

		protected internal override void AfterCall(InterceptContext context)
		{
			PropertyChangedEventHandler handler = _handlers.SafeAdd(context.ReflectedType, delegate
			{
				FieldInfo field = context.ReflectedType.GetMembers<FieldInfo>(ReflectionHelper.AllInstanceMembers, typeof(PropertyChangedEventHandler))[0];
				return context.Instance.GetValue<object, VoidType, PropertyChangedEventHandler>(field, null);
			});
			handler(context.Instance, new PropertyChangedEventArgs(context.MethodName.MakePropertyName()));
			base.AfterCall(context);
		}

		#endregion
	}
}