namespace Ecng.Xaml.DevExp
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Reflection;
	using System.Windows;

	using DevExpress.Xpf.Grid.LookUp;

	using Ecng.Common;
	using Ecng.Reflection;

	public class EntityPropertyEditor : LookUpEditSettings
	{
		static EntityPropertyEditor()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(EntityPropertyEditor), new FrameworkPropertyMetadata(typeof(EntityPropertyEditor)));
		}

		public EntityPropertyEditor()
		{
			DisplayMember = "FullDisplayName";
			ValueMember = "Name";
		}
	}

	public class EntityProperty
	{
		public string Name { get; set; }

		public string DisplayName { get; set; }

		public EntityProperty Parent { get; set; }

		public IEnumerable<EntityProperty> Properties { get; set; }

		public string FullDisplayName => Parent == null ? DisplayName : "{0} -> {1}".Put(Parent.FullDisplayName, DisplayName);

		public string ParentName => Parent == null ? string.Empty : Parent.Name;

		public override string ToString()
		{
			return "{0} ({1})".Put(Name, FullDisplayName);
		}
	}

	public static class EntityPropertyHelper
	{
		public static List<EntityProperty> GetEntityProperties(this Type type, Func<PropertyInfo, bool> filter = null)
		{
			return type.GetEntityProperties(null, new HashSet<Type>(), filter ?? (p => true));
		}

		private static List<EntityProperty> GetEntityProperties(this Type type, EntityProperty parent, HashSet<Type> processed, Func<PropertyInfo, bool> filter)
		{
			var properties = new List<EntityProperty>();

			if (processed.Contains(type))
				return properties;

			var propertyInfos = type
				.GetMembers<PropertyInfo>(BindingFlags.Public | BindingFlags.Instance)
				.Where(filter);

			processed.Add(type);

			foreach (var pi in propertyInfos)
			{
				var nameAttr = pi.GetAttribute<DisplayNameAttribute>();
				var displayName = nameAttr == null ? pi.Name : nameAttr.DisplayName;

				var prop = new EntityProperty
				{
					Name = (parent != null ? parent.Name + "." : string.Empty) + pi.Name,
					Parent = parent,
					DisplayName = displayName,
				};

				if (!pi.PropertyType.IsPrimitive() && !pi.PropertyType.IsNullable())
				{
					prop.Properties = GetEntityProperties(pi.PropertyType, prop, processed, filter);
				}

				properties.Add(prop);
			}

			processed.Remove(type);

			return properties;
		}

		public static object GetPropValue(this object entity, string name)
		{
			var value = entity;

			foreach (var part in name.Split('.'))
			{
				var info = value?.GetType().GetProperty(part);

				if (info == null)
					return null;

				value = info.GetValue(value, null);
			}

			return value;
		}
	}
}