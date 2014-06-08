namespace Ecng.Reflection.Path
{
	using System;
	using System.Collections.Generic;

	public abstract class MemberProxyItem
	{
		protected MemberProxyItem(FastInvoker invoker)
		{
			if (invoker == null)
				throw new ArgumentNullException("invoker");

			Invoker = invoker;
		}

		public FastInvoker Invoker { get; private set; }

		public abstract object Invoke(object instance, IDictionary<string, object> args);

		public virtual void SetValue(object instance, object value)
		{
			throw new NotSupportedException();
		}
	}
}