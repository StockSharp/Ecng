namespace Ecng.Interop.Dde
{
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.Serialization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	public class DdeSettings : Cloneable<DdeSettings>, IPersistable
	{
		public DdeSettings()
		{
			Server = "EXCEL";
			Topic = "[Book1.xlsx].Sheet1";
		}

		[DisplayName("Сервер")]
		[Description("Название DDE сервера.")]
		[PropertyOrder(0)]
		public string Server { get; set; }

		[DisplayName("Топик")]
		[Description("Название топика (например, для Экселя имеет формат [Book Name].Sheet Name).")]
		[PropertyOrder(1)]
		public string Topic { get; set; }

		[DisplayName("Отступ колонки")]
		[Description("Отступ колонки от левого верхнего угла.")]
		[PropertyOrder(2)]
		public int ColumnOffset { get; set; }

		[DisplayName("Отступ строчки")]
		[Description("Отступ строчки от левого верхнего угла.")]
		[PropertyOrder(3)]
		public int RowOffset { get; set; }

		[DisplayName("Заголовки")]
		[Description("Выводить ли названия колонок.")]
		[PropertyOrder(4)]
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
			Server = storage.GetValue<string>("Service");
			Topic = storage.GetValue<string>("Topic");
			ColumnOffset = storage.GetValue<int>("ColumnOffset");
			RowOffset = storage.GetValue<int>("RowOffset");
			ShowHeaders = storage.GetValue<bool>("ShowHeaders");
		}

		public void Save(SettingsStorage storage)
		{
			storage.SetValue("Service", Server);
			storage.SetValue("Topic", Topic);
			storage.SetValue("ColumnOffset", ColumnOffset);
			storage.SetValue("RowOffset", RowOffset);
			storage.SetValue("ShowHeaders", ShowHeaders);
		}
	}
}