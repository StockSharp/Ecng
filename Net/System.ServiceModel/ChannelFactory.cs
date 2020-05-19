namespace System.ServiceModel
{
	public class ChannelFactory<TChannel>
	{
		public TChannel CreateChannel() => throw new NotImplementedException();
	}
}