namespace System.ServiceModel
{
	public class ChannelFactory<TService>
	{
		public TResult Invoke<TResult>(Func<TService, TResult> handler)
		{
			return default;
		}
	}
}