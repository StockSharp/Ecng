﻿namespace Ecng.Common;

#if !NET5_0_OR_GREATER
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

static class ProcessExtensions
{
	public static bool Associated(this Process process)
	{
		if (process is null)
			throw new ArgumentNullException(nameof(process));

		return process.Handle != default;
	}

	// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Diagnostics.Process/src/System/Diagnostics/Process.cs#L1422

	/// <summary>
	/// Instructs the Process component to wait for the associated process to exit, or
	/// for the <paramref name="cancellationToken"/> to be canceled.
	/// </summary>
	/// <returns>
	/// A task that will complete when the process has exited, cancellation has been requested,
	/// or an error occurs.
	/// </returns>
	public static async Task WaitForExitAsync(this Process process, CancellationToken cancellationToken = default)
	{
		if (process is null)
			throw new ArgumentNullException(nameof(process));

		// Because the process has already started by the time this method is called,
		// we're in a race against the process to set up our exit handlers before the process
		// exits. As a result, there are several different flows that must be handled:
		//
		// CASE 1: WE ENABLE EVENTS
		// This is the "happy path". In this case we enable events.
		//
		// CASE 1.1: PROCESS EXITS OR IS CANCELED AFTER REGISTERING HANDLER
		// This case continues the "happy path". The process exits or waiting is canceled after
		// registering the handler and no special cases are needed.
		//
		// CASE 1.2: PROCESS EXITS BEFORE REGISTERING HANDLER
		// It's possible that the process can exit after we enable events but before we reigster
		// the handler. In that case we must check for exit after registering the handler.
		//
		//
		// CASE 2: PROCESS EXITS BEFORE ENABLING EVENTS
		// The process may exit before we attempt to enable events. In that case EnableRaisingEvents
		// will throw an exception like this:
		//     System.InvalidOperationException : Cannot process request because the process (42) has exited.
		// In this case we catch the InvalidOperationException. If the process has exited, our work
		// is done and we return. If for any reason (now or in the future) enabling events fails
		// and the process has not exited, bubble the exception up to the user.
		//
		//
		// CASE 3: USER ALREADY ENABLED EVENTS
		// In this case the user has already enabled raising events. Re-enabling events is a no-op
		// as the value hasn't changed. However, no-op also means that if the process has already
		// exited, EnableRaisingEvents won't throw an exception.
		//
		// CASE 3.1: PROCESS EXITS OR IS CANCELED AFTER REGISTERING HANDLER
		// (See CASE 1.1)
		//
		// CASE 3.2: PROCESS EXITS BEFORE REGISTERING HANDLER
		// (See CASE 1.2)

		if (!process.Associated())
		{
			throw new InvalidOperationException("Not associated.");
		}

		if (!process.HasExited)
		{
			// Early out for cancellation before doing more expensive work
			cancellationToken.ThrowIfCancellationRequested();
		}

		try
		{
			// CASE 1: We enable events
			// CASE 2: Process exits before enabling events (and throws an exception)
			// CASE 3: User already enabled events (no-op)
			process.EnableRaisingEvents = true;
		}
		catch (InvalidOperationException)
		{
			// CASE 2: If the process has exited, our work is done, otherwise bubble the
			// exception up to the user
			if (process.HasExited)
			{
				await WaitUntilOutputEOF(cancellationToken).NoWait();
				return;
			}

			throw;
		}

		var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

		EventHandler handler = (_, _) => tcs.TrySetResult(true);
		process.Exited += handler;

		try
		{
			if (process.HasExited)
			{
				// CASE 1.2 & CASE 3.2: Handle race where the process exits before registering the handler
			}
			else
			{
				// CASE 1.1 & CASE 3.1: Process exits or is canceled here
				using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken), false))
				{
					await tcs.Task.NoWait();
				}
			}

			// Wait until output streams have been drained
			await WaitUntilOutputEOF(cancellationToken).NoWait();
		}
		finally
		{
			process.Exited -= handler;
		}

		async Task WaitUntilOutputEOF(CancellationToken cancellationToken)
		{
			var tasks = new List<Task>(2);

			if (process.StartInfo.RedirectStandardOutput)
				tasks.Add(DrainStreamAsync(process.StandardOutput, cancellationToken));

			if (process.StartInfo.RedirectStandardError)
				tasks.Add(DrainStreamAsync(process.StandardError, cancellationToken));

			if (tasks.Count > 0)
				await Task.WhenAll(tasks).NoWait();

			static async Task DrainStreamAsync(StreamReader reader, CancellationToken token)
			{
				var buffer = new char[1024];

				while (!reader.EndOfStream && !token.IsCancellationRequested)
					await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
			}
		}
	}
}
#endif
