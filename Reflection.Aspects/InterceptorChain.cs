namespace Ecng.Reflection.Aspects
{
	using System.Collections.Generic;

	class InterceptorChain : Interceptor
	{
		#region Interceptors

		private readonly List<Interceptor> _interceptors = new List<Interceptor>();

		public IList<Interceptor> Interceptors => _interceptors;

		#endregion

		#region Interceptor Members

		protected internal override void BeforeCall(InterceptContext context)
		{
			foreach (var interceptor in _interceptors)
				interceptor.BeforeCall(context);
		}

		protected internal override void AfterCall(InterceptContext context)
		{
			foreach (var interceptor in _interceptors)
				interceptor.AfterCall(context);
		}

		protected internal override void Catch(InterceptContext context)
		{
			foreach (var interceptor in _interceptors)
				interceptor.Catch(context);
		}

		protected internal override void Finally(InterceptContext context)
		{
			foreach (var interceptor in _interceptors)
				interceptor.Finally(context);
		}

		#endregion
	}
}