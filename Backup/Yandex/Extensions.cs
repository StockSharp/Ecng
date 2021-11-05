namespace Ecng.Backup.Yandex
{
	using System;
	using System.IO;

	using Disk.SDK;

	using Ecng.Common;
	using Ecng.Reflection;

	static class Extensions
	{
		[Obsolete]
		public static void UndoDispose(this MemoryStream stream)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));

			stream.SetValue("_isOpen", true);
			stream.SetValue("_writable", true);
			stream.SetValue("_expandable", true);
		}

		public static TResult AsyncWait<TArg, TResult>(this DiskSdkClient client, string eventName, Action action, Func<TArg, TResult> process)
			where TArg : SdkEventArgs
		{
			if (client is null)
				throw new ArgumentNullException(nameof(client));

			if (action is null)
				throw new ArgumentNullException(nameof(action));

			if (eventName.IsEmpty())
				throw new ArgumentNullException(nameof(eventName));

			if (process is null)
				throw new ArgumentNullException(nameof(process));

			var sync = new SyncObject();
			var pulsed = false;
			Exception error = null;
			TResult result = default;

			EventHandler<TArg> handler = (s, e) =>
			{
				if (e.Error != null)
					error = e.Error;
				else
					result = process(e);

				lock (sync)
				{
					pulsed = true;
					sync.Pulse();
				}
			};

			var evt = typeof(DiskSdkClient).GetEvent(eventName);

			evt.AddEventHandler(client, handler);

			try
			{
				action();

				lock (sync)
				{
					if (!pulsed)
						sync.Wait();
				}
			}
			finally
			{
				evt.RemoveEventHandler(client, handler);
			}

			error?.Throw();

			return result;
		}
	}
}