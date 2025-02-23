namespace Ecng.ComponentModel;

using System;

/// <summary>
/// Indicates that the property is associated with an OAuth service, identified by a unique service identifier.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class OAuthAttribute(long serviceId) : Attribute
{
	/// <summary>
	/// Gets the unique identifier of the OAuth service.
	/// </summary>
	public long ServiceId { get; } = serviceId;
}