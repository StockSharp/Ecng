namespace Ecng.Net;

using System.Net.Sockets;

public class RetryPolicyInfo
{
	private int _readMaxCount;

	public int ReadMaxCount
	{
		get => _readMaxCount;
		set
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid value.");

			_readMaxCount = value;
		}
	}

	private int _writeMaxCount;

	public int WriteMaxCount
	{
		get => _writeMaxCount;
		set
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid value.");

			_writeMaxCount = value;
		}
	}

	private TimeSpan _initialDelay = TimeSpan.FromSeconds(1);

	public TimeSpan InitialDelay
	{
		get => _initialDelay;
		set
		{
			if (value <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid value.");

			_initialDelay = value;
		}
	}

	private TimeSpan _maxDelay = TimeSpan.FromSeconds(30);

	public TimeSpan MaxDelay
	{
		get => _maxDelay;
		set
		{
			if (value <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid value.");

			_maxDelay = value;
		}
	}

	public ISet<SocketError> Track { get; } = new SynchronizedSet<SocketError>
	{
		SocketError.TimedOut,
		SocketError.NoData,
		SocketError.HostNotFound,
	};
}