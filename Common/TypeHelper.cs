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

	/// <summary>
	/// Provides helper methods for working with types and related operations.
	/// </summary>
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

		/// <summary>
		/// Creates an instance of the specified type using the given arguments.
		/// </summary>
		/// <param name="type">The type to instantiate.</param>
		/// <param name="args">The constructor arguments.</param>
		/// <returns>Returns the created instance as an object.</returns>
		public static object CreateInstance(this Type type, params object[] args)
			=> type.CreateInstance<object>(args);

		/// <summary>
		/// Creates an instance of a specified type T using the given arguments.
		/// </summary>
		/// <typeparam name="T">The type to instantiate.</typeparam>
		/// <param name="type">The type to instantiate.</param>
		/// <param name="args">The constructor arguments.</param>
		/// <returns>Returns the created instance as T.</returns>
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

		/// <summary>
		/// Makes a generic type using the provided type arguments.
		/// </summary>
		/// <param name="type">The generic type definition.</param>
		/// <param name="args">The type arguments.</param>
		/// <returns>Returns the constructed generic type.</returns>
		public static Type Make(this Type type, params Type[] args)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			if (args is null)
				throw new ArgumentNullException(nameof(args));

			return type.MakeGenericType(args);
		}

		/// <summary>
		/// Makes a generic type using a list of provided type arguments.
		/// </summary>
		/// <param name="type">The generic type definition.</param>
		/// <param name="args">The type arguments as an IEnumerable.</param>
		/// <returns>Returns the constructed generic type.</returns>
		public static Type Make(this Type type, IEnumerable<Type> args)
		{
			return type.Make([.. args]);
		}

		/// <summary>
		/// Determines whether a type is considered primitive, including common system types.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>Returns true if the type is primitive or commonly handled as a primitive.</returns>
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

		/// <summary>
		/// Determines if a type is numeric, including floating-point and decimal types.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>Returns true if the type is numeric.</returns>
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

		/// <summary>
		/// Determines if a type is an integer numeric type.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>Returns true if the type is an integer numeric type.</returns>
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

		/// <summary>
		/// Retrieves the type name as a string either in assembly-qualified form or not.
		/// </summary>
		/// <param name="type">The type to convert.</param>
		/// <param name="isAssemblyQualifiedName">True to return the assembly-qualified name.</param>
		/// <returns>Returns the string representation of the type.</returns>
		public static string GetTypeAsString(this Type type, bool isAssemblyQualifiedName)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			return type.TryGetCSharpAlias().IsEmpty(type.GetTypeName(isAssemblyQualifiedName));
		}

		/// <summary>
		/// Determines whether the specified type is a struct.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>Returns true if the type is a struct.</returns>
		public static bool IsStruct(this Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			return type.IsValueType && !type.IsEnum();
		}

		/// <summary>
		/// Determines whether the specified type is an enum.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>Returns true if the type is an enum.</returns>
		public static bool IsEnum(this Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			//
			// 2 times faster than Type.IsEnum
			//
			return type.BaseType == _enumType;
		}

		/// <summary>
		/// Determines whether the specified type is an attribute.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>Returns true if the type is an attribute.</returns>
		public static bool IsAttribute(this Type type)
			=> type.Is<Attribute>();

		/// <summary>
		/// Determines whether the specified type is a delegate.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>Returns true if the type is a delegate.</returns>
		public static bool IsDelegate(this Type type)
			=> type.Is<Delegate>();

		/// <summary>
		/// Creates an uninitialized instance of the specified generic type parameter.
		/// </summary>
		/// <typeparam name="TEntity">The type to instantiate.</typeparam>
		/// <returns>Returns the newly created uninitialized instance.</returns>
		public static TEntity CreateUnitialized<TEntity>()
		{
			return (TEntity)typeof(TEntity).CreateUnitialized();
		}

		/// <summary>
		/// Creates an uninitialized instance of the specified type.
		/// </summary>
		/// <param name="type">The type to instantiate.</param>
		/// <returns>Returns the newly created uninitialized instance.</returns>
		public static object CreateUnitialized(this Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			return FormatterServices.GetUninitializedObject(type);
		}

		/// <summary>
		/// Disposes the source if it implements IDisposable.
		/// </summary>
		/// <typeparam name="TSource">The type of the source.</typeparam>
		/// <param name="source">The instance to dispose.</param>
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

		/// <summary>
		/// Gets the name of the application.
		/// </summary>
		public static string ApplicationName => _applicationName.Value;

		private static readonly Lazy<string> _applicationNameWithVersion = new(() =>
		{
			var asm = Assembly.GetEntryAssembly();

			if (asm is null)
				return "None";

			return ApplicationName + " v" + asm.GetName().Version;
		});

		/// <summary>
		/// Gets the name of the application with version.
		/// </summary>
		public static string ApplicationNameWithVersion => _applicationNameWithVersion.Value;

		// http://stackoverflow.com/questions/8517159/how-to-detect-at-runtime-that-net-version-4-5-currently-running-your-code

		/// <summary>
		/// Determines if the current environment is .NET 4.5 or newer.
		/// </summary>
		/// <returns>Returns true if .NET 4.5 or newer is running.</returns>
		public static bool IsNet45OrNewer()
		{
			// Class "ReflectionContext" exists from .NET 4.5 onwards.
			return Type.GetType("System.Reflection.ReflectionContext", false) != null;
		}

		/// <summary>
		/// Gets the fully qualified name of a type or its assembly-qualified name.
		/// </summary>
		/// <param name="type">The type to convert.</param>
		/// <param name="isAssemblyQualifiedName">Whether to return the assembly-qualified name.</param>
		/// <returns>Returns the string representation of the type name.</returns>
		public static string GetTypeName(this Type type, bool isAssemblyQualifiedName)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			return isAssemblyQualifiedName
				? type.AssemblyQualifiedName
				: $"{type.FullName}, {type.Assembly.GetName().Name}";
		}

		/// <summary>
		/// Gets the default value for the specified type.
		/// </summary>
		/// <param name="type">The type to get the default value for.</param>
		/// <returns>Returns the default value as an object.</returns>
		public static object GetDefaultValue(this Type type)
		{
			return type.IsValueType ? Activator.CreateInstance(type) : null;
		}

		// http://stackoverflow.com/questions/57383/in-c-how-can-i-rethrow-innerexception-without-losing-stack-trace

		/// <summary>
		/// Throws the specified exception while preserving the original stack trace.
		/// </summary>
		/// <param name="ex">The exception to throw.</param>
		public static void Throw(this Exception ex)
		{
			if (ex is null)
				throw new ArgumentNullException(nameof(ex));

			_remoteStackTraceString.SetValue(ex, ex.StackTrace + Environment.NewLine);

			throw ex;
		}

		/// <summary>
		/// Generates a salt byte array of the specified size.
		/// </summary>
		/// <param name="saltSize">The size of the salt array to create.</param>
		/// <returns>Returns a byte array representing the salt.</returns>
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

		/// <summary>
		/// Creates a scope for the specified value with optional ownership.
		/// </summary>
		/// <typeparam name="T">The type of the resource.</typeparam>
		/// <param name="value">The resource to be scoped.</param>
		/// <param name="ownInstance">Whether the scope owns the instance.</param>
		/// <returns>Returns the newly created Scope.</returns>
		public static Scope<T> ToScope<T>(this T value, bool ownInstance = true)
		{
			return new Scope<T>(value, ownInstance);
		}

		/// <summary>
		/// Returns either a single exception or an aggregate if there are multiple exceptions.
		/// </summary>
		/// <param name="errors">A collection of exceptions.</param>
		/// <returns>Returns one exception or an AggregateException.</returns>
		public static Exception SingleOrAggr(this IList<Exception> errors)
		{
			if (errors is null)
				throw new ArgumentNullException(nameof(errors));

			return errors.Count == 1 ? errors[0] : new AggregateException(errors);
		}

		/// <summary>
		/// Checks if a value is null and throws an exception if it is.
		/// </summary>
		/// <typeparam name="T">The reference type to check.</typeparam>
		/// <param name="value">The value to check for null.</param>
		/// <param name="paramName">The name of the parameter.</param>
		/// <returns>Returns the provided value if not null.</returns>
		public static T CheckOnNull<T>(this T value, string paramName = "value")
			where T : class
		{
			if (value is null)
				throw new ArgumentNullException(paramName);

			return value;
		}

		/// <summary>
		/// Retrieves the platform attribute for the specified type.
		/// </summary>
		/// <param name="type">The type to inspect.</param>
		/// <returns>Returns the platform value defined by TargetPlatformAttribute or AnyCPU.</returns>
		public static Platforms GetPlatform(this Type type) => type.GetAttribute<TargetPlatformAttribute>()?.Platform ?? Platforms.AnyCPU;

		/// <summary>
		/// Extracts the high word from the integer.
		/// </summary>
		/// <param name="iValue">The integer value.</param>
		/// <returns>Returns the high word.</returns>
		public static int HiWord(this int iValue)
		{
			return (iValue >> 16) & 0xFFFF;
		}

		/// <summary>
		/// Extracts the low word from the integer.
		/// </summary>
		/// <param name="iValue">The integer value.</param>
		/// <returns>Returns the low word.</returns>
		public static int LoWord(this int iValue)
		{
			return iValue & 0xFFFF;
		}

		// https://stackoverflow.com/a/30528667

		/// <summary>
		/// Checks if a dynamic or regular object has a property with the specified name.
		/// </summary>
		/// <param name="settings">The object to inspect.</param>
		/// <param name="name">The property name.</param>
		/// <returns>Returns true if the property exists.</returns>
		public static bool HasProperty(this object settings, string name)
		{
			if (settings is ExpandoObject)
				return ((IDictionary<string, object>)settings).ContainsKey(name);

			return settings.GetType().GetProperty(name) != null;
		}

		/// <summary>
		/// Indicates whether a type is the specified base type or derives from it.
		/// </summary>
		/// <typeparam name="TBase">The base type to check.</typeparam>
		/// <param name="type">The type to compare.</param>
		/// <param name="canSame">Allows checking if the types are the same.</param>
		/// <returns>Returns true if type is or derives from the base type.</returns>
		public static bool Is<TBase>(this Type type, bool canSame = true)
			=> type.Is(typeof(TBase), canSame);

		/// <summary>
		/// Indicates whether a type is the specified base type or derives from it.
		/// </summary>
		/// <param name="type">The type to compare.</param>
		/// <param name="baseType">The base type.</param>
		/// <param name="canSame">Allows checking if the types are the same.</param>
		/// <returns>Returns true if type is or derives from the base type.</returns>
		public static bool Is(this Type type, Type baseType, bool canSame = true)
			=> baseType.CheckOnNull(nameof(baseType)).IsAssignableFrom(type) && (canSame || type != baseType);

		/// <summary>
		/// Determines if the specified type is generated by a compiler.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>Returns true if the type has a CompilerGeneratedAttribute.</returns>
		public static bool IsAutoGenerated(this Type type)
			=> type.GetAttribute<CompilerGeneratedAttribute>() is not null;

		/// <summary>
		/// Ensures the static constructor for a type is run.
		/// </summary>
		/// <param name="type">The type to use.</param>
		public static void EnsureRunClass(this Type type)
			=> RuntimeHelpers.RunClassConstructor(type.TypeHandle);

		/// <summary>
		/// Checks if a string is a valid web link.
		/// </summary>
		/// <param name="link">The link to check.</param>
		/// <returns>Returns true if the link is a valid absolute URI.</returns>
		public static bool IsValidWebLink(this string link)
			=> Uri.TryCreate(link, UriKind.Absolute, out var uri) && uri.IsWebLink();

		/// <summary>
		/// Checks if a URI is a web-based link (http, https, or ftp).
		/// </summary>
		/// <param name="uri">The URI to check.</param>
		/// <returns>Returns true if the URI is web-based.</returns>
		public static bool IsWebLink(this Uri uri)
			=> uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeFtp;
	}
}