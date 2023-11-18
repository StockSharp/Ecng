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
}
