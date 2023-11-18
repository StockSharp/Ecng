namespace Ecng.ComponentModel;

using System;
using System.IO;
using System.Text;

/// <summary>
/// Code generation visitor.
/// </summary>
public interface ICodeGenVisitor
{
	int CurrIndent { get; }

	ICodeGenVisitor ChangeIndent(bool increase);

	/// <summary>
	/// Add code line.
	/// </summary>
	/// <param name="line">Code line.</param>
	/// <returns><see cref="ICodeGenVisitor"/></returns>
	ICodeGenVisitor AddLine(string line);

	/// <summary>
	/// Add text.
	/// </summary>
	/// <param name="text">Text.</param>
	/// <returns><see cref="ICodeGenVisitor"/></returns>
	ICodeGenVisitor Add(string text);

	ICodeGenVisitor AddWithIndent(string text);
}

public abstract class BaseCodeGenVisitor : ICodeGenVisitor
{
	private string _indent = string.Empty;

	protected BaseCodeGenVisitor()
    {
    }

	int ICodeGenVisitor.CurrIndent => _indent.Length;

	ICodeGenVisitor ICodeGenVisitor.AddLine(string text)
	{
		if (text == "}")
			ChangeIndent(false);

		WriteLine($"{_indent}{text}");

		if (text == "{")
			ChangeIndent(true);

		return this;
	}

	ICodeGenVisitor ICodeGenVisitor.AddWithIndent(string text)
	{
		Write($"{_indent}{text}");
		return this;
	}

	ICodeGenVisitor ICodeGenVisitor.Add(string text)
	{
		Write(text);
		return this;
	}

	protected abstract void WriteLine(string text);
	protected abstract void Write(string text);

	public ICodeGenVisitor ChangeIndent(bool increase)
	{
		if (increase)
			_indent += "\t";
		else
		{
			if (_indent.Length == 0)
				throw new InvalidOperationException("Mismatched closing brace.");

			_indent = _indent.Substring(0, _indent.Length - 1);
		}

		return this;
	}
}

/// <summary>
/// <see cref="ICodeGenVisitor"/> implementation for <see cref="StringBuilder"/>.
/// </summary>
public class StringBuilderCodeGenVisitor : BaseCodeGenVisitor
{
	private readonly StringBuilder _builder;

	/// <summary>
	/// Initializes a new instance of the <see cref="StringBuilderCodeGenVisitor"/>.
	/// </summary>
	/// <param name="builder"><see cref="StringBuilder"/></param>
	public StringBuilderCodeGenVisitor(StringBuilder builder)
    {
		_builder = builder ?? throw new ArgumentNullException(nameof(builder));
	}

	protected override void WriteLine(string text)
		=> _builder.AppendLine(text);

	protected override void Write(string text)
		=> _builder.Append(text);
}

/// <summary>
/// <see cref="ICodeGenVisitor"/> implementation for <see cref="StreamWriter"/>.
/// </summary>
public class StreamWriterCodeGenVisitor : BaseCodeGenVisitor
{
	private readonly StreamWriter _writer;

	/// <summary>
	/// Initializes a new instance of the <see cref="StreamWriterCodeGenVisitor"/>.
	/// </summary>
	/// <param name="writer"><see cref="StringBuilder"/></param>
	public StreamWriterCodeGenVisitor(StreamWriter writer)
	{
		_writer = writer ?? throw new ArgumentNullException(nameof(writer));
	}

	protected override void WriteLine(string text)
		=> _writer.WriteLine(text);

	protected override void Write(string text)
		=> _writer.Write(text);
}