namespace Ecng.Common;

using System;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Represents a reference tuple with enumerable object values.
/// </summary>
public interface IRefTuple
{
	/// <summary>
	/// Gets or sets the values of the tuple.
	/// </summary>
	IEnumerable<object> Values { get; set; }
}

/// <summary>
/// Represents a pair of reference values.
/// </summary>
/// <typeparam name="TFirst">Type of the first element.</typeparam>
/// <typeparam name="TSecond">Type of the second element.</typeparam>
public class RefPair<TFirst, TSecond> : IRefTuple
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RefPair{TFirst, TSecond}"/> class.
	/// </summary>
	public RefPair()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RefPair{TFirst, TSecond}"/> class with specified values.
	/// </summary>
	/// <param name="first">The first element.</param>
	/// <param name="second">The second element.</param>
	public RefPair(TFirst first, TSecond second)
	{
		First = first;
		Second = second;
	}

	/// <summary>
	/// Gets or sets the first element.
	/// </summary>
	public TFirst First { get; set; }

	/// <summary>
	/// Gets or sets the second element.
	/// </summary>
	public TSecond Second { get; set; }

	/// <summary>
	/// Gets or sets the tuple values.
	/// </summary>
	public virtual IEnumerable<object> Values
	{
		get => [First, Second];
		set
		{
			First = (TFirst)value.ElementAt(0);
			Second = (TSecond)value.ElementAt(1);
		}
	}

	/// <summary>
	/// Returns a string that represents the tuple.
	/// </summary>
	/// <returns>A string representation of the tuple.</returns>
	public override string ToString()
		=> "[" + GetValuesString() + "]";

	/// <summary>
	/// Gets a string that represents the tuple values.
	/// </summary>
	/// <returns>A string representation of the tuple values.</returns>
	protected virtual string GetValuesString()
		=> First + ", " + Second;

	/// <summary>
	/// Converts the tuple to a KeyValuePair.
	/// </summary>
	/// <returns>A KeyValuePair containing the first and second elements.</returns>
	public KeyValuePair<TFirst, TSecond> ToValuePair()
		=> new(First, Second);
}

/// <summary>
/// Represents a triple of reference values.
/// </summary>
/// <typeparam name="TFirst">Type of the first element.</typeparam>
/// <typeparam name="TSecond">Type of the second element.</typeparam>
/// <typeparam name="TThird">Type of the third element.</typeparam>
public class RefTriple<TFirst, TSecond, TThird> : RefPair<TFirst, TSecond>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RefTriple{TFirst, TSecond, TThird}"/> class.
	/// </summary>
	public RefTriple()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RefTriple{TFirst, TSecond, TThird}"/> class with specified values.
	/// </summary>
	/// <param name="first">The first element.</param>
	/// <param name="second">The second element.</param>
	/// <param name="third">The third element.</param>
	public RefTriple(TFirst first, TSecond second, TThird third)
		: base(first, second)
	{
		Third = third;
	}

	/// <summary>
	/// Gets or sets the third element.
	/// </summary>
	public TThird Third { get; set; }

	/// <summary>
	/// Gets or sets the tuple values.
	/// </summary>
	public override IEnumerable<object> Values
	{
		get => base.Values.Concat([Third]);
		set
		{
			base.Values = value;
			Third = (TThird)value.ElementAt(2);
		}
	}

	/// <summary>
	/// Gets a string that represents the tuple values.
	/// </summary>
	/// <returns>A string representation of the tuple values.</returns>
	protected override string GetValuesString()
		=> base.GetValuesString() + ", " + Third;
}

/// <summary>
/// Represents a quadruple of reference values.
/// </summary>
/// <typeparam name="TFirst">Type of the first element.</typeparam>
/// <typeparam name="TSecond">Type of the second element.</typeparam>
/// <typeparam name="TThird">Type of the third element.</typeparam>
/// <typeparam name="TFourth">Type of the fourth element.</typeparam>
public class RefQuadruple<TFirst, TSecond, TThird, TFourth> : RefTriple<TFirst, TSecond, TThird>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RefQuadruple{TFirst, TSecond, TThird, TFourth}"/> class.
	/// </summary>
	public RefQuadruple()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RefQuadruple{TFirst, TSecond, TThird, TFourth}"/> class with specified values.
	/// </summary>
	/// <param name="first">The first element.</param>
	/// <param name="second">The second element.</param>
	/// <param name="third">The third element.</param>
	/// <param name="fourth">The fourth element.</param>
	public RefQuadruple(TFirst first, TSecond second, TThird third, TFourth fourth)
		: base(first, second, third)
	{
		Fourth = fourth;
	}

	/// <summary>
	/// Gets or sets the fourth element.
	/// </summary>
	public TFourth Fourth { get; set; }

	/// <summary>
	/// Gets or sets the tuple values.
	/// </summary>
	public override IEnumerable<object> Values
	{
		get => base.Values.Concat([Fourth]);
		set
		{
			base.Values = value;
			Fourth = (TFourth)value.ElementAt(3);
		}
	}

	/// <summary>
	/// Gets a string that represents the tuple values.
	/// </summary>
	/// <returns>A string representation of the tuple values.</returns>
	protected override string GetValuesString()
		=> base.GetValuesString() + ", " + Fourth;
}

