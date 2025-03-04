namespace Ecng.Serialization;

using System;

using Ecng.Common;

/// <summary>
/// Context for continue on exception.
/// </summary>
public class ContinueOnExceptionContext
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ContinueOnExceptionContext"/>.
	/// </summary>
	public event Action<Exception> Error;

	/// <summary>
	/// Do not encrypt.
	/// </summary>
	public bool DoNotEncrypt { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ContinueOnExceptionContext"/>.
	/// </summary>
	/// <param name="ex">The exception.</param>
	/// <returns>Operation result.</returns>
	public static bool TryProcess(Exception ex)
	{
		if (ex is null)
			throw new ArgumentNullException(nameof(ex));

		var ctx = Scope<ContinueOnExceptionContext>.Current;

		if (ctx is null)
			return false;

		ctx.Value.Process(ex);
		return true;
	}

	/// <summary>
	/// Process the exception.
	/// </summary>
	/// <param name="ex">The exception.</param>
	public void Process(Exception ex)
	{
		if (ex is null)
			throw new ArgumentNullException(nameof(ex));

		Error?.Invoke(ex);
	}
}