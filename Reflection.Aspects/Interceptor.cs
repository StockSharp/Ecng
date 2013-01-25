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

			if (Interceptor.Context.CanProcess(InterceptTypes.Begin))
				BeforeCall(Interceptor.Context);
		}

		#endregion

		#region Catch

		public void Catch(Exception ex)
		{
			Interceptor.Context.Exception = ex;

			if (Interceptor.Context.CanProcess(InterceptTypes.Catch))
				Catch(Interceptor.Context);
		}

		#endregion

		#region End

		public void End(object returnValue, IDictionary<string, object> refOutArgs)
		{
			Interceptor.Context.ReturnValue = returnValue;
			Interceptor.Context.RefOutArgs = refOutArgs;

			if (Interceptor.Context.CanProcess(InterceptTypes.End))
				AfterCall(Interceptor.Context);
		}

		#endregion

		#region Finally

		public void Finally()
		{
			if (Interceptor.Context.CanProcess(InterceptTypes.Finally))
				Finally(Interceptor.Context);

			Scope<InterceptContext>.Current.Dispose();
		}

		#endregion

		private static InterceptContext Context
		{
			get
			{
				return Scope<InterceptContext>.Current.Value;
			}
		}

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
					throw new ArgumentNullException("key");

				return attr.Type;
			});
		}

		protected internal virtual void BeforeCall(InterceptContext context) { }
		protected internal virtual void AfterCall(InterceptContext context) { }
		protected internal virtual void Catch(InterceptContext context) { }
		protected internal virtual void Finally(InterceptContext context) { }
	}
}