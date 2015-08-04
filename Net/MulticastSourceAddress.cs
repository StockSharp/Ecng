namespace Ecng.Net
{
	using System.ComponentModel;
	using System.Net;

	public class MulticastSourceAddress
	{
		[DisplayName("Group Address")]
		[Description("UDP multicast group address.")]
		public IPAddress GroupAddress { get; set; }

		[DisplayName("Source Address")]
		[Description("UDP multicast source address.")]
		public IPAddress SourceAddress { get; set; }

		[Description("Local port.")]
		public int Port { get; set; }
	}
}