namespace Ecng.Data
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Linq;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	#endregion

	public abstract class SqlRenderer : NamedObject
	{
		#region Private Fields

		//private static readonly Dictionary<Type, string> _tables = new Dictionary<Type, string>();
		//private static Dictionary<string, string> _procedures = new Dictionary<string, string>();

		private readonly Dictionary<DbType, Converter<Range<int>, string>> _typeNames = new Dictionary<DbType, Converter<Range<int>, string>>();

		#endregion

		#region SqlRenderer.ctor()

		protected SqlRenderer(string name)
			: base(name)
		{
		}

		#endregion

		private IEnumerable<string> _reservedWords;

		public string FormatReserver(string name)
		{
			if (_reservedWords == null)
				_reservedWords = ReservedWords.Select(arg => arg.ToLowerInvariant());

			if (_reservedWords.Contains(name.ToLowerInvariant()))
				name = "[{0}]".Put(name);

			return name;
		}

		#region AddTypeName

		protected void AddTypeName(DbType type, string name)
		{
			AddTypeName(type, delegate { return name; });
		}

		protected void AddTypeName(DbType type, Converter<Range<int>, string> converter)
		{
			_typeNames.Add(type, converter);
		}

		#endregion

		public abstract string GetIdentitySelect(Schema schema);

		public string FormatParameter(string value)
		{
			if (value.IsEmpty())
				throw new ArgumentNullException("value");

			return ParameterPrefix + value + ParameterSuffix;
		}

		public string UnformatParameter(string value)
		{
			if (value.IsEmpty())
				throw new ArgumentNullException("value");

			if (value.Length < ParameterPrefix.Length + ParameterSuffix.Length + 1)
				throw new ArgumentOutOfRangeException("value");

			value = value.Substring(ParameterPrefix.Length);
			return value.Substring(0, value.Length - ParameterSuffix.Length);
		}

		public string GetTypeName(DbType type, Range<int> length)
		{
			if (!_typeNames.ContainsKey(type))
				throw new ArgumentException("Type {0} is not supported.".Put(type), "type");

			return _typeNames[type](length);
		}

		protected abstract string ParameterPrefix { get; }
		protected virtual string ParameterSuffix { get { return string.Empty; } }

		protected abstract string[] ReservedWords { get; }
	}
}