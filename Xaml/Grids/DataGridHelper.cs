namespace Ecng.Xaml.Grids
{
	using System;
	using System.IO;
	using System.Reflection;
	using System.Windows.Controls;
	using System.Windows.Data;
	using System.Windows;
	using System.Windows.Markup;
	using System.Windows.Media;
	using System.Xml;

	using Ecng.Collections;

	public static class DataGridHelper
	{
		public static DataGridColumn AddTextColumn(this DataGrid dataGrid, string fieldName, string title)
		{
			var column = CreateTextColumn(fieldName, title);
			dataGrid.Columns.Add(column);
			return column;
		}

		public static void AddTemplateColumn(
			this DataGrid dataGrid, string fieldName, string title,
			Type cellControl, DependencyProperty cellControlBindingProperty,
			Type cellEditingControl, DependencyProperty cellEditingControlBindingProperty)
		{
			var cellTemplate = CreateDataTemplate(cellControl, cellControlBindingProperty, fieldName);
			var cellEditingTemplate = CreateDataTemplate(cellEditingControl, cellEditingControlBindingProperty, fieldName);
			dataGrid.Columns.Add(CreateTemplateColumn(fieldName, title, cellTemplate, cellEditingTemplate));
		}

		private static DataTemplate CreateDataTemplate(Type controlType, DependencyProperty cellControlBindingProperty, string fieldName)
		{
			var fefactory = new FrameworkElementFactory(controlType);
			var placeBinding = new Binding();
			fefactory.SetBinding(cellControlBindingProperty, placeBinding);
			placeBinding.Path = new PropertyPath(fieldName);
			placeBinding.NotifyOnTargetUpdated = true;
			placeBinding.NotifyOnSourceUpdated = true;
			placeBinding.Mode = BindingMode.TwoWay;
			placeBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
			return new DataTemplate
			{
				VisualTree = fefactory
			};
		}

		private static DataGridTextColumn CreateTextColumn(string fieldName, string title)
		{
			return new DataGridTextColumn
			{
				Header = title,
				Binding = new Binding(fieldName),
			};
		}

		private static DataGridTemplateColumn CreateTemplateColumn(string fieldName, string title, DataTemplate cellTemplate, DataTemplate cellEditingTemplate)
		{
			return new DataGridTemplateColumn
			{
				Header = title,
				CellTemplate = cellTemplate,
				CellEditingTemplate = cellEditingTemplate,
				SortMemberPath = fieldName
			};
		}

		public static T GetVisualChild<T>(Visual parent)
			where T : Visual
		{
			var child = default(T);
			var numVisuals = VisualTreeHelper.GetChildrenCount(parent);

			for (var i = 0; i < numVisuals; i++)
			{
				var v = (Visual)VisualTreeHelper.GetChild(parent, i);

				child = v as T ?? GetVisualChild<T>(v);

				if (child != null)
				{
					break;
				}
			}

			return child;
		}

		public static object GetValueFromCell(this DataGridCell cell)
		{
			return cell.Do((prop, obj) => prop == null ? null : prop.GetValue(obj, null));
		}

		public static void SetValueToCell(this DataGridCell cell, object value)
		{
			if (cell == null)
				throw new ArgumentNullException("cell");

			cell.Do((prop, obj) =>
			{
				if (prop.CanWrite)
					prop.SetValue(obj, value, null);
				return null;
			});
		}

		private static object Do(this DataGridCell cell, Func<PropertyInfo, object, object> func)
		{
			if (cell == null)
				throw new ArgumentNullException("cell");

			if (func == null)
				throw new ArgumentNullException("func");

			var obj = cell.DataContext;
			var t = obj.GetType();
			var p = t.GetProperty(cell.Column.SortMemberPath);

			return func(p, obj);
		}

		public static T XamlClone<T>(this T xamlObj)
		{
			return (T)XamlReader.Load(XmlReader.Create(new StringReader(XamlWriter.Save(xamlObj))));
		}

		public static Style XamlClone(this Style xamlObj)
		{
			var newStyle = XamlClone<Style>(xamlObj);

			if (xamlObj.TargetType != newStyle.TargetType)
			{
				newStyle.TargetType = xamlObj.TargetType;

				newStyle.Setters.Clear();
				newStyle.Setters.AddRange(xamlObj.Setters);

				newStyle.Triggers.Clear();
				newStyle.Triggers.AddRange(xamlObj.Triggers);
			}

			return newStyle;
		}
	}
}