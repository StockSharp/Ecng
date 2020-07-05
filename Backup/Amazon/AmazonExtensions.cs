namespace Ecng.Backup.Amazon
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	using global::Amazon;

	using Ecng.Common;

	/// <summary>
	/// Extension class for AWS.
	/// </summary>
	public static class AmazonExtensions
	{
		private static RegionEndpoint[] _endpoints;

		/// <summary>
		/// All regions.
		/// </summary>
		public static IEnumerable<RegionEndpoint> Endpoints
		{
			get
			{
				lock (typeof(AmazonExtensions))
				{
					if (_endpoints == null)
					{
						_endpoints = typeof(RegionEndpoint)
							.GetFields(BindingFlags.Static | BindingFlags.Public)
							.Where(f => f.FieldType == typeof(RegionEndpoint))
							.Select(f => (RegionEndpoint)f.GetValue(null))
							.ToArray();
					}
				}

				return _endpoints;
			}
		}

		/// <summary>
		/// Get region by name.
		/// </summary>
		/// <param name="name">Region name.</param>
		/// <returns>Region.</returns>
		public static RegionEndpoint GetEndpoint(string name)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			var region = Endpoints.FirstOrDefault(e =>
				e.SystemName.CompareIgnoreCase(name) ||
				e.SystemName.Remove("-").CompareIgnoreCase(name) ||
				e.DisplayName.CompareIgnoreCase(name));

			return region ?? RegionEndpoint.GetBySystemName(name);
		}
	}
}