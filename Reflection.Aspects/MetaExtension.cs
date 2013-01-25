namespace Ecng.Reflection.Aspects
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection.Emit;

	#endregion

	public static class MetaExtension
	{
		#region Private Fields

		private static readonly Dictionary<Type, Type> _extendedTypes = new Dictionary<Type, Type>();

		#endregion

		#region MetaExtension.ctor()

		static MetaExtension()
		{
			foreach (var type in AssemblyHolder.CachedTypes)
			{
				var baseExtensionType = GetBaseExtensionType(type);

				if (baseExtensionType != null)
					_extendedTypes.Add(baseExtensionType, type);
			}
		}

		#endregion

		#region Create

		public static Type Create(Type baseType)
		{
			if (baseType == null)
				throw new ArgumentNullException("baseType");

			if (!baseType.IsPublic)
				throw new ArgumentException("baseType");

			var attrs = baseType.GetAttributes<MetaExtensionAttribute>();
			if (attrs.IsEmpty())
				throw new ArgumentException("Type '{0}' isn't marked MetaExtensionAttribute.".Put(baseType), "baseType");

			return _extendedTypes.SafeAdd(baseType, delegate
			{
				foreach (var attr in attrs.OrderBy(item => item.Order))
				{
					var typeGenerator = AssemblyHolder.CreateType(baseType.Name + "Derive" + Guid.NewGuid(), TypeAttributes.Class | TypeAttributes.Public, baseType);

					var context = new MetaExtensionContext(typeGenerator, baseType);
					attr.Extend(context, attr.Order);
					baseType = context.TypeGenerator.CompileType();

					foreach (var fieldValue in context.AfterEmitInitFields)
						baseType.GetMember<FieldInfo>(fieldValue.Key.Builder.Name).SetValue(null, fieldValue.Value);
				}

				return baseType;
			});
		}

		#endregion

		#region GetBaseExtensionType

		private static Type GetBaseExtensionType(Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			if (type.BaseType != null && type.BaseType.GetAttribute<MetaExtensionAttribute>() != null)
				return type.BaseType;

			var interfaces = type.GetInterfaces();

			if (interfaces.Length == 1 && interfaces[0].GetAttribute<MetaExtensionAttribute>() != null)
				return interfaces[0];

			throw new ArgumentException("type");
		}

		#endregion
	}
}