namespace Ecng.Interop.Dde
{
	using System;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;

	using Ecng.Common;
	using Ecng.Serialization;

	[DisplayName("DDE settings")]
	public class DdeSettings : Cloneable<DdeSettings>, IPersistable
	{
		public DdeSettings()
		{
			Server = "EXCEL";
			Topic = "[Book1.xlsx]Sheet1";
		}

		[Display(Name = "Server", Description = "DDE server name.", Order = 0)]
		public string Server { get; set; }

		[Display(Name = "Topic", Description = "Topic name (like [Book1.xlsx].Sheet1).", Order = 1)]
		public string Topic { get; set; }

		private int _columnOffset;

		[Display(Name = "Column offset", Description = "Column offset from left top corner.", Order = 2)]
		public int ColumnOffset
		{
			get { return _columnOffset; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException();

				_columnOffset = value;
			}
		}

		private int _rowOffset;

		[Display(Name = "Row offset", Description = "Row offset from left top corner.", Order = 2)]
		public int RowOffset
		{
			get { return _rowOffset; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException();

				_rowOffset = value;
			}
		}

		[Display(Name = "Headers", Description = "Show headers name.", Order = 2)]
		public bool ShowHeaders { get; set; }

		public void Apply(DdeSettings clone)
		{
			PersistableHelper.Apply(this, clone);
		}

		public override DdeSettings Clone()
		{
			return PersistableHelper.Clone(this);
		}

		public void Load(SettingsStorage storage)
		{
			Server = storage.GetValue<string>(nameof(Server));
			Topic = storage.GetValue<string>(nameof(Topic));
			ColumnOffset = storage.GetValue<int>(nameof(ColumnOffset));
			RowOffset = storage.GetValue<int>(nameof(RowOffset));
			ShowHeaders = storage.GetValue<bool>(nameof(ShowHeaders));
		}

		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Server), Server);
			storage.SetValue(nameof(Topic), Topic);
			storage.SetValue(nameof(ColumnOffset), ColumnOffset);
			storage.SetValue(nameof(RowOffset), RowOffset);
			storage.SetValue(nameof(ShowHeaders), ShowHeaders);
		}
	}
}