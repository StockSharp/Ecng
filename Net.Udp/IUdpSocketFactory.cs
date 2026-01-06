namespace Ecng.Net;

using System.Net.Sockets;

/// <summary>
/// Factory for creating UDP sockets.
/// </summary>
public interface IUdpSocketFactory
{
	/// <summary>
	/// Creates a new UDP socket.
	/// </summary>
	/// <param name="addressFamily">The address family (IPv4 or IPv6).</param>
	/// <returns>The UDP socket.</returns>
	IUdpSocket Create(AddressFamily addressFamily = AddressFamily.InterNetwork);
}

/// <summary>
/// Default factory creating real sockets.
/// </summary>
public class RealUdpSocketFactory : IUdpSocketFactory
{
	/// <inheritdoc />
	IUdpSocket IUdpSocketFactory.Create(AddressFamily addressFamily)
		=> new RealUdpSocket(addressFamily);
}