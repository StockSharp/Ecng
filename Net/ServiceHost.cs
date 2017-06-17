namespace Ecng.Net
{
	using System;
	using System.ServiceModel;

	public class ServiceHost<T> : ServiceHost
	{
		public ServiceHost()
			: base(typeof(T))
		{
		}

		public ServiceHost(T instance)
			: base(instance)
		{
		}

		public T Instance => (T)SingletonInstance;

		protected override void OnClosed()
		{
			var instance = Instance as IDisposable;
			instance?.Dispose();

			base.OnClosed();
		}
	}
}