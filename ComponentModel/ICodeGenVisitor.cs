namespace Ecng.ComponentModel;

using System;
using System.Text;

/// <summary>
/// Code generation visitor.
/// </summary>
public interface ICodeGenVisitor
{
	/// <summary>
	/// Add code line.
	/// </summary>
	/// <param name="line">Code line.</param>
	/// <returns><see cref="IDiagramCodeGenVisitor"/></returns>
	ICodeGenVisitor AddLine(string line);
}

/// <summary>
/// <see cref="ICodeGenVisitor"/> implementation for <see cref="StringBuilder"/>.
/// </summary>
public class StringBuilderCodeGenVisitor : ICodeGenVisitor
{
	private readonly StringBuilder _builder;
	private string _indent = string.Empty;

	/// <summary>
	/// Initializes a new instance of the <see cref="StringBuilderCodeGenVisitor"/>.
	/// </summary>
	/// <param name="builder"><see cref="StringBuilder"/></param>
	public StringBuilderCodeGenVisitor(StringBuilder builder)
    {
		_builder = builder ?? throw new ArgumentNullException(nameof(builder));
	}

	ICodeGenVisitor ICodeGenVisitor.AddLine(string line)
	{
		if (line == "}")
		{
			if (_indent.Length == 0)
				throw new InvalidOperationException("Mismatched closing brace.");

			_indent = _indent.Substring(0, _indent.Length - 1);
		}
		
		_builder.AppendLine($"{_indent}{line}");

		if (line == "{")
			_indent += "\t";

		return this;
	}
}