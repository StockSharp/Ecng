namespace Ecng.Net
{
	using System.ComponentModel;
	using System.Net;

	using Ecng.Common;
	using Ecng.Serialization;

	public class MulticastSourceAddress : IPersistable
	{
		[DisplayName("Group Address")]
		[Description("UDP multicast group address.")]
		public IPAddress GroupAddress { get; set; }

		[DisplayName("Source Address")]
		[Description("UDP multicast source address.")]
		public IPAddress SourceAddress { get; set; }

		[Description("Local port.")]
		public int Port { get; set; }

		public void Load(SettingsStorage storage)
		{
			SourceAddress = storage.GetValue<IPAddress>("SourceAddress");
			Port = storage.GetValue<int>("Port");
			GroupAddress = storage.GetValue<IPAddress>("GroupAddress");
		}

		public void Save(SettingsStorage storage)
		{
			storage.SetValue("SourceAddress", SourceAddress.To<string>());
			storage.SetValue("Port", Port);
			storage.SetValue("GroupAddress", GroupAddress.To<string>());
		}
	}
}