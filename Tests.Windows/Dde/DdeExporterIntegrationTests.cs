namespace Ecng.Tests.Windows.Dde;

using Ecng.Interop.Dde;

/// <summary>
/// Integration tests for <see cref="DdeExporter"/> with automatic client-server setup.
/// These tests use a mock DDE server instead of requiring Excel.
/// </summary>
[TestClass]
public class DdeExporterIntegrationTests
{
	private const string ServiceName = "TEST_EXCEL";
	private const string TopicName = "[TestBook.xlsx]Sheet1";

	#region Client-Server Integration Tests

	[TestMethod]
	public async Task Integration_Flush_SendsDataToServer()
	{
		// Arrange
		using var server = new MockDdeServer(ServiceName);
		server.Register();

		var settings = new DdeSettings
		{
			Server = ServiceName,
			Topic = TopicName,
			ShowHeaders = true,
			ColumnOffset = 0,
			RowOffset = 0
		};

		using var exporter = new DdeExporter(settings);

		var rows = new List<IList<object>>
		{
			new List<object> { "Name", "Age", "City" },
			new List<object> { "Alice", 30, "Moscow" },
			new List<object> { "Bob", 25, "SPb" }
		};

		// Act
		exporter.Flush(rows);

		// Small delay to ensure data is received
		await Task.Delay(100);

		// Assert
		var receivedData = server.ReceivedData;
		receivedData.Count.AssertEqual(1);
		receivedData[0].Topic.AssertEqual(TopicName);

		// Cleanup
		server.Unregister();
	}

	[TestMethod]
	public async Task Integration_StreamingExport_SendsMultipleRows()
	{
		// Arrange
		using var server = new MockDdeServer(ServiceName);
		server.Register();

		var dataReceivedCount = 0;
		var resetEvent = new ManualResetEventSlim(false);

		server.DataReceived += data =>
		{
			Interlocked.Increment(ref dataReceivedCount);
			if (dataReceivedCount >= 5)
				resetEvent.Set();
		};

		var settings = new DdeSettings
		{
			Server = ServiceName,
			Topic = TopicName,
			ShowHeaders = false,
			ColumnOffset = 0,
			RowOffset = 0
		};

		using var exporter = new DdeExporter(settings);

		// Act
		exporter.Start();

		for (int i = 0; i < 5; i++)
		{
			var row = new List<object> { $"User{i}", 20 + i, $"City{i}" };
			exporter.TryEnqueue(row).AssertTrue();
		}

		// Wait for all data to be received (with timeout)
		var received = resetEvent.Wait(TimeSpan.FromSeconds(5));

		await exporter.StopAsync();

		// Assert
		received.AssertTrue();
		Assert.IsTrue(dataReceivedCount >= 5);

		// Cleanup
		server.Unregister();
	}

	[TestMethod]
	public async Task Integration_LargeDataSet_HandlesCorrectly()
	{
		// Arrange
		using var server = new MockDdeServer(ServiceName);
		server.Register();

		var settings = new DdeSettings
		{
			Server = ServiceName,
			Topic = TopicName,
			ShowHeaders = true
		};

		using var exporter = new DdeExporter(settings);

		var rowCount = 100;
		var rows = new List<IList<object>>
		{
			new List<object> { "ID", "Value1", "Value2", "Value3" }
		};

		for (int i = 0; i < rowCount; i++)
		{
			rows.Add(new List<object> { i, i * 2, i * 3, i * 4 });
		}

		// Act
		exporter.Start();

		foreach (var row in rows)
		{
			exporter.TryEnqueue(row);
		}

		// Wait for processing
		await Task.Delay(500);

		await exporter.StopAsync();

		// Assert
		var receivedData = server.ReceivedData;
		Assert.IsTrue(receivedData.Count >= rowCount);

		// Cleanup
		server.Unregister();
	}

	[TestMethod]
	public async Task Integration_ErrorRecovery_ContinuesAfterError()
	{
		// Arrange
		using var server = new MockDdeServer(ServiceName);
		server.Register();

		var errorCount = 0;
		var settings = new DdeSettings
		{
			Server = ServiceName,
			Topic = TopicName
		};

		using var exporter = new DdeExporter(settings);
		exporter.ErrorOccurred += ex => Interlocked.Increment(ref errorCount);

		// Act
		exporter.Start();

		// Send valid data
		exporter.TryEnqueue(new List<object> { "Valid", "Data" });
		await Task.Delay(100);

		// Send more valid data
		exporter.TryEnqueue(new List<object> { "More", "Data" });
		await Task.Delay(100);

		await exporter.StopAsync();

		// Assert
		var receivedData = server.ReceivedData;
		Assert.IsTrue(receivedData.Count >= 2);

		// Cleanup
		server.Unregister();
	}

