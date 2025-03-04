namespace Ecng.Server.Utils;

using System;
using System.IO;
using System.Reflection;

using Ecng.Serialization;
using Ecng.Logging;

using Microsoft.Extensions.Logging;

/// <summary>
/// Services IO utils.
/// </summary>
public static class ServicePath
{
	/// <summary>
	/// Current service directory.
	/// </summary>
	public static string ServiceDir => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

	/// <summary>
	/// Get path to Data directory.
	/// </summary>
	public static string DataDir => Path.Combine(ServiceDir, "Data");

	/// <summary>
	/// 
	/// </summary>
	/// <param name="logger"></param>
	/// <param name="dataDir"></param>
	/// <param name="defaultLevel"></param>
	/// <returns></returns>
	public static LogManager CreateLogManager(this ILogger logger, string dataDir, LogLevels defaultLevel)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));

		Directory.CreateDirectory(dataDir);

		var serializer = JsonSerializer<SettingsStorage>.CreateDefault();

		var logSettingsFile = Path.Combine(dataDir, $"logManager.{serializer.FileExtension}");

		var logManager = new LogManager
		{
			Application = { LogLevel = defaultLevel }
		};

		if (File.Exists(logSettingsFile))
		{
			logManager.Load(serializer.Deserialize(logSettingsFile));
		}
		else
		{
			logManager.Listeners.Add(new FileLogListener
			{
				Append = true,
				FileName = "logs",
				LogDirectory = Path.Combine(dataDir, "Logs"),
				SeparateByDates = SeparateByDateModes.SubDirectories,
				HistoryPolicy = FileLogHistoryPolicies.Delete,
			});

			serializer.Serialize(logManager.Save(), logSettingsFile);
		}

		logManager.Listeners.Add(new ServiceLogListener(logger));

		return logManager;
	}

	/// <summary>
	/// Restart service.
	/// </summary>
	public static void Restart()
	{
		// https://stackoverflow.com/a/220451
		// solution required setup
		Environment.Exit(1);
	}
}