namespace Ecng.Reflection.Path
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;

	using Ecng.Common;

	class MethodProxyItem : MemberProxyItem
	{
		#region MethodProxyItem.ctor()

		public MethodProxyItem(MethodInfo method, IEnumerable<Param> parameters)
			: base(FastInvoker.Create(method))
		{
			if (parameters == null)
				throw new ArgumentNullException("parameters");

			Parameters = parameters;
		}

		#endregion

		public IEnumerable<Param> Parameters { get; private set; }

		#region MemberProxyItem Members

		public override object Invoke(object instance, IDictionary<string, object> args)
		{
			var methodArgs = new List<object>();

			foreach (var parameter in Parameters)
			{
				switch (parameter.Type)
				{
					case ParamType.Direct:
						methodArgs.Add(parameter.Value);
						break;
					case ParamType.Reference:
						methodArgs.Add(Convert.ChangeType(args[parameter.Name], parameter.Info.ParameterType, null));
						break;
					default:
						throw new InvalidOperationException();
				}
			}

			if (Invoker.Member.IsStatic())
			{
				if (methodArgs.Count > 1)
					return Invoker.ReturnInvoke(methodArgs.ToArray());
				else if (methodArgs.Count == 1)
					return Invoker.ReturnInvoke(methodArgs[0]);
				else
					return Invoker.ReturnInvoke(ArrayHelper<object>.EmptyArray);				
			}
			else
			{
				if (methodArgs.Count > 1)
					return Invoker.ReturnInvoke(instance, methodArgs.ToArray());
				else if (methodArgs.Count == 1)
					return Invoker.ReturnInvoke(instance, methodArgs[0]);
				else
					return Invoker.ReturnInvoke(instance, ArrayHelper<object>.EmptyArray);	
			}
		}

		#endregion
	}
}