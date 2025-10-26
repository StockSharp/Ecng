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
	public static string ServiceDir => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;

	/// <summary>
	/// Get path to Data directory.
	/// </summary>
	public static string DataDir => Path.Combine(ServiceDir, "Data");

	/// <summary>
	/// Creates and configures an <see cref="LogManager"/> that persists settings to the specified data directory,
	/// writes logs to files, and forwards messages to the provided <see cref="ILogger"/>.
	/// </summary>
	/// <param name="logger">The Microsoft.Extensions.Logging logger used to mirror log messages via <see cref="ServiceLogListener"/>.</param>
	/// <param name="dataDir">The writable directory where the log manager settings file and log files are stored.</param>
	/// <param name="defaultLevel">The default application log level used when no persisted settings are found.</param>
	/// <returns>A configured <see cref="LogManager"/> instance.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is <c>null</c>.</exception>
	/// <remarks>
	/// If a settings file (logManager.{serializer extension}) exists in <paramref name="dataDir"/>, it is loaded; otherwise,
	/// a default file listener is created that writes to the <c>Logs</c> subdirectory and the settings are saved.
	/// </remarks>
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