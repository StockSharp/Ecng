namespace Ecng.Tests.Logging;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Ecng.Logging;
using Ecng.Serialization;
using Ecng.Server.Utils;

using Microsoft.Extensions.Logging;

/// <summary>
/// Routing what the framework logger is given into the log the application actually keeps.
///
/// A component written against <see cref="ILogger{TCategoryName}"/> otherwise writes to whatever providers the
/// host installed, which in a Windows service is nowhere anybody reads; and its silence looks exactly like a
/// component that never ran.
/// </summary>
[TestClass]
public class LogManagerLoggerProviderTests : BaseTestClass
{
	private sealed class CapturingListener : ILogListener
	{
		public ConcurrentQueue<LogMessage> Messages { get; } = new();

		bool ILogListener.CanSave => false;
		void IDisposable.Dispose() { }
		void IPersistable.Load(SettingsStorage storage) { }
		void IPersistable.Save(SettingsStorage storage) { }

		void ILogListener.WriteMessages(IEnumerable<LogMessage> messages)
		{
			foreach (var m in messages)
				Messages.Enqueue(m);
		}
	}

	private static (LogManager manager, CapturingListener listener) CreateManager()
	{
		var manager = new LogManager { Application = { LogLevel = LogLevels.Verbose }, FlushInterval = TimeSpan.FromMilliseconds(10) };
		var listener = new CapturingListener();
		manager.Listeners.Add(listener);
		return (manager, listener);
	}

	/// <summary>The log hands messages to its listeners on its own timer, so a reader has to let it.</summary>
	private static void WaitFor(CapturingListener listener, Func<bool> condition)
	{
		for (var i = 0; i < 200 && !condition(); i++)
			Thread.Sleep(10);
	}

	[TestMethod]
	public void WhatTheLoggerIsGivenReachesTheLog()
	{
		var (manager, listener) = CreateManager();
		using var provider = new LogManagerLoggerProvider(manager);

		provider.CreateLogger("Worker").LogInformation("started");

		WaitFor(listener, () => listener.Messages.Any(m => m.Message == "started"));

		var message = listener.Messages.FirstOrDefault(m => m.Message == "started");

		IsNotNull(message, "a line written through the framework logger never reached the log");
		AreEqual(LogLevels.Info, message.Level);
	}

	/// <summary>Each category is its own source, so a line says which part of the service wrote it.</summary>
	[TestMethod]
	public void EachCategoryIsItsOwnSource()
	{
		var (manager, listener) = CreateManager();
		using var provider = new LogManagerLoggerProvider(manager);

		provider.CreateLogger("Worker").LogInformation("from the worker");
		provider.CreateLogger("CpuMonitor").LogInformation("from the monitor");

		WaitFor(listener, () => listener.Messages.Count >= 2);

		AreEqual("Worker", listener.Messages.First(m => m.Message == "from the worker").Source.Name);
		AreEqual("CpuMonitor", listener.Messages.First(m => m.Message == "from the monitor").Source.Name);
	}

	[TestMethod]
	public void TheSameCategoryKeepsTheSameSource()
	{
		var (manager, _) = CreateManager();
		using var provider = new LogManagerLoggerProvider(manager);

		provider.CreateLogger("Worker").LogInformation("one");
		provider.CreateLogger("Worker").LogInformation("two");

		AreEqual(1, manager.Sources.Count(s => s.Name == "Worker"), "one category grew more than one source");
	}

	[TestMethod]
	public void LevelsKeepTheirMeaning()
	{
		var (manager, listener) = CreateManager();
		using var provider = new LogManagerLoggerProvider(manager);

		var logger = provider.CreateLogger("Worker");

		logger.LogTrace("t");
		logger.LogDebug("d");
		logger.LogInformation("i");
		logger.LogWarning("w");
		logger.LogError("e");
		logger.LogCritical("c");

		WaitFor(listener, () => listener.Messages.Count >= 6);

		LogLevels levelOf(string text) => listener.Messages.First(m => m.Message == text).Level;

		AreEqual(LogLevels.Verbose, levelOf("t"));
		AreEqual(LogLevels.Debug, levelOf("d"));
		AreEqual(LogLevels.Info, levelOf("i"));
		AreEqual(LogLevels.Warning, levelOf("w"));
		AreEqual(LogLevels.Error, levelOf("e"));
		AreEqual(LogLevels.Error, levelOf("c"), "a critical line must not be quieter than an error");
	}

