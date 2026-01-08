namespace Ecng.IO;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Represents an object that supports committing changes.
/// </summary>
public interface ICommitable
{
	/// <summary>
	/// Commits the changes.
	/// </summary>
	void Commit();
}

/// <summary>
/// Represents an object that supports asynchronous committing changes.
/// </summary>
public interface IAsyncCommitable
{
	/// <summary>
	/// Asynchronously commits the changes.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	ValueTask CommitAsync(CancellationToken cancellationToken = default);
}
