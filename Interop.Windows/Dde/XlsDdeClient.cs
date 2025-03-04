namespace Ecng.Interop.Dde;

using System;
using System.Collections.Generic;

using Ecng.Common;

using NDde.Client;

/// <summary>
/// A client for interacting with Excel via Dynamic Data Exchange (DDE), providing methods to start, stop, and send data.
/// </summary>
public class XlsDdeClient(DdeSettings settings) : Disposable
{
	private DdeClient _client;

	/// <summary>
	/// Gets a value indicating whether the DDE client is currently started and connected.
	/// </summary>
	public bool IsStarted => _client != null;

	/// <summary>
	/// Gets the settings used to configure the DDE client.
	/// </summary>
	/// <exception cref="ArgumentNullException">Thrown when the settings provided during construction are null.</exception>
	public DdeSettings Settings { get; } = settings ?? throw new ArgumentNullException(nameof(settings));

	/// <summary>
	/// Starts the DDE client and establishes a connection to the specified server and topic.
	/// </summary>
	public void Start()
	{
		_client = new DdeClient(Settings.Server, Settings.Topic);
		_client.Connect();
	}

	/// <summary>
	/// Stops the DDE client and disconnects from the server if currently connected.
	/// </summary>
	public void Stop()
	{
		if (_client.IsConnected)
			_client.Disconnect();

		_client = null;
	}

	/// <summary>
	/// Sends a block of data to Excel via DDE using the specified row and column offsets.
	/// </summary>
	/// <param name="rows">A list of rows, where each row is a list of cell values to send.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="rows"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="rows"/> is empty.</exception>
	public void Poke(IList<IList<object>> rows)
	{
		if (rows is null)
			throw new ArgumentNullException(nameof(rows));

		if (rows.Count == 0)
			throw new ArgumentOutOfRangeException(nameof(rows));

		if (!Settings.ShowHeaders)
			rows.RemoveAt(0);

		var rowStart = 1 + Settings.RowOffset;
		var columnStart = 1 + Settings.ColumnOffset;
		var colCount = rows.Count == 0 ? 0 : rows[0].Count;

		_client.Poke($"R{rowStart}C{columnStart}:R{rowStart + rows.Count}C{columnStart + colCount}",
			XlsDdeSerializer.Serialize(rows), 0x0090 | 0x4000, (int)TimeSpan.FromSeconds(10).TotalMilliseconds);
	}

	/// <summary>
	/// Releases managed resources by stopping the DDE client if it is started.
	/// </summary>
	protected override void DisposeManaged()
	{
		if (IsStarted)
			Stop();

		base.DisposeManaged();
	}
}