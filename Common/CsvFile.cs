//////////////////////////////////////////////////////////////////////////////
// This source code and all associated files and resources are copyrighted by
// the author(s). This source code and all associated files and resources may
// be used as long as they are used according to the terms and conditions set
// forth in The Code Project Open License (CPOL).
//
// Copyright (c) 2012 Jonathan Wood
// http://www.blackbeltcoder.com
//

using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ecng.Common;

/// <summary>
/// Determines how empty lines are interpreted when reading CSV files.
/// These values do not affect empty lines that occur within quoted fields
/// or empty lines that appear at the end of the input file.
/// </summary>
public enum EmptyLineBehavior
{
	/// <summary>
	/// Empty lines are interpreted as a line with zero columns.
	/// </summary>
	NoColumns,
	/// <summary>
	/// Empty lines are interpreted as a line with a single empty column.
	/// </summary>
	EmptyColumn,
	/// <summary>
	/// Empty lines are skipped over as though they did not exist.
	/// </summary>
	Ignore,
	/// <summary>
	/// An empty line is interpreted as the end of the input file.
	/// </summary>
	EndOfFile,
}

/// <summary>
/// Common base class for CSV reader and writer classes.
/// </summary>
public abstract class CsvFileCommon : Disposable
{
	/// <summary>
	/// These are special characters in CSV files. If a column contains any
	/// of these characters, the entire column is wrapped in double quotes.
	/// </summary>
	protected char[] SpecialChars = [';', '"', '\r', '\n'];

	// Indexes into SpecialChars for characters with specific meaning
	private const int _delimiterIndex = 0;
	private const int _quoteIndex = 1;

	/// <summary>
	/// Gets/sets the character used for column delimiters.
	/// </summary>
	public char Delimiter
	{
		get => SpecialChars[_delimiterIndex];
		set => SpecialChars[_delimiterIndex] = value;
	}

	/// <summary>
	/// Gets/sets the character used for column quotes.
	/// </summary>
	public char Quote
	{
		get => SpecialChars[_quoteIndex];
		set => SpecialChars[_quoteIndex] = value;
	}
}

/// <summary>
/// Class for reading from comma-separated-value (CSV) files
/// </summary>
public class CsvFileReader : CsvFileCommon, IDisposable
{
	// Private members
	private readonly TextReader _reader;
	private int _currPos;
	private readonly EmptyLineBehavior _emptyLineBehavior;
	private readonly string _lineSeparator;

	/// <summary>
	/// The current line being read.
	/// </summary>
	public string CurrLine { get; private set; }

	/// <summary>
	/// Initializes a new instance of the CsvFileReader class for the
	/// specified stream.
	/// </summary>
	/// <param name="stream">The stream to read from</param>
	/// <param name="lineSeparator">The line separator to use.</param>
	/// <param name="emptyLineBehavior">Determines how empty lines are handled</param>
	public CsvFileReader(Stream stream, string lineSeparator,
		EmptyLineBehavior emptyLineBehavior = EmptyLineBehavior.NoColumns)
		: this(new StreamReader(stream), lineSeparator, emptyLineBehavior)
	{
	}

	/// <summary>
	/// Initializes a new instance of the CsvFileReader class for the
	/// specified file path.
	/// </summary>
	/// <param name="path">The name of the CSV file to read from</param>
	/// <param name="lineSeparator">The line separator to use.</param>
	/// <param name="emptyLineBehavior">Determines how empty lines are handled</param>
	public CsvFileReader(string path, string lineSeparator,
		EmptyLineBehavior emptyLineBehavior = EmptyLineBehavior.NoColumns)
		: this(new StreamReader(path), lineSeparator, emptyLineBehavior)
	{
	}

