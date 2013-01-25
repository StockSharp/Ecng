namespace Ecng.Common
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using System.Runtime.Serialization;

	public static class TypeHelper
	{
		private static readonly Type _enumType = typeof(Enum);

		public static T CreateInstance<T>(this Type type, params object[] args)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			if (args == null)
				throw new ArgumentNullException("args");
			
			return Activator.CreateInstance(type, args).To<T>();
		}

		public static T CreateInstanceArgs<T>(this Type type, object[] args)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			if (args == null)
				throw new ArgumentNullException("args");

			Func<Type, object[], object> func = Activator.CreateInstance;
			return func(type, args).To<T>();
		}

		public static Type Make(this Type type, params Type[] args)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			if (args == null)
				throw new ArgumentNullException("args");

			return type.MakeGenericType(args);
		}

		public static Type Make(this Type type, IEnumerable<Type> args)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			if (args == null)
				throw new ArgumentNullException("args");

			return type.MakeGenericType(args.ToArray());
		}

		public static bool IsPrimitive(this Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			return (
						type.IsPrimitive ||
						type.IsEnum() ||
						type == typeof(decimal) ||
						type == typeof(string) ||
						type == typeof(DateTime) ||
						type == typeof(Guid) ||
						type == typeof(byte[]) ||
						type == typeof(TimeSpan)
					);
		}

		public static string GetTypeAsString(this Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			return Converter.GetAlias(type) ?? type.AssemblyQualifiedName /*"{0}, {1}".Put(type.FullName, type.Assembly.GetName().Name)*/;
		}

		public static bool IsStruct(this Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			return type.IsValueType && !type.IsEnum();
		}

		public static bool IsEnum(this Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			//
			// 2 times faster than Type.IsEnum
			//
			return type.BaseType == _enumType;
		}

		public static TEntity CreateUnitialized<TEntity>()
		{
			return (TEntity)typeof(TEntity).CreateUnitialized();
		}

		public static object CreateUnitialized(this Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			return FormatterServices.GetUninitializedObject(type);
		}

		public static void DoIf<TSource, TDestination>(this TSource source, Action<TDestination> handler)
			where TDestination : class 
		{
			if (handler == null)
				throw new ArgumentNullException("handler");

			var destination = source as TDestination;

			if (destination == null)
				return;

			handler(destination);
		}

		public static void DoDispose<TSource>(this TSource source)
		{
			source.DoIf<TSource, IDisposable>(d => d.Dispose());
		}

#if !SILVERLIGHT
		private static readonly Lazy<string> _applicationName = new Lazy<string>(() =>
		{
			var asm = Assembly.GetEntryAssembly();
			var attr = asm.GetAttribute<AssemblyProductAttribute>();
			return attr != null ? attr.Product : asm.GetName().Name;
		});

		public static string ApplicationName
		{
			get { return _applicationName.Value; }
		}
#endif

		// http://stackoverflow.com/questions/8517159/how-to-detect-at-runtime-that-net-version-4-5-currently-running-your-code
		public static bool IsNet45OrNewer()
		{
			// Class "ReflectionContext" exists from .NET 4.5 onwards.
			return Type.GetType("System.Reflection.ReflectionContext", false) != null;
		}
	}
}