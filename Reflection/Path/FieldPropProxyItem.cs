namespace Ecng.Reflection.Path
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;

	class FieldPropProxyItem : MemberProxyItem
	{
		public FieldPropProxyItem(MemberInfo member)
			: base(Create(member))
		{
		}

		private static FastInvoker Create(MemberInfo member)
		{
			if (member == null)
				throw new ArgumentNullException("member");

			if (member is PropertyInfo)
				return FastInvoker.Create((PropertyInfo)member, true);
			else
				return FastInvoker.Create((FieldInfo)member, true);
		}

		#region MemberProxyItem Members

		public override object Invoke(object instance, IDictionary<string, object> args)
		{
			return Invoker.Member.IsStatic() ? Invoker.StaticGetValue() : Invoker.GetValue(instance);
		}

		#endregion
	}
}