namespace Ecng.Tests.Net.Udp;

using System.Net;
using System.Net.Sockets;

using Ecng.Net.Udp;

[TestClass]
public class UdpSocketTests : BaseTestClass
{
	[TestMethod]
	public void RealUdpSocket_CanCreate()
	{
		using var socket = new RealUdpSocket();
		IsNotNull(socket);
	}

	[TestMethod]
	public void RealUdpSocket_CanBind()
	{
		using var socket = new RealUdpSocket();
		// Bind to any available port
		socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
	}

	[TestMethod]
	public void RealUdpSocket_CanSetSocketOption()
	{
		using var socket = new RealUdpSocket();
		socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
	}

	[TestMethod]
	public void RealUdpSocketFactory_CreatesSockets()
	{
		var factory = new RealUdpSocketFactory();
		IsNotNull(factory);

		using var socket1 = factory.Create();
		using var socket2 = factory.Create();

		IsNotNull(socket1);
		IsNotNull(socket2);
		AreNotSame(socket1, socket2);
	}

	[TestMethod]
	public async Task RealUdpSocket_SendReceive_Loopback()
	{
		// Arrange
		using var receiver = new RealUdpSocket();
		receiver.Bind(new IPEndPoint(IPAddress.Loopback, 0));

		// Get the actual port assigned
		using var tempSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		tempSocket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
		var receiverPort = ((IPEndPoint)tempSocket.LocalEndPoint).Port;
		tempSocket.Close();

		// Re-bind receiver to known port
		using var receiver2 = new RealUdpSocket();
		receiver2.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
		receiver2.Bind(new IPEndPoint(IPAddress.Loopback, receiverPort));

		// Sender
		using var sender = new UdpClient();
		var testData = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

		// Act
		var receiveTask = Task.Run(async () =>
		{
			var buffer = new byte[1024];
			var result = await receiver2.ReceiveFromAsync(buffer, SocketFlags.None, new IPEndPoint(IPAddress.Any, 0), CancellationToken);
			return (result.ReceivedBytes, buffer);
		});

		await Task.Delay(100); // Give receiver time to start
		await sender.SendAsync(testData, new IPEndPoint(IPAddress.Loopback, receiverPort));

		// Assert
		var (receivedBytes, receivedBuffer) = await receiveTask;
		AreEqual(testData.Length, receivedBytes);

		for (int i = 0; i < testData.Length; i++)
			AreEqual(testData[i], receivedBuffer[i]);
	}
}
