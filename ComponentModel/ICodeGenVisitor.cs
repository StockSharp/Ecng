namespace Ecng.ComponentModel;

using System;
using System.IO;
using System.Text;

/// <summary>
/// Code generation visitor.
/// </summary>
public interface ICodeGenVisitor
{
	/// <summary>
	/// Gets the current indentation level in characters.
	/// </summary>
	int CurrIndent { get; }

	/// <summary>
	/// Changes the current indentation level.
	/// </summary>
	/// <param name="increase">If set to <c>true</c>, increases the indentation; otherwise, decreases it.</param>
	/// <returns>The current instance of <see cref="ICodeGenVisitor"/>.</returns>
	ICodeGenVisitor ChangeIndent(bool increase);

	/// <summary>
	/// Adds a line of code with the current indentation.
	/// </summary>
	/// <param name="line">The code line to add.</param>
	/// <returns>The current instance of <see cref="ICodeGenVisitor"/>.</returns>
	ICodeGenVisitor AddLine(string line);

	/// <summary>
	/// Adds the specified text without a newline.
	/// </summary>
	/// <param name="text">The text to add.</param>
	/// <returns>The current instance of <see cref="ICodeGenVisitor"/>.</returns>
	ICodeGenVisitor Add(string text);

	/// <summary>
	/// Adds the specified text with the current indentation and without a newline.
	/// </summary>
	/// <param name="text">The text to add.</param>
	/// <returns>The current instance of <see cref="ICodeGenVisitor"/>.</returns>
	ICodeGenVisitor AddWithIndent(string text);
}

/// <summary>
/// Base class for code generation visitors. Provides common implementations for indentation 
/// management and writing code lines/text.
/// </summary>
public abstract class BaseCodeGenVisitor : ICodeGenVisitor
{
	private string _indent = string.Empty;

	/// <summary>
	/// Initializes a new instance of the <see cref="BaseCodeGenVisitor"/> class.
	/// </summary>
	protected BaseCodeGenVisitor()
	{
	}

	/// <inheritdoc/>
	int ICodeGenVisitor.CurrIndent => _indent.Length;

	/// <inheritdoc/>
	ICodeGenVisitor ICodeGenVisitor.AddLine(string text)
	{
		if (text == "}")
			ChangeIndent(false);

		WriteLine($"{_indent}{text}");

		if (text == "{")
			ChangeIndent(true);

		return this;
	}

	/// <inheritdoc/>
	ICodeGenVisitor ICodeGenVisitor.AddWithIndent(string text)
	{
		if (text == "}")
			ChangeIndent(false);

		Write($"{_indent}{text}");

		if (text == "{")
			ChangeIndent(true);

		return this;
	}

	/// <inheritdoc/>
	ICodeGenVisitor ICodeGenVisitor.Add(string text)
	{
		Write(text);
		return this;
	}

	/// <summary>
	/// Writes a line of text.
	/// </summary>
	/// <param name="text">The text to write.</param>
	protected abstract void WriteLine(string text);

	/// <summary>
	/// Writes text without a newline.
	/// </summary>
	/// <param name="text">The text to write.</param>
	protected abstract void Write(string text);

	/// <summary>
	/// Changes the current indentation level.
	/// </summary>
	/// <param name="increase">
	/// If <c>true</c>, increases the indentation by adding a tab character; if <c>false</c>, 
	/// decreases the indentation.
	/// </param>
	/// <returns>The current instance of <see cref="ICodeGenVisitor"/>.</returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown when attempting to decrease indentation below zero (mismatched closing brace).
	/// </exception>
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
/// Implements <see cref="ICodeGenVisitor"/> using a <see cref="StringBuilder"/> for code generation.
/// </summary>
/// <remarks>
/// Use this visitor when you prefer accumulating code into a <see cref="StringBuilder"/>.
/// </remarks>
/// <param name="builder">The <see cref="StringBuilder"/> instance used to build code.</param>
public class StringBuilderCodeGenVisitor(StringBuilder builder) : BaseCodeGenVisitor
{
	private readonly StringBuilder _builder = builder ?? throw new ArgumentNullException(nameof(builder));

	/// <summary>
	/// Writes a line to the underlying <see cref="StringBuilder"/>, followed by a newline.
	/// </summary>
	/// <param name="text">The text to write.</param>
	protected override void WriteLine(string text)
		=> _builder.AppendLine(text);

	/// <summary>
	/// Writes text to the underlying <see cref="StringBuilder"/> without a newline.
	/// </summary>
	/// <param name="text">The text to write.</param>
	protected override void Write(string text)
		=> _builder.Append(text);
}

/// <summary>
/// Implements <see cref="ICodeGenVisitor"/> using a <see cref="StreamWriter"/> for code generation.
/// </summary>
/// <remarks>
/// Use this visitor when you need to write code directly to a stream via <see cref="StreamWriter"/>.
/// </remarks>
/// <param name="writer">The <see cref="StreamWriter"/> instance used for outputting code.</param>
public class StreamWriterCodeGenVisitor(StreamWriter writer) : BaseCodeGenVisitor
{
	private readonly StreamWriter _writer = writer ?? throw new ArgumentNullException(nameof(writer));

	/// <summary>
	/// Writes a line to the underlying <see cref="StreamWriter"/>, followed by a newline.
	/// </summary>
	/// <param name="text">The text to write.</param>
	protected override void WriteLine(string text)
		=> _writer.WriteLine(text);

	/// <summary>
	/// Writes text to the underlying <see cref="StreamWriter"/> without a newline.
	/// </summary>
	/// <param name="text">The text to write.</param>
	protected override void Write(string text)
		=> _writer.Write(text);
}