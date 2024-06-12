namespace Ecng.ComponentModel;

using System;

[AttributeUsage(AttributeTargets.Property)]
public class OAuthAttribute : Attribute
{
    public OAuthAttribute(long serviceId)
    {
		ServiceId = serviceId;
	}

	public long ServiceId { get; }
}