namespace Ecng.Net
{
	using System;
#if NETFRAMEWORK
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
#else
	public class ServiceHost<T>
	{
		public ServiceHost()
		{
		}

		public ServiceHost(T instance)
		{
		}

		public T Instance => default;

		public void Open() => throw new NotSupportedException();
		public void Close() => throw new NotSupportedException();
	}
#endif
}