namespace Ecng.Xaml.Grids
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Globalization;
	using System.Reflection;
	using System.Windows;
	using System.Windows.Data;

	static class GroupItemHelper
	{
		private static object GetParent(object groupItem)
		{
			try
			{
				return groupItem
					.GetType()
					.GetProperty("Parent", BindingFlags.NonPublic | BindingFlags.Instance)
					.GetGetMethod(true)
					.Invoke(groupItem, BindingFlags.NonPublic | BindingFlags.Instance, null, new object[0], System.Globalization.CultureInfo.CurrentCulture);
			}
			catch (Exception)
			{
				return null;
			}
		}

		public static PropertyGroupDescriptionEx GetPropertyGroupDescription(this object groupItem)
		{
			var parent = GetParent(groupItem);

			return parent == null
			       	? null
			       	: parent
			       	  	.GetType()
			       	  	.GetProperty("GroupBy", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true)
			       	  	.Invoke(parent, BindingFlags.NonPublic | BindingFlags.Instance, null, new object[0], System.Globalization.CultureInfo.CurrentCulture) as PropertyGroupDescriptionEx;
		}

		public static IList<GroupDescription> GetPropertyGroupDescriptions(this object groupItem)
		{
			object parent = null;

			var tmp = GetParent(groupItem);
			while (tmp != null)
			{
				parent = tmp;
				tmp = GetParent(tmp);
			}

			return parent == null
			       	? new List<GroupDescription>()
			       	: parent
			       	  	.GetType()
			       	  	.GetProperty("GroupDescriptions", BindingFlags.Public | BindingFlags.Instance)
			       	  	.GetGetMethod(true)
			       	  	.Invoke(parent, BindingFlags.Instance, null, new object[0], System.Globalization.CultureInfo.CurrentCulture) as IList<GroupDescription>;
		}
	}

	class GroupItemToGroupHeaderConverter : IValueConverter
	{
		#region Implementation of IValueConverter

		/// <summary>
		/// Converts a value. 
		/// </summary>
		/// <returns>
		/// A converted value. If the method returns null, the valid null value is used.
		/// </returns>
		/// <param name="value">The value produced by the binding source.</param><param name="targetType">The type of the binding target property.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var description = value.GetPropertyGroupDescription();

			return description != null 
				? description.Header 
				: value;
		}

		/// <summary>
		/// Converts a value. 
		/// </summary>
		/// <returns>
		/// A converted value. If the method returns null, the valid null value is used.
		/// </returns>
		/// <param name="value">The value that is produced by the binding target.</param><param name="targetType">The type to convert to.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}

	class GroupItemToLeftMarginConverter : IValueConverter
	{
		#region Implementation of IValueConverter

		/// <summary>
		/// Converts a value. 
		/// </summary>
		/// <returns>
		/// A converted value. If the method returns null, the valid null value is used.
		/// </returns>
		/// <param name="value">The value produced by the binding source.</param><param name="targetType">The type of the binding target property.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var collection = value.GetPropertyGroupDescriptions();
			var description = value.GetPropertyGroupDescription();

			var thicknes = parameter as string;
			var margin = String.IsNullOrWhiteSpace(thicknes)
			             	? new Thickness(1)
							: (Thickness)new ThicknessConverter().ConvertFromInvariantString(thicknes);

			margin.Left = margin.Left * collection.IndexOf(description);

			return margin;
		}

		/// <summary>
		/// Converts a value. 
		/// </summary>
		/// <returns>
		/// A converted value. If the method returns null, the valid null value is used.
		/// </returns>
		/// <param name="value">The value that is produced by the binding target.</param><param name="targetType">The type to convert to.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}