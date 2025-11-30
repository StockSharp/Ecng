namespace Ecng.Tests.Windows.Dde;

using Ecng.Interop.Dde;

/// <summary>
/// Unit tests for <see cref="DdeExporter"/> class.
/// These tests do not require a DDE server.
/// </summary>
[TestClass]
public class DdeExporterTests
{
	#region Construction and Initialization

	[TestMethod]
	public void Construction_WithValidSettings_CreatesExporter()
	{
		// Arrange
		var settings = new DdeSettings
		{
			Server = "EXCEL",
			Topic = "[Book1.xlsx]Sheet1",
			ColumnOffset = 0,
			RowOffset = 0,
			ShowHeaders = true
		};

		// Act
		using var exporter = new DdeExporter(settings);

		// Assert
		exporter.IsRunning.AssertFalse();
		exporter.Settings.Server.AssertEqual("EXCEL");
		exporter.Settings.Topic.AssertEqual("[Book1.xlsx]Sheet1");
	}

	[TestMethod]
	public void Construction_WithNullSettings_ThrowsArgumentNullException()
	{
		// Act & Assert
		try
		{
			var exporter = new DdeExporter(null);
			Assert.Fail("Expected ArgumentNullException");
		}
		catch (ArgumentNullException)
		{
			// Expected
		}
	}

	[TestMethod]
	public void Settings_AreAccessibleAfterConstruction()
	{
		// Arrange
		var settings = new DdeSettings
		{
			Server = "TEST_SERVER",
			Topic = "[TestBook.xlsx]Sheet1",
			ColumnOffset = 5,
			RowOffset = 10,
			ShowHeaders = false
		};

		using var exporter = new DdeExporter(settings);

		// Act & Assert
		exporter.Settings.Server.AssertEqual("TEST_SERVER");
		exporter.Settings.Topic.AssertEqual("[TestBook.xlsx]Sheet1");
		exporter.Settings.ColumnOffset.AssertEqual(5);
		exporter.Settings.RowOffset.AssertEqual(10);
		exporter.Settings.ShowHeaders.AssertFalse();
	}

	#endregion

	#region TryEnqueue - Without Server

	[TestMethod]
	public void TryEnqueue_WhenNotRunning_ReturnsFalse()
	{
		// Arrange
		using var exporter = new DdeExporter(new DdeSettings());
		var row = new List<object> { "A", "B", "C" };

		// Act
		var result = exporter.TryEnqueue(row);

		// Assert
		result.AssertFalse();
	}

	[TestMethod]
	public async Task TryEnqueue_AfterStop_ReturnsFalse()
	{
		// Arrange
		using var server = new MockDdeServer("TEST");
		server.Register();

		using var exporter = new DdeExporter(new DdeSettings
		{
			Server = "TEST",
			Topic = "[Test.xlsx]Sheet1"
		});

		var row = new List<object> { "Test" };

		// Act
		exporter.Start();
		exporter.TryEnqueue(row).AssertTrue();

		await exporter.StopAsync();
		var resultAfterStop = exporter.TryEnqueue(row);

		// Assert
		resultAfterStop.AssertFalse();

		// Cleanup
		server.Unregister();
	}

	#endregion

	#region Flush Validation

	[TestMethod]
	public void Flush_WithNullRows_ThrowsArgumentNullException()
	{
		// Arrange
		using var exporter = new DdeExporter(new DdeSettings());

		// Act & Assert
		try
		{
			exporter.Flush(null);
			Assert.Fail("Expected ArgumentNullException");
		}
		catch (ArgumentNullException)
		{
			// Expected
		}
	}

	[TestMethod]
	public void Flush_WithEmptyRows_ThrowsFromXlsDdeClient()
	{
		// Arrange
		using var exporter = new DdeExporter(new DdeSettings());
		var rows = new List<IList<object>>();

		// Act & Assert
		try
		{
			exporter.Flush(rows);
			Assert.Fail("Expected ArgumentOutOfRangeException");
		}
		catch (ArgumentOutOfRangeException)
		{
			// Expected
		}
	}

	#endregion

	#region Disposal

	[TestMethod]
	public void Dispose_WhenNotRunning_DoesNotThrow()
	{
		// Arrange
		var exporter = new DdeExporter(new DdeSettings());

		// Act & Assert - should not throw
		exporter.Dispose();
	}

	[TestMethod]
	public void Dispose_MultipleTimes_DoesNotThrow()
	{
		// Arrange
		var exporter = new DdeExporter(new DdeSettings());

		// Act & Assert - should not throw
		exporter.Dispose();
		exporter.Dispose();
		exporter.Dispose();
	}

	#endregion

	#region Thread Safety

	[TestMethod]
	public async Task TryEnqueue_FromMultipleThreads_ThreadSafe()
	{
		// Arrange
		using var server = new MockDdeServer("TEST_THREAD");
		server.Register();

		using var exporter = new DdeExporter(new DdeSettings
		{
			Server = "TEST_THREAD",
			Topic = "[Test.xlsx]Sheet1"
		});

		exporter.Start();

		var successCount = 0;
		var tasks = new List<Task>();

		// Act - Enqueue from multiple threads
		for (int i = 0; i < 10; i++)
		{
			var taskIndex = i;
			tasks.Add(Task.Run(() =>
			{
				for (int j = 0; j < 10; j++)
				{
					var row = new List<object> { taskIndex, j, taskIndex * j };
					if (exporter.TryEnqueue(row))
					{
						Interlocked.Increment(ref successCount);
					}
				}
			}));
		}

		await Task.WhenAll(tasks);
		await Task.Delay(100);

		// Assert
		successCount.AssertEqual(100);

		// Cleanup
		await exporter.StopAsync();
		server.Unregister();
	}

	#endregion
}