	/// <summary>An error line without its exception is an error nobody can act on.</summary>
	[TestMethod]
	public void AnExceptionIsCarriedIntoTheLog()
	{
		var (manager, listener) = CreateManager();
		using var provider = new LogManagerLoggerProvider(manager);

		provider.CreateLogger("Worker").LogError(new InvalidOperationException("boom"), "sample failed");

		WaitFor(listener, () => listener.Messages.Any(m => m.Level == LogLevels.Error));

		var message = listener.Messages.First(m => m.Level == LogLevels.Error);

		IsTrue(message.Message.Contains("sample failed", StringComparison.Ordinal), message.Message);
		IsTrue(message.Message.Contains("boom", StringComparison.Ordinal), "the exception did not travel with the line");
	}

	/// <summary>
	/// Forwarding both ways at once would make every message loop between the two logs forever, so a manager
	/// that already writes into the framework logger cannot also be fed from it.
	/// </summary>
	[TestMethod]
	public void FeedingBothWaysIsRefused()
	{
		var (manager, _) = CreateManager();
		manager.Listeners.Add(new ServiceLogListener(new NullLogger()));

		Throws<InvalidOperationException>(() => new LogManagerLoggerProvider(manager));
	}

	/// <summary>
	/// A category is a full type name, and a log column is not the place for one: what a reader needs is
	/// which part of the service wrote the line.
	/// </summary>
	[TestMethod]
	public void ASourceIsNamedAfterTheTypeAloneNotItsNamespace()
	{
		var (manager, listener) = CreateManager();
		using var provider = new LogManagerLoggerProvider(manager);

		provider.CreateLogger("StockSharp.Web.Servers.Mail.CpuMonitorService").LogInformation("shortened");

		WaitFor(listener, () => listener.Messages.Any(m => m.Message == "shortened"));

		AreEqual("CpuMonitorService", listener.Messages.First(m => m.Message == "shortened").Source.Name);
	}

	/// <summary>Shortening must not make two different things look like one.</summary>
	[TestMethod]
	public void TwoTypesOfTheSameNameStaySeparate()
	{
		var (manager, listener) = CreateManager();
		using var provider = new LogManagerLoggerProvider(manager);

		provider.CreateLogger("StockSharp.Web.Servers.Mail.Worker").LogInformation("from mail");
		provider.CreateLogger("StockSharp.Web.Servers.Video.Worker").LogInformation("from video");

		WaitFor(listener, () => listener.Messages.Count >= 2);

		var mail = listener.Messages.First(m => m.Message == "from mail").Source.Name;
		var video = listener.Messages.First(m => m.Message == "from video").Source.Name;

		AreEqual("Worker", mail);
		AreNotEqual(mail, video, "two workers from different services share one name in the log");
		IsTrue(video.EndsWithIgnoreCase("Worker"), video);
	}

	/// <summary>One log file, one clock: the rest of it is stamped in UTC.</summary>
	[TestMethod]
	public void LinesAreStampedOnTheSameClockAsTheRest()
	{
		var (manager, listener) = CreateManager();
		using var provider = new LogManagerLoggerProvider(manager);

		var before = DateTime.UtcNow.AddSeconds(-5);

		provider.CreateLogger("Worker").LogInformation("stamped");

		WaitFor(listener, () => listener.Messages.Any(m => m.Message == "stamped"));

		var time = listener.Messages.First(m => m.Message == "stamped").Time;

		IsTrue(time >= before && time <= DateTime.UtcNow.AddSeconds(5),
			$"the line is stamped on another clock: {time:O} against {DateTime.UtcNow:O}");
	}

	/// <summary>
	/// Recording an exception that came without a message of its own. Every framework overload that takes an
	/// exception also demands a message, so a catch block with nothing to add would have to invent one.
	/// </summary>
	[TestMethod]
	public void TheExceptionIsRecordedWithoutAnInventedMessage()
	{
		var logger = new CapturingLogger();
		var boom = new InvalidOperationException("boom");

		logger.LogError(boom);

		AreEqual(LogLevel.Error, logger.Level);
		AreSame(boom, logger.Exception);
		AreEqual(string.Empty, logger.Text, "a message was invented for an exception that came without one");
	}

	[TestMethod]
	public void NothingIsRecordedForNothing()
	{
		Throws<ArgumentNullException>(() => new CapturingLogger().LogError((Exception)null));
		Throws<ArgumentNullException>(() => ((ILogger)null).LogError(new Exception()));
	}

	private sealed class CapturingLogger : ILogger
	{
		public LogLevel Level { get; private set; }
		public Exception Exception { get; private set; }
		public string Text { get; private set; }

		IDisposable ILogger.BeginScope<TState>(TState state) => null;
		bool ILogger.IsEnabled(LogLevel logLevel) => true;

		void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			Level = logLevel;
			Exception = exception;
			Text = formatter(state, exception);
		}
	}

	private sealed class NullLogger : ILogger
	{
		IDisposable ILogger.BeginScope<TState>(TState state) => null;
		bool ILogger.IsEnabled(LogLevel logLevel) => true;
		void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
	}
}
