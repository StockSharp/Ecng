namespace Ecng.Net
{
	using System;
	using System.Configuration;
	using System.ServiceModel;
	using System.ServiceModel.Activation;

	using Ecng.Common;

	public class WsdlBaseAddressFactory : ServiceHostFactory
	{
		sealed class WsdlBaseAddressHost : ServiceHost
		{
			public WsdlBaseAddressHost(Type serviceType, Uri[] baseAddresses)
				: base(serviceType, GetBaseAddresses(serviceType, baseAddresses))
			{
			}

			private static Uri[] GetBaseAddresses(Type serviceType, Uri[] baseAddresses)
			{
				var address = ConfigurationManager.AppSettings["baseHttpAddress"];

				return !address.IsEmpty() ? new[] { new Uri(new Uri(address), serviceType.Name + ".svc") } : baseAddresses;
			}
		}

		protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
		{
			return new WsdlBaseAddressHost(serviceType, baseAddresses);
		}
	}
}