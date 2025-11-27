namespace Ecng.Configuration;

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Ecng.Common;

/// <summary>
/// Provides access to application configuration sections, groups, and services.
/// </summary>
public static class ConfigManager
{
	private static readonly Dictionary<Type, ConfigurationSection> _sections = [];
	private static readonly Dictionary<Type, ConfigurationSectionGroup> _sectionGroups = [];

	private static readonly Lock _sync = new();
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

	/// <summary>
	/// Gets the underlying configuration file used by the application.
	/// </summary>
	public static Configuration InnerConfig { get; }

	private static Configuration SafeInnerConfig()
		=> InnerConfig ?? throw new InvalidOperationException("Configuration file is not available.");

	#region GetSection

	/// <summary>
	/// Returns the configuration section of type T.
	/// </summary>
	/// <typeparam name="T">The type of the configuration section.</typeparam>
	/// <returns>The configuration section instance of type T.</returns>
	public static T GetSection<T>()
		where T : ConfigurationSection
		=> (T)GetSection(typeof(T));

	/// <summary>
	/// Returns the configuration section matching the specified type, or null if not found.
	/// </summary>
	/// <param name="sectionType">The type of the configuration section.</param>
	/// <returns>The configuration section instance or null.</returns>
	public static ConfigurationSection GetSection(Type sectionType)
		=> _sections.TryGetValue(sectionType, out var section)
		? section
		: null;

	/// <summary>
	/// Returns the configuration section of type T specified by the section name.
	/// </summary>
	/// <typeparam name="T">The type of the configuration section.</typeparam>
	/// <param name="sectionName">The name of the configuration section.</param>
	/// <returns>The configuration section instance of type T.</returns>
	public static T GetSection<T>(string sectionName)
		where T : ConfigurationSection
		=> (T)GetSection(sectionName);

	/// <summary>
	/// Returns the configuration section specified by the section name.
	/// </summary>
	/// <param name="sectionName">The name of the configuration section.</param>
	/// <returns>The configuration section instance.</returns>
	public static ConfigurationSection GetSection(string sectionName)
		=> SafeInnerConfig().GetSection(sectionName);

	#endregion

	#region GetGroup

	/// <summary>
	/// Returns the configuration section group of type T.
	/// </summary>
	/// <typeparam name="T">The type of the configuration section group.</typeparam>
	/// <returns>The configuration section group instance of type T.</returns>
	public static T GetGroup<T>()
		where T : ConfigurationSectionGroup
		=> (T)GetGroup(typeof(T));

	/// <summary>
	/// Returns the configuration section group matching the specified type, or null if not found.
	/// </summary>
	/// <param name="sectionGroupType">The type of the configuration section group.</param>
	/// <returns>The configuration section group instance or null.</returns>
	public static ConfigurationSectionGroup GetGroup(Type sectionGroupType)
		=> _sectionGroups.TryGetValue(sectionGroupType, out var group)
			? group
			: null;

	/// <summary>
	/// Returns the configuration section group of type T specified by the section name.
	/// </summary>
	/// <typeparam name="T">The type of the configuration section group.</typeparam>
	/// <param name="sectionName">The name of the configuration section group.</param>
	/// <returns>The configuration section group instance of type T.</returns>
	public static T GetGroup<T>(string sectionName)
		where T : ConfigurationSectionGroup
		=> (T)GetGroup(sectionName);

	/// <summary>
	/// Returns the configuration section group specified by the section name.
	/// </summary>
	/// <param name="sectionName">The name of the configuration section group.</param>
	/// <returns>The configuration section group instance.</returns>
	public static ConfigurationSectionGroup GetGroup(string sectionName)
		=> SafeInnerConfig().GetSectionGroup(sectionName);

	#endregion

	/// <summary>
	/// Tries to get a configuration value from the configuration file using the specified key.
	/// </summary>
	/// <typeparam name="T">The expected value type.</typeparam>
	/// <param name="name">The key name.</param>
	/// <param name="defaultValue">The default value if the key is not found or empty.</param>
	/// <returns>The configuration value, or the default value if not found.</returns>
	public static T TryGet<T>(string name, T defaultValue = default)
	{
		var str = AppSettings.Get(name);

		return str.IsEmpty() ? defaultValue : str.To<T>();
	}

	/// <summary>
	/// Gets the application settings.
	/// </summary>
	public static NameValueCollection AppSettings => ConfigurationManager.AppSettings;

	/// <summary>
	/// Occurs when a service is registered.
	/// </summary>
	public static event Action<Type, object> ServiceRegistered;

