namespace Ecng.Net
{
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Net;

	using Ecng.Common;
	using Ecng.Serialization;

	[TypeConverter(typeof(ExpandableObjectConverter))]
	public class MulticastSourceAddress : IPersistable
	{
		[Display(Name = "Group Address", Description = "UDP multicast group address.", Order = 0)]
		[IpAddress(AsString = true)]
		public IPAddress GroupAddress { get; set; }

		[Display(Name = "Source Address", Description = "UDP multicast source address.", Order = 1)]
		[IpAddress(AsString = true)]
		public IPAddress SourceAddress { get; set; }

		[Display(Name = "Port", Description = "Local port.", Order = 2)]
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