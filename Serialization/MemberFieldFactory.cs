namespace Ecng.Serialization
{
	#region Using Directives

	using System;
	using System.Reflection;

	using Ecng.Common;
	using Ecng.Reflection;

	#endregion

	[Serializable]
	public class MemberFieldFactory<T> : FieldFactory<T, string>
		where T : MemberInfo
	{
		#region MemberFieldFactory.ctor()

		public MemberFieldFactory(Field field, int order)
			: base(field, order)
		{
		}

		#endregion

		#region FieldFactory<T, string> Members

		protected internal override T OnCreateInstance(ISerializer serializer, string source)
		{
			var parts = source.Split('/');

			var type = parts[0].To<Type>();
			if (parts.Length == 1)
				return type.To<T>();
			else
				return type.GetMember<T>(parts[1]);
		}

		protected internal override string OnCreateSource(ISerializer serializer, T instance)
		{
			if (instance.ReflectedType != null)
				return instance.ReflectedType.AssemblyQualifiedName + "/" + instance.Name;
			else
				return instance.To<Type>().AssemblyQualifiedName;
		}

		#endregion
	}

	public sealed class MemberAttribute : ReflectionFieldFactoryAttribute
	{
		#region ReflectionFieldFactoryAttribute Members

		protected override Type GetFactoryType(Field field)
		{
			return typeof(MemberFieldFactory<>).Make(field.Type);
		}

		#endregion
	}
}