namespace Ecng.ComponentModel;

using System.Runtime.Serialization;
using System;

/// <summary>
/// Specifies the possible connection states.
/// </summary>
[Serializable]
[DataContract]
public enum ConnectionStates
{
	/// <summary>
	/// The connection is currently disconnected.
	/// </summary>
	[EnumMember]
	Disconnected,

	/// <summary>
	/// The connection is in the process of disconnecting.
	/// </summary>
	[EnumMember]
	Disconnecting,

	/// <summary>
	/// The connection is in the process of connecting.
	/// </summary>
	[EnumMember]
	Connecting,

	/// <summary>
	/// The connection is successfully established.
	/// </summary>
	[EnumMember]
	Connected,

	/// <summary>
	/// The connection is attempting to reconnect.
	/// </summary>
	[EnumMember]
	Reconnecting,

	/// <summary>
	/// The connection has been restored.
	/// </summary>
	[EnumMember]
	Restored,

	/// <summary>
	/// The connection attempt has failed.
	/// </summary>
	[EnumMember]
	Failed,
}