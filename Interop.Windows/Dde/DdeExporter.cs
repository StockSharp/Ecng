namespace Ecng.Interop.Dde;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// Manages DDE (Dynamic Data Exchange) export operations with async/await support.
/// Handles background processing of data rows and streaming to Excel.
/// </summary>
public class DdeExporter : Disposable
{
	private readonly XlsDdeClient _ddeClient;
	private Channel<IList<object>> _channel;
	private Task _exportTask;
	private CancellationTokenSource _cts;

	/// <summary>
	/// Occurs when an error happens during background export processing.
	/// </summary>
	public event Action<Exception> ErrorOccurred;

	/// <summary>
	/// Gets a value indicating whether the exporter is currently running.
	/// </summary>
	public bool IsRunning { get; private set; }

	/// <summary>
	/// Gets the DDE settings used by this exporter.
	/// </summary>
	public DdeSettings Settings => _ddeClient.Settings;

	/// <summary>
	/// Initializes a new instance of the <see cref="DdeExporter"/> class.
	/// </summary>
	/// <param name="settings">The DDE settings to use.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is null.</exception>
	public DdeExporter(DdeSettings settings)
	{
		if (settings is null)
			throw new ArgumentNullException(nameof(settings));

		_ddeClient = new XlsDdeClient(settings);
	}

	/// <summary>
	/// Starts the DDE export background processing.
	/// Initiates connection to Excel and begins processing queued rows.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown when the exporter is already running.</exception>
	public void Start()
	{
		if (IsRunning)
			throw new InvalidOperationException("DDE exporter is already running.");

		_cts = new CancellationTokenSource();
		_channel = Channel.CreateUnbounded<IList<object>>(new UnboundedChannelOptions
		{
			SingleReader = true,
			SingleWriter = false
		});

		_ddeClient.Start();
		IsRunning = true;

		_exportTask = Task.Run(async () => await ProcessQueueAsync(_cts.Token), _cts.Token);
	}

	/// <summary>
	/// Stops the DDE export background processing.
	/// Completes the channel, waits for pending operations, and disconnects from Excel.
	/// </summary>
	/// <param name="timeout">Maximum time to wait for graceful shutdown. Default is 5 seconds.</param>
	/// <returns>A task that represents the asynchronous stop operation.</returns>
	public async Task StopAsync(TimeSpan? timeout = null)
	{
		if (!IsRunning)
			return;

		_channel?.Writer.TryComplete();

		var actualTimeout = timeout ?? TimeSpan.FromSeconds(5);

		try
		{
			if (_exportTask != null)
			{
				await _exportTask.WaitAsync(actualTimeout);
			}
		}
		catch (TimeoutException)
		{
			_cts?.Cancel();
		}
		finally
		{
			_ddeClient.Stop();
			IsRunning = false;

			_cts?.Dispose();
			_cts = null;
		}
	}

	/// <summary>
	/// Attempts to enqueue a data row for asynchronous export to Excel.
	/// </summary>
	/// <param name="row">The row data to export.</param>
	/// <returns>True if the row was successfully enqueued; false if the exporter is not running or the queue is full.</returns>
	public bool TryEnqueue(IList<object> row)
	{
		if (!IsRunning || _channel == null)
			return false;

		return _channel.Writer.TryWrite(row);
	}

	/// <summary>
	/// Performs a one-time synchronous export of all rows to Excel.
	/// Creates a temporary DDE connection, sends all data, and disconnects.
	/// </summary>
	/// <param name="rows">The rows to export. Must include headers if ShowHeaders is enabled.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="rows"/> is null.</exception>
	public void Flush(IEnumerable<IList<object>> rows)
	{
		if (rows is null)
			throw new ArgumentNullException(nameof(rows));

		using var client = new XlsDdeClient(_ddeClient.Settings);
		client.Start();

		try
		{
			client.Poke(rows.ToList());
		}
		finally
		{
			client.Stop();
		}
	}

	/// <summary>
	/// Background processing loop that reads from the channel and sends rows to Excel.
	/// </summary>
	private async Task ProcessQueueAsync(CancellationToken ct)
	{
		try
		{
			var reader = _channel.Reader;

			while (await reader.WaitToReadAsync(ct))
			{
				while (reader.TryRead(out var row))
				{
					_ddeClient.Poke([row]);
				}
			}
		}
		catch (OperationCanceledException)
		{
			// Expected on cancellation
		}
		catch (Exception ex)
		{
			ErrorOccurred?.Invoke(ex);
		}
	}

	/// <summary>
	/// Releases managed resources by stopping the exporter and disposing the DDE client.
	/// </summary>
	protected override void DisposeManaged()
	{
		StopAsync().GetAwaiter().GetResult();
		_ddeClient?.Dispose();
		base.DisposeManaged();
	}
}
