namespace Ecng.Server.Utils;

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

using Ecng.Collections;
using Ecng.Common;
using Ecng.Logging;

/// <summary>
/// Writes what components log through <see cref="ILogger"/> into a <see cref="LogManager"/>, giving each
/// logger category a source of its own.
/// </summary>
/// <remarks>
/// A service that keeps its own log gets two of them otherwise: the one it writes deliberately, and whatever
/// the host does with <see cref="ILogger"/> — which for a Windows service is nowhere anybody reads. A
/// component then appears never to have started, because a component that says nothing and a component that
/// is not running look the same from outside. With this provider installed, code written against the
/// framework logger — including code that is not ours — lands in the same file as everything else, under the
/// name of the category that wrote it.
/// </remarks>
public class LogManagerLoggerProvider : ILoggerProvider
{
	private sealed class CategorySource : BaseLogReceiver
	{
		public CategorySource(string name)
		{
			Name = name;
		}
	}

	private sealed class CategoryLogger(ILogReceiver receiver) : ILogger
	{
		private readonly ILogReceiver _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));

		IDisposable ILogger.BeginScope<TState>(TState state) => null;

		bool ILogger.IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

		void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			if (logLevel == LogLevel.None)
				return;

			var text = formatter?.Invoke(state, exception) ?? state?.ToString();

			// The exception is the half of an error worth acting on, and the framework keeps it beside the
			// text rather than in it.
			if (exception is not null)
				text = text.IsEmpty() ? exception.ToString() : $"{text}{Environment.NewLine}{exception}";

			if (text.IsEmpty())
				return;

			// The rest of the log is stamped in UTC; a line from here must read on the same clock.
			_receiver.AddLog(new LogMessage(_receiver, DateTime.UtcNow, ToLevel(logLevel), text));
		}

		private static LogLevels ToLevel(LogLevel level) => level switch
		{
			LogLevel.Trace => LogLevels.Verbose,
			LogLevel.Debug => LogLevels.Debug,
			LogLevel.Information => LogLevels.Info,
			LogLevel.Warning => LogLevels.Warning,
			_ => LogLevels.Error,
		};
	}

	private readonly LogManager _logManager;
	private readonly SynchronizedDictionary<string, CategorySource> _sources = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly SynchronizedSet<string> _takenNames = new(StringComparer.InvariantCultureIgnoreCase);

	/// <summary>
	/// Initializes a new instance of the <see cref="LogManagerLoggerProvider"/>.
	/// </summary>
	/// <param name="logManager">The log to write into.</param>
	/// <exception cref="InvalidOperationException">
	/// The log already writes into <see cref="ILogger"/> through a <see cref="ServiceLogListener"/>. Carrying
	/// messages both ways would put every one of them in a loop, so the direction has to be chosen: build the
	/// log without a logger to feed it from one.
	/// </exception>
	public LogManagerLoggerProvider(LogManager logManager)
	{
		_logManager = logManager ?? throw new ArgumentNullException(nameof(logManager));

		if (_logManager.Listeners.OfType<ServiceLogListener>().Any())
			throw new InvalidOperationException("The log already writes into ILogger; feeding it from ILogger as well would loop.");
	}

	ILogger ILoggerProvider.CreateLogger(string categoryName) => CreateLogger(categoryName);

	/// <summary>
	/// The logger for a category, backed by that category's own source.
	/// </summary>
	/// <param name="categoryName">Category name, normally the full name of the type that logs.</param>
	/// <returns>Logger.</returns>
	public ILogger CreateLogger(string categoryName)
	{
		if (categoryName.IsEmpty())
			throw new ArgumentNullException(nameof(categoryName));

		var source = _sources.SafeAdd(categoryName, name =>
		{
			var created = new CategorySource(ShortestFreeName(name));
			_logManager.Sources.Add(created);
			return created;
		});

		return new CategoryLogger(source);
	}

	/// <summary>
	/// The shortest tail of a category that no other source is already using.
	/// </summary>
	/// <remarks>
	/// A category is the full name of the type that logs, and a name column is not the place for one: what
	/// a reader wants is which part of the service wrote the line. The tail alone usually says that -- but
	/// two services can both have a Worker, and a log where two things look like one is worse than a long
	/// name, so a taken name grows by a segment until it is its own.
	/// </remarks>
	private string ShortestFreeName(string categoryName)
	{
		var parts = categoryName.Split('.');

		for (var take = 1; take < parts.Length; take++)
		{
			var candidate = parts.Skip(parts.Length - take).JoinDot();

			if (!_takenNames.Contains(candidate))
			{
				_takenNames.Add(candidate);
				return candidate;
			}
		}

		_takenNames.Add(categoryName);
		return categoryName;
	}

	void IDisposable.Dispose()
	{
		GC.SuppressFinalize(this);

		foreach (var source in _sources.SyncGet(d => d.Values.ToArray()))
			_logManager.Sources.Remove(source);

		_sources.Clear();
	}
}
