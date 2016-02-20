namespace Ecng.Net
{
	using System.ComponentModel;
	using System.Net;

	using Ecng.Common;
	using Ecng.Serialization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	public class MulticastSourceAddress : IPersistable
	{
		[DisplayName("Group Address")]
		[Description("UDP multicast group address.")]
		[IpAddress(AsString = true)]
		[PropertyOrder(0)]
		public IPAddress GroupAddress { get; set; }

		[DisplayName("Source Address")]
		[Description("UDP multicast source address.")]
		[IpAddress(AsString = true)]
		[PropertyOrder(1)]
		public IPAddress SourceAddress { get; set; }

		[Description("Local port.")]
		[PropertyOrder(2)]
		public int Port { get; set; }

		public void Load(SettingsStorage storage)
		{
			SourceAddress = storage.GetValue<IPAddress>(nameof(SourceAddress));
			Port = storage.GetValue<int>(nameof(Port));
			GroupAddress = storage.GetValue<IPAddress>(nameof(GroupAddress));
		}

		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(SourceAddress), SourceAddress.To<string>());
			storage.SetValue(nameof(Port), Port);
			storage.SetValue(nameof(GroupAddress), GroupAddress.To<string>());
		}

		public override string ToString()
		{
			return GroupAddress + " " + SourceAddress + " " + Port;
		}
	}
}