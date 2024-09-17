namespace Ecng.Configuration
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Configuration;
	using System.Diagnostics;
	using System.Linq;

	using Ecng.Common;

	public static class ConfigManager
	{
		private static readonly Dictionary<Type, ConfigurationSection> _sections = [];
		private static readonly Dictionary<Type, ConfigurationSectionGroup> _sectionGroups = [];

		private static readonly SyncObject _sync = new();
		private static readonly Dictionary<Type, Dictionary<string, object>> _services = [];

		#region ConfigManager.cctor()

		static ConfigManager()
		{
			try 
			{	        
				//http://csharp-tipsandtricks.blogspot.com/2010/01/identifying-whether-execution-context.html
				InnerConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex);
			}

			if (InnerConfig != null)
			{
				Trace.WriteLine("ConfigManager FilePath=" + InnerConfig.FilePath);

				void InitSections(ConfigurationSectionCollection sections)
				{
					try
					{
						foreach (ConfigurationSection section in sections)
						{
							if (!_sections.ContainsKey(section.GetType()))
								_sections.Add(section.GetType(), section);
						}
					}
					catch (Exception ex)
					{
						Trace.WriteLine(ex);
					}
				}

				void InitSectionGroups(ConfigurationSectionGroupCollection groups)
				{
					foreach (ConfigurationSectionGroup sectionGroup in groups)
					{
						if (!_sectionGroups.ContainsKey(sectionGroup.GetType()))
							_sectionGroups.Add(sectionGroup.GetType(), sectionGroup);

						InitSections(sectionGroup.Sections);
						InitSectionGroups(sectionGroup.SectionGroups);
					}
				}

				InitSections(InnerConfig.Sections);
				InitSectionGroups(InnerConfig.SectionGroups);
			}
		}

		#endregion

		public static Configuration InnerConfig { get; }

		#region GetSection

		public static T GetSection<T>()
			where T : ConfigurationSection
			=> (T)GetSection(typeof(T));

		public static ConfigurationSection GetSection(Type sectionType)
			=> _sections.ContainsKey(sectionType) ? _sections[sectionType] : null;

		public static T GetSection<T>(string sectionName)
			where T : ConfigurationSection
			=> (T)GetSection(sectionName);

		public static ConfigurationSection GetSection(string sectionName)
			=> InnerConfig.GetSection(sectionName);

		#endregion

		#region GetGroup

		public static T GetGroup<T>()
			where T : ConfigurationSectionGroup
			=> (T)GetGroup(typeof(T));

		public static ConfigurationSectionGroup GetGroup(Type sectionGroupType)
			=> _sectionGroups.ContainsKey(sectionGroupType)
				? _sectionGroups[sectionGroupType]
				: null;

		public static T GetGroup<T>(string sectionName)
			where T : ConfigurationSectionGroup
			=> (T)GetGroup(sectionName);

		public static ConfigurationSectionGroup GetGroup(string sectionName)
			=> InnerConfig.GetSectionGroup(sectionName);

		#endregion

		/// <summary>
		/// Try get value from config file.
		/// </summary>
		/// <typeparam name="T">Value type.</typeparam>
		/// <param name="name">Key name.</param>
		/// <param name="defaultValue">Default value.</param>
		/// <returns>Value.</returns>
		public static T TryGet<T>(string name, T defaultValue = default)
		{
			var str = AppSettings.Get(name);

			return str.IsEmpty() ? defaultValue : str.To<T>();
		}

		public static NameValueCollection AppSettings => ConfigurationManager.AppSettings;

		public static event Action<Type, object> ServiceRegistered;

		private static readonly Dictionary<Type, List<Action<object>>> _subscribers = [];

		public static void SubscribeOnRegister<T>(Action<T> registered)
		{
			if (registered is null)
				throw new ArgumentNullException(nameof(registered));

			if (!_subscribers.TryGetValue(typeof(T), out var subscribers))
			{
				subscribers = [];
				_subscribers.Add(typeof(T), subscribers);
			}

			subscribers.Add(svc => registered((T)svc));
		}

		private static void RaiseServiceRegistered(Type type, object service)
		{
			ServiceRegistered?.Invoke(type, service);

			if (!_subscribers.TryGetValue(type, out var subscribers))
				return;

			foreach (var subscriber in subscribers)
				subscriber(service);
		}

		private static Dictionary<string, object> GetDict<T>() => GetDict(typeof(T));

		private static Dictionary<string, object> GetDict(Type type)
		{
			if (_services.TryGetValue(type, out var dict))
				return dict;

			dict = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
			_services.Add(type, dict);
			return dict;
		}

		public static void RegisterService<T>(T service)
			=> RegisterService(typeof(T).AssemblyQualifiedName, service);

		public static void RegisterService<T>(string name, T service)
		{
			lock (_sync)
				GetDict<T>()[name] = service;

			RaiseServiceRegistered(typeof(T), service);
		}

		public static bool IsServiceRegistered<T>()
			=> IsServiceRegistered<T>(typeof(T).AssemblyQualifiedName);

		public static bool IsServiceRegistered<T>(string name)
		{
			lock (_sync)
				return GetDict<T>().ContainsKey(name);
		}

		public static T TryGetService<T>()
			=> IsServiceRegistered<T>() ? GetService<T>() : default;

		public static void TryRegisterService<T>(T service)
		{
			if (IsServiceRegistered<T>())
				return;

			RegisterService(service);
		}

		public static T GetService<T>()
			=> GetService<T>(typeof(T).AssemblyQualifiedName);

		public static T GetService<T>(string name)
		{
			lock (_sync)
				return (T)GetDict<T>()[name];
		}

		public static IEnumerable<T> GetServices<T>()
		{
			lock (_sync)
				return GetDict<T>().Values.Cast<T>().Distinct().ToArray();
		}
	}
}