namespace Ecng.Common;

using System;
using System.Threading;

/// <summary>
/// Base identifier generator.
/// </summary>
public abstract class IdGenerator
{
	/// <summary>
	/// Initializes a new instance of the <see cref="IdGenerator"/> class.
	/// </summary>
	protected IdGenerator()
	{
	}

	/// <summary>
	/// Gets the next identifier.
	/// </summary>
	/// <returns>The next identifier.</returns>
	public abstract long GetNextId();
}

/// <summary>
/// Identifier generator that automatically increments the identifier by 1.
/// </summary>
public class IncrementalIdGenerator : IdGenerator
{
	/// <summary>
	/// Initializes a new instance of the <see cref="IncrementalIdGenerator"/> class.
	/// </summary>
	public IncrementalIdGenerator()
	{
	}

	private long _current;

	/// <summary>
	/// Gets or sets the current identifier.
	/// </summary>
	public long Current
	{
		get => _current;
		set => _current = value;
	}

	/// <summary>
	/// Gets the next identifier by incrementing the current value.
	/// </summary>
	/// <returns>The next identifier.</returns>
	public override long GetNextId()
	{
		return Interlocked.Increment(ref _current);
	}
}

/// <summary>
/// Identifier generator based on automatic incrementation where the initial value is the number of milliseconds
/// elapsed since the start of the day.
/// </summary>
public class MillisecondIncrementalIdGenerator : IncrementalIdGenerator
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MillisecondIncrementalIdGenerator"/> class.
	/// </summary>
	public MillisecondIncrementalIdGenerator()
	{
		Current = (long)(DateTime.Now - DateTime.Today).TotalMilliseconds;
	}
}

/// <summary>
/// Identifier generator based on automatic incrementation starting from the current Unix time in seconds (UTC).
/// </summary>
public class UTCIncrementalIdGenerator : IncrementalIdGenerator
{
	/// <summary>
	/// Initializes a new instance of the <see cref="UTCIncrementalIdGenerator"/> class.
	/// </summary>
	public UTCIncrementalIdGenerator()
	{
		Current = (long)DateTime.UtcNow.ToUnix();
	}
}

/// <summary>
/// Identifier generator based on automatic incrementation starting from the current Unix time in milliseconds (UTC).
/// </summary>
public class UTCMlsIncrementalIdGenerator : IncrementalIdGenerator
{
	/// <summary>
	/// Initializes a new instance of the <see cref="UTCMlsIncrementalIdGenerator"/> class.
	/// </summary>
	public UTCMlsIncrementalIdGenerator()
	{
		Current = (long)DateTime.UtcNow.ToUnix(false);
	}
}

/// <summary>
/// Identifier generator based on milliseconds.
/// Each subsequent call to <see cref="GetNextId"/> returns the number of milliseconds elapsed since the instance was created.
/// </summary>
public class MillisecondIdGenerator : IdGenerator
{
	private readonly DateTime _start;

	/// <summary>
	/// Initializes a new instance of the <see cref="MillisecondIdGenerator"/> class.
	/// </summary>
	public MillisecondIdGenerator()
	{
		_start = DateTime.Now;
	}

	/// <summary>
	/// Gets the next identifier representing the number of milliseconds elapsed since the object was created.
	/// </summary>
	/// <returns>The number of milliseconds elapsed.</returns>
	public override long GetNextId()
	{
		return (long)(DateTime.Now - _start).TotalMilliseconds;
	}
}

/// <summary>
/// Identifier generator based on the current Unix time in milliseconds (UTC).
/// </summary>
public class UTCMillisecondIdGenerator : IdGenerator
{
	/// <summary>
	/// Gets the next identifier based on Unix time in milliseconds.
	/// </summary>
	/// <returns>The Unix timestamp in milliseconds.</returns>
	public override long GetNextId()
	{
		return (long)TimeHelper.UnixNowMls;
	}
}

/// <summary>
/// Identifier generator based on the current Unix time in seconds (UTC).
/// </summary>
public class UTCSecondIdGenerator : IdGenerator
{
	/// <summary>
	/// Gets the next identifier based on Unix time in seconds.
	/// </summary>
	/// <returns>The Unix timestamp in seconds.</returns>
	public override long GetNextId()
	{
		return (long)TimeHelper.UnixNowS;
	}
}

/// <summary>
/// Identifier generator that uses the current UTC ticks as the identifier.
/// </summary>
public class TickIdGenerator : IdGenerator
{
	/// <summary>
	/// Gets the next identifier based on the current UTC ticks.
	/// </summary>
	/// <returns>The current UTC ticks.</returns>
	public override long GetNextId()
	{
		return DateTime.UtcNow.Ticks;
	}
}

/// <summary>
/// Identifier generator that starts at the current UTC ticks and increments by 1 for each call.
/// </summary>
public class TickIncrementalIdGenerator : IncrementalIdGenerator
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TickIncrementalIdGenerator"/> class,
	/// setting the initial identifier value to the current UTC ticks.
	/// </summary>
	public TickIncrementalIdGenerator()
	{
		Current = DateTime.UtcNow.Ticks;
	}
}