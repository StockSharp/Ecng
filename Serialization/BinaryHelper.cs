namespace Ecng.Serialization
{
	using System;
	using System.IO;
	using System.Reflection;
	using System.Runtime.Serialization;

	using Ecng.Reflection;
	using Ecng.Common;

	public static class BinaryHelper
	{
		[Obsolete]
		public static void UndoDispose(this MemoryStream stream)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));

			stream.SetValue("_isOpen", true);
			stream.SetValue("_writable", true);
			stream.SetValue("_expandable", true);
		}

		#region Write

		[Obsolete("Use WriteEx method.")]
		public static void Write(this Stream stream, object value)
		{
			stream.WriteEx(value);
		}

		#endregion

		public static T FromString<T>(this string value)
			where T : MemberInfo
		{
			if (value.IsEmpty())
				throw new ArgumentNullException(nameof(value));

			var parts = value.Split('/');

			var type = parts[0].To<Type>();
			return parts.Length == 1 ? type.To<T>() : type.GetMember<T>(parts[1]);
		}

		public static string ToString<T>(this T member, bool isAssemblyQualifiedName)
			where T : MemberInfo
		{
			if (member is null)
				throw new ArgumentNullException(nameof(member));

			if (member.ReflectedType != null)
				return member.ReflectedType.GetTypeName(isAssemblyQualifiedName) + "/" + member.Name;
			else
				return member.To<Type>().GetTypeName(isAssemblyQualifiedName);
		}

		public static T DeserializeDataContract<T>(this Stream stream)
		{
			return (T)new DataContractSerializer(typeof(T)).ReadObject(stream);
		}

		public static void SerializeDataContract<T>(this Stream stream, T value)
		{
			new DataContractSerializer(typeof(T)).WriteObject(stream, value);
		}
	}
}