namespace Ecng.ComponentModel;

using System;

public static class ICodeGenVisitorExtensions
{
	public static ICodeGenVisitor AddLine(this ICodeGenVisitor visitor)
	{
		if (visitor is null)
			throw new ArgumentNullException(nameof(visitor));

		return visitor.AddLine(string.Empty);
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

	public static IDisposable ChangeIndent(this ICodeGenVisitor visitor)
		=> new IndentToken(visitor);
}
