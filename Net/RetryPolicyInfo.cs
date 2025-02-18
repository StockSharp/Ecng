namespace Ecng.Net;

using System.Net.Sockets;

using Ecng.Localization;

/// <summary>
/// Provides configuration information for retry policies, including retry counts and delays.
/// </summary>
public class RetryPolicyInfo
{
	private int _readMaxCount;

	/// <summary>
	/// Gets or sets the maximum number of retry attempts for read operations.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when the value is less than 0.
	/// </exception>
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

	/// <summary>
	/// Gets or sets the maximum number of retry attempts for write operations.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when the value is less than 0.
	/// </exception>
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

	/// <summary>
	/// Gets or sets the initial delay to wait between retry attempts.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when the value is less than or equal to <see cref="TimeSpan.Zero"/>.
	/// </exception>
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

	/// <summary>
	/// Gets or sets the maximum delay allowed between retry attempts.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when the value is less than or equal to <see cref="TimeSpan.Zero"/>.
	/// </exception>
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

	/// <summary>
	/// Gets the set of socket errors for which retry attempts should be tracked.
	/// </summary>
	public ISet<SocketError> Track { get; } = new SynchronizedSet<SocketError>
	{
		SocketError.TimedOut,
		SocketError.NoData,
		SocketError.HostNotFound,
	};
}