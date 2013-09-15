namespace Ecng.Serialization
{
	using System;
	using System.Reflection;

	using Ecng.Common;
	using Ecng.Reflection;

	[Serializable]
	public class MemberFieldFactory<T> : FieldFactory<T, string>
		where T : MemberInfo
	{
		private readonly bool _isAssemblyQualifiedName;

		public MemberFieldFactory(Field field, int order, bool isAssemblyQualifiedName)
			: base(field, order)
		{
			_isAssemblyQualifiedName = isAssemblyQualifiedName;
		}

		protected internal override T OnCreateInstance(ISerializer serializer, string source)
		{
			var parts = source.Split('/');

			var type = parts[0].To<Type>();
			return parts.Length == 1 ? type.To<T>() : type.GetMember<T>(parts[1]);
		}

		protected internal override string OnCreateSource(ISerializer serializer, T instance)
		{
			if (instance.ReflectedType != null)
				return instance.ReflectedType.GetTypeName(_isAssemblyQualifiedName) + "/" + instance.Name;
			else
				return instance.To<Type>().GetTypeName(_isAssemblyQualifiedName);
		}
	}

	public sealed class MemberAttribute : ReflectionFieldFactoryAttribute
	{
		public MemberAttribute()
		{
			IsAssemblyQualifiedName = true;
		}

		public bool IsAssemblyQualifiedName { get; set; }

		protected override Type GetFactoryType(Field field)
		{
			return typeof(MemberFieldFactory<>).Make(field.Type);
		}

		protected override object[] GetArgs(Field field)
		{
			return new object[] { IsAssemblyQualifiedName };
		}
	}
}