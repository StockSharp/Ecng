namespace Ecng.Configuration
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Configuration;
	using System.Diagnostics;
	using System.Linq;
#if NETFRAMEWORK
	using System.Web;
	using System.Web.Configuration;
#endif

	using Ecng.Common;

#if !NETSTANDARD
	using Microsoft.Practices.Unity;
	using Microsoft.Practices.Unity.Configuration;
	using Microsoft.Practices.ServiceLocation;
	using NativeServiceLocator = Microsoft.Practices.ServiceLocation.ServiceLocator;
#endif

	public static class ConfigManager
	{
		private static readonly Dictionary<Type, ConfigurationSection> _sections = new();
		private static readonly Dictionary<Type, ConfigurationSectionGroup> _sectionGroups = new();

		private static readonly SyncObject _sync = new();
		private static readonly Dictionary<Type, Dictionary<string, object>> _services = new();
#if !NETSTANDARD
		private static readonly UnityContainer _unityContainer;
#endif

		#region ConfigManager.cctor()

		static ConfigManager()
		{
#if NETCOREAPP || NETSTANDARD
			try 
			{	        
				//http://csharp-tipsandtricks.blogspot.com/2010/01/identifying-whether-execution-context.html
				InnerConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex);
				Console.WriteLine(ex);
			}
#else
			//http://csharp-tipsandtricks.blogspot.com/2010/01/identifying-whether-execution-context.html
			InnerConfig = //Assembly.GetEntryAssembly() != null
						HttpRuntime.AppDomainId is null
				? ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
				: WebConfigurationManager.OpenWebConfiguration(HttpRuntime.AppDomainAppVirtualPath);
#endif

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

#if !NETSTANDARD
				_unityContainer = new UnityContainer();

				var unity = GetSection<UnityConfigurationSection>();
				if (unity != null)
					_unityContainer.LoadConfiguration(unity);

				var locator = new UnityServiceLocator(_unityContainer);
				NativeServiceLocator.SetLocatorProvider(() => locator);
#endif
			}
		}

		#endregion

		public static Configuration InnerConfig { get; }

		#region GetSection

		public static T GetSection<T>()
			where T : ConfigurationSection
		{
			return (T)GetSection(typeof(T));
		}

		public static ConfigurationSection GetSection(Type sectionType)
		{
			return _sections.ContainsKey(sectionType) ? _sections[sectionType] : null;
		}

		public static T GetSection<T>(string sectionName)
			where T : ConfigurationSection
		{
			return (T)GetSection(sectionName);
		}

		public static ConfigurationSection GetSection(string sectionName)
		{
			return InnerConfig.GetSection(sectionName);
		}

		#endregion

		#region GetSectionByType

		public static T GetSectionByType<T>()
			where T : ConfigurationSection
		{
			return (T)GetSectionByType(typeof(T));
		}

		public static ConfigurationSection GetSectionByType(Type type)
		{
			var attr = type.GetAttribute<ConfigSectionAttribute>();

			if (attr is null)
				throw new ArgumentException("Type '{0}' isn't marked ConfigSectionAttribute.".Put(type));

			return GetSection(attr.SectionType);
		}

		#endregion

		#region GetGroup

		public static T GetGroup<T>()
			where T : ConfigurationSectionGroup
		{
			return (T)GetGroup(typeof(T));
		}

		public static ConfigurationSectionGroup GetGroup(Type sectionGroupType)
		{
			return _sectionGroups.ContainsKey(sectionGroupType) ? _sectionGroups[sectionGroupType] : null;
		}

		public static T GetGroup<T>(string sectionName)
			where T : ConfigurationSectionGroup
		{
			return (T)GetGroup(sectionName);
		}

		public static ConfigurationSectionGroup GetGroup(string sectionName)
		{
			return InnerConfig.GetSectionGroup(sectionName);
		}

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
			try
			{
				var str = AppSettings.Get(name);

				if (!str.IsEmpty())
					return str.To<T>();
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex);
			}

			return defaultValue;
		}

		public static NameValueCollection AppSettings => ConfigurationManager.AppSettings;

		public static event Action<Type, object> ServiceRegistered;

		private static readonly Dictionary<Type, List<Action<object>>> _subscribers = new();

		public static void SubscribeOnRegister<T>(Action<T> registered)
		{
			if (registered is null)
				throw new ArgumentNullException(nameof(registered));

			if (!_subscribers.TryGetValue(typeof(T), out var subscribers))
			{
				subscribers = new List<Action<object>>();
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
		{
			RegisterService(typeof(T).AssemblyQualifiedName, service);
		}

		public static void RegisterService<T>(string name, T service)
		{
			lock (_sync)
			{
#if !NETSTANDARD
				_unityContainer.RegisterInstance(name, service);
#endif
				GetDict<T>()[name] = service;
			}

			RaiseServiceRegistered(typeof(T), service);
		}

		public static bool IsServiceRegistered<T>() => IsServiceRegistered<T>(typeof(T).AssemblyQualifiedName);

		public static bool IsServiceRegistered<T>(string name)
		{
			lock (_sync)
			{
				if (GetDict<T>().ContainsKey(name))
					return true;

				return
#if NETSTANDARD
					false;
#else
				_unityContainer.IsRegistered<T>();
#endif
			}
		}

		public static T TryGetService<T>()
		{
			return IsServiceRegistered<T>() ? GetService<T>() : default;
		}

		public static void TryRegisterService<T>(T service)
		{
			if (IsServiceRegistered<T>())
				return;

			RegisterService(service);
		}

#if !NETSTANDARD
		private static IServiceLocator ServiceLocator => NativeServiceLocator.Current;
#endif

		public static T GetService<T>() => GetService<T>(typeof(T).AssemblyQualifiedName);

		public static T GetService<T>(string name)
		{
			object service = null;

			lock (_sync)
			{
				var dict = GetDict<T>();

				if (dict.TryGetValue(name, out service) == true)
					return (T)service;

#if !NETSTANDARD
				service = ServiceLocator.GetInstance<T>();
#endif

				if (service != null)
				{
					// service T can register itself in the constructor
					if (!dict.ContainsKey(name))
						dict.Add(name, service);
				}
			}

			//(service as IDelayInitService)?.Init();
			return (T)service;
		}

		public static IEnumerable<T> GetServices<T>()
		{
			IEnumerable<T> services;

			lock (_sync)
				services = GetDict<T>().Values.Cast<T>().ToArray();
#if !NETSTANDARD
			services = services.Concat(ServiceLocator.GetAllInstances<T>()).ToArray();
#endif
			return services.Distinct();
		}
	}
}