namespace Ecng.Reflection.Aspects
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Reflection;

	using Ecng.Collections;
	using Ecng.Common;
		//using MethodType = Wintellect.PowerCollections.Pair<System.Reflection.MethodBase, InterceptTypes>;

	#endregion

	public abstract class Interceptor
	{
		#region Private Fields

		private static readonly Dictionary<MethodBase, InterceptTypes> _types = new Dictionary<MethodBase, InterceptTypes>();

		#endregion

		#region Begin

		public void Begin(object instance, IDictionary<string, object> inRefArgs)
		{
			var trace = new StackTrace(1);
			var method = trace.GetFrame(0).GetMethod();

			new Scope<InterceptContext>(new InterceptContext(instance, GetType(method), method, inRefArgs));

			if (Context.CanProcess(InterceptTypes.Begin))
				BeforeCall(Context);
		}

		#endregion

		#region Catch

		public void Catch(Exception ex)
		{
			Context.Exception = ex;

			if (Context.CanProcess(InterceptTypes.Catch))
				Catch(Context);
		}

		#endregion

		#region End

		public void End(object returnValue, IDictionary<string, object> refOutArgs)
		{
			Context.ReturnValue = returnValue;
			Context.RefOutArgs = refOutArgs;

			if (Context.CanProcess(InterceptTypes.End))
				AfterCall(Context);
		}

		#endregion

		#region Finally

		public void Finally()
		{
			if (Context.CanProcess(InterceptTypes.Finally))
				Finally(Context);

			Scope<InterceptContext>.Current.Dispose();
		}

		#endregion

		private static InterceptContext Context => Scope<InterceptContext>.Current.Value;

		private static InterceptTypes GetType(MethodBase method)
		{
			return _types.SafeAdd(method, key =>
			{
				var attr = method.GetAttribute<InterceptorAttribute>();

				if (attr == null && method is MethodInfo)
				{
					var owner = ((MethodInfo)method).GetAccessorOwner();

					if (owner != null)
						attr = owner.GetAttribute<InterceptorAttribute>();
				}

				if (attr == null)
					throw new ArgumentNullException(nameof(key));

				return attr.Type;
			});
		}

		protected internal virtual void BeforeCall(InterceptContext context) { }
		protected internal virtual void AfterCall(InterceptContext context) { }
		protected internal virtual void Catch(InterceptContext context) { }
		protected internal virtual void Finally(InterceptContext context) { }
	}
}