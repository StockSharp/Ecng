namespace Ecng.Web.UI
{
	using System;
	using System.Collections;
	using System.Web.UI;
	using System.Data;
	using System.Reflection;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Reflection;

	public class EnumDataSourceView : DataSourceView
	{
		public EnumDataSourceView(EnumDataSource owner, string name, Type enumType)
			: base(owner, name)
		{
			if (enumType == null)
				throw new ArgumentNullException(nameof(enumType));

			EnumType = enumType;
		}

		public Type EnumType { get; }

		protected override IEnumerable ExecuteSelect(DataSourceSelectArguments arguments)
		{
			var table = new DataTable();
			table.Columns.Add(new DataColumn("Name", typeof(string)));
			table.Columns.Add(new DataColumn("Value", typeof(object)));
			table.Columns.Add(new DataColumn("DisplayName", typeof(string)));
			table.Columns.Add(new DataColumn("Description", typeof(string)));

			foreach (var value in EnumType.GetValues())
			{
				var name = value.To<Enum>().GetName();

				var info = EnumType.GetMember<FieldInfo>(name);

				var displayName = info.GetDisplayName();

				if (displayName == null)
				{
					displayName = name;

					for (var i = name.Length - 1; i > 2; i--)
					{
						if (char.IsUpper(displayName[i]))
							displayName = displayName.Insert(i, " ");
					}
				}

				table.Rows.Add(name, value, displayName, info.GetDescription());
			}

			table.AcceptChanges();

			var view = new DataView(table);

			if (!arguments.SortExpression.IsEmpty())
				view.Sort = arguments.SortExpression;

			return view;
		}
	}
}