namespace Ecng.Compilation.Expressions;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using Ecng.Collections;
using Ecng.Common;

/// <summary>
/// Extension class for <see cref="ExpressionFormula{TResult}"/>.
/// </summary>
[CLSCompliant(false)]
public static class ExpressionHelper
{
	private class Parser
	{
		private enum States
		{
			None,
			Name,
			SquareBracket,
			//Function,
		}

		private readonly Stack<States> _state = new();

		public (string expression, string[] variables) Parse(string input)
		{
			if (input.IsEmpty())
				throw new ArgumentNullException(nameof(input));

			var variables = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);

			var i = 0;
			var state = States.None;

			var code = new StringBuilder();
			var nameBuilder = new StringBuilder();

			void AppendVar(string name)
			{
				var varIdx = variables.SafeAdd(name, key => variables.Count);
				code.Append($"values[{varIdx}]");
			}

			States PushState(States newState)
			{
				_state.Push(state);
				return newState;
			}

			while (i < input.Length)
			{
				var c = input[i];

				switch (state)
				{
					case States.None:
					{
						switch (c)
						{
							case '[':
							{
								state = PushState(States.SquareBracket);

								if (!nameBuilder.IsEmpty())
									throw new InvalidOperationException($"Var is not empty and {nameBuilder}.");

								break;
							}

							case ']':
								code.Append(c);
								break;

							case '+':
							case '-':
							case '*':
							case '/':
							case '(':
							case ')':
							case '>':
							case '<':
							case '=':
							case '!':
							case '&':
							case '|':
							case ' ':
							case ',':
								code.Append(c);
								break;

							default:
								if (c.IsDigit() || c == '.' || (c == 'm' && i > 0 && input[i - 1].IsDigit()))
								{
									code.Append(c);
								}
								else
								{
									state = PushState(States.Name);
									nameBuilder.Append(c);
								}
								
								break;
						}

						break;
					}
					case States.Name:
					{
						switch (c)
						{
							case '+':
							case '-':
							case '*':
							case '/':
							case '(':
							case ')':
							case '>':
							case '<':
							case '=':
							case '!':
							case '&':
							case '|':
							case ' ':
							case ',':
							{
								var name = nameBuilder.GetAndClear();

								if (_funcReplaces.TryGetValue(name, out var replace))
								{
									code.Append(replace);
									//state = PushState(States.Function);
								}
								else
								{
									AppendVar(name);
								}

								state = _state.Pop();
								code.Append(c);
								break;
							}
							default:
								nameBuilder.Append(c);
								break;
						}

						break;
					}
					case States.SquareBracket:
					{
						switch (c)
						{
							case ']':
								state = _state.Pop();
								AppendVar(nameBuilder.GetAndClear());
								break;
							default:
								nameBuilder.Append(c);
								break;
						}

						break;
					}
					default:
						throw new InvalidOperationException($"State: {state}");
				}

				i++;
			}

			if (!nameBuilder.IsEmpty())
				AppendVar(nameBuilder.GetAndClear());

			return (code.ToString(), variables.Keys.ToArray());
		}
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

	private const string _template = @"using System;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Compilation.Expressions;

class TempExpressionFormula : ExpressionFormula<__result_type>
{
	public TempExpressionFormula(string expression, IEnumerable<string> variables)
		: base(expression, variables)
	{
	}

	public override __result_type Calculate(decimal[] values)
	{
		return __insert_code;
	}
}";

	/// <summary>
	/// Get variables from the expression.
	/// </summary>
	/// <param name="expression">Text expression.</param>
	/// <returns>Variables.</returns>
	public static string[] GetVariables(string expression)
	{
		var (_, variables) = new Parser().Parse(expression);

		return variables;
	}

	/// <summary>
	/// Compile mathematical formula.
	/// </summary>
	/// <typeparam name="TResult">Result type.</typeparam>
	/// <param name="compiler"><see cref="ICompiler"/>.</param>
	/// <param name="context"><see cref="AssemblyLoadContextVisitor"/>.</param>
	/// <param name="expression">Text expression.</param>
	/// <param name="cache"><see cref="ICompilerCache"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns>Compiled mathematical formula.</returns>
	public static ExpressionFormula<TResult> Compile<TResult>(this ICompiler compiler, AssemblyLoadContextVisitor context, string expression, ICompilerCache cache = default, CancellationToken cancellationToken = default)
	{
		if (compiler is null)
			throw new ArgumentNullException(nameof(compiler));

		try
		{
			var refs = new HashSet<string>(new[]
			{
				typeof(object).Assembly.Location,
				typeof(ExpressionHelper).Assembly.Location,
				typeof(MathHelper).Assembly.Location,
				"System.Runtime.dll".ToFullRuntimePath(),
			}, StringComparer.InvariantCultureIgnoreCase);

			var (code, variables) = new Parser().Parse(expression);

			var sources = new[] { _template.Replace("__insert_code", code).Replace("__result_type", typeof(TResult).TryGetCSharpAlias() ?? typeof(TResult).Name) };

			if (cache?.TryGetBuild(sources, refs, out var assembly) != true)
			{
				var result = compiler.Compile("IndexExpression", sources, refs, cancellationToken);

				assembly = result.Assembly;

				if (assembly is null)
					return ExpressionFormula<TResult>.CreateError(result.Errors.Where(e => e.Type == CompilationErrorTypes.Error).Select(e => e.Message).JoinNL());
				else
					cache?.AddBuild(sources, refs, assembly);
			}

			return context.LoadFromStream(assembly.To<Stream>()).GetType("TempExpressionFormula").CreateInstance<ExpressionFormula<TResult>>(expression, variables);
		}
		catch (Exception ex)
		{
			return ExpressionFormula<TResult>.CreateError(ex.ToString());
		}
	}
}