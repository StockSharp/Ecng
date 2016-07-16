namespace Ecng.Configuration
{
	using System;
	using System.Collections.Generic;
#if !SILVERLIGHT
	using System.Configuration;
	using System.Diagnostics;
	using System.Web;
	using System.Web.Configuration;

	using Ecng.Common;

	using Microsoft.Practices.Unity;
	using Microsoft.Practices.Unity.Configuration;
#endif
	using Microsoft.Practices.ServiceLocation;
	using NativeServiceLocator = Microsoft.Practices.ServiceLocation.ServiceLocator;

	public static class ConfigManager
	{
#if SILVERLIGHT
		private sealed class ConfigServiceLocator : ServiceLocatorImplBase
		{
			protected override object DoGetInstance(Type serviceType, string key)
			{
				throw new NotSupportedException();
			}

			protected override IEnumerable<object> DoGetAllInstances(Type serviceType)
			{
				throw new NotSupportedException();
			}
		}
#else

		private static readonly Dictionary<Type, ConfigurationSection> _sections = new Dictionary<Type, ConfigurationSection>();
		private static readonly Dictionary<Type, ConfigurationSectionGroup> _sectionGroups = new Dictionary<Type, ConfigurationSectionGroup>();

		private static readonly SyncObject _sync = new SyncObject();
		private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
#endif

		#region ConfigManager.cctor()

		static ConfigManager()
		{
#if !SILVERLIGHT
			//http://csharp-tipsandtricks.blogspot.com/2010/01/identifying-whether-execution-context.html
			InnerConfig = //Assembly.GetEntryAssembly() != null
						HttpRuntime.AppDomainId==null ? 
				ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
				: WebConfigurationManager.OpenWebConfiguration(HttpRuntime.AppDomainAppVirtualPath);

			Trace.WriteLine("ConfigManager FilePath=" + InnerConfig.FilePath);

		    Action<ConfigurationSectionCollection> initSections = sections =>
		    {
                // Из-за ошибки в велсе приходится оборачивать в try
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
		    };

			Action<ConfigurationSectionGroupCollection> initSectionGroups = null;
			initSectionGroups = groups =>
			{
				foreach (ConfigurationSectionGroup sectionGroup in groups)
				{
					if (!_sectionGroups.ContainsKey(sectionGroup.GetType()))
						_sectionGroups.Add(sectionGroup.GetType(), sectionGroup);

					initSections(sectionGroup.Sections);
					initSectionGroups(sectionGroup.SectionGroups);
				}
			};

			initSections(InnerConfig.Sections);
			initSectionGroups(InnerConfig.SectionGroups);

			UnityContainer = new UnityContainer();

			var unity = GetSection<UnityConfigurationSection>();
			if (unity != null)
				UnityContainer.LoadConfiguration(unity);

			var locator = new UnityServiceLocator(UnityContainer);
#else
			var locator = new ConfigServiceLocator();
#endif
			
			NativeServiceLocator.SetLocatorProvider(() => locator);
		}

		#endregion

#if !SILVERLIGHT
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

			if (attr == null)
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

		public static UnityContainer UnityContainer { get; }

		public static event Action<Type, object> ServiceRegistered;

		public static void RegisterService<T>(T service)
		{
			UnityContainer.RegisterInstance(service);

			lock (_sync)
				_services[typeof(T)] = service;

			ServiceRegistered?.Invoke(typeof(T), service);
		}

		public static bool IsServiceRegistered<T>()
		{
			lock (_sync)
			{
				var isReg = _services.ContainsKey(typeof(T));

				if (isReg)
					return true;
			}

			return UnityContainer.IsRegistered<T>();
		}

		public static T TryGetService<T>()
		{
			return IsServiceRegistered<T>() ? GetService<T>() : default(T);
		}

		public static void TryRegisterService<T>(T service)
		{
			if (IsServiceRegistered<T>())
				return;

			RegisterService(service);
		}
#endif

		public static IServiceLocator ServiceLocator => NativeServiceLocator.Current;

		public static T GetService<T>()
		{
			object service;

			lock (_sync)
			{
				if (!_services.TryGetValue(typeof(T), out service))
				{
					service = ServiceLocator.GetInstance<T>();

					if (service != null)
					{
						// service T can register itseft in the constructor
						if (!_services.ContainsKey(typeof(T)))
							_services.Add(typeof(T), service);
					}
				}
			}

			(service as IDelayInitService)?.Init();
			return (T)service;
		}

		public static T GetService<T>(string name)
		{
			return ServiceLocator.GetInstance<T>(name);
		}

		public static IEnumerable<T> GetServices<T>()
		{
			return ServiceLocator.GetAllInstances<T>();
		}

		public static string ToFullPathIfNeed(this string path)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));

			return path.Replace("%Documents%", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
		}
	}
}