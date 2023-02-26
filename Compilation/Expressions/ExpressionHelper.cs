namespace Ecng.Compilation.Expressions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text.RegularExpressions;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;

	/// <summary>
	/// Extension class for <see cref="ExpressionFormula{TResult}"/>.
	/// </summary>
	[CLSCompliant(false)]
	public static class ExpressionHelper
	{
		private const string _idPattern = @"(#*)(@*)(#*)(\w*\.*)(\**)(\w+(\/*)\w+)@\w+";

		public static readonly Regex IdRegex = new($@"(?<id>{_idPattern})");
		public static readonly Regex CandleRegex = new(@"(?<id>\b[O|H|L|C|V|B]\b|TS|BS|LEN|OI)", RegexOptions.IgnoreCase);
		private static readonly Regex _nameRegex = new(@"(?<name>(\w+))");
		private static readonly Regex _bracketsVarRegex = new(@"\[(?<name>[^\]]*)\]");

		/// <summary>
		/// To get all identifiers from mathematical formula.
		/// </summary>
		/// <param name="expression">Mathematical formula.</param>
		/// <returns>Identifiers.</returns>
		public static IEnumerable<string> GetIds(Regex idRegex, string expression)
		{
			if (idRegex is null)
				throw new ArgumentNullException(nameof(idRegex));

			return
				from Match match in idRegex.Matches(expression)
				where match.Success
				select match.Groups["id"].Value;
		}

		private static IEnumerable<Group> GetVariableNames(string expression)
		{
			return
				from Match match in _nameRegex.Matches(expression)
				where match.Success
				select match.Groups["name"];
		}

		/// <summary>
		/// Escape mathematical formula from identifiers.
		/// </summary>
		/// <param name="expression">Unescaped text.</param>
		/// <returns>Escaped text.</returns>
		public static string Encode(Regex idRegex, string expression)
		{
			foreach (var id in GetIds(idRegex, expression).Distinct(StringComparer.InvariantCultureIgnoreCase))
			{
				expression = expression.Replace(id, $"[{{{id}}}]");
			}

			return expression;
		}

		/// <summary>
		/// Undo escape mathematical formula with identifiers.
		/// </summary>
		/// <param name="expression">Escaped text.</param>
		/// <returns>Unescaped text.</returns>
		public static string Decode(string expression, out IDictionary<string, string> replaces)
		{
			replaces = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

			foreach (var match in _bracketsVarRegex.Matches(expression).Cast<Match>().OrderByDescending(m => m.Index))
			{
				var varName = match.Groups["name"].Value;

				if (!replaces.ContainsKey(varName))
					replaces.Add(varName, $"VAR{replaces.Count}@VAR");
			}

			return replaces.Aggregate(expression, (current, pair) => current.ReplaceIgnoreCase($"[{pair.Key}]", pair.Value).ReplaceIgnoreCase(pair.Key, pair.Value));
		}

		/// <summary>
		/// Available functions.
		/// </summary>
		public static IEnumerable<string> Functions => _funcReplaces.CachedKeys;

		private const string _prefix = nameof(MathHelper) + ".";
		private static readonly CachedSynchronizedDictionary<string, string> _funcReplaces = new(StringComparer.InvariantCultureIgnoreCase)
		{
			{ "abs", _prefix + nameof(MathHelper.Abs) },
			{ "acos", _prefix + nameof(MathHelper.Acos) },
			{ "asin", _prefix + nameof(MathHelper.Asin) },
			{ "atan", _prefix + nameof(MathHelper.Atan) },
			{ "ceiling", _prefix + nameof(MathHelper.Ceiling) },
			{ "cos", _prefix + nameof(MathHelper.Cos) },
			{ "exp", _prefix + nameof(MathHelper.Exp) },
			{ "floor", _prefix + nameof(MathHelper.Floor) },
			//{ "ieeeremainder", _prefix + nameof(MathHelper.IEEERemainer) },
			{ "log", _prefix + nameof(MathHelper.Log) },
			{ "log10", _prefix + nameof(MathHelper.Log10) },
			{ "max", _prefix + nameof(MathHelper.Max) },
			{ "min", _prefix + nameof(MathHelper.Min) },
			{ "pow", _prefix + nameof(MathHelper.Pow) },
			{ "round", _prefix + nameof(MathHelper.Round) },
			{ "sign", _prefix + nameof(MathHelper.Sign) },
			{ "sin", _prefix + nameof(MathHelper.Sin) },
			{ "sqrt", _prefix + nameof(MathHelper.Sqrt) },
			{ "tan", _prefix + nameof(MathHelper.Tan) },
			{ "truncate", _prefix + nameof(MathHelper.Truncate) },
		};

		private class ErrorExpressionFormula<TResult> : ExpressionFormula<TResult>
		{
			public ErrorExpressionFormula(string error)
				: base(error)
			{
			}

			public override TResult Calculate(decimal[] prices)
				=> throw new NotSupportedException(Error);
		}

		public static ExpressionFormula<TResult> CreateError<TResult>(string errorText)
			=> new ErrorExpressionFormula<TResult>(errorText);

		private static string Escape(Regex idRegex, string text, bool useIds, out IEnumerable<string> identifiers)
		{
			static string ReplaceFuncs(string text)
			{
				var dict = new Dictionary<string, string>();

				foreach (var pair in _funcReplaces.CachedPairs)
				{
					var what = pair.Key + "(";

					if (!text.ContainsIgnoreCase(what))
						continue;

					var rnd = TypeHelper.GenerateSalt(16).Base64();

					dict.Add(rnd, pair.Value + "(");
					text = text.ReplaceIgnoreCase(what, rnd);
				}

				foreach (var pair in dict)
				{
					text = text.ReplaceIgnoreCase(pair.Key, pair.Value);
				}

				return text;
			}

			if (text.IsEmptyOrWhiteSpace())
				throw new ArgumentNullException(nameof(text));

			if (useIds)
			{
				text = Decode(text.ToUpperInvariant(), out _);
				identifiers = GetIds(idRegex, text).Distinct().ToArray();

				var i = 0;
				foreach (var id in identifiers)
				{
					text = text.ReplaceIgnoreCase(id, $"values[{i}]");
					i++;
				}

				if (i == 0)
					throw new InvalidOperationException($"Expression '{text}' do not contains any identifiers.");

				return ReplaceFuncs(text);
			}
			else
			{
				//var textWithoutFunctions = _funcReplaces
				//	.CachedPairs
				//	.Aggregate(text, (current, pair) => current.ReplaceIgnoreCase(pair.Key, string.Empty));

				const string dotSep = "__DOT__";

				text = text.Replace(".", dotSep);

				var groups = GetVariableNames(text)
					.Where(g => !g.Value.ContainsIgnoreCase(dotSep) && !long.TryParse(g.Value, out _) && !_funcReplaces.ContainsKey(g.Value))
					.ToArray();

				var dict = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);

				foreach (var g in groups.OrderByDescending(g => g.Index))
				{
					if (!dict.TryGetValue(g.Value, out var i))
					{
						i = dict.Count;
						dict.Add(g.Value, i);
					}

					text = text.Remove(g.Index, g.Length).Insert(g.Index, $"values[{i}]");
				}

				identifiers = dict.Keys.ToArray();

				text = text.Replace(dotSep, ".");

				return ReplaceFuncs(text);
			}
		}

		private const string _template = @"using System;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Compilation.Expressions;

