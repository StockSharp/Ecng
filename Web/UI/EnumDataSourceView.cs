namespace Ecng.Web.UI
{
	using System;
	using System.Collections;
	using System.Web.UI;
	using System.Data;
	using System.Reflection;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.Reflection;

	public class EnumDataSourceView : DataSourceView
	{
		public EnumDataSourceView(EnumDataSource owner, string name, Type enumType)
			: base(owner, name)
		{
			if (enumType == null)
				throw new ArgumentNullException("enumType");

			EnumType = enumType;
		}

		public Type EnumType { get; private set; }

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

				string displayName;
				var nameAttr = info.GetAttribute<DisplayNameAttribute>();
				if (nameAttr != null)
					displayName = nameAttr.DisplayName;
				else
				{
					displayName = name;

					for (var i = name.Length - 1; i > 2; i--)
					{
						if (char.IsUpper(displayName[i]))
							displayName = displayName.Insert(i, " ");
					}
				}

				var descAttr = info.GetAttribute<DescriptionAttribute>();
				var description = descAttr != null ? descAttr.Description : null;

				table.Rows.Add(name, value, displayName, description);
			}

			table.AcceptChanges();

			var view = new DataView(table);

			if (!arguments.SortExpression.IsEmpty())
				view.Sort = arguments.SortExpression;

			return view;
		}
	}
}