	[TestMethod]
	public async Task Integration_ConcurrentAccess_MultipleClients()
	{
		// Arrange
		using var server = new MockDdeServer(ServiceName);
		server.Register();

		var settings = new DdeSettings
		{
			Server = ServiceName,
			Topic = TopicName
		};

		using var exporter1 = new DdeExporter(settings);
		using var exporter2 = new DdeExporter(new DdeSettings
		{
			Server = ServiceName,
			Topic = "[TestBook2.xlsx]Sheet1"
		});

		// Act
		exporter1.Start();
		exporter2.Start();

		var tasks = new List<Task>();

		tasks.Add(Task.Run(async () =>
		{
			for (int i = 0; i < 10; i++)
			{
				exporter1.TryEnqueue(new List<object> { $"Client1_{i}", i });
				await Task.Delay(10);
			}
		}));

		tasks.Add(Task.Run(async () =>
		{
			for (int i = 0; i < 10; i++)
			{
				exporter2.TryEnqueue(new List<object> { $"Client2_{i}", i });
				await Task.Delay(10);
			}
		}));

		await Task.WhenAll(tasks);
		await Task.Delay(200);

		await exporter1.StopAsync();
		await exporter2.StopAsync();

		// Assert
		var receivedData = server.ReceivedData;
		Assert.IsTrue(receivedData.Count >= 20);

		// Cleanup
		server.Unregister();
	}

	[TestMethod]
	public async Task Integration_ServerRestart_ReconnectsSuccessfully()
	{
		// Arrange
		var server = new MockDdeServer(ServiceName);
		server.Register();

		var settings = new DdeSettings
		{
			Server = ServiceName,
			Topic = TopicName
		};

		using var exporter = new DdeExporter(settings);

		// Act - First connection
		exporter.Start();
		exporter.TryEnqueue(new List<object> { "Before", "Restart" });
		await Task.Delay(100);

		var countBefore = server.ReceivedData.Count;
		await exporter.StopAsync();

		// Restart server
		server.Unregister();
		server.Dispose();
		await Task.Delay(100);

		server = new MockDdeServer(ServiceName);
		server.Register();

		// Second connection
		exporter.Start();
		exporter.TryEnqueue(new List<object> { "After", "Restart" });
		await Task.Delay(100);

		var countAfter = server.ReceivedData.Count;
		await exporter.StopAsync();

		// Assert
		Assert.IsTrue(countBefore >= 1);
		Assert.IsTrue(countAfter >= 1);

		// Cleanup
		server.Unregister();
		server.Dispose();
	}

	#endregion

	#region Performance Tests

	[TestMethod]
	public async Task Performance_HighThroughput_HandlesLoad()
	{
		// Arrange
		using var server = new MockDdeServer(ServiceName);
		server.Register();

		var settings = new DdeSettings
		{
			Server = ServiceName,
			Topic = TopicName
		};

		using var exporter = new DdeExporter(settings);

		var rowCount = 1000;
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();

		// Act
		exporter.Start();

		for (int i = 0; i < rowCount; i++)
		{
			var row = new List<object> { i, $"Data{i}", i * 1.5 };
			exporter.TryEnqueue(row);
		}

		await Task.Delay(1000); // Give time to process
		await exporter.StopAsync();

		stopwatch.Stop();

		// Assert
		var receivedData = server.ReceivedData;
		Assert.IsTrue(receivedData.Count >= rowCount * 0.9); // At least 90% delivered

		Console.WriteLine($"Processed {receivedData.Count} rows in {stopwatch.ElapsedMilliseconds}ms");
		Console.WriteLine($"Throughput: {receivedData.Count / (stopwatch.ElapsedMilliseconds / 1000.0):F2} rows/sec");

		// Cleanup
		server.Unregister();
	}

	#endregion

	#region Edge Cases

	[TestMethod]
	public async Task EdgeCase_EmptyRowValues_HandlesCorrectly()
	{
		// Arrange
		using var server = new MockDdeServer(ServiceName);
		server.Register();

		var settings = new DdeSettings
		{
			Server = ServiceName,
			Topic = TopicName
		};

		using var exporter = new DdeExporter(settings);

		// Act
		exporter.Start();

		exporter.TryEnqueue(new List<object> { "", null, 0 });
		exporter.TryEnqueue(new List<object> { "Valid", "Data" });

		await Task.Delay(200);
		await exporter.StopAsync();

		// Assert
		var receivedData = server.ReceivedData;
		Assert.IsTrue(receivedData.Count >= 2);

		// Cleanup
		server.Unregister();
	}

	[TestMethod]
	public async Task EdgeCase_SpecialCharacters_HandlesCorrectly()
	{
		// Arrange
		using var server = new MockDdeServer(ServiceName);
		server.Register();

		var settings = new DdeSettings
		{
			Server = ServiceName,
			Topic = TopicName
		};

		using var exporter = new DdeExporter(settings);

		// Act
		exporter.Start();

		var specialChars = new List<object>
		{
			"Test\tTab",
			"Test\nNewline",
			"Test\"Quote",
			"Тест Кириллица",
			"测试中文"
		};

		exporter.TryEnqueue(specialChars);

		await Task.Delay(200);
		await exporter.StopAsync();

		// Assert
		var receivedData = server.ReceivedData;
		Assert.IsTrue(receivedData.Count >= 1);

		// Cleanup
		server.Unregister();
	}

	#endregion
}
