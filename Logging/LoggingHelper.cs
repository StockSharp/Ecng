namespace Ecng.Logging;

using System.Reflection;
using System.Threading.Tasks;

/// <summary>
/// Extension class for <see cref="ILogSource"/>.
/// </summary>
public static class LoggingHelper
{
	/// <summary>
	/// To record a message to the log.
	/// </summary>
	/// <param name="receiver">Logs receiver.</param>
	/// <param name="getMessage">The function returns the text for <see cref="LogMessage.Message"/>.</param>
	public static void AddInfoLog(this ILogReceiver receiver, Func<string> getMessage)
	{
		receiver.AddLog(LogLevels.Info, getMessage);
	}

	/// <summary>
	/// To record a warning to the log.
	/// </summary>
	/// <param name="receiver">Logs receiver.</param>
	/// <param name="getMessage">The function returns the text for <see cref="LogMessage.Message"/>.</param>
	public static void AddWarningLog(this ILogReceiver receiver, Func<string> getMessage)
	{
		receiver.AddLog(LogLevels.Warning, getMessage);
	}

	/// <summary>
	/// To record an error to the log.
	/// </summary>
	/// <param name="receiver">Logs receiver.</param>
	/// <param name="getMessage">The function returns the text for <see cref="LogMessage.Message"/>.</param>
	public static void AddErrorLog(this ILogReceiver receiver, Func<string> getMessage)
	{
		receiver.AddLog(LogLevels.Error, getMessage);
	}

	/// <summary>
	/// To record a message to the log.
	/// </summary>
	/// <param name="receiver">Logs receiver.</param>
	/// <param name="level">The level of the log message.</param>
	/// <param name="getMessage">The function that returns the text for <see cref="LogMessage.Message"/>.</param>
	public static void AddLog(this ILogReceiver receiver, LogLevels level, Func<string> getMessage)
	{
		if (receiver == null)
			throw new ArgumentNullException(nameof(receiver));

		receiver.AddLog(new LogMessage(receiver, receiver.CurrentTimeUtc, level, getMessage));
	}

	/// <summary>
	/// To record a message to the log.
	/// </summary>
	/// <param name="receiver">Logs receiver.</param>
	/// <param name="message">Text message.</param>
	/// <param name="args">Text message settings. Used if the message is a format string.</param>
	public static void AddInfoLog(this ILogReceiver receiver, string message, params object[] args)
	{
		receiver.AddMessage(LogLevels.Info, message, args);
	}

	/// <summary>
	/// To record a verbose message to the log.
	/// </summary>
	/// <param name="receiver">Logs receiver.</param>
	/// <param name="message">Text message.</param>
	/// <param name="args">Text message settings. Used if the message is a format string.</param>
	public static void AddVerboseLog(this ILogReceiver receiver, string message, params object[] args)
	{
		receiver.AddMessage(LogLevels.Verbose, message, args);
	}

	/// <summary>
	/// To record a debug message to the log.
	/// </summary>
	/// <param name="receiver">Logs receiver.</param>
	/// <param name="message">Text message.</param>
	/// <param name="args">Text message settings. Used if the message is a format string.</param>
	public static void AddDebugLog(this ILogReceiver receiver, string message, params object[] args)
	{
		receiver.AddMessage(LogLevels.Debug, message, args);
	}

	/// <summary>
	/// To record a warning to the log.
	/// </summary>
	/// <param name="receiver">Logs receiver.</param>
	/// <param name="message">Text message.</param>
	/// <param name="args">Text message settings. Used if the message is a format string.</param>
	public static void AddWarningLog(this ILogReceiver receiver, string message, params object[] args)
	{
		receiver.AddMessage(LogLevels.Warning, message, args);
	}

	/// <summary>
	/// To record an error to the log.
	/// </summary>
	/// <param name="receiver">Logs receiver.</param>
	/// <param name="exception">Error details.</param>
	public static void AddErrorLog(this ILogReceiver receiver, Exception exception)
	{
		receiver.AddErrorLog(exception, null);
	}