	private static readonly Dictionary<Type, List<Action<object>>> _subscribers = [];

	/// <summary>
	/// Subscribes to the service registration event for the specified type.
	/// </summary>
	/// <typeparam name="T">The type of service to subscribe for.</typeparam>
	/// <param name="registered">The action to invoke when a service is registered.</param>
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

	private static Dictionary<string, object> GetDict<T>()
		=> GetDict(typeof(T));

	private static Dictionary<string, object> GetDict(Type type)
	{
		if (_services.TryGetValue(type, out var dict))
			return dict;

		dict = new(StringComparer.InvariantCultureIgnoreCase);
		_services.Add(type, dict);
		return dict;
	}

	/// <summary>
	/// Registers the specified service of type T using the default name.
	/// </summary>
	/// <typeparam name="T">The type of the service to register.</typeparam>
	/// <param name="service">The service instance.</param>
	public static void RegisterService<T>(T service)
		=> RegisterService(typeof(T).AssemblyQualifiedName, service);

	/// <summary>
	/// Registers the specified service of type T with the provided name.
	/// </summary>
	/// <typeparam name="T">The type of the service to register.</typeparam>
	/// <param name="name">The unique name to register the service.</param>
	/// <param name="service">The service instance.</param>
	public static void RegisterService<T>(string name, T service)
	{
		using (_sync.EnterScope())
			GetDict<T>()[name] = service;

		RaiseServiceRegistered(typeof(T), service);
	}

	/// <summary>
	/// Checks if a service of type T is registered using the default name.
	/// </summary>
	/// <typeparam name="T">The type of the service.</typeparam>
	/// <returns>True if the service is registered; otherwise, false.</returns>
	public static bool IsServiceRegistered<T>()
		=> IsServiceRegistered<T>(typeof(T).AssemblyQualifiedName);

	/// <summary>
	/// Checks if a service of type T with the provided name is registered.
	/// </summary>
	/// <typeparam name="T">The type of the service.</typeparam>
	/// <param name="name">The unique name of the service.</param>
	/// <returns>True if the service is registered; otherwise, false.</returns>
	public static bool IsServiceRegistered<T>(string name)
	{
		using (_sync.EnterScope())
			return GetDict<T>().ContainsKey(name);
	}

	/// <summary>
	/// Tries to retrieve the registered service of type T, returning default if not registered.
	/// </summary>
	/// <typeparam name="T">The type of the service.</typeparam>
	/// <returns>The service instance, or default if not registered.</returns>
	public static T TryGetService<T>()
		=> IsServiceRegistered<T>() ? GetService<T>() : default;

	/// <summary>
	/// Registers the service of type T if it is not already registered.
	/// </summary>
	/// <typeparam name="T">The type of the service.</typeparam>
	/// <param name="service">The service instance.</param>
	public static void TryRegisterService<T>(T service)
	{
		if (IsServiceRegistered<T>())
			return;

		RegisterService(service);
	}

	/// <summary>
	/// Occurs when a service fallback is invoked to construct a service.
	/// </summary>
	public static event Func<Type, string, object> ServiceFallback;

	/// <summary>
	/// Retrieves the service of type T by the default name, using fallback if necessary.
	/// </summary>
	/// <typeparam name="T">The type of the service.</typeparam>
	/// <returns>The service instance.</returns>
	public static T GetService<T>()
		=> GetService<T>(typeof(T).AssemblyQualifiedName);

	/// <summary>
	/// Retrieves the service of type T by specified name, using fallback if necessary.
	/// </summary>
	/// <typeparam name="T">The type of the service.</typeparam>
	/// <param name="name">The unique name of the service.</param>
	/// <returns>The service instance.</returns>
	public static T GetService<T>(string name)
	{
		using (_sync.EnterScope())
		{
			var dict = GetDict<T>();

			if (dict.TryGetValue(name, out var service))
				return (T)service;
		}

		var fallback = ServiceFallback ?? throw new InvalidOperationException($"Service '{name}' not registered.");

		var typed = (T)fallback(typeof(T), name) ?? throw new InvalidOperationException($"Service '{name}' not constructed.");

		RegisterService(name, typed);

		return typed;
	}

	/// <summary>
	/// Returns all distinct registered services of type T.
	/// </summary>
	/// <typeparam name="T">The type of the services.</typeparam>
	/// <returns>An enumerable of service instances.</returns>
	public static IEnumerable<T> GetServices<T>()
	{
		using (_sync.EnterScope())
			return [.. GetDict<T>().Values.Cast<T>().Distinct()];
	}
}