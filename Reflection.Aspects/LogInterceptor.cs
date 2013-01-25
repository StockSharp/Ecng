namespace Ecng.Reflection.Aspects
{
	#region Using Directives

	using Ecng.Common;
	using Ecng.Collections;

	using Microsoft.Practices.EnterpriseLibrary.Logging;

	#endregion

	public class LogInterceptor : Interceptor
	{
		#region Interceptor Members

		protected internal override void BeforeCall(InterceptContext context)
		{
			var entry = new LogEntry { Message = "Before Call: Type - {0} Method - {1}".Put(context.ReflectedType, context.MethodName) };
			context.InRefArgs.CopyTo(entry.ExtendedProperties);
			Logger.Write(entry);

			base.BeforeCall(context);
		}

		protected internal override void Catch(InterceptContext context)
		{
			var entry = new LogEntry { Message = "Exception Raised: Exception - {0}".Put(context.Exception) };
			Logger.Write(entry);

			base.Catch(context);
		}

		protected internal override void AfterCall(InterceptContext context)
		{
			var entry = new LogEntry { Message = "After Call: Return Value - {0}".Put(context.ReturnValue) };
			context.RefOutArgs.CopyTo(entry.ExtendedProperties);
			Logger.Write(entry);

			base.AfterCall(context);
		}

		#endregion
	}
}