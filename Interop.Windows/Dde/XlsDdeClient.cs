namespace Ecng.Interop.Dde
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using NDde.Client;

	public class XlsDdeClient : Disposable
	{
		private DdeClient _client;

		public XlsDdeClient(DdeSettings settings)
		{
			Settings = settings ?? throw new ArgumentNullException(nameof(settings));
		}

		public bool IsStarted => _client != null;

		public DdeSettings Settings { get; }

		public void Start()
		{
			_client = new DdeClient(Settings.Server, Settings.Topic);
			_client.Connect();
		}

		public void Stop()
		{
			if (_client.IsConnected)
				_client.Disconnect();

			_client = null;
		}

		public void Poke(IList<IList<object>> rows)
		{
			if (rows is null)
				throw new ArgumentNullException(nameof(rows));

			if (rows.Count == 0)
				throw new ArgumentOutOfRangeException(nameof(rows));

			if (!Settings.ShowHeaders)
				rows.RemoveAt(0);

			var rowStart = 1 + Settings.RowOffset;
			var columnStart = 1 + Settings.ColumnOffset;
			var colCount = rows.Count == 0 ? 0 : rows[0].Count;

			_client.Poke($"R{rowStart}C{columnStart}:R{rowStart + rows.Count}C{columnStart + colCount}",
				XlsDdeSerializer.Serialize(rows), 0x0090 | 0x4000, (int)TimeSpan.FromSeconds(10).TotalMilliseconds);
		}

		protected override void DisposeManaged()
		{
			if (IsStarted)
				Stop();

			base.DisposeManaged();
		}
	}
}