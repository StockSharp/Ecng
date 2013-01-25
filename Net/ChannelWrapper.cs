namespace Ecng.Net
{
	using System;
	using System.ServiceModel;

	using Ecng.Common;

	public class ChannelWrapper<TChannel> : Wrapper<TChannel>
		where TChannel : class//, IClientChannel
	{
		public ChannelWrapper(TChannel channel)
		{
			base.Value = channel;
		}

		protected override void DisposeManaged()
		{
			var channel = (IClientChannel)base.Value;
			try
			{
				channel.Close();
			}
			catch (CommunicationException)
			{
				channel.Abort();
			}
			catch (TimeoutException)
			{
				channel.Abort();
			}
			catch// (Exception)
			{
				channel.Abort();
				throw;
			}

			base.DisposeManaged();
		}

		public override Wrapper<TChannel> Clone()
		{
			return new ChannelWrapper<TChannel>(base.Value);
		}
	}
}