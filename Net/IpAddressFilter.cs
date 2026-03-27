namespace Ecng.Net;

/// <summary>
/// Reusable IP address filter supporting exact addresses and CIDR subnet masks.
/// Thread-safe, immutable after construction.
/// </summary>
public class IpAddressFilter
{
	private readonly HashSet<IPAddress> _exactAddresses;
	private readonly (IPAddress network, int prefixLength)[] _subnets;

	/// <summary>
	/// Creates filter from comma-separated string of IPs and/or CIDR masks.
	/// Example: "192.168.1.1, 10.0.0.0/8, 2001:db8::/32"
	/// </summary>
	public IpAddressFilter(string restrictions)
		: this(restrictions.IsEmpty() ? [] : restrictions.SplitByComma())
	{
	}

	/// <summary>
	/// Creates filter from enumerable of IP/CIDR strings.
	/// </summary>
	public IpAddressFilter(IEnumerable<string> restrictions)
	{
		_exactAddresses = [];
		var subnets = new List<(IPAddress, int)>();

		foreach (var entry in restrictions)
		{
			var trimmed = entry.Trim();
			if (trimmed.IsEmpty())
				continue;

			if (trimmed.Contains('/'))
			{
				var slashIdx = trimmed.IndexOf('/');
				var network = trimmed[..slashIdx].To<IPAddress>();
				var prefix = trimmed[(slashIdx + 1)..].To<int>();
				subnets.Add((network, prefix));
			}
			else
			{
				_exactAddresses.Add(trimmed.To<IPAddress>());
			}
		}

		_subnets = [.. subnets];
	}

	/// <summary>
	/// Returns true if the filter has no restrictions (empty list = allow all).
	/// </summary>
	public bool IsEmpty => _exactAddresses.Count == 0 && _subnets.Length == 0;

	/// <summary>
	/// Check if address is allowed by this filter.
	/// Returns true if address matches any exact IP or falls within any CIDR range.
	/// Loopback addresses (127.0.0.1, ::1) are always treated as matching each other.
	/// </summary>
	public bool IsAllowed(IPAddress address)
	{
		if (address is null)
			throw new ArgumentNullException(nameof(address));

		if (_exactAddresses.Contains(address))
			return true;

		// Loopback equivalence: if checking a loopback address,
		// also check if any other loopback form is in exact list
		if (address.IsLoopback())
		{
			if (_exactAddresses.Any(a => a.IsLoopback()))
				return true;
		}

		foreach (var (network, prefixLength) in _subnets)
		{
			if (address.IsInSubnet($"{network}/{prefixLength}"))
				return true;
		}

		return false;
	}
}

/// <summary>
/// Extension methods for <see cref="IpAddressFilter"/>.
/// </summary>
public static class IpAddressFilterExtensions
{
	/// <summary>
	/// Parse comma-separated IP restrictions string into <see cref="IpAddressFilter"/>.
	/// Returns empty filter if string is null/empty.
	/// </summary>
	public static IpAddressFilter ToIpFilter(this string restrictions)
		=> new(restrictions);
}
