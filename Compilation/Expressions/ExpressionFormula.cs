namespace Ecng.Compilation.Expressions
{
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
		/// <param name="identifiers">Identifiers.</param>
		protected ExpressionFormula(string expression, IEnumerable<string> identifiers)
		{
			Expression = expression.ThrowIfEmpty(nameof(expression));
			Identifiers = identifiers ?? throw new ArgumentNullException(nameof(identifiers));
		}

		internal ExpressionFormula(string error)
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
		/// Identifiers.
		/// </summary>
		public IEnumerable<string> Identifiers { get; }

		/// <summary>
		/// Available functions.
		/// </summary>
		public static IEnumerable<string> Functions => ExpressionHelper.Functions;

		/// <inheritdoc />
		public override string ToString() => Error ?? Expression;
	}
}