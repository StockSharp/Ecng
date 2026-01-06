namespace Ecng.Tests.Net;

using System.Net;
using System.Net.Sockets;

using Ecng.Net;

[TestClass]
public class UdpSocketTests : BaseTestClass
{
	[TestMethod]
	public void RealUdpSocket_CanCreate_IPv4()
	{
		using var socket = new RealUdpSocket();
		IsNotNull(socket);
	}

	[TestMethod]
	public void RealUdpSocket_CanCreate_IPv6()
	{
		using var socket = new RealUdpSocket(AddressFamily.InterNetworkV6);
		IsNotNull(socket);
	}

	[TestMethod]
	public void RealUdpSocket_ThrowsOnInvalidAddressFamily()
	{
		Throws<ArgumentOutOfRangeException>(() => new RealUdpSocket(AddressFamily.Unix));
		Throws<ArgumentOutOfRangeException>(() => new RealUdpSocket(AddressFamily.AppleTalk));
	}

	[TestMethod]
	public void RealUdpSocket_CanBind_IPv4()
	{
		using var socket = new RealUdpSocket();
		// Bind to any available port
		socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
	}

	[TestMethod]
	public void RealUdpSocket_CanBind_IPv6()
	{
		using var socket = new RealUdpSocket(AddressFamily.InterNetworkV6);
		// Bind to any available port
		socket.Bind(new IPEndPoint(IPAddress.IPv6Loopback, 0));
	}

	[TestMethod]
	public void RealUdpSocket_CanSetSocketOption()
	{
		using var socket = new RealUdpSocket();
		socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
	}

	[TestMethod]
	public void RealUdpSocket_ThrowsObjectDisposedException_AfterDispose()
	{
		var socket = new RealUdpSocket();
		socket.Dispose();

		Throws<ObjectDisposedException>(() => socket.Bind(new IPEndPoint(IPAddress.Loopback, 0)));
		Throws<ObjectDisposedException>(() => socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true));
	}

	[TestMethod]
	public async Task RealUdpSocket_ThrowsObjectDisposedException_OnReceiveAfterDispose()
	{
		var socket = new RealUdpSocket();
		socket.Dispose();

		await ThrowsAsync<ObjectDisposedException>(
			() => socket.ReceiveAsync(new byte[1024], SocketFlags.None, CancellationToken).AsTask());

		await ThrowsAsync<ObjectDisposedException>(
			() => socket.ReceiveFromAsync(new byte[1024], SocketFlags.None, new IPEndPoint(IPAddress.Any, 0), CancellationToken).AsTask());
	}

	[TestMethod]
	public void RealUdpSocketFactory_CreatesSockets_IPv4()
	{
		IUdpSocketFactory factory = new RealUdpSocketFactory();
		IsNotNull(factory);

		using var socket1 = factory.Create();
		using var socket2 = factory.Create();

		IsNotNull(socket1);
		IsNotNull(socket2);
		AreNotSame(socket1, socket2);
	}

	[TestMethod]
	public void RealUdpSocketFactory_CreatesSockets_IPv6()
	{
		IUdpSocketFactory factory = new RealUdpSocketFactory();

		using var socket = factory.Create(AddressFamily.InterNetworkV6);
		IsNotNull(socket);
	}

	[TestMethod]
	public async Task RealUdpSocket_SendReceive_Loopback_IPv4()
	{
		// Arrange - get available port
		int receiverPort;
		using (var tempSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
		{
			tempSocket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
			receiverPort = ((IPEndPoint)tempSocket.LocalEndPoint).Port;
		}

		// Receiver
		using var receiver = new RealUdpSocket();
		receiver.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
		receiver.Bind(new IPEndPoint(IPAddress.Loopback, receiverPort));

		// Sender
		using var sender = new UdpClient();
		var testData = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

		// Act
		var receiveTask = Task.Run(async () =>
		{
			var buffer = new byte[1024];
			var result = await receiver.ReceiveFromAsync(buffer, SocketFlags.None, new IPEndPoint(IPAddress.Any, 0), CancellationToken);
			return (result.ReceivedBytes, buffer);
		});

		await Task.Delay(100, CancellationToken); // Give receiver time to start
		await sender.SendAsync(testData, new IPEndPoint(IPAddress.Loopback, receiverPort), CancellationToken);

		// Assert
		var (receivedBytes, receivedBuffer) = await receiveTask;
		AreEqual(testData.Length, receivedBytes);

		for (int i = 0; i < testData.Length; i++)
			AreEqual(testData[i], receivedBuffer[i]);
	}

	[TestMethod]
	public async Task RealUdpSocket_SendReceive_Loopback_IPv6()
	{
		// Arrange - get available port
		int receiverPort;
		using (var tempSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp))
		{
			tempSocket.Bind(new IPEndPoint(IPAddress.IPv6Loopback, 0));
			receiverPort = ((IPEndPoint)tempSocket.LocalEndPoint).Port;
		}

		// Receiver
		using var receiver = new RealUdpSocket(AddressFamily.InterNetworkV6);
		receiver.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
		receiver.Bind(new IPEndPoint(IPAddress.IPv6Loopback, receiverPort));

		// Sender
		using var sender = new UdpClient(AddressFamily.InterNetworkV6);
		var testData = new byte[] { 0xAA, 0xBB, 0xCC };

		// Act
		var receiveTask = Task.Run(async () =>
		{
			var buffer = new byte[1024];
			var result = await receiver.ReceiveFromAsync(buffer, SocketFlags.None, new IPEndPoint(IPAddress.IPv6Any, 0), CancellationToken);
			return (result.ReceivedBytes, buffer);
		}, CancellationToken);

		await Task.Delay(100, CancellationToken);
		await sender.SendAsync(testData, new IPEndPoint(IPAddress.IPv6Loopback, receiverPort), CancellationToken);

		// Assert
		var (receivedBytes, receivedBuffer) = await receiveTask;
		AreEqual(testData.Length, receivedBytes);

		for (int i = 0; i < testData.Length; i++)
			AreEqual(testData[i], receivedBuffer[i]);
	}

	[TestMethod]
	public void UdpReceiveFromResult_Properties()
	{
		// Arrange & Act
		var endpoint = new IPEndPoint(IPAddress.Loopback, 1234);
		var result = new SocketReceiveFromResult
		{
			ReceivedBytes = 100,
			RemoteEndPoint = endpoint
		};

		// Assert
		AreEqual(100, result.ReceivedBytes);
		AreEqual(endpoint, result.RemoteEndPoint);
	}

	[TestMethod]
	public void RealUdpSocket_CanDisposeMultipleTimes()
	{
		var socket = new RealUdpSocket();

		// Should not throw
		socket.Dispose();
		socket.Dispose();
		socket.Dispose();
	}
}
