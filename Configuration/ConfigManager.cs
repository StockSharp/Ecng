namespace Ecng.Configuration
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Configuration;
	using System.Diagnostics;
	using System.Web;
#if !NETCOREAPP && !NETSTANDARD
	using System.Web.Configuration;
#endif

	using Ecng.Common;

	using Microsoft.Practices.Unity;
	using Microsoft.Practices.Unity.Configuration;
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
						HttpRuntime.AppDomainId == null
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

				UnityContainer = new UnityContainer();

				var unity = GetSection<UnityConfigurationSection>();
				if (unity != null)
					UnityContainer.LoadConfiguration(unity);

				var locator = new UnityServiceLocator(UnityContainer);
				NativeServiceLocator.SetLocatorProvider(() => locator);
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

		public static NameValueCollection AppSettings => ConfigurationManager.AppSettings;

		public static UnityContainer UnityContainer { get; }

		public static event Action<Type, object> ServiceRegistered;

		private static readonly Dictionary<Type, List<Action<object>>> _subscribers = new Dictionary<Type, List<Action<object>>>();

		public static void SubscribeOnRegister<T>(Action<T> registered)
		{
			if (registered == null)
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

		public static void RegisterService<T>(T service)
		{
			lock (_sync)
			{
				UnityContainer.RegisterInstance(service);
				_services[typeof(T)] = service;
			}

			RaiseServiceRegistered(typeof(T), service);
		}

		public static void RegisterService<T>(string name, T service)
		{
			lock (_sync)
			{
				UnityContainer.RegisterInstance(name, service);
				_services[typeof(T)] = service;
			}

			RaiseServiceRegistered(typeof(T), service);
		}

		public static bool IsServiceRegistered<T>()
		{
			lock (_sync)
			{
				var isReg = _services.ContainsKey(typeof(T));

				if (isReg)
					return true;

				return UnityContainer.IsRegistered<T>();
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

		public static IServiceLocator ServiceLocator => NativeServiceLocator.Current;

		public static T GetService<T>()
		{
			object service;

			lock (_sync)
			{
				if (_services.TryGetValue(typeof(T), out service))
					return (T)service;

				service = ServiceLocator.GetInstance<T>();

				if (service != null)
				{
					// service T can register itself in the constructor
					if (!_services.ContainsKey(typeof(T)))
						_services.Add(typeof(T), service);
				}
			}

			//(service as IDelayInitService)?.Init();
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
	}
}