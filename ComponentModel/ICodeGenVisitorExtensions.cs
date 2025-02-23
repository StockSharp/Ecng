namespace Ecng.ComponentModel;

using System;

/// <summary>
/// Provides extension methods for <see cref="ICodeGenVisitor"/> for code generation tasks.
/// </summary>
public static class ICodeGenVisitorExtensions
{
	/// <summary>
	/// Adds an empty line to the code generation output.
	/// </summary>
	/// <param name="visitor">The code generation visitor.</param>
	/// <returns>The code generation visitor with the added empty line.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="visitor"/> is null.</exception>
	public static ICodeGenVisitor AddLine(this ICodeGenVisitor visitor)
	{
		if (visitor is null)
			throw new ArgumentNullException(nameof(visitor));

		return visitor.AddLine(string.Empty);
	}

	/// <summary>
	/// Adds an empty text line with the current indentation.
	/// </summary>
	/// <param name="visitor">The code generation visitor.</param>
	/// <returns>The code generation visitor with the added indented line.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="visitor"/> is null.</exception>
	public static ICodeGenVisitor AddWithIndent(this ICodeGenVisitor visitor)
	{
		if (visitor is null)
			throw new ArgumentNullException(nameof(visitor));

		return visitor.AddWithIndent(string.Empty);
	}

	private class IndentToken : IDisposable
	{
		private readonly ICodeGenVisitor _visitor;

		public IndentToken(ICodeGenVisitor visitor)
		{
			_visitor = visitor ?? throw new ArgumentNullException(nameof(visitor));
			_visitor.ChangeIndent(true);
		}

		void IDisposable.Dispose()
		{
			_visitor.ChangeIndent(false);
			GC.SuppressFinalize(this);
		}
	}

	/// <summary>
	/// Changes the indentation level of the code generation visitor.
	/// Returns an <see cref="IDisposable"/> token that resets the indentation when disposed.
	/// </summary>
	/// <param name="visitor">The code generation visitor.</param>
	/// <returns>An <see cref="IDisposable"/> token for managing the indentation scope.</returns>
	public static IDisposable ChangeIndent(this ICodeGenVisitor visitor)
		=> new IndentToken(visitor);
}
