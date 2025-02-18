namespace Ecng.Net;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using Ecng.ComponentModel;
using Ecng.Serialization;

/// <summary>
/// Represents the configuration for a UDP multicast source address.
/// </summary>
[TypeConverter(typeof(ExpandableObjectConverter))]
public class MulticastSourceAddress : NotifiableObject, IPersistable
{
	private IPAddress _groupAddress;

	/// <summary>
	/// Gets or sets the UDP multicast group address.
	/// </summary>
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

	/// <summary>
	/// Gets or sets the UDP multicast source address.
	/// </summary>
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

	/// <summary>
	/// Gets or sets the local port.
	/// </summary>
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
	/// Gets or sets a value indicating whether the configuration is enabled.
	/// </summary>
	[Display(Name = "Enabled", Description = "Is configuration enabled.", Order = 3)]
	public bool IsEnabled { get; set; }

	/// <summary>
	/// Loads settings from the specified SettingsStorage.
	/// </summary>
	/// <param name="storage">The settings storage to load from.</param>
	public void Load(SettingsStorage storage)
	{
		SourceAddress = storage.GetValue<IPAddress>(nameof(SourceAddress));
		Port = storage.GetValue<int>(nameof(Port));
		GroupAddress = storage.GetValue<IPAddress>(nameof(GroupAddress));
		IsEnabled = storage.GetValue(nameof(IsEnabled), IsEnabled);
	}

	/// <summary>
	/// Saves settings to the specified SettingsStorage.
	/// </summary>
	/// <param name="storage">The settings storage to save to.</param>
	public void Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(SourceAddress), SourceAddress.To<string>())
			.Set(nameof(Port), Port)
			.Set(nameof(GroupAddress), GroupAddress.To<string>())
			.Set(nameof(IsEnabled), IsEnabled);
	}

	/// <summary>
	/// Returns a string that represents the current multicast source address.
	/// </summary>
	/// <returns>A string containing the group address, source address, and port.</returns>
	public override string ToString()
	{
		return GroupAddress + " " + SourceAddress + " " + Port;
	}

	/// <summary>
	/// Returns a hash code for the current multicast source address.
	/// </summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		return GroupAddress?.GetHashCode() ?? 0 ^ Port.GetHashCode() ^ SourceAddress?.GetHashCode() ?? 0;
	}

	/// <summary>
	/// Determines whether the specified object is equal to the current multicast source address.
	/// </summary>
	/// <param name="obj">The object to compare with the current instance.</param>
	/// <returns>true if the specified object is equal to the current instance; otherwise, false.</returns>
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