//////////////////////////////////////////////////////////////////////////////
// This source code and all associated files and resources are copyrighted by
// the author(s). This source code and all associated files and resources may
// be used as long as they are used according to the terms and conditions set
// forth in The Code Project Open License (CPOL).
//
// Copyright (c) 2012 Jonathan Wood
// http://www.blackbeltcoder.com
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Ecng.Common
{
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
		private const int DelimiterIndex = 0;
		private const int QuoteIndex = 1;

		/// <summary>
		/// Gets/sets the character used for column delimiters.
		/// </summary>
		public char Delimiter
		{
			get => SpecialChars[DelimiterIndex];
			set => SpecialChars[DelimiterIndex] = value;
		}

		/// <summary>
		/// Gets/sets the character used for column quotes.
		/// </summary>
		public char Quote
		{
			get => SpecialChars[QuoteIndex];
			set => SpecialChars[QuoteIndex] = value;
		}
	}

	/// <summary>
	/// Class for reading from comma-separated-value (CSV) files
	/// </summary>
	public class CsvFileReader : CsvFileCommon, IDisposable
	{
		// Private members
		private readonly TextReader Reader;
		private string CurrLine;
		private int CurrPos;
		private readonly EmptyLineBehavior EmptyLineBehavior;
		private readonly string _lineSeparator;

		/// <summary>
		/// Initializes a new instance of the CsvFileReader class for the
		/// specified stream.
		/// </summary>
		/// <param name="stream">The stream to read from</param>
		/// <param name="lineSeparator">The line separator to use.</param>
		/// <param name="emptyLineBehavior">Determines how empty lines are handled</param>
		public CsvFileReader(Stream stream,
			string lineSeparator,
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
		public CsvFileReader(string path,
			string lineSeparator,
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
		public CsvFileReader(TextReader reader,
			string lineSeparator,
			EmptyLineBehavior emptyLineBehavior = EmptyLineBehavior.NoColumns)
		{
			if (lineSeparator.IsEmpty())
				throw new ArgumentNullException(nameof(lineSeparator));

			Reader = reader;
			_lineSeparator = lineSeparator;
			EmptyLineBehavior = emptyLineBehavior;
		}

		/// <summary>
		/// Reads a row of columns from the current CSV file. Returns false if no
		/// more data could be read because the end of the file was reached.
		/// </summary>
		/// <param name="columns">Collection to hold the columns read</param>
		public bool ReadRow(List<string> columns)
		{
			// Verify required argument
			if (columns is null)
				throw new ArgumentNullException(nameof(columns));

		ReadNextLine:
			// Read next line from the file
			CurrLine = Reader.ReadLine();
			CurrPos = 0;
			// Test for end of file
			if (CurrLine is null)
				return false;
			// Test for empty line
			if (CurrLine.Length == 0)
			{
				switch (EmptyLineBehavior)
				{
					case EmptyLineBehavior.NoColumns:
						columns.Clear();
						return true;
					case EmptyLineBehavior.Ignore:
						goto ReadNextLine;
					case EmptyLineBehavior.EndOfFile:
						return false;
				}
			}

			// Parse line
			string column;
			int numColumns = 0;
			while (true)
			{
				// Read next column
				if (CurrPos < CurrLine.Length && CurrLine[CurrPos] == Quote)
					column = ReadQuotedColumn();
				else
					column = ReadUnquotedColumn();
				// Add column to list
				if (numColumns < columns.Count)
					columns[numColumns] = column;
				else
					columns.Add(column);
				numColumns++;
				// Break if we reached the end of the line
				if (CurrLine is null || CurrPos == CurrLine.Length)
					break;
				// Otherwise skip delimiter
				Debug.Assert(CurrLine[CurrPos] == Delimiter);
				CurrPos++;
			}
			// Remove any unused columns from collection
			if (numColumns < columns.Count)
				columns.RemoveRange(numColumns, columns.Count - numColumns);
			// Indicate success
			return true;
		}

		/// <summary>
		/// Reads a quoted column by reading from the current line until a
		/// closing quote is found or the end of the file is reached. On return,
		/// the current position points to the delimiter or the end of the last
		/// line in the file. Note: CurrLine may be set to null on return.
		/// </summary>
		private string ReadQuotedColumn()
		{
			// Skip opening quote character
			Debug.Assert(CurrPos < CurrLine.Length && CurrLine[CurrPos] == Quote);
			CurrPos++;

			// Parse column
			StringBuilder builder = new();
			while (true)
			{
				while (CurrPos == CurrLine.Length)
				{
					// End of line so attempt to read the next line
					CurrLine = Reader.ReadLine();
					CurrPos = 0;
					// Done if we reached the end of the file
					if (CurrLine is null)
						return builder.ToString();
					// Otherwise, treat as a multi-line field
					builder.Append(_lineSeparator);
				}

				// Test for quote character
				if (CurrLine[CurrPos] == Quote)
				{
					// If two quotes, skip first and treat second as literal
					int nextPos = (CurrPos + 1);
					if (nextPos < CurrLine.Length && CurrLine[nextPos] == Quote)
						CurrPos++;
					else
						break;  // Single quote ends quoted sequence
				}
				// Add current character to the column
				builder.Append(CurrLine[CurrPos++]);
			}

			if (CurrPos < CurrLine.Length)
			{
				// Consume closing quote
				Debug.Assert(CurrLine[CurrPos] == Quote);
				CurrPos++;
				// Append any additional characters appearing before next delimiter
				builder.Append(ReadUnquotedColumn());
			}
			// Return column value
			return builder.ToString();
		}

		/// <summary>
		/// Reads an unquoted column by reading from the current line until a
		/// delimiter is found or the end of the line is reached. On return, the
		/// current position points to the delimiter or the end of the current
		/// line.
		/// </summary>
		private string ReadUnquotedColumn()
		{
			int startPos = CurrPos;
			CurrPos = CurrLine.IndexOf(Delimiter, CurrPos);
			if (CurrPos == -1)
				CurrPos = CurrLine.Length;
			if (CurrPos > startPos)
				return CurrLine.Substring(startPos, CurrPos - startPos);
			return string.Empty;
		}

		/// <inheritdoc />
		protected override void DisposeManaged()
		{
			Reader.Dispose();
			base.DisposeManaged();
		}
	}

	/// <summary>
	/// Class for writing to comma-separated-value (CSV) files.
	/// </summary>
	public class CsvFileWriter : CsvFileCommon, IDisposable
	{
		private StreamWriter Writer { get; }

		// Private members
		private string OneQuote;
		private string TwoQuotes;
		private string QuotedFormat;

		/// <summary>
		/// Initializes a new instance of the CsvFileWriter class for the
		/// specified stream.
		/// </summary>
		/// <param name="stream">The stream to write to</param>
		/// <param name="encoding">The text encoding.</param>
		public CsvFileWriter(Stream stream, Encoding encoding = null) {
			Writer = encoding != null ?
				new StreamWriter(stream, encoding) :
				new StreamWriter(stream);
		}

		/// <summary>
		/// Initializes a new instance of the CsvFileWriter class for the
		/// specified file path.
		/// </summary>
		/// <param name="path">The name of the CSV file to write to</param>
		public CsvFileWriter(string path)
		{
			Writer = new StreamWriter(path);
		}

		/// <summary>
		/// Writes a row of columns to the current CSV file.
		/// </summary>
		/// <param name="columns">The list of columns to write</param>
		public void WriteRow(IEnumerable<string> columns)
		{
			// Verify required argument
			if (columns is null)
				throw new ArgumentNullException(nameof(columns));

			var i = 0;

			// Write each column
			foreach (var c in columns)
			{
				// Add delimiter if this isn't the first column
				if (i > 0)
					Writer.Write(Delimiter);

				// Write this column
				Writer.Write(Encode(c ?? string.Empty));
				i++;
			}

			Writer.WriteLine();
		}

		/// <summary>
		/// Encodes a column's value for output.
		/// </summary>
		/// <param name="column"></param>
		/// <returns></returns>
		public string Encode(string column)
		{
			// Ensure we're using current quote character
			if (OneQuote is null || OneQuote[0] != Quote)
			{
				OneQuote = $"{Quote}";
				TwoQuotes = string.Format("{0}{0}", Quote);
				QuotedFormat = string.Format("{0}{{0}}{0}", Quote);
			}

			// Write this column
			if (column.IndexOfAny(SpecialChars) != -1)
				column = QuotedFormat.Put(column.Replace(OneQuote, TwoQuotes));

			return column;
		}

		/// <inheritdoc />
		protected override void DisposeManaged()
		{
			Writer.Dispose();
			base.DisposeManaged();
		}
	}
}
