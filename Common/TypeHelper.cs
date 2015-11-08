namespace Ecng.Common
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using System.Runtime.Serialization;

	public static class TypeHelper
	{
		private static readonly FieldInfo _remoteStackTraceString;

		static TypeHelper()
		{
			// Get the _remoteStackTraceString of the Exception class
			_remoteStackTraceString = typeof(Exception)
				.GetField("_remoteStackTraceString",
					BindingFlags.Instance | BindingFlags.NonPublic); // MS.Net

			if (_remoteStackTraceString == null)
				_remoteStackTraceString = typeof(Exception)
				.GetField("remote_stack_trace",
					BindingFlags.Instance | BindingFlags.NonPublic); // Mono
		}

		private static readonly Type _enumType = typeof(Enum);

		public static T CreateInstance<T>(this Type type, params object[] args)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			if (args == null)
				throw new ArgumentNullException(nameof(args));
			
			return Activator.CreateInstance(type, args).To<T>();
		}

		public static T CreateInstanceArgs<T>(this Type type, object[] args)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			if (args == null)
				throw new ArgumentNullException(nameof(args));

			Func<Type, object[], object> func = Activator.CreateInstance;
			return func(type, args).To<T>();
		}

		public static Type Make(this Type type, params Type[] args)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			if (args == null)
				throw new ArgumentNullException(nameof(args));

			return type.MakeGenericType(args);
		}

		public static Type Make(this Type type, IEnumerable<Type> args)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			if (args == null)
				throw new ArgumentNullException(nameof(args));

			return type.MakeGenericType(args.ToArray());
		}

		public static bool IsPrimitive(this Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			return (
						type.IsPrimitive ||
						type.IsEnum() ||
						type == typeof(decimal) ||
						type == typeof(string) ||
						type == typeof(DateTime) ||
						type == typeof(DateTimeOffset) ||
						type == typeof(Guid) ||
						type == typeof(byte[]) ||
						type == typeof(TimeSpan)
					);
		}

		public static bool IsNumeric(this Type type)
		{
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.Decimal:
				case TypeCode.Double:
				case TypeCode.Single:
					return true;
				default:
					return false;
			}
		}

		public static string GetTypeAsString(this Type type, bool isAssemblyQualifiedName)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			return Converter.GetAlias(type) ?? type.GetTypeName(isAssemblyQualifiedName) /*"{0}, {1}".Put(type.FullName, type.Assembly.GetName().Name)*/;
		}

		public static bool IsStruct(this Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			return type.IsValueType && !type.IsEnum();
		}

		public static bool IsEnum(this Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			//
			// 2 times faster than Type.IsEnum
			//
			return type.BaseType == _enumType;
		}

		public static bool IsAttribute(this Type type)
		{
			return typeof(Attribute).IsAssignableFrom(type);
		}

		public static bool IsDelegate(this Type type)
		{
			return typeof(Delegate).IsAssignableFrom(type);
		}

		public static bool IsWpfColor(this Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			return type == typeof (System.Windows.Media.Color);
		}

#if !SILVERLIGHT
		public static bool IsWinColor(this Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			return type == typeof(System.Drawing.Color);
		}
#endif

		public static TEntity CreateUnitialized<TEntity>()
		{
			return (TEntity)typeof(TEntity).CreateUnitialized();
		}

		public static object CreateUnitialized(this Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			return FormatterServices.GetUninitializedObject(type);
		}

		public static void DoIf<TSource, TDestination>(this TSource source, Action<TDestination> handler)
			where TDestination : class 
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

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
			if (asm == null)
				return "None";
			var attr = asm.GetAttribute<AssemblyTitleAttribute>();
			return attr != null ? attr.Title : asm.GetName().Name;
		});

		public static string ApplicationName => _applicationName.Value;

		private static readonly Lazy<string> _applicationNameWithVersion = new Lazy<string>(() =>
		{
			var asm = Assembly.GetEntryAssembly();

			if (asm == null)
				return "None";

			return ApplicationName + " v" + asm.GetName().Version;
		});

		public static string ApplicationNameWithVersion => _applicationNameWithVersion.Value;
#endif

		// http://stackoverflow.com/questions/8517159/how-to-detect-at-runtime-that-net-version-4-5-currently-running-your-code
		public static bool IsNet45OrNewer()
		{
			// Class "ReflectionContext" exists from .NET 4.5 onwards.
			return Type.GetType("System.Reflection.ReflectionContext", false) != null;
		}

		public static string GetTypeName(this Type type, bool isAssemblyQualifiedName)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			return isAssemblyQualifiedName ? type.AssemblyQualifiedName : "{0}, {1}".Put(type.FullName,
#if SILVERLIGHT
				new AssemblyName(type.Assembly.FullName).Name
#else
				type.Assembly.GetName().Name
#endif
			);
		}

		public static object GetDefaultValue(this Type type)
		{
			return type.IsValueType ? Activator.CreateInstance(type) : null;
		}

		// http://stackoverflow.com/questions/57383/in-c-how-can-i-rethrow-innerexception-without-losing-stack-trace
		public static void Throw(this Exception ex)
		{
			if (ex == null)
				throw new ArgumentNullException(nameof(ex));

			_remoteStackTraceString.SetValue(ex, ex.StackTrace + Environment.NewLine);

			throw ex;
		}
	}
}