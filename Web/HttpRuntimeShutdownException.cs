namespace Ecng.Web
{
	#region Using Directives

	using System;
	using System.Web;

	#endregion

	public sealed class HttpRuntimeShutdownException : ApplicationException
	{
		#region HttpRuntimeShutdownException.ctor()

		public HttpRuntimeShutdownException(string message, ApplicationShutdownReason reason, string stackTrace)
			: base(message)
		{
			Reason = reason;
			_stackTrace = stackTrace;
		}

		#endregion

		#region Reason

		public ApplicationShutdownReason Reason { get; private set; }

		#endregion

		#region Exception Members

		private readonly string _stackTrace;

		public override string StackTrace => _stackTrace;

		#endregion
	}
}