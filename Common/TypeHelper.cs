namespace Ecng.Common
{
	using System;
	using System.Collections.Generic;
	using System.Dynamic;
	using System.Linq;
	using System.Reflection;
	using System.Runtime.Serialization;
	using System.Security.Cryptography;
	using System.Runtime.CompilerServices;

	public static class TypeHelper
	{
		private static readonly FieldInfo _remoteStackTraceString;

		static TypeHelper()
		{
			// Get the _remoteStackTraceString of the Exception class
			_remoteStackTraceString = typeof(Exception)
				.GetField("_remoteStackTraceString",
					BindingFlags.Instance | BindingFlags.NonPublic); // MS.Net

			if (_remoteStackTraceString is null)
				_remoteStackTraceString = typeof(Exception)
				.GetField("remote_stack_trace",
					BindingFlags.Instance | BindingFlags.NonPublic); // Mono
		}

		private static readonly Type _enumType = typeof(Enum);

		public static object CreateInstance(this Type type, params object[] args)
			=> type.CreateInstance<object>(args);

		public static T CreateInstance<T>(this Type type, params object[] args)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			if (args is null)
				throw new ArgumentNullException(nameof(args));

			var obj = type is ITypeConstructor ctor
				? ctor.CreateInstance(args)
				: Activator.CreateInstance(type, args);

			return obj.To<T>();
		}

		public static Type Make(this Type type, params Type[] args)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			if (args is null)
				throw new ArgumentNullException(nameof(args));

			return type.MakeGenericType(args);
		}

		public static Type Make(this Type type, IEnumerable<Type> args)
		{
			return type.Make(args.ToArray());
		}

		public static bool IsPrimitive(this Type type)
		{
			if (type is null)
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
						type == typeof(TimeSpan) ||
						type == typeof(TimeZoneInfo)
					);
		}

		public static bool IsNumeric(this Type type)
			=> Type.GetTypeCode(type) switch
			{
				TypeCode.Byte or
				TypeCode.SByte or
				TypeCode.UInt16 or
				TypeCode.UInt32 or
				TypeCode.UInt64 or
				TypeCode.Int16 or
				TypeCode.Int32 or
				TypeCode.Int64 or
				TypeCode.Decimal or
				TypeCode.Double or
				TypeCode.Single
					=> true,
				_ => false,
			};

		public static bool IsNumericInteger(this Type type)
			=> Type.GetTypeCode(type) switch
			{
				TypeCode.Byte or
				TypeCode.SByte or
				TypeCode.UInt16 or
				TypeCode.UInt32 or
				TypeCode.UInt64 or
				TypeCode.Int16 or
				TypeCode.Int32 or
				TypeCode.Int64
					=> true,
				_ => false,
			};

		public static string GetTypeAsString(this Type type, bool isAssemblyQualifiedName)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			return type.TryGetCSharpAlias().IsEmpty(type.GetTypeName(isAssemblyQualifiedName)) /*"{0}, {1}".Put(type.FullName, type.Assembly.GetName().Name)*/;
		}

		public static bool IsStruct(this Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			return type.IsValueType && !type.IsEnum();
		}

		public static bool IsEnum(this Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			//
			// 2 times faster than Type.IsEnum
			//
			return type.BaseType == _enumType;
		}

		public static bool IsAttribute(this Type type)
			=> type.Is<Attribute>();

		public static bool IsDelegate(this Type type)
			=> type.Is<Delegate>();

		public static TEntity CreateUnitialized<TEntity>()
		{
			return (TEntity)typeof(TEntity).CreateUnitialized();
		}

		public static object CreateUnitialized(this Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			return FormatterServices.GetUninitializedObject(type);
		}

		public static void DoDispose<TSource>(this TSource source)
		{
			if (source is IDisposable disposable)
				disposable.Dispose();
		}

		private static readonly Lazy<string> _applicationName = new(() =>
		{
			var asm = Assembly.GetEntryAssembly();
			if (asm is null)
				return "None";
			var attr = asm.GetAttribute<AssemblyTitleAttribute>();
			return attr != null ? attr.Title : asm.GetName().Name;
		});

		public static string ApplicationName => _applicationName.Value;

		private static readonly Lazy<string> _applicationNameWithVersion = new(() =>
		{
			var asm = Assembly.GetEntryAssembly();

			if (asm is null)
				return "None";

			return ApplicationName + " v" + asm.GetName().Version;
		});

		public static string ApplicationNameWithVersion => _applicationNameWithVersion.Value;

		// http://stackoverflow.com/questions/8517159/how-to-detect-at-runtime-that-net-version-4-5-currently-running-your-code
		public static bool IsNet45OrNewer()
		{
			// Class "ReflectionContext" exists from .NET 4.5 onwards.
			return Type.GetType("System.Reflection.ReflectionContext", false) != null;
		}

		public static string GetTypeName(this Type type, bool isAssemblyQualifiedName)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			return isAssemblyQualifiedName
				? type.AssemblyQualifiedName
				: $"{type.FullName}, {type.Assembly.GetName().Name}";
		}

		public static object GetDefaultValue(this Type type)
		{
			return type.IsValueType ? Activator.CreateInstance(type) : null;
		}

		// http://stackoverflow.com/questions/57383/in-c-how-can-i-rethrow-innerexception-without-losing-stack-trace
		public static void Throw(this Exception ex)
		{
			if (ex is null)
				throw new ArgumentNullException(nameof(ex));

			_remoteStackTraceString.SetValue(ex, ex.StackTrace + Environment.NewLine);

			throw ex;
		}

		public static byte[] GenerateSalt(int saltSize)
