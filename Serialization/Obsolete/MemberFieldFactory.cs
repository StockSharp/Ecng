namespace Ecng.Serialization
{
	using System;
	using System.Reflection;

	using Ecng.Common;

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
			return source.FromString<T>();
		}

		protected internal override string OnCreateSource(ISerializer serializer, T instance)
		{
			return instance.ToString(_isAssemblyQualifiedName);
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