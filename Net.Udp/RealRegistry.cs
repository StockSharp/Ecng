namespace Ecng.Net;

/// <summary>
/// Registry for real UDP implementations.
/// </summary>
public static class RealRegistry
{
	static RealRegistry()
	{
		SocketFactory = new RealUdpSocketFactory();
		PacketReceiverFactory = new RealPacketReceiverFactory(SocketFactory);
	}

	/// <summary>
	/// The UDP socket factory.
	/// </summary>
	public static IUdpSocketFactory SocketFactory { get; }

	/// <summary>
	/// The packet receiver factory.
	/// </summary>
	public static IPacketReceiverFactory PacketReceiverFactory { get; }
}