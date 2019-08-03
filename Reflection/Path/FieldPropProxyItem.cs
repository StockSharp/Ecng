namespace Ecng.Reflection.Path
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;

	class FieldPropProxyItem : MemberProxyItem
	{
		private FastInvoker _setter;

		public FieldPropProxyItem(MemberInfo member)
			: base(Create(member, true))
		{
		}

		private static FastInvoker Create(MemberInfo member, bool isGetter)
		{
			if (member == null)
				throw new ArgumentNullException(nameof(member));

			if (member is PropertyInfo prop)
				return FastInvoker.Create(prop, isGetter);
			else
				return FastInvoker.Create((FieldInfo)member, isGetter);
		}

		public override object Invoke(object instance, IDictionary<string, object> args)
		{
			return Invoker.Member.IsStatic() ? Invoker.StaticGetValue() : Invoker.GetValue(instance);
		}

		public override void SetValue(object instance, object value)
		{
			if (_setter == null)
				_setter = Create(Invoker.Member, false);
			
			_setter.SetValue(instance, value);
		}
	}
}