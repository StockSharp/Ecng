namespace Ecng.Data;

/// <summary>
/// Specifies a command timeout to apply to database commands via a <see cref="Scope{T}"/>.
/// </summary>
/// <param name="timeout">The timeout duration.</param>
public class DatabaseCommandTimeout(TimeSpan timeout)
{
	/// <summary>
	/// Gets the timeout duration for the database command.
	/// </summary>
	public TimeSpan Timeout { get; } = timeout;
}