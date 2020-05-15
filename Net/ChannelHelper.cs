namespace Ecng.Net
{
	using System;
	using System.IO;
	using System.Net;
	using System.ServiceModel;
#if NETFRAMEWORK
	using System.Linq;
	using System.ServiceModel.Channels;
	using System.ServiceModel.Configuration;

	using Ecng.Common;
	using Ecng.Configuration;
#endif

	public static class ChannelHelper
	{
		public static ChannelFactory<TChannel> Create<TChannel>(string endpointName, Func<ChannelFactory<TChannel>> createFactory)
		{
#if NETFRAMEWORK
			if (createFactory == null)
				throw new ArgumentNullException(nameof(createFactory));

			if (!endpointName.IsEmpty())
			{
				var section = ConfigManager.GetSection<ClientSection>();

				if (section != null)
				{
					var endpoint = section.Endpoints.Cast<ChannelEndpointElement>().FirstOrDefault(e => e.Name.CompareIgnoreCase(endpointName));

					if (endpoint != null)
						return new ChannelFactory<TChannel>(endpointName);
				}
			}

			return createFactory();
#else
			throw new PlatformNotSupportedException();
#endif
		}

		public static void Invoke<TChannel>(this ChannelFactory<TChannel> factory, Action<TChannel> handler)
			where TChannel : class
		{
			if (factory == null)
				throw new ArgumentNullException(nameof(factory));

			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			using (var channel = new ChannelWrapper<TChannel>(factory.CreateChannel()))
			{
				handler(channel.Value);
			}
		}

		public static TResult Invoke<TChannel, TResult>(this ChannelFactory<TChannel> factory, Func<TChannel, TResult> handler)
			where TChannel : class
		{
			if (factory == null)
				throw new ArgumentNullException(nameof(factory));

			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			using (var channel = new ChannelWrapper<TChannel>(factory.CreateChannel()))
				return handler(channel.Value);
		}

		public static IPEndPoint GetClientEndPoint()
		{
#if NETFRAMEWORK
			if (!OperationContext.Current.IncomingMessageProperties.TryGetValue(RemoteEndpointMessageProperty.Name, out var value))
				return null;

			var prop = (RemoteEndpointMessageProperty)value;
			return new IPEndPoint(prop.Address.To<IPAddress>(), prop.Port);
#else
			throw new PlatformNotSupportedException();
#endif
		}

		// http://devdump.wordpress.com/2008/12/07/disposing-return-values/
		public static Stream MakePinned(this Stream stream)
		{
			if (stream == null)
				return null;

			OperationContext.Current.OperationCompleted += (sender, args) => stream.Dispose();

			return stream;
		}
	}
}