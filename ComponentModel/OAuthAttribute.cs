namespace Ecng.ComponentModel;

using System;

[AttributeUsage(AttributeTargets.Property)]
public class OAuthAttribute(long serviceId) : Attribute
{
	public long ServiceId { get; } = serviceId;
}