	/// <summary>
	/// Initializes a new instance of the CsvFileReader class for the
	/// specified file path.
	/// </summary>
	/// <param name="reader">The file reader.</param>
	/// <param name="lineSeparator">The line separator to use.</param>
	/// <param name="emptyLineBehavior">Determines how empty lines are handled</param>
	public CsvFileReader(TextReader reader, string lineSeparator,
		EmptyLineBehavior emptyLineBehavior = EmptyLineBehavior.NoColumns)
	{
		if (lineSeparator.IsEmpty())
			throw new ArgumentNullException(nameof(lineSeparator));

		_reader = reader;
		_lineSeparator = lineSeparator;
		_emptyLineBehavior = emptyLineBehavior;
	}

	/// <summary>
	/// Reads a row of columns from the current CSV file.
	/// </summary>
	/// <param name="columns">Collection to hold the columns read</param>
	/// <returns><see langword="true"/> if a row was successfully read; <see langword="false"/> if the end of the file was reached.</returns>
	public bool ReadRow(List<string> columns)
		=> AsyncContext.Run(() => ReadRowAsync(columns, default));

	/// <summary>
	/// Asynchronously reads a row of columns from the current CSV file. Returns false if no
	/// more data could be read because the end of the file was reached.
	/// </summary>
	/// <param name="columns">Collection to hold the columns read</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	public async Task<bool> ReadRowAsync(List<string> columns, CancellationToken cancellationToken = default)
	{
		if (columns is null)
			throw new ArgumentNullException(nameof(columns));

	ReadNextLineAsync:
		CurrLine = await _reader.ReadLineAsync(
#if NET7_0_OR_GREATER
			cancellationToken
#else
#endif
		).ConfigureAwait(false);
		_currPos = 0;

		if (CurrLine is null)
			return false;

		if (CurrLine.Length == 0)
		{
			switch (_emptyLineBehavior)
			{
				case EmptyLineBehavior.NoColumns:
					columns.Clear();
					return true;
				case EmptyLineBehavior.Ignore:
					goto ReadNextLineAsync;
				case EmptyLineBehavior.EndOfFile:
					return false;
			}
		}

		string column;
		var numColumns = 0;

		while (true)
		{
			cancellationToken.ThrowIfCancellationRequested();
			
			if (_currPos < CurrLine.Length && CurrLine[_currPos] == Quote)
				column = await ReadQuotedColumnAsync(cancellationToken).ConfigureAwait(false);
			else
				column = ReadUnquotedColumn();
			
			if (numColumns < columns.Count)
				columns[numColumns] = column;
			else
				columns.Add(column);

			numColumns++;

			if (CurrLine is null || _currPos == CurrLine.Length)
				break;

			Debug.Assert(CurrLine[_currPos] == Delimiter);
			_currPos++;
		}

		if (numColumns < columns.Count)
			columns.RemoveRange(numColumns, columns.Count - numColumns);

		return true;
	}

	private async Task<string> ReadQuotedColumnAsync(CancellationToken cancellationToken)
	{
		Debug.Assert(_currPos < CurrLine.Length && CurrLine[_currPos] == Quote);
		_currPos++;

		StringBuilder builder = new();

		while (true)
		{
			cancellationToken.ThrowIfCancellationRequested();

			while (_currPos == CurrLine.Length)
			{
				CurrLine = await _reader.ReadLineAsync(
#if NET7_0_OR_GREATER
			cancellationToken
#else
#endif
				).ConfigureAwait(false);
				
				_currPos = 0;

				if (CurrLine is null)
					return builder.ToString();

				builder.Append(_lineSeparator);
			}

			if (CurrLine[_currPos] == Quote)
			{
				var nextPos = _currPos + 1;

				if (nextPos < CurrLine.Length && CurrLine[nextPos] == Quote)
					_currPos++;
				else
					break;
			}

			builder.Append(CurrLine[_currPos++]);
		}

		if (_currPos < CurrLine.Length)
		{
			Debug.Assert(CurrLine[_currPos] == Quote);

			_currPos++;
			builder.Append(ReadUnquotedColumn());
		}

		return builder.ToString();
	}

	private string ReadUnquotedColumn()
	{
		var startPos = _currPos;

		_currPos = CurrLine.IndexOf(Delimiter, _currPos);

		if (_currPos == -1)
			_currPos = CurrLine.Length;

		if (_currPos > startPos)
			return CurrLine.Substring(startPos, _currPos - startPos);

		return string.Empty;
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		_reader.Dispose();
		base.DisposeManaged();
	}
}

