namespace Ecng.Logging;

using System.Reflection;
using System.Runtime.CompilerServices;

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

		receiver.AddLog(new LogMessage(receiver, receiver.CurrentTime, level, getMessage));
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

		receiver.AddLog(new LogMessage(receiver, receiver.CurrentTime, LogLevels.Error, () =>
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

		receiver.AddLog(new LogMessage(receiver, receiver.CurrentTime, level, message, args));
	}

	/// <summary>
	/// To record an error to the ambient <see cref="LogManager.Application"/>.
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
	/// <remarks>
	/// The answer is remembered per source and dropped as a whole whenever any source changes its
	/// <see cref="ILogSource.LogLevel"/> or <see cref="ILogSource.Parent"/>, so a chain that stands
	/// still costs a field read. A chain reaching a source that keeps either property outside
	/// <see cref="BaseLogSource"/> is remembered only up to that link: from there on the level is
	/// read again on every call, since it can move without the cache being told.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static LogLevels GetLogLevel(this ILogSource source)
	{
		if (source is BaseLogSource target)
		{
			var set = LogLevelCache.Current;
			var entry = target.LevelCache;

			if (entry is not null && ReferenceEquals(entry.Set, set))
				return entry.Level;

			var walk = target.WalkCache;

			// The settled prefix of the chain has already been walked once, so only the part whose
			// level has to be read again is left.
			if (walk is not null && ReferenceEquals(walk.Set, set))
				return Walk(walk.From);

			return WalkLogLevel(target, set);
		}

		return WalkForeign(source);
	}

	private static LogLevels WalkForeign(ILogSource source)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		// A source that is no BaseLogSource has nowhere to keep an answer, so it is walked the way
		// every source used to be.
		return Walk(source);
	}

	private static LogLevels Walk(ILogSource source)
	{
		do
		{
			var level = source.LogLevel;

			if (level != LogLevels.Inherit)
				return level;

			source = source.Parent;
		}
		while (source is not null);

		return LogLevels.Inherit;
	}

	private static LogLevels WalkLogLevel(BaseLogSource target, LogLevelEntry[] set)
	{
		if (!target.IsLevelCacheable)
		{
			// Its own level can move without the cache hearing about it, so the only thing worth
			// remembering is that the walk starts here - which stays true whatever the level does.
			target.SetCachedWalk(set, target);
			return Walk(target);
		}

		var current = target;

		while (true)
		{
			var level = current.LogLevel;

			if (level != LogLevels.Inherit)
				return Remember(target, current, set, level);

			var parent = current.Parent;

			if (parent is null)
				return Remember(target, current, set, LogLevels.Inherit);

			if (parent is not BaseLogSource next || !next.IsLevelCacheable)
			{
				// Everything walked so far is Inherit and cannot move without invalidating the set,
				// so that prefix is settled and only the rest is walked from now on.
				target.SetCachedWalk(set, parent);
				return Walk(parent);
			}

			// An ancestor that already answered in this set answers for the whole chain below it,
			// since every link in between is Inherit.
			var known = next.LevelCache;

			if (known is not null && ReferenceEquals(known.Set, set))
			{
				target.SetCachedLogLevel(known);
				return known.Level;
			}

			var shortcut = next.WalkCache;

			if (shortcut is not null && ReferenceEquals(shortcut.Set, set))
			{
				target.SetCachedWalk(set, shortcut.From);
				return Walk(shortcut.From);
			}

			current = next;
		}
	}

	private static LogLevels Remember(BaseLogSource target, BaseLogSource current, LogLevelEntry[] set, LogLevels level)
	{
		// Nothing above took part in this answer, so the link that gave it remembers it for itself
		// as well as for the source that asked.
		var entry = LogLevelCache.Entry(set, level);

		current.SetCachedLogLevel(entry);

		if (!ReferenceEquals(current, target))
			target.SetCachedLogLevel(entry);

		return level;
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
	/// Wrap the specified async action in a try/catch clause with logging.
	/// </summary>
	/// <param name="action">The async action to execute.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	public static async Task DoWithLogAsync(this Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		try
		{
			await action(cancellationToken);
		}
		catch (Exception ex)
		{
			if (!cancellationToken.IsCancellationRequested)
				ex.LogError();
		}
	}

	/// <summary>
	/// Wrap the specified async function in a try/catch clause with logging.
	/// </summary>
	/// <typeparam name="T">The type of the returned result.</typeparam>
	/// <param name="action">The async function to execute.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The resulting value, or the default value of T if an error occurs.</returns>
	public static async Task<T> DoWithLogAsync<T>(this Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		try
		{
			return await action(cancellationToken);
		}
		catch (Exception ex)
		{
			if (!cancellationToken.IsCancellationRequested)
				ex.LogError();

			return default;
		}
	}

	/// <summary>
	/// Wrap the specified async action in a try/catch clause with logging.
	/// </summary>
	/// <param name="action">The async action to execute.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	public static async ValueTask DoWithLogAsync(this Func<CancellationToken, ValueTask> action, CancellationToken cancellationToken = default)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		try
		{
			await action(cancellationToken);
		}
		catch (Exception ex)
		{
			if (!cancellationToken.IsCancellationRequested)
				ex.LogError();
		}
	}

	/// <summary>
	/// Wrap the specified async function in a try/catch clause with logging.
	/// </summary>
	/// <typeparam name="T">The type of the returned result.</typeparam>
	/// <param name="action">The async function to execute.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The resulting value, or the default value of T if an error occurs.</returns>
	public static async ValueTask<T> DoWithLogAsync<T>(this Func<CancellationToken, ValueTask<T>> action, CancellationToken cancellationToken = default)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		try
		{
			return await action(cancellationToken);
		}
		catch (Exception ex)
		{
			if (!cancellationToken.IsCancellationRequested)
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
			else if (t.IsCompletedSuccessfully)
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
			else if (t.IsCompletedSuccessfully)
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

	/// <summary>
	/// To record an error to the log.
	/// </summary>
	/// <param name="receiver">Logs receiver.</param>
	/// <param name="exception">Error details.</param>
	public static void LogError(this ILogReceiver receiver, Exception exception)
		=> receiver.AddErrorLog(exception);
}
