namespace Ecng.ComponentModel;

using System;
using System.Runtime.Serialization;

using Ecng.Common;
using Ecng.Serialization;

/// <summary>
/// Represents a price with a specific value and type.
/// </summary>
/// <remarks>
/// This class supports arithmetic operations, cloning, formatting, and persistence.
/// </remarks>
[Serializable]
[DataContract]
public class Price : Equatable<Price>, IPersistable, IOperable<Price>, IFormattable
{
	static Price()
	{
		Converter.AddTypedConverter<Price, decimal>(input => (decimal)input);
		Converter.AddTypedConverter<decimal, Price>(input => input);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Price"/> class.
	/// </summary>
	public Price() { }

	/// <summary>
	/// Initializes a new instance of the <see cref="Price"/> class with the specified value and type.
	/// </summary>
	/// <param name="value">The numeric value.</param>
	/// <param name="type">The price type (measure).</param>
	public Price(decimal value, PriceTypes type)
	{
		Value = value;
		Type = type;
	}

	/// <summary>
	/// Gets or sets the price type.
	/// </summary>
	[DataMember]
	public PriceTypes Type { get; set; }

	/// <summary>
	/// Gets or sets the numeric value of the price.
	/// </summary>
	[DataMember]
	public decimal Value { get; set; }

	/// <summary>
	/// Creates a copy of the current <see cref="Price"/>.
	/// </summary>
	/// <returns>A new <see cref="Price"/> instance that is a copy of this instance.</returns>
	public override Price Clone()
	{
		return new()
		{
			Type = Type,
			Value = Value,
		};
	}

	/// <summary>
	/// Compares the current instance with another <see cref="Price"/> object.
	/// </summary>
	/// <param name="other">The other <see cref="Price"/> to compare with.</param>
	/// <returns>
	/// A value less than zero if this instance is less than <paramref name="other"/>,
	/// zero if they are equal, or a value greater than zero if this instance is greater than <paramref name="other"/>.
	/// </returns>
	public override int CompareTo(Price other)
	{
		if (this == other)
			return 0;

		if (this < other)
			return -1;

		return 1;
	}

	/// <inheritdoc />
	public override int GetHashCode() => Type.GetHashCode() ^ Value.GetHashCode();

	/// <inheritdoc />
	public override bool Equals(object obj) => base.Equals(obj);

	/// <inheritdoc />
	protected override bool OnEquals(Price other)
		=> EqualsImpl(other);

	private bool EqualsImpl(Price p)
		=> Type == p.Type && Value == p.Value;

	/// <summary>
	/// Implicitly converts an <see cref="int"/> to a <see cref="Price"/>.
	/// </summary>
	/// <param name="value">The integer value to convert.</param>
	/// <returns>A new <see cref="Price"/> with the specified value.</returns>
	public static implicit operator Price(int value) => new() { Value = value };

	/// <summary>
	/// Implicitly converts a <see cref="decimal"/> to a <see cref="Price"/>.
	/// </summary>
	/// <param name="value">The decimal value to convert.</param>
	/// <returns>A new <see cref="Price"/> with the specified value.</returns>
	public static implicit operator Price(decimal value) => new() { Value = value };

	/// <summary>
	/// Explicitly converts a <see cref="Price"/> to a <see cref="double"/>.
	/// </summary>
	/// <param name="value">The <see cref="Price"/> to convert.</param>
	/// <returns>The double representation of the price value.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	public static explicit operator double(Price value)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value));

		return (double)(decimal)value;
	}

	/// <summary>
	/// Explicitly converts a <see cref="Price"/> to a nullable <see cref="double"/>.
	/// </summary>
	/// <param name="value">The <see cref="Price"/> to convert.</param>
	/// <returns>The nullable double representation of the price value, or null if <paramref name="value"/> is null.</returns>
	public static explicit operator double?(Price value)
	{
		if (value is null)
			return null;

		return (double)value;
	}