	/// <summary>
	/// To record an error to the log.
	/// </summary>
	/// <param name="receiver">Logs receiver.</param>
	/// <param name="exception">Error details.</param>
	/// <param name="format">A format string.</param>
	public static void AddErrorLog(this ILogReceiver receiver, Exception exception, string format)
	{
		if (receiver == null)
			throw new ArgumentNullException(nameof(receiver));

		if (exception == null)
			throw new ArgumentNullException(nameof(exception));

		receiver.AddLog(new LogMessage(receiver, receiver.CurrentTimeUtc, LogLevels.Error, () =>
		{
			var msg = exception.ToString();

			if (exception is ReflectionTypeLoadException refExc)
			{
				msg += Environment.NewLine
					+ refExc
						.LoaderExceptions
						.Select(e => e.ToString())
						.JoinNL();
			}

			if (format != null)
				msg = format.Put(msg);

			return msg;
		}));
	}

	/// <summary>
	/// To record an error to the log.
	/// </summary>
	/// <param name="receiver">Logs receiver.</param>
	/// <param name="message">Text message.</param>
	/// <param name="args">Text message settings. Used if the message is a format string.</param>
	public static void AddErrorLog(this ILogReceiver receiver, string message, params object[] args)
	{
		receiver.AddMessage(LogLevels.Error, message, args);
	}

	private static void AddMessage(this ILogReceiver receiver, LogLevels level, string message, params object[] args)
	{
		if (receiver == null)
			throw new ArgumentNullException(nameof(receiver));

		if (level < receiver.LogLevel)
			return;

		receiver.AddLog(new LogMessage(receiver, receiver.CurrentTimeUtc, level, message, args));
	}

	/// <summary>
	/// To record an error to the <see cref="LogManager.Application"/>.
	/// </summary>
	/// <param name="error">Error.</param>
	/// <param name="format">A format string.</param>
	public static void LogError(this Exception error, string format = null)
	{
		if (error == null)
			throw new ArgumentNullException(nameof(error));

		LogManager.Instance?.Application.AddErrorLog(error, format);
	}

	/// <summary>
	/// Get <see cref="ILogSource.LogLevel"/> for the source. If the value is equal to <see cref="LogLevels.Inherit"/>,
	/// then the parental source level is taken.
	/// </summary>
	/// <param name="source">The log source.</param>
	/// <returns>The logging level.</returns>
	public static LogLevels GetLogLevel(this ILogSource source)
	{
		if (source == null)
			throw new ArgumentNullException(nameof(source));

		do
		{
			var level = source.LogLevel;

			if (level != LogLevels.Inherit)
				return level;

			source = source.Parent;
		}
		while (source != null);
		
		return LogLevels.Inherit;
	}

