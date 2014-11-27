namespace Ecng.Serialization
{
	using System;
	using System.Globalization;
	using System.IO;
	using System.Text;

	using Ecng.Common;

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

	public static class PersistableHelper
	{
		/// <summary>
		/// Создать и инициализировать объем.
		/// </summary>
		/// <param name="storage">Хранилище настроек, на основе которого будет создает объект.</param>
		/// <returns>Объект.</returns>
		public static T LoadEntire<T>(this SettingsStorage storage)
			where T : IPersistable
		{
			if (storage == null)
				throw new ArgumentNullException("storage");

			var instance = storage.GetValue<Type>("type").CreateInstance<T>();
			instance.Load(storage.GetValue<SettingsStorage>("settings"));
			return instance;
		}

		public static SettingsStorage SaveEntire(this IPersistable persistable, bool isAssemblyQualifiedName)
		{
			if (persistable == null)
				throw new ArgumentNullException("persistable");

			var storage = new SettingsStorage();
			storage.SetValue("type", persistable.GetType().GetTypeName(isAssemblyQualifiedName));
			storage.SetValue("settings", persistable.Save());
			return storage;
		}

		public static T Clone<T>(this T obj)
			where T : IPersistable
		{
			if (obj.IsNull())
				return default(T);

			var clone = obj.GetType().CreateInstance<T>();
			clone.Load(obj.Save());
			return clone;
		}

		public static void Apply<T>(this T obj, T clone)
			where T : IPersistable
		{
			obj.Load(clone.Save());
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="persistable">Сохраняемый объект.</param>
		/// <returns>Хранилище настроек.</returns>
		public static SettingsStorage Save(this IPersistable persistable)
		{
			if (persistable == null)
				throw new ArgumentNullException("persistable");

			var storage = new SettingsStorage();
			persistable.Save(storage);
			return storage;
		}

		public static T Load<T>(this SettingsStorage storage)
			where T : IPersistable, new()
		{
			if (storage == null)
				throw new ArgumentNullException("storage");

			var obj = new T();
			obj.Load(storage);
			return obj;
		}

		/// <summary>
		/// Добавить значение в настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		/// <param name="name">Название значения.</param>
		/// <param name="persistable">Сохраняемый объект.</param>
		public static void SetValue(this SettingsStorage storage, string name, IPersistable persistable)
		{
			if (storage == null)
				throw new ArgumentNullException("storage");

			storage.SetValue(name, persistable.Save());
		}

		public static SettingsStorage LoadSettingsStorage(this string value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			var serializer = new XmlSerializer<SettingsStorage>();
			var bytes = Encoding.UTF8.GetBytes(value);

			return CultureInfo.InvariantCulture.DoInCulture(() => serializer.Deserialize(bytes));
		}

		public static string SaveSettingsStorage(this SettingsStorage settings)
		{
			if (settings == null)
				throw new ArgumentNullException("settings");

			var serializer = new XmlSerializer<SettingsStorage>();

			using (var s = new MemoryStream())
			{
				var stream = s;

				CultureInfo.InvariantCulture.DoInCulture(() => serializer.Serialize(settings, stream));

				return Encoding.UTF8.GetString(stream.ToArray());
			}
		}
	}
}