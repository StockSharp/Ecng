namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Reflection;
	using System.Runtime.Serialization;
	using System.Text;

	using Ecng.Reflection;
	using Ecng.Common;

	public static class BinaryHelper
	{
		public static void UndoDispose(this MemoryStream stream)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));

			stream.SetValue("_isOpen", true);
			stream.SetValue("_writable", true);
			stream.SetValue("_expandable", true);
		}

		// убрать когда перейдем на 4.5 полностью
		private class LeaveOpenStreamReader : StreamReader
		{
			public LeaveOpenStreamReader(Stream stream, Encoding encoding)
				: base(stream, encoding ?? Encoding.UTF8)
			{
				this.SetValue("_closable", false);
			}
		}

		public static IEnumerable<string> EnumerateLines(this Stream stream, Encoding encoding = null)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));

			using (var sr = new LeaveOpenStreamReader(stream, encoding ?? Encoding.UTF8))
			{
				while (!sr.EndOfStream)
					yield return sr.ReadLine();
			}
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