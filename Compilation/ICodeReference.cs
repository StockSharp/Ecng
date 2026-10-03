namespace Ecng.Compilation;

/// <summary>
/// Represents a reference to code that supports persistence.
/// </summary>
public interface ICodeReference : IAsyncPersistable
{
	/// <summary>
	/// Gets the identifier of the code reference.
	/// </summary>
	string Id { get; }

	/// <summary>
	/// Gets the name of the code reference.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Gets the location of the code reference.
	/// </summary>
	string Location { get; }

	/// <summary>
	/// Gets a value indicating whether the code reference is valid.
	/// </summary>
	bool IsValid { get; }

	/// <summary>
	/// File system abstraction available to derived classes.
	/// </summary>
	IFileSystem FileSystem { get; }

	/// <summary>
	/// Asynchronously retrieves the images associated with the code reference.
	/// </summary>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains an enumerable of tuples with image name and image body.</returns>
	ValueTask<IEnumerable<(string name, byte[] body)>> GetImages(CancellationToken cancellationToken);
}

/// <summary>
/// Provides a base implementation for a code reference with persistence support.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BaseCodeReference"/>.
/// </remarks>
/// <param name="fileSystem">File system abstraction to use.</param>
public abstract class BaseCodeReference(IFileSystem fileSystem) : ICodeReference
{
	/// <summary>
	/// File system abstraction available to derived classes.
	/// </summary>
	public IFileSystem FileSystem { get; } = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

	/// <summary>
	/// Gets the identifier of the code reference. Defaults to the location.
	/// </summary>
	public virtual string Id => Location;

	/// <summary>
	/// Gets the name of the code reference.
	/// </summary>
	public abstract string Name { get; }

	/// <summary>
	/// Gets the location of the code reference.
	/// </summary>
	public abstract string Location { get; }

	/// <summary>
	/// Gets a value indicating whether the code reference is valid.
	/// </summary>
	public abstract bool IsValid { get; }

	/// <summary>
	/// Loads the settings from the specified storage.
	/// </summary>
	/// <param name="storage">The storage containing the settings.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	public abstract Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken);

	/// <summary>
	/// Saves the settings to the specified storage.
	/// </summary>
	/// <param name="storage">The storage where the settings will be saved.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	public abstract Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken);

	/// <summary>
	/// Returns a string that represents the current code reference.
	/// </summary>
	/// <returns>The location of the code reference.</returns>
	public override string ToString() => Location;

	/// <summary>
	/// Asynchronously retrieves the images associated with the code reference.
	/// </summary>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains an enumerable of tuples with image name and image body.</returns>
	public abstract ValueTask<IEnumerable<(string name, byte[] body)>> GetImages(CancellationToken cancellationToken);
}
