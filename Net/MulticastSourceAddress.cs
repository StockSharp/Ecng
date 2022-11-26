namespace Ecng.Net;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using Ecng.ComponentModel;
using Ecng.Serialization;

[TypeConverter(typeof(ExpandableObjectConverter))]
public class MulticastSourceAddress : NotifiableObject, IPersistable
{
	private IPAddress _groupAddress;

	[Display(Name = "Group Address", Description = "UDP multicast group address.", Order = 0)]
	//[IpAddress(AsString = true)]
	public IPAddress GroupAddress
	{
		get => _groupAddress;
		set
		{
			NotifyChanging();
			_groupAddress = value;
			NotifyChanged();
		}
	}

	private IPAddress _sourceAddress;

	[Display(Name = "Source Address", Description = "UDP multicast source address.", Order = 1)]
	//[IpAddress(AsString = true)]
	public IPAddress SourceAddress
	{
		get => _sourceAddress;
		set
		{
			NotifyChanging();
			_sourceAddress = value;
			NotifyChanged();
		}
	}

	private int _port;

	[Display(Name = "Port", Description = "Local port.", Order = 2)]
	public int Port
	{
		get => _port;
		set
		{
			NotifyChanging();
			_port = value;
			NotifyChanged();
		}
	}

	/// <summary>
	/// Is configuration enabled.
	/// </summary>
	[Display(Name = "Enabled", Description = "Is configuration enabled.", Order = 3)]
	public bool IsEnabled { get; set; }

	public void Load(SettingsStorage storage)
	{
		SourceAddress = storage.GetValue<IPAddress>(nameof(SourceAddress));
		Port = storage.GetValue<int>(nameof(Port));
		GroupAddress = storage.GetValue<IPAddress>(nameof(GroupAddress));
		IsEnabled = storage.GetValue(nameof(IsEnabled), IsEnabled);
	}

	public void Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(SourceAddress), SourceAddress.To<string>())
			.Set(nameof(Port), Port)
			.Set(nameof(GroupAddress), GroupAddress.To<string>())
			.Set(nameof(IsEnabled), IsEnabled);
	}

	public override string ToString()
	{
		return GroupAddress + " " + SourceAddress + " " + Port;
	}

	public override int GetHashCode()
	{
		return GroupAddress?.GetHashCode() ?? 0 ^ Port.GetHashCode() ^ SourceAddress?.GetHashCode() ?? 0;
	}

	public override bool Equals(object obj)
	{
		if (obj is not MulticastSourceAddress addr)
			return false;

		if (GroupAddress is null)
		{
			if (addr.GroupAddress != null)
				return false;
		}
		else if (addr.GroupAddress is null)
			return false;
		else if (!GroupAddress.Equals(addr.GroupAddress))
			return false;

		if (Port != addr.Port)
			return false;

		if (SourceAddress is null)
		{
			if (addr.SourceAddress != null)
				return false;
		}
		else if (addr.SourceAddress is null)
			return false;
		else if (!SourceAddress.Equals(addr.SourceAddress))
			return false;

		return true;
	}
}