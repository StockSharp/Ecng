namespace Ecng.Interop;

using System;
using System.Collections.Generic;
using System.Text;

using Ecng.Common;

/// <summary>
/// Represents a collection of debug information fields that can be used for logging or debugging purposes.
/// </summary>
public class DebugStringInfo
{
	private readonly List<(string name, object value)> _fields = [];

	/// <summary>
	/// Adds a debug field with the specified name and value to the collection.
	/// </summary>
	/// <param name="name">The name of the debug field. Cannot be null or empty.</param>
	/// <param name="value">The value of the debug field. Cannot be null.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null or empty, or when <paramref name="value"/> is null.</exception>
	public void AddField(string name, object value)
	{
		if (name.IsEmpty())
			throw new ArgumentNullException(nameof(name));

		if (value == null)
			throw new ArgumentNullException(nameof(value));

		_fields.Add((name, value));
	}

	/// <inheritdoc />
	public override string ToString()
	{
		var sb = new StringBuilder();

		foreach (var (name, value) in _fields)
		{
			if (sb.Length > 0)
				sb.Append('|');

			sb.AppendFormat("{0}={1}", name, value);
		}

		return sb.ToString();
	}
}