	/// <summary>
	/// Explicitly converts a <see cref="Price"/> to a <see cref="decimal"/>.
	/// </summary>
	/// <param name="value">The <see cref="Price"/> to convert.</param>
	/// <returns>The decimal representation of the price value.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	/// <exception cref="InvalidOperationException">Thrown when the price type is <see cref="PriceTypes.Percent"/>.</exception>
	public static explicit operator decimal(Price value)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value));

		if (value.IsPercent)
			throw new InvalidOperationException(nameof(PriceTypes.Percent));

		return value.Value;
	}

	/// <summary>
	/// Explicitly converts a <see cref="Price"/> to a nullable <see cref="decimal"/>.
	/// </summary>
	/// <param name="value">The <see cref="Price"/> to convert.</param>
	/// <returns>The nullable decimal representation of the price value, or null if <paramref name="value"/> is null.</returns>
	public static explicit operator decimal?(Price value)
	{
		if (value is null)
			return null;

		return (decimal)value;
	}

	/// <summary>
	/// Determines whether two <see cref="Price"/> objects are not equal.
	/// </summary>
	/// <param name="v1">The first <see cref="Price"/> object.</param>
	/// <param name="v2">The second <see cref="Price"/> object.</param>
	/// <returns><c>true</c> if the prices are not equal; otherwise, <c>false</c>.</returns>
	public static bool operator !=(Price v1, Price v2)
	{
		if (v1 is null)
			return v2 is not null;

		if (v2 is null)
			return true;

		return !v1.EqualsImpl(v2);
	}

	/// <summary>
	/// Determines whether two <see cref="Price"/> objects are equal.
	/// </summary>
	/// <param name="v1">The first <see cref="Price"/> object.</param>
	/// <param name="v2">The second <see cref="Price"/> object.</param>
	/// <returns><c>true</c> if the prices are equal; otherwise, <c>false</c>.</returns>
	public static bool operator ==(Price v1, Price v2)
	{
		if (v1 is null)
			return v2 is null;

		if (v2 is null)
			return false;

		return v1.EqualsImpl(v2);
	}

	private static Price CreateResult(Price v1, Price v2, Func<decimal, decimal, decimal> operation, Func<decimal, decimal, decimal> percentOperation)
	{
		if (v1 is null)
			return null;

		if (v2 is null)
			return null;

		if (operation is null)
			throw new ArgumentNullException(nameof(operation));

		if (percentOperation is null)
			throw new ArgumentNullException(nameof(percentOperation));

		if (v1.IsLimit || v2.IsLimit)
			throw new ArgumentException("Limited value cannot participate in mathematical operations.");

		var result = new Price
		{
			Type = v1.Type,
		};

		if (v1.Type == v2.Type)
		{
			result.Value = operation(v1.Value, v2.Value);
		}
		else
		{
			result.Type = v1.IsPercent ? v2.Type : v1.Type;

			var nonPerValue = v1.IsPercent ? v2.Value : v1.Value;
			var perValue = v1.IsPercent ? v1.Value : v2.Value;

			result.Value = percentOperation(nonPerValue, perValue * nonPerValue.Abs() / 100.0m);
		}

		return result;
	}

	/// <summary>
	/// Adds two <see cref="Price"/> instances.
	/// </summary>
	/// <param name="v1">The first <see cref="Price"/>.</param>
	/// <param name="v2">The second <see cref="Price"/>.</param>
	/// <returns>The sum of the two prices.</returns>
	public static Price operator +(Price v1, Price v2)
		=> CreateResult(v1, v2, (v1, v2) => v1 + v2, (nonPer, per) => nonPer + per);

	/// <summary>
	/// Multiplies two <see cref="Price"/> instances.
	/// </summary>
	/// <param name="v1">The first <see cref="Price"/>.</param>
	/// <param name="v2">The second <see cref="Price"/>.</param>
	/// <returns>The product of the two prices.</returns>
	public static Price operator *(Price v1, Price v2)
		=> CreateResult(v1, v2, (v1, v2) => v1 * v2, (nonPer, per) => nonPer * per);

	/// <summary>
	/// Subtracts one <see cref="Price"/> from another.
	/// </summary>
	/// <param name="v1">The <see cref="Price"/> to subtract from.</param>
	/// <param name="v2">The <see cref="Price"/> to subtract.</param>
	/// <returns>The difference of the two prices.</returns>
	public static Price operator -(Price v1, Price v2)
		=> CreateResult(v1, v2, (v1, v2) => v1 - v2, (nonPer, per) => (v1.IsPercent ? (per - nonPer) : (nonPer - per)));

	/// <summary>
	/// Divides one <see cref="Price"/> by another.
	/// </summary>
	/// <param name="v1">The numerator <see cref="Price"/>.</param>
	/// <param name="v2">The denominator <see cref="Price"/>.</param>
	/// <returns>The quotient of the division.</returns>
	public static Price operator /(Price v1, Price v2)
		=> CreateResult(v1, v2, (v1, v2) => v1 / v2, (nonPer, per) => v1.IsPercent ? per / nonPer : nonPer / per);

	private static bool? MoreThan(Price v1, Price v2)
	{
		if (v1 is null)
			return null;

		if (v2 is null)
			return null;

		if (v1.Type != v2.Type)
			throw new ArgumentException($"{v1.Type}!={v2.Type}");

		return v1.Value > v2.Value;
	}

	/// <summary>
	/// Determines whether one <see cref="Price"/> is greater than another.
	/// </summary>
	/// <param name="v1">The first <see cref="Price"/>.</param>
	/// <param name="v2">The second <see cref="Price"/>.</param>
	/// <returns><c>true</c> if <paramref name="v1"/> is greater than <paramref name="v2"/>; otherwise, <c>false</c>.</returns>
	public static bool operator >(Price v1, Price v2) => MoreThan(v1, v2) == true;

	/// <summary>
	/// Determines whether one <see cref="Price"/> is greater than or equal to another.
	/// </summary>
	/// <param name="v1">The first <see cref="Price"/>.</param>
	/// <param name="v2">The second <see cref="Price"/>.</param>
	/// <returns><c>true</c> if <paramref name="v1"/> is greater than or equal to <paramref name="v2"/>; otherwise, <c>false</c>.</returns>
	public static bool operator >=(Price v1, Price v2) => v1 == v2 || v1 > v2;

	/// <summary>
	/// Determines whether one <see cref="Price"/> is less than another.
	/// </summary>
	/// <param name="v1">The first <see cref="Price"/>.</param>
	/// <param name="v2">The second <see cref="Price"/>.</param>
	/// <returns><c>true</c> if <paramref name="v1"/> is less than <paramref name="v2"/>; otherwise, <c>false</c>.</returns>
	public static bool operator <(Price v1, Price v2) => MoreThan(v2, v1) == true;

	/// <summary>
	/// Determines whether one <see cref="Price"/> is less than or equal to another.
	/// </summary>
	/// <param name="v1">The first <see cref="Price"/>.</param>
	/// <param name="v2">The second <see cref="Price"/>.</param>
	/// <returns><c>true</c> if <paramref name="v1"/> is less than or equal to <paramref name="v2"/>; otherwise, <c>false</c>.</returns>
	public static bool operator <=(Price v1, Price v2) => v1 == v2 || MoreThan(v2, v1) == true;

	Price IOperable<Price>.Add(Price other) => this + other;
	Price IOperable<Price>.Subtract(Price other) => this - other;
	Price IOperable<Price>.Multiply(Price other) => this * other;
	Price IOperable<Price>.Divide(Price other) => this / other;

	/// <summary>
	/// Returns a <see cref="Price"/> whose value is the negation of the specified price.
	/// </summary>
	/// <param name="v">The <see cref="Price"/> to negate.</param>
	/// <returns>A new <see cref="Price"/> with the opposite sign.</returns>
	public static Price operator -(Price v)
	{
		if (v is null)
			return null;

		return new()
		{
			Type = v.Type,
			Value = -v.Value
		};
	}

	private bool IsPercent => Type == PriceTypes.Percent;
	private bool IsLimit => Type == PriceTypes.Limit;

	/// <inheritdoc />
	public override string ToString() => ToString(null, null);

	/// <inheritdoc />
	public string ToString(string format, IFormatProvider formatProvider)
	{
		var str = Value.ToString(format, formatProvider);

		if (Type != PriceTypes.Absolute)
			str += IsPercent ? PercentChar : LimitChar;

		return str;
	}

	/// <summary>
	/// Percent sign.
	/// </summary>
	public const char PercentChar = '%';

	/// <summary>
	/// Limit sign.
	/// </summary>
	public const char LimitChar = 'l';

	void IPersistable.Load(SettingsStorage storage)
	{
		Type = storage.GetValue<PriceTypes>(nameof(Type));
		Value = storage.GetValue<decimal>(nameof(Value));
	}

	void IPersistable.Save(SettingsStorage storage)
	{
		storage.Set(nameof(Type), Type.To<string>());
		storage.Set(nameof(Value), Value);
	}
}