class TempExpressionFormula : ExpressionFormula<__result_type>
{
	public TempExpressionFormula(string expression, IEnumerable<string> identifiers)
		: base(expression, identifiers)
	{
	}

	public override __result_type Calculate(decimal[] values)
	{
		return __insert_code;
	}
}";
		/// <summary>
		/// Compile mathematical formula.
		/// </summary>
		/// <param name="compiler"><see cref="ICompiler"/>.</param>
		/// <param name="context"><see cref="AssemblyLoadContextVisitor"/>.</param>
		/// <param name="expression">Text expression.</param>
		/// <param name="useIds">Use ids as variables.</param>
		/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
		/// <returns>Compiled mathematical formula.</returns>
		public static ExpressionFormula<decimal> Compile(this ICompiler compiler, AssemblyLoadContextVisitor context, string expression, bool useIds, CancellationToken cancellationToken = default)
			=> Compile<decimal>(compiler, context, expression, IdRegex, useIds, cancellationToken);

		/// <summary>
		/// Compile mathematical formula.
		/// </summary>
		/// <typeparam name="TResult">Result type.</typeparam>
		/// <param name="compiler"><see cref="ICompiler"/>.</param>
		/// <param name="context"><see cref="AssemblyLoadContextVisitor"/>.</param>
		/// <param name="expression">Text expression.</param>
		/// <param name="useIds">Use ids as variables.</param>
		/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
		/// <returns>Compiled mathematical formula.</returns>
		public static ExpressionFormula<TResult> Compile<TResult>(this ICompiler compiler, AssemblyLoadContextVisitor context, string expression, Regex idRegex, bool useIds, CancellationToken cancellationToken = default)
		{
			if (compiler is null)
				throw new ArgumentNullException(nameof(compiler));

			try
			{
				var code = Escape(idRegex, expression, useIds, out var identifiers);

				var refs = new HashSet<string>(new[]
				{
					typeof(object).Assembly.Location,
					typeof(ExpressionHelper).Assembly.Location,
					typeof(MathHelper).Assembly.Location,
					"System.Runtime.dll".ToFullRuntimePath(),
				}, StringComparer.InvariantCultureIgnoreCase);

				var result = compiler.Compile(context, "IndexExpression", _template.Replace("__insert_code", code).Replace("__result_type", typeof(TResult).TryGetCSharpAlias() ?? typeof(TResult).Name), refs, cancellationToken);

				var formula = result.Assembly is null
					? new ErrorExpressionFormula<TResult>(result.Errors.Where(e => e.Type == CompilationErrorTypes.Error).Select(e => e.Message).JoinNL())
					: result.Assembly.GetType("TempExpressionFormula").CreateInstance<ExpressionFormula<TResult>>(expression, identifiers);

				return formula;
			}
			catch (Exception ex)
			{
				return new ErrorExpressionFormula<TResult>(ex.ToString());
			}
		}
	}
}