	/// <summary>
	/// Wrap the specified action in a try/catch clause with logging.
	/// </summary>
	/// <param name="action">The action to execute.</param>
	public static void DoWithLog(this Action action)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		try
		{
			action();
		}
		catch (Exception ex)
		{
			ex.LogError();
		}
	}

	/// <summary>
	/// Wrap the specified function in a try/catch clause with logging.
	/// </summary>
	/// <typeparam name="T">The type of the returned result.</typeparam>
	/// <param name="action">The function to execute.</param>
	/// <returns>The resulting value, or the default value of T if an error occurs.</returns>
	public static T DoWithLog<T>(this Func<T> action)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		try
		{
			return action();
		}
		catch (Exception ex)
		{
			ex.LogError();
			return default;
		}
	}

	/// <summary>
	/// Executes the function that returns a dictionary, logs any exceptions, and logs a specific error for each key/value pair.
	/// </summary>
	/// <typeparam name="T">The type of the dictionary key.</typeparam>
	/// <param name="action">The function to execute that returns a dictionary.</param>
	public static void DoWithLog<T>(Func<IDictionary<T, Exception>> action)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		try
		{
			var dict = action();

			foreach (var pair in dict)
			{
				new InvalidOperationException(pair.Key.ToString(), pair.Value).LogError("Corrupted file.");
			}
		}
		catch (Exception ex)
		{
			ex.LogError();
		}
	}

	/// <summary>
	/// The filter that only accepts messages of <see cref="LogLevels.Warning"/> type.
	/// </summary>
	public static readonly Func<LogMessage, bool> OnlyWarning = message => message.Level == LogLevels.Warning;

	/// <summary>
	/// The filter that only accepts messages of <see cref="LogLevels.Error"/> type.
	/// </summary>
	public static readonly Func<LogMessage, bool> OnlyError = message => message.Level == LogLevels.Error;

	/// <summary>
	/// Filters messages based on provided filters.
	/// </summary>
	/// <param name="messages">The collection of messages to filter.</param>
	/// <param name="filters">A collection of filter predicates to determine which messages to include.</param>
	/// <returns>An enumerable of filtered messages.</returns>
	public static IEnumerable<LogMessage> Filter(this IEnumerable<LogMessage> messages, ICollection<Func<LogMessage, bool>> filters)
	{
		if (filters.Count > 0)
			messages = messages.Where(m => filters.Any(f => f(m)));

		return messages;
	}

	/// <summary>
	/// Writes a single log message using the specified listener.
	/// </summary>
	/// <param name="listener">The log listener.</param>
	/// <param name="message">The log message to write.</param>
	public static void WriteMessage(this ILogListener listener, LogMessage message)
		=> listener.CheckOnNull(nameof(listener)).WriteMessages([message]);

	/// <summary>
	/// Continues the task, observing any errors and optionally executing the specified action.
	/// </summary>
	/// <param name="task">The task to observe.</param>
	/// <param name="observer">An action to handle exceptions if the task faults.</param>
	/// <param name="other">An optional action to execute if the task completes successfully.</param>
	/// <returns>A new task representing the continuation.</returns>
	public static Task ObserveError(this Task task, Action<Exception> observer, Action<Task> other = null)
	{
		if (task is null) throw new ArgumentNullException(nameof(task));
		if (observer is null) throw new ArgumentNullException(nameof(observer));

		return task.ContinueWith(t =>
		{
			// observe
			if (t.IsFaulted)
				observer(t.Exception);
			else
				other?.Invoke(t);
		});
	}

	/// <summary>
	/// Continues the generic task, observing any errors and optionally executing the specified action.
	/// </summary>
	/// <typeparam name="T">The type of the task result.</typeparam>
	/// <param name="task">The task to observe.</param>
	/// <param name="observer">An action to handle exceptions if the task faults.</param>
	/// <param name="other">An optional action to execute if the task completes successfully.</param>
	/// <returns>A new task representing the continuation.</returns>
	public static Task ObserveError<T>(this Task<T> task, Action<Exception> observer, Action<Task<T>> other = null)
	{
		if (task is null) throw new ArgumentNullException(nameof(task));
		if (observer is null) throw new ArgumentNullException(nameof(observer));

		return task.ContinueWith(t =>
		{
			// observe
			if (t.IsFaulted)
				observer(t.Exception);
			else
				other?.Invoke(t);
		});
	}

	/// <summary>
	/// Observes errors from the task and logs them.
	/// </summary>
	/// <param name="task">The task to observe.</param>
	/// <returns>A new task representing the continuation.</returns>
	public static Task ObserveErrorAndLog(this Task task)
		=> task.ObserveError(ex => ex.LogError());

	/// <summary>
	/// Observes errors from the task and traces them using <see cref="Trace.WriteLine(object)"/>.
	/// </summary>
	/// <param name="task">The task to observe.</param>
	/// <returns>A new task representing the continuation.</returns>
	public static Task ObserveErrorAndTrace(this Task task)
		=> task.ObserveError(ex => Trace.WriteLine(ex));
}