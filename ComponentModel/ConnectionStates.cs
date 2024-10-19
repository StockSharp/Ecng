namespace Ecng.ComponentModel;

using System.Runtime.Serialization;
using System;

[Serializable]
[DataContract]
public enum ConnectionStates
{
	[EnumMember]
	Disconnected,

	[EnumMember]
	Disconnecting,

	[EnumMember]
	Connecting,

	[EnumMember]
	Connected,

	[EnumMember]
	Reconnecting,

	[EnumMember]
	Restored,

	[EnumMember]
	Failed,
}