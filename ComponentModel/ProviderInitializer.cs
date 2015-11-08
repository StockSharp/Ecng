namespace Ecng.ComponentModel
{
	#region Using Directives

	using System;
	using System.Collections.Specialized;
	using System.ComponentModel;
	using System.Configuration;
	using System.Configuration.Provider;

	using Ecng.Common;
	using Ecng.Serialization;

	#endregion

	/// <summary>
	/// 
	/// </summary>
	public static class ProviderInitializer
	{
		/// <summary>
		/// Initializes the specified provider.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="settings">The settings.</param>
		/// <returns></returns>
		public static T Initialize<T>(this ProviderSettings settings)
			where T : ProviderBase
		{
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));

			var providerType = settings.Type.To<Type>();

			if (providerType.IsGenericTypeDefinition)
				providerType = providerType.Make(typeof(T).GetGenericArguments());

			return (T)Initialize(settings, providerType);
		}

		/// <summary>
		/// Initializes the specified provider.
		/// </summary>
		/// <param name="providerSettings">The provider settings.</param>
		/// <param name="providerType">Type of the provider.</param>
		/// <returns></returns>
		public static ProviderBase Initialize(this ProviderSettings providerSettings, Type providerType)
		{
			if (providerSettings == null)
				throw new ArgumentNullException(nameof(providerSettings));

			if (providerType == null)
				throw new ArgumentNullException(nameof(providerType));

			var provider = providerType.CreateInstance<ProviderBase>();
			provider.Initialize(providerSettings.Name, providerSettings.Parameters);
			return provider;
		}

		/// <summary>
		/// Initializes the specified provider.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="config">The config.</param>
		public static void Initialize<TProvider>(this TProvider provider, NameValueCollection config)
			where TProvider : ProviderBase
		{
			provider.Initialize(config, false);
		}

		/// <summary>
		/// Initializes the specified provider.
		/// </summary>
		/// <typeparam name="TProvider">The type of the provider.</typeparam>
		/// <param name="provider">The provider.</param>
		/// <param name="config">The config.</param>
		/// <param name="removeFields">if set to <c>true</c> [remove fields].</param>
		public static void Initialize<TProvider>(this TProvider provider, NameValueCollection config, bool removeFields)
			where TProvider : ProviderBase
		{
			((ProviderBase)provider).Initialize(config, removeFields);
		}

		/// <summary>
		/// Initializes the specified provider.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="config">The config.</param>
		/// <param name="removeFields">if set to <c>true</c> [remove fields].</param>
		public static void Initialize(this ProviderBase provider, NameValueCollection config, bool removeFields)
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			if (config == null)
				throw new ArgumentNullException(nameof(config));

			var fields = provider.GetType().GetSchema().Fields.SerializableFields;
			var source = new SerializationItemCollection();
			foreach (var field in fields)
			{
				var value = config[field.Name].To(field.Type);

				if (value == null)
				{
					var attr = field.Member.GetAttribute<DefaultValueAttribute>();

					if (attr != null)
						value = attr.Value;
				}

				source.Add(new SerializationItem(field, value));
			}
			GetSerializer(provider.GetType()).Deserialize(source, fields, provider);

			//var fields = _fields.SafeAdd(provider.GetType(), key =>
			//{
			//    var pairs = new List<FieldPair>();

			//    var ignoredMembers = key.GetAttributes<IgnoreAttribute>().Select(attr => attr.MemberName);

			//    foreach (var info in key.GetMembers<FieldInfo>(ReflectionHelper.AllInstanceMembers, true))
			//    {
			//        if (info.GetAttribute<IgnoreAttribute>() != null || ignoredMembers.Contains(info.Name))
			//            continue;

			//        string fieldName;

			//        var fieldAttr = info.GetAttribute<FieldAttribute>();
			//        if (fieldAttr != null)
			//            fieldName = fieldAttr.Name;
			//        else
			//        {
			//            fieldName = info.Name;

			//            if (fieldName[0] == '_')
			//                fieldName = fieldName.Substring(1, fieldName.Length - 1);
			//        }

			//        var defValue = new NullableEx<object>();

			//        var defValueAttr = info.GetAttribute<DefaultValueAttribute>();

			//        if (defValueAttr != null)
			//            defValue.Value = defValueAttr.Value;

			//        pairs.Add(new FieldPair(FastInvoker<ProviderBase, object, VoidType>.Create(info, false), fieldName, defValue));
			//    }

			//    return pairs;
			//});

			//foreach (var field in fields)
			//{
			//    var fieldType = field.First.Member.GetMemberType();

			//    var value = field.Third.HasValue ?
			//        config.GetValue(fieldType, field.Second, field.Third.Value) : 
			//        config.GetValue(fieldType, field.Second);

			//    field.First.SetValue(provider, value);

			//    if (removeFields)
			//        config.Remove(field.Second);
			//}
		}

		private static ISerializer GetSerializer(Type entityType)
		{
			return new BinarySerializer<int>().GetSerializer(entityType);
		}
	}
}