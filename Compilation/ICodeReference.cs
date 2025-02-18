namespace Ecng.Compilation;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Serialization;

/// <summary>
/// Represents a reference to code that supports persistence.
/// </summary>
public interface ICodeReference : IPersistable
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
	/// Asynchronously retrieves the images associated with the code reference.
	/// </summary>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains an enumerable of tuples with image name and image body.</returns>
	ValueTask<IEnumerable<(string name, byte[] body)>> GetImages(CancellationToken cancellationToken);
}

/// <summary>
/// Provides a base implementation for a code reference with persistence support.
/// </summary>
public abstract class BaseCodeReference : ICodeReference
{
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
	public abstract void Load(SettingsStorage storage);

	/// <summary>
	/// Saves the settings to the specified storage.
	/// </summary>
	/// <param name="storage">The storage where the settings will be saved.</param>
	public abstract void Save(SettingsStorage storage);

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