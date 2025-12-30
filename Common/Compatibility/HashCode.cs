#if NETSTANDARD2_0
namespace System;

/// <summary>
/// Compatibility replacement for <see cref="System.HashCode"/> on platforms that don't provide it.
/// This is a simple, deterministic hash combiner suitable for use in GetHashCode implementations.
/// </summary>
public struct HashCode
{
	private int _hash;

	/// <summary>
	/// Adds a value into the hash computation.
	/// </summary>
	public void Add<T>(T value)
	{
		var h = value is null ? 0 : value.GetHashCode();
		unchecked { _hash = (_hash * 31) + h; }
	}

	/// <summary>
	/// Returns the computed hash code.
	/// </summary>
	public int ToHashCode() => _hash;

	/// <summary>
	/// Combines two values into a hash code.
	/// </summary>
	public static int Combine<T1, T2>(T1 v1, T2 v2)
	{
		var hc = new HashCode();
		hc.Add(v1);
		hc.Add(v2);
		return hc.ToHashCode();
	}

	/// <summary>
	/// Combines three values into a hash code.
	/// </summary>
	public static int Combine<T1, T2, T3>(T1 v1, T2 v2, T3 v3)
	{
		var hc = new HashCode();
		hc.Add(v1);
		hc.Add(v2);
		hc.Add(v3);
		return hc.ToHashCode();
	}

	/// <summary>
	/// Combines four values into a hash code.
	/// </summary>
	public static int Combine<T1, T2, T3, T4>(T1 v1, T2 v2, T3 v3, T4 v4)
	{
		var hc = new HashCode();
		hc.Add(v1);
		hc.Add(v2);
		hc.Add(v3);
		hc.Add(v4);
		return hc.ToHashCode();
	}
}
#endif