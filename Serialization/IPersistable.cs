namespace Ecng.Serialization
{
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>
	/// Interface for objects whose state can be persisted synchronously using a SettingsStorage.
	/// </summary>
	public interface IPersistable
	{
		/// <summary>
		/// Loads the state of the object from the specified SettingsStorage.
		/// </summary>
		/// <param name="storage">The SettingsStorage instance from which to load the data.</param>
		void Load(SettingsStorage storage);

		/// <summary>
		/// Saves the state of the object to the specified SettingsStorage.
		/// </summary>
		/// <param name="storage">The SettingsStorage instance to which to save the data.</param>
		void Save(SettingsStorage storage);
	}

	/// <summary>
	/// Interface for objects whose state can be persisted asynchronously using a SettingsStorage.
	/// </summary>
	public interface IAsyncPersistable
	{
		/// <summary>
		/// Asynchronously loads the state of the object from the specified SettingsStorage.
		/// </summary>
		/// <param name="storage">The SettingsStorage instance from which to load the data.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous load operation.</returns>
		Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken);

		/// <summary>
		/// Asynchronously saves the state of the object to the specified SettingsStorage.
		/// </summary>
		/// <param name="storage">The SettingsStorage instance to which to save the data.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous save operation.</returns>
		Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken);
	}
}