#if NET5_0_OR_GREATER
			=> RandomNumberGenerator.GetBytes(saltSize);
#else
		{
			var salt = new byte[saltSize];

			using var saltGen = new RNGCryptoServiceProvider();
			saltGen.GetBytes(salt);

			return salt;
		}
#endif

		public static Scope<T> ToScope<T>(this T value, bool ownInstance = true)
		{
			return new Scope<T>(value, ownInstance);
		}

		public static Exception SingleOrAggr(this IList<Exception> errors)
		{
			if (errors is null)
				throw new ArgumentNullException(nameof(errors));

			return errors.Count == 1 ? errors[0] : new AggregateException(errors);
		}

		public static T CheckOnNull<T>(this T value, string paramName = "value")
			where T : class
		{
			if (value is null)
				throw new ArgumentNullException(paramName);

			return value;
		}

		public static Platforms GetPlatform(this Type type) => type.GetAttribute<TargetPlatformAttribute>()?.Platform ?? Platforms.AnyCPU;

		public static int HiWord(this int iValue)
		{
			return (iValue >> 16) & 0xFFFF;
		}

		public static int LoWord(this int iValue)
		{
			return iValue & 0xFFFF;
		}

		// https://stackoverflow.com/a/30528667
		public static bool HasProperty(this object settings, string name)
		{
			if (settings is ExpandoObject)
				return ((IDictionary<string, object>)settings).ContainsKey(name);

			return settings.GetType().GetProperty(name) != null;
		}

		public static bool Is<TBase>(this Type type, bool canSame = true)
			=> type.Is(typeof(TBase), canSame);

		public static bool Is(this Type type, Type baseType, bool canSame = true)
			=> baseType.CheckOnNull(nameof(baseType)).IsAssignableFrom(type) && (canSame || type != baseType);

		public static bool IsAutoGenerated(this Type type)
			=> type.GetAttribute<CompilerGeneratedAttribute>() is not null;

		public static void EnsureRunClass(this Type type)
			=> RuntimeHelpers.RunClassConstructor(type.TypeHandle);

		/// <summary>
		/// </summary>
		public static bool IsValidWebLink(this string link)
			=> Uri.TryCreate(link, UriKind.Absolute, out var uri) && uri.IsWebLink();

		/// <summary>
		/// </summary>
		public static bool IsWebLink(this Uri uri)
			=> uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeFtp;
	}
}