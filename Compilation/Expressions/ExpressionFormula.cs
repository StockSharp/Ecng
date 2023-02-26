namespace Ecng.Compilation.Expressions;

using System;
using System.Collections.Generic;

using Ecng.Common;

/// <summary>
/// Compiled mathematical formula.
/// </summary>
/// <typeparam name="TResult">Result type.</typeparam>
public abstract class ExpressionFormula<TResult>
{
	/// <summary>
	/// To calculate the basket value.
	/// </summary>
	/// <param name="values">Inner values.</param>
	/// <returns>The basket value.</returns>
	public abstract TResult Calculate(decimal[] values);

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpressionFormula{TResult}"/>.
	/// </summary>
	/// <param name="expression">Mathematical formula.</param>
	/// <param name="variables">Variables.</param>
	protected ExpressionFormula(string expression, IEnumerable<string> variables)
	{
		Expression = expression.ThrowIfEmpty(nameof(expression));
		Variables = variables ?? throw new ArgumentNullException(nameof(variables));
	}

	private ExpressionFormula(string error)
	{
		Error = error.ThrowIfEmpty(nameof(error));
	}

	/// <summary>
	/// Mathematical formula.
	/// </summary>
	public string Expression { get; }

	/// <summary>
	/// Compilation error.
	/// </summary>
	public string Error { get; }

	/// <summary>
	/// Variables.
	/// </summary>
	public IEnumerable<string> Variables { get; }

	/// <inheritdoc />
	public override string ToString() => Error ?? Expression;

	private class ErrorExpressionFormula : ExpressionFormula<TResult>
	{
		public ErrorExpressionFormula(string error)
			: base(error)
		{
		}

		public override TResult Calculate(decimal[] prices)
			=> throw new NotSupportedException(Error);
	}

	public static ExpressionFormula<TResult> CreateError(string errorText)
		=> new ErrorExpressionFormula(errorText);
}