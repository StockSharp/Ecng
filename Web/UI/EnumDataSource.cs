namespace Ecng.Web.UI
{
	using System;
	using System.Collections;
	using System.ComponentModel;
	using System.Web.UI;

	using Ecng.Common;

	[PersistChildren(false)]
	[ParseChildren(true)]
	[DefaultProperty("RootType")]
	[ToolboxData("<{0}:EnumDataSource runat=\"server\"></{0}:EnumDataSource>")]
	public class EnumDataSource : DataSourceControl
	{
		private EnumDataSourceView _view;

		public string EnumType { get; set; }

		#region DataSourceControl Members

		protected override DataSourceView GetView(string viewName)
		{
			return _view ?? (_view = new EnumDataSourceView(this, viewName, EnumType.To<Type>()));
		}

		protected override ICollection GetViewNames()
		{
			return new [] { "DefaultView" };
		}

		#endregion
	}
}