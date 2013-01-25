namespace Ecng.Web
{
	#region Using Directives

	using System.Web;

	using Ecng.Reflection;

	#endregion

	public sealed class HttpRuntimeEx
	{
		#region Private Fields

		private readonly HttpRuntime _runtime;

		#endregion

		#region HttpRuntimeEx.ctor()

		private HttpRuntimeEx(HttpRuntime runtime)
		{
			_runtime = runtime;
		}

		#endregion

		#region Current

		public static HttpRuntimeEx Current
		{
			get
			{
				var runtime = typeof(HttpRuntime).GetValue<VoidType, HttpRuntime>("_theRuntime", null);
				return runtime != null ? new HttpRuntimeEx(runtime) : null;
			}
		}

		#endregion

		#region Error

		private HttpRuntimeShutdownException _error;

		public HttpRuntimeShutdownException Error
		{
			get
			{
				if (_error == null)
				{
					var message = _runtime.GetValue<HttpRuntime, VoidType, string>("_shutDownMessage", null);
					var reason = _runtime.GetValue<HttpRuntime, VoidType, ApplicationShutdownReason>("_shutdownReason", null);
					var stackTrace = _runtime.GetValue<HttpRuntime, VoidType, string>("_shutDownStack", null);
					_error = new HttpRuntimeShutdownException(message, reason, stackTrace);
				}

				return _error;
			}
		}

		#endregion

	}
}