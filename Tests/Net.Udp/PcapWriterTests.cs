namespace Ecng.Tests.Net.Udp;

using System;
using System.IO;
using System.Linq;

using Ecng.IO;
using Ecng.Net.Udp;
using Ecng.UnitTesting;

using SharpPcap;
using SharpPcap.LibPcap;

[TestClass]
public class PcapWriterTests : BaseTestClass
{
	[TestMethod]
	public void PcapStreamWriter_WritesValidHeader()
	{
		// Arrange
		using var stream = new MemoryStream();

		// Act
		using (var writer = new PcapStreamWriter(stream))
		{
			// Just create writer, don't write any packets
		}

		// Assert - verify PCAP global header (24 bytes)
		var data = stream.ToArray();
		AreEqual(24, data.Length);

		// Magic number (little-endian): 0xa1b2c3d4
		AreEqual((byte)0xd4, data[0]);
		AreEqual((byte)0xc3, data[1]);
		AreEqual((byte)0xb2, data[2]);
		AreEqual((byte)0xa1, data[3]);

		// Version major: 2
		AreEqual((byte)2, data[4]);
		AreEqual((byte)0, data[5]);

		// Version minor: 4
		AreEqual((byte)4, data[6]);
		AreEqual((byte)0, data[7]);

		// Link type at offset 20: Ethernet = 1
		AreEqual((byte)1, data[20]);
		AreEqual((byte)0, data[21]);
		AreEqual((byte)0, data[22]);
		AreEqual((byte)0, data[23]);
	}

	[TestMethod]
	public void PcapStreamWriter_WritesPacketWithCorrectHeader()
	{
		// Arrange
		using var stream = new MemoryStream();
		var timestamp = new DateTime(2024, 6, 15, 12, 30, 45, DateTimeKind.Utc);
		var packetData = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

		// Act
		using (var writer = new PcapStreamWriter(stream))
		{
			writer.Write(timestamp, packetData);
		}

		// Assert
		var data = stream.ToArray();

		// Global header (24) + packet header (16) + data (5) = 45 bytes
		AreEqual(45, data.Length);

		// Packet captured length at offset 32 (24 + 8)
		var capturedLen = BitConverter.ToUInt32(data, 32);
		AreEqual(5u, capturedLen);

		// Packet original length at offset 36 (24 + 12)
		var originalLen = BitConverter.ToUInt32(data, 36);
		AreEqual(5u, originalLen);

		// Packet data starts at offset 40
		AreEqual((byte)0x01, data[40]);
		AreEqual((byte)0x02, data[41]);
		AreEqual((byte)0x03, data[42]);
		AreEqual((byte)0x04, data[43]);
		AreEqual((byte)0x05, data[44]);
	}

	[TestMethod]
	public void PcapStreamWriter_CurrentSize_TracksCorrectly()
	{
		// Arrange
		using var stream = new MemoryStream();
		var packetData = new byte[100];

		// Act & Assert
		using var writer = new PcapStreamWriter(stream);

		// After header
		AreEqual(24, writer.CurrentSize);

		// After first packet: header (24) + packet header (16) + data (100) = 140
		writer.Write(DateTime.UtcNow, packetData);
		AreEqual(140, writer.CurrentSize);

		// After second packet: 140 + 16 + 100 = 256
		writer.Write(DateTime.UtcNow, packetData);
		AreEqual(256, writer.CurrentSize);
	}

	[TestMethod]
	public void PcapStreamWriter_WithMemoryFileSystem_WritesFile()
	{
		// Arrange
		var fs = new MemoryFileSystem();
		var path = "/test/output.pcap";
		var packetData = new byte[] { 0xAA, 0xBB, 0xCC };

		// Act
		using (var writer = new PcapStreamWriter(fs, path))
		{
			writer.Write(DateTime.UtcNow, packetData);
		}

		// Assert
		IsTrue(fs.FileExists(path));
		AreEqual(24 + 16 + 3, fs.GetFileLength(path)); // header + packet header + data
	}

