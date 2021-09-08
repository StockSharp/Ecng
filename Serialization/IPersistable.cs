namespace Ecng.Serialization
{
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>
	/// Интерфейс, описывающий сохраняемый объект.
	/// </summary>
	public interface IPersistable
	{
		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		void Load(SettingsStorage storage);

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		void Save(SettingsStorage storage);
	}

	public interface IAsyncPersistable
	{
		Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken);

		Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken);
	}
}