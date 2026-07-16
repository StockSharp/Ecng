namespace Ecng.Tests;

using System.Reflection;

using Ecng.Net;
using Ecng.IO;
using Ecng.IO.Compression;
using Ecng.Interop;

/// <summary>
/// Guards that the blocking sync-over-async wrappers stay marked <see cref="ObsoleteAttribute"/>
/// so their use (which parks a thread-pool thread for the whole I/O round-trip) surfaces as a
/// compiler warning. See the deadlock/starvation audit of the web API.
/// </summary>
[TestClass]
public class SyncWrapperObsoleteTests : BaseTestClass
{
	[TestMethod]
	[DataRow(typeof(RestSharpHelper), "Invoke")]
	[DataRow(typeof(RestSharpHelper), "Invoke2")]
	[DataRow(typeof(WebSocketClient), "Connect")]
	[DataRow(typeof(WebSocketClient), "Disconnect")]
	[DataRow(typeof(WebSocketClient), "Send")]
	[DataRow(typeof(CompressionHelper), "Zip")]
	[DataRow(typeof(CompressionHelper), "Compress")]
	[DataRow(typeof(CompressionHelper), "Uncompress")]
	[DataRow(typeof(CompressionHelper), "UnGZip")]
	[DataRow(typeof(CompressionHelper), "UnDeflate")]
	[DataRow(typeof(CompressionHelper), "DeflateTo")]
	[DataRow(typeof(CompressionHelper), "DeflateFrom")]
	[DataRow(typeof(FileSystemZipExtensions), "Zip")]
	[DataRow(typeof(TransactionFileStream), "Commit")]
	[DataRow(typeof(HardwareInfo), "GetId")]
	[DataRow(typeof(ProcessExtensions), "Execute")]
	public void SyncWrapper_IsMarkedObsolete(Type type, string methodName)
	{
		var overloads = type
			.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
			.Where(m => m.Name == methodName)
			.ToArray();

		overloads.Length.AssertGreater(0, $"{type.Name}.{methodName} not found");

		overloads.Any(m => m.GetCustomAttribute<ObsoleteAttribute>() is not null)
			.AssertTrue($"{type.Name}.{methodName} must have an [Obsolete] sync-over-async overload");
	}
}