	[TestMethod]
	public void PcapStreamWriter_VerifyWithSharpPcap()
	{
		// Arrange - write PCAP with our writer
		var tempFile = Path.Combine(LocalFileSystem.Instance.GetTempPath(), "test.pcap");

		var timestamp1 = new DateTime(2024, 1, 15, 10, 20, 30, DateTimeKind.Utc);
		var timestamp2 = new DateTime(2024, 1, 15, 10, 20, 31, DateTimeKind.Utc);

		// Create minimal Ethernet frame (14 bytes header + payload)
		var ethFrame1 = CreateMinimalEthernetFrame([0x01, 0x02, 0x03]);
		var ethFrame2 = CreateMinimalEthernetFrame([0x04, 0x05]);

		using (var writer = new PcapStreamWriter(LocalFileSystem.Instance, tempFile))
		{
			writer.Write(timestamp1, ethFrame1);
			writer.Write(timestamp2, ethFrame2);
		}

		// Act - read back with SharpPcap
		using var device = new CaptureFileReaderDevice(tempFile);
		device.Open();

		var packets = new System.Collections.Generic.List<RawCapture>();
		device.OnPacketArrival += (sender, e) => packets.Add(e.GetPacket());
		device.Capture();

		// Assert
		AreEqual(2, packets.Count);

		// Verify first packet
		AreEqual(ethFrame1.Length, packets[0].Data.Length);
		IsTrue(packets[0].Data.SequenceEqual(ethFrame1));

		// Verify second packet
		AreEqual(ethFrame2.Length, packets[1].Data.Length);
		IsTrue(packets[1].Data.SequenceEqual(ethFrame2));
	}

	[TestMethod]
	public void PcapStreamWriter_MultiplePackets_VerifyWithSharpPcap()
	{
		// Arrange
		var tempFile = Path.Combine(LocalFileSystem.Instance.GetTempPath(), "test_multi.pcap");

		const int packetCount = 100;
		var baseTime = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		using (var writer = new PcapStreamWriter(LocalFileSystem.Instance, tempFile))
		{
			for (int i = 0; i < packetCount; i++)
			{
				var payload = new byte[50 + i]; // Variable size packets
				for (int j = 0; j < payload.Length; j++)
					payload[j] = (byte)(i + j);

				var frame = CreateMinimalEthernetFrame(payload);
				writer.Write(baseTime.AddMilliseconds(i * 10), frame);
			}
		}

		// Act - read back with SharpPcap
		using var device = new CaptureFileReaderDevice(tempFile);
		device.Open();

		var packets = new System.Collections.Generic.List<RawCapture>();
		device.OnPacketArrival += (sender, e) => packets.Add(e.GetPacket());
		device.Capture();

		// Assert
		AreEqual(packetCount, packets.Count);

		for (int i = 0; i < packetCount; i++)
		{
			var expectedPayloadLen = 50 + i;
			var expectedFrameLen = 14 + expectedPayloadLen; // Ethernet header + payload
			AreEqual(expectedFrameLen, packets[i].Data.Length);
		}
	}

	[TestMethod]
	public void PcapStreamWriterFactory_CreatesWriter()
	{
		// Arrange
		var fs = new MemoryFileSystem();
		var factory = new PcapStreamWriterFactory(fs);
		var path = "/output.pcap";

		// Act
		using (var writer = factory.Create(path))
		{
			writer.Write(DateTime.UtcNow, [1, 2, 3]);
		}

		// Assert
		IsTrue(fs.FileExists(path));
	}

	[TestMethod]
	public void PcapStreamWriter_ThrowsAfterDispose()
	{
		// Arrange
		using var stream = new MemoryStream();
		var writer = new PcapStreamWriter(stream);
		writer.Dispose();

		// Act & Assert
		Throws<ObjectDisposedException>(() => writer.Write(DateTime.UtcNow, [1]));
	}

	[TestMethod]
	public void PcapStreamWriter_ThrowsOnNullData()
	{
		// Arrange
		using var stream = new MemoryStream();
		using var writer = new PcapStreamWriter(stream);

		// Act & Assert
		Throws<ArgumentNullException>(() => writer.Write(DateTime.UtcNow, null));
	}

	private static byte[] CreateMinimalEthernetFrame(byte[] payload)
	{
		// Minimal Ethernet frame: 6 bytes dst MAC + 6 bytes src MAC + 2 bytes EtherType + payload
		var frame = new byte[14 + payload.Length];

		// Destination MAC (broadcast)
		frame[0] = 0xFF; frame[1] = 0xFF; frame[2] = 0xFF;
		frame[3] = 0xFF; frame[4] = 0xFF; frame[5] = 0xFF;

		// Source MAC
		frame[6] = 0x00; frame[7] = 0x11; frame[8] = 0x22;
		frame[9] = 0x33; frame[10] = 0x44; frame[11] = 0x55;

		// EtherType (IPv4 = 0x0800)
		frame[12] = 0x08;
		frame[13] = 0x00;

		// Payload
		Array.Copy(payload, 0, frame, 14, payload.Length);

		return frame;
	}
}
