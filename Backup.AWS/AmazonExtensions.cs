namespace Ecng.Backup.Amazon;

using System;
using System.Collections.Generic;
using System.Linq;

using global::Amazon;

using Ecng.Common;

/// <summary>
/// Extension class for AWS.
/// </summary>
[CLSCompliant(false)]
public static class AmazonExtensions
{
	/// <summary>
	/// Get region by name.
	/// </summary>
	/// <param name="name">Region name.</param>
	/// <returns>Region.</returns>
	public static RegionEndpoint GetEndpoint(string name)
	{
		if (name.IsEmpty())
			throw new ArgumentNullException(nameof(name));

		var region = RegionEndpoint.EnumerableAllRegions.FirstOrDefault(e =>
			e.SystemName.EqualsIgnoreCase(name) ||
			e.SystemName.Remove("-").EqualsIgnoreCase(name) ||
			e.DisplayName.EqualsIgnoreCase(name));

		return region ?? RegionEndpoint.GetBySystemName(name);
	}
}
