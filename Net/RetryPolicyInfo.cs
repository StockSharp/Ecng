namespace Ecng.Net;

using System.Net.Sockets;

using Ecng.Localization;

public class RetryPolicyInfo
{
	private int _readMaxCount;

	public int ReadMaxCount
	{
		get => _readMaxCount;
		set
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid value.".Localize());

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
				throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid value.".Localize());

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
				throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid value.".Localize());

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
				throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid value.".Localize());

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