/// <summary>
/// Represents a quintuple of reference values.
/// </summary>
/// <typeparam name="TFirst">Type of the first element.</typeparam>
/// <typeparam name="TSecond">Type of the second element.</typeparam>
/// <typeparam name="TThird">Type of the third element.</typeparam>
/// <typeparam name="TFourth">Type of the fourth element.</typeparam>
/// <typeparam name="TFifth">Type of the fifth element.</typeparam>
public class RefFive<TFirst, TSecond, TThird, TFourth, TFifth> : RefQuadruple<TFirst, TSecond, TThird, TFourth>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RefFive{TFirst, TSecond, TThird, TFourth, TFifth}"/> class.
	/// </summary>
	public RefFive()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RefFive{TFirst, TSecond, TThird, TFourth, TFifth}"/> class with specified values.
	/// </summary>
	/// <param name="first">The first element.</param>
	/// <param name="second">The second element.</param>
	/// <param name="third">The third element.</param>
	/// <param name="fourth">The fourth element.</param>
	/// <param name="fifth">The fifth element.</param>
	public RefFive(TFirst first, TSecond second, TThird third, TFourth fourth, TFifth fifth)
		: base(first, second, third, fourth)
	{
		Fifth = fifth;
	}

	/// <summary>
	/// Gets or sets the fifth element.
	/// </summary>
	public TFifth Fifth { get; set; }

	/// <summary>
	/// Gets or sets the tuple values.
	/// </summary>
	public override IEnumerable<object> Values
	{
		get => base.Values.Concat([Fifth]);
		set
		{
			base.Values = value;
			Fifth = (TFifth)value.ElementAt(4);
		}
	}

	/// <summary>
	/// Gets a string that represents the tuple values.
	/// </summary>
	/// <returns>A string representation of the tuple values.</returns>
	protected override string GetValuesString()
		=> base.GetValuesString() + ", " + Fifth;
}

/// <summary>
/// Provides factory methods to create reference tuples.
/// </summary>
public static class RefTuple
{
	/// <summary>
	/// Creates a new pair of reference values.
	/// </summary>
	/// <typeparam name="TFirst">Type of the first element.</typeparam>
	/// <typeparam name="TSecond">Type of the second element.</typeparam>
	/// <param name="first">The first element.</param>
	/// <param name="second">The second element.</param>
	/// <returns>A new instance of <see cref="RefPair{TFirst, TSecond}"/>.</returns>
	public static RefPair<TFirst, TSecond> Create<TFirst, TSecond>(TFirst first, TSecond second)
		=> new(first, second);

	/// <summary>
	/// Creates a new triple of reference values.
	/// </summary>
	/// <typeparam name="TFirst">Type of the first element.</typeparam>
	/// <typeparam name="TSecond">Type of the second element.</typeparam>
	/// <typeparam name="TThird">Type of the third element.</typeparam>
	/// <param name="first">The first element.</param>
	/// <param name="second">The second element.</param>
	/// <param name="third">The third element.</param>
	/// <returns>A new instance of <see cref="RefTriple{TFirst, TSecond, TThird}"/>.</returns>
	public static RefTriple<TFirst, TSecond, TThird> Create<TFirst, TSecond, TThird>(TFirst first, TSecond second, TThird third)
		=> new(first, second, third);

	/// <summary>
	/// Creates a new quadruple of reference values.
	/// </summary>
	/// <typeparam name="TFirst">Type of the first element.</typeparam>
	/// <typeparam name="TSecond">Type of the second element.</typeparam>
	/// <typeparam name="TThird">Type of the third element.</typeparam>
	/// <typeparam name="TFourth">Type of the fourth element.</typeparam>
	/// <param name="first">The first element.</param>
	/// <param name="second">The second element.</param>
	/// <param name="third">The third element.</param>
	/// <param name="fourth">The fourth element.</param>
	/// <returns>A new instance of <see cref="RefQuadruple{TFirst, TSecond, TThird, TFourth}"/>.</returns>
	public static RefQuadruple<TFirst, TSecond, TThird, TFourth> Create<TFirst, TSecond, TThird, TFourth>(TFirst first, TSecond second, TThird third, TFourth fourth)
		=> new(first, second, third, fourth);

	/// <summary>
	/// Creates a new quintuple of reference values.
	/// </summary>
	/// <typeparam name="TFirst">Type of the first element.</typeparam>
	/// <typeparam name="TSecond">Type of the second element.</typeparam>
	/// <typeparam name="TThird">Type of the third element.</typeparam>
	/// <typeparam name="TFourth">Type of the fourth element.</typeparam>
	/// <typeparam name="TFifth">Type of the fifth element.</typeparam>
	/// <param name="first">The first element.</param>
	/// <param name="second">The second element.</param>
	/// <param name="third">The third element.</param>
	/// <param name="fourth">The fourth element.</param>
	/// <param name="fifth">The fifth element.</param>
	/// <returns>A new instance of <see cref="RefFive{TFirst, TSecond, TThird, TFourth, TFifth}"/>.</returns>
	public static RefFive<TFirst, TSecond, TThird, TFourth, TFifth> Create<TFirst, TSecond, TThird, TFourth, TFifth>(TFirst first, TSecond second, TThird third, TFourth fourth, TFifth fifth)
		=> new(first, second, third, fourth, fifth);

	private static readonly RefFive<int, int, int, int, int> _t = new();

	/// <summary>
	/// Gets the name of the member at the specified index.
	/// </summary>
	/// <param name="idx">The index of the member.</param>
	/// <returns>The name of the corresponding member.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
	public static string GetName(int idx)
	{
		return idx switch
		{
			0 => nameof(_t.First),
			1 => nameof(_t.Second),
			2 => nameof(_t.Third),
			3 => nameof(_t.Fourth),
			4 => nameof(_t.Fifth),
			_ => throw new ArgumentOutOfRangeException(nameof(idx)),
		};
	}
}