/// <summary>
/// Class for writing to comma-separated-value (CSV) files.
/// </summary>
public class CsvFileWriter : CsvFileCommon, IDisposable
{
	private readonly StreamWriter _writer;

	private string _oneQuote;
	private string _twoQuotes;
	private string _quotedFormat;

	/// <summary>
	/// Initializes a new instance of the CsvFileWriter class for the
	/// specified stream.
	/// </summary>
	/// <param name="stream">The stream to write to</param>
	/// <param name="encoding">The text encoding.</param>
	public CsvFileWriter(Stream stream, Encoding encoding = null)
	{
		_writer = encoding != null ? new(stream, encoding) : new(stream);
	}

	/// <summary>
	/// Initializes a new instance of the CsvFileWriter class for the
	/// specified file path.
	/// </summary>
	/// <param name="path">The name of the CSV file to write to</param>
	public CsvFileWriter(string path)
	{
		_writer = new(path);
	}

	/// <summary>
	/// Gets or sets the character used to separate lines in the CSV file.
	/// </summary>
	public string LineSeparator
	{
		get => _writer.NewLine;
		set => _writer.NewLine = value;
	}

	/// <summary>
	/// Writes a row of columns to the current CSV file.
	/// </summary>
	/// <param name="columns">The list of columns to write</param>
	public void WriteRow(IEnumerable<string> columns)
		=> AsyncContext.Run(() => WriteRowAsync(columns));

	/// <summary>
	/// Asynchronously writes a row of columns to the current CSV file.
	/// </summary>
	/// <param name="columns">The list of columns to write</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	public async Task WriteRowAsync(IEnumerable<string> columns, CancellationToken cancellationToken = default)
	{
		if (columns is null)
			throw new ArgumentNullException(nameof(columns));

		var i = 0;

		foreach (var c in columns)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (i > 0)
			{
				await _writer.WriteAsync(Delimiter.ToString()
#if NET7_0_OR_GREATER
					, cancellationToken
#else
#endif
				).ConfigureAwait(false);
			}

			await _writer.WriteAsync(Encode(c ?? string.Empty)
#if NET7_0_OR_GREATER
			, cancellationToken
#else
#endif
			).ConfigureAwait(false);

			i++;
		}

		await _writer.WriteLineAsync().ConfigureAwait(false);
	}

	/// <summary>
	/// Asynchronously flushes the writer.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	public Task FlushAsync(CancellationToken cancellationToken = default)
	{
		// StreamWriter's FlushAsync does not accept a CancellationToken in .NET Standard2.0;
		// the token is provided for API symmetry.
		return _writer.FlushAsync(
#if NET8_0_OR_GREATER
			cancellationToken
#else
#endif
		);
	}

	/// <summary>
	/// Encodes a column's value for output.
	/// </summary>
	/// <param name="column"></param>
	/// <returns></returns>
	public string Encode(string column)
	{
		// Ensure we're using current quote character
		if (_oneQuote is null || _oneQuote[0] != Quote)
		{
			_oneQuote = $"{Quote}";
			_twoQuotes = string.Format("{0}{0}", Quote);
			_quotedFormat = string.Format("{0}{{0}}{0}", Quote);
		}

		// Write this column
		if (column.IndexOfAny(SpecialChars) != -1)
			column = _quotedFormat.Put(column.Replace(_oneQuote, _twoQuotes));

		return column;
	}

	/// <summary>
	/// Clears all buffers for the current writer and causes any buffered data to be written to the underlying stream.
	/// </summary>
	public void Flush() => AsyncContext.Run(() => FlushAsync());

	/// <summary>
	/// Truncates the underlying stream used by the <see cref="CsvFileWriter"/> by clearing its content.
	/// </summary>
	public void Truncate() => _writer.Truncate();

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		_writer.Dispose();
		base.DisposeManaged();
	}
}