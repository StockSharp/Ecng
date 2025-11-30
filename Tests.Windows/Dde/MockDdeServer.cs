namespace Ecng.Tests.Windows.Dde;

using NDde.Server;

/// <summary>
/// Mock DDE server for testing purposes.
/// Simulates Excel DDE server without requiring actual Excel installation.
/// </summary>
class MockDdeServer : DdeServer
{
	private readonly object _lock = new();
	private readonly List<PokeData> _receivedData = new();

	/// <summary>
	/// Gets all data received via Poke requests.
	/// </summary>
	public IReadOnlyList<PokeData> ReceivedData
	{
		get
		{
			lock (_lock)
				return _receivedData.ToArray();
		}
	}

	/// <summary>
	/// Event triggered when data is poked to the server.
	/// </summary>
	public event Action<PokeData> DataReceived;

	/// <summary>
	/// Initializes a new instance of the <see cref="MockDdeServer"/> class.
	/// </summary>
	/// <param name="service">The service name (e.g., "EXCEL").</param>
	public MockDdeServer(string service)
		: base(service)
	{
	}

	/// <summary>
	/// Clears all received data.
	/// </summary>
	public void ClearReceivedData()
	{
		lock (_lock)
			_receivedData.Clear();
	}

	/// <summary>
	/// Called when a client attempts to poke data to the server.
	/// </summary>
	protected override PokeResult OnPoke(DdeConversation conversation, string item, byte[] data, int format)
	{
		var pokeData = new PokeData
		{
			Topic = conversation.Topic,
			Item = item,
			Data = data,
			Format = format,
			Timestamp = DateTime.UtcNow
		};

		lock (_lock)
			_receivedData.Add(pokeData);

		DataReceived?.Invoke(pokeData);

		return PokeResult.Processed;
	}

	/// <summary>
	/// Called when a client requests data from the server.
	/// Returns empty byte array as we're a mock server.
	/// </summary>
	protected override RequestResult OnRequest(DdeConversation conversation, string item, int format)
	{
		return RequestResult.NotProcessed;
	}

	/// <summary>
	/// Called when a client starts an advise loop.
	/// </summary>
	protected override bool OnStartAdvise(DdeConversation conversation, string item, int format)
	{
		return true;
	}

	/// <summary>
	/// Called when a client stops an advise loop.
	/// </summary>
	protected override void OnStopAdvise(DdeConversation conversation, string item)
	{
	}

	/// <summary>
	/// Called when a client executes a command.
	/// </summary>
	protected override ExecuteResult OnExecute(DdeConversation conversation, string command)
	{
		return ExecuteResult.NotProcessed;
	}

	/// <summary>
	/// Represents data received via DDE Poke.
	/// </summary>
	public class PokeData
	{
		/// <summary>
		/// Gets or sets the topic name.
		/// </summary>
		public string Topic { get; set; }

		/// <summary>
		/// Gets or sets the item name (cell range in Excel format).
		/// </summary>
		public string Item { get; set; }

		/// <summary>
		/// Gets or sets the raw data bytes.
		/// </summary>
		public byte[] Data { get; set; }

		/// <summary>
		/// Gets or sets the clipboard format.
		/// </summary>
		public int Format { get; set; }

		/// <summary>
		/// Gets or sets the timestamp when data was received.
		/// </summary>
		public DateTime Timestamp { get; set; }

		/// <summary>
		/// Gets the data as string (assuming CF_TEXT format).
		/// </summary>
		public string DataAsString => System.Text.Encoding.ASCII.GetString(Data).TrimEnd('\0');
	}
}
