namespace Ecng.Net
{
	using System;
	using System.Reflection;
#if NETFRAMEWORK
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.ServiceModel;
	using System.ServiceModel.Channels;
	using System.ServiceModel.Description;
	using System.ServiceModel.Dispatcher;
#endif

	using Ecng.Common;
	using Ecng.Reflection;

	/// <summary>
	/// The attribute for the WCF server that automatically records all errors to <see cref="LoggingHelper.LogError"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class ErrorLoggingAttribute : Attribute
#if NETFRAMEWORK
		, IServiceBehavior
#endif
	{
#if NETFRAMEWORK
		private sealed class ErrorHandler : IErrorHandler
		{
			private readonly Action<Exception> _errorHandler;

			public ErrorHandler(Action<Exception> errorHandler)
			{
				_errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
			}

			void IErrorHandler.ProvideFault(Exception error, MessageVersion version, ref Message fault)
			{
			}

			bool IErrorHandler.HandleError(Exception error)
			{
				_errorHandler(error);
				return true;
			}
		}

		void IServiceBehavior.Validate(ServiceDescription description, ServiceHostBase serviceHostBase)
		{
		}

		void IServiceBehavior.AddBindingParameters(ServiceDescription description, ServiceHostBase serviceHostBase,
												   Collection<ServiceEndpoint> endpoints,
												   BindingParameterCollection parameters)
		{
		}

		void IServiceBehavior.ApplyDispatchBehavior(ServiceDescription description, ServiceHostBase serviceHostBase)
		{
			var errorHandler = new ErrorHandler(_errorHandler);

			foreach (var channelDispatcher in serviceHostBase.ChannelDispatchers.Cast<ChannelDispatcher>())
				channelDispatcher.ErrorHandlers.Add(errorHandler);
		}
#endif

		private readonly Action<Exception> _errorHandler;

		public ErrorLoggingAttribute(Type owner, string method)
		{
			_errorHandler = owner.GetMember<MethodInfo>(method).CreateDelegate<Action<Exception>>();
		}
	}
}