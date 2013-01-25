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

		public T Instance
		{
			get { return (T)SingletonInstance; }
		}

		protected override void OnClosed()
		{
			if (Instance is IDisposable)
				((IDisposable)Instance).Dispose();

			base.OnClosed();
		}
	}
}