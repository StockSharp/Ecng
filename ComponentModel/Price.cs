namespace Ecng.ComponentModel;

using System;
using System.Runtime.Serialization;

using Ecng.Common;
using Ecng.Serialization;

[Serializable]
[DataContract]
public class Price : Equatable<Price>, IPersistable, IOperable<Price>
{
	public Price() { }

	public Price(decimal value, PriceTypes type)
	{
		Value = value;
		Type = type;
	}

	/// <summary>
	/// Measure value.
	/// </summary>
	[DataMember]
	public PriceTypes Type { get; set; }

	/// <summary>
	/// Value.
	/// </summary>
	[DataMember]
	public decimal Value { get; set; }

	/// <summary>
	/// Create a copy of <see cref="Price"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Price Clone()
	{
		return new()
		{
			Type = Type,
			Value = Value,
		};
	}

	/// <summary>
	/// Compare <see cref="Price"/> on the equivalence.
	/// </summary>
	/// <param name="other">Another value with which to compare.</param>
	/// <returns>The result of the comparison.</returns>
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
	/// Cast <see cref="int"/> object to the type <see cref="Price"/>.
	/// </summary>
	/// <param name="value"><see cref="int"/> value.</param>
	/// <returns>Object <see cref="Price"/>.</returns>
	public static implicit operator Price(int value) => new() { Value = value };

	/// <summary>
	/// Cast <see cref="decimal"/> object to the type <see cref="Price"/>.
	/// </summary>
	/// <param name="value"><see cref="decimal"/> value.</param>
	/// <returns>Object <see cref="Price"/>.</returns>
	public static implicit operator Price(decimal value) => new() { Value = value };

	/// <summary>
	/// Cast object from <see cref="Price"/> to <see cref="double"/>.
	/// </summary>
	/// <param name="value">Object <see cref="Price"/>.</param>
	/// <returns><see cref="double"/> value.</returns>
	public static explicit operator double(Price value)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value));

		return (double)(decimal)value;
	}

	/// <summary>
	/// Cast object from <see cref="Price"/> to nullable <see cref="double"/>.
	/// </summary>
	/// <param name="value">Object <see cref="Price"/>.</param>
	/// <returns>Nullable <see cref="double"/> value.</returns>
	public static explicit operator double?(Price value)
	{
		if (value is null)
			return null;

		return (double)value;
	}

	/// <summary>
	/// Cast object from <see cref="Price"/> to <see cref="decimal"/>.
	/// </summary>
	/// <param name="value">Object <see cref="Price"/>.</param>
	/// <returns><see cref="decimal"/> value.</returns>
	public static explicit operator decimal(Price value)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value));

		if (value.IsPercent)
			throw new InvalidOperationException(nameof(PriceTypes.Percent));

		return value.Value;
	}

	/// <summary>
	/// Cast object from <see cref="Price"/> to nullable <see cref="decimal"/>.
	/// </summary>
	/// <param name="value">Object <see cref="Price"/>.</param>
	/// <returns>Nullable <see cref="decimal"/> value.</returns>
	public static explicit operator decimal?(Price value)
	{
		if (value is null)
			return null;

		return (decimal)value;
	}

	/// <summary>
	/// Compare two values in the inequality (if the value of different types, the conversion will be used).
	/// </summary>
	/// <param name="v1">First value.</param>
	/// <param name="v2">Second value.</param>
	/// <returns><see langword="true" />, if the values are equals, otherwise, <see langword="false" />.</returns>
	public static bool operator !=(Price v1, Price v2)
	{
		if (v1 is null)
			return v2 is not null;

		if (v2 is null)
			return true;

		return v1.EqualsImpl(v2);
	}

	/// <summary>
	/// Compare two values for equality (if the value of different types, the conversion will be used).
	/// </summary>
	/// <param name="v1">First value.</param>
	/// <param name="v2">Second value.</param>
	/// <returns><see langword="true" />, if the values are equals, otherwise, <see langword="false" />.</returns>
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
	/// Add the two objects <see cref="Price"/>.
	/// </summary>
	/// <param name="v1">First object <see cref="Price"/>.</param>
	/// <param name="v2">Second object <see cref="Price"/>.</param>
	/// <returns>The result of addition.</returns>
	public static Price operator +(Price v1, Price v2)
		=> CreateResult(v1, v2, (v1, v2) => v1 + v2, (nonPer, per) => nonPer + per);

	/// <summary>
	/// Multiply the two objects <see cref="Price"/>.
	/// </summary>
	/// <param name="v1">First object <see cref="Price"/>.</param>
	/// <param name="v2">Second object <see cref="Price"/>.</param>
	/// <returns>The result of the multiplication.</returns>
	public static Price operator *(Price v1, Price v2)
		=> CreateResult(v1, v2, (v1, v2) => v1 * v2, (nonPer, per) => nonPer * per);

	/// <summary>
	/// Subtract the value <see cref="Price"/> from another.
	/// </summary>
	/// <param name="v1">First object <see cref="Price"/>.</param>
	/// <param name="v2">Second object <see cref="Price"/>.</param>
	/// <returns>The result of the subtraction.</returns>
	public static Price operator -(Price v1, Price v2)
		=> CreateResult(v1, v2, (v1, v2) => v1 - v2, (nonPer, per) => (v1.IsPercent ? (per - nonPer) : (nonPer - per)));

	/// <summary>
	/// Divide the value <see cref="Price"/> to another.
	/// </summary>
	/// <param name="v1">First object <see cref="Price"/>.</param>
	/// <param name="v2">Second object <see cref="Price"/>.</param>
	/// <returns>The result of the division.</returns>
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

	public static bool operator >(Price v1, Price v2) => MoreThan(v1, v2) == true;
	public static bool operator >=(Price v1, Price v2) => v1 == v2 || v1 > v2;
	public static bool operator <(Price v1, Price v2) => MoreThan(v2, v1) == true;
	public static bool operator <=(Price v1, Price v2) => v1 == v2 || MoreThan(v2, v1) == true;

	Price IOperable<Price>.Add(Price other) => this + other;
	Price IOperable<Price>.Subtract(Price other) => this - other;
	Price IOperable<Price>.Multiply(Price other) => this * other;
	Price IOperable<Price>.Divide(Price other) => this / other;

	/// <summary>
	/// Get the value with the opposite sign from the value <see cref="Price.Value"/>.
	/// </summary>
	/// <param name="v">Price.</param>
	/// <returns>Opposite value.</returns>
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
	public override string ToString()
	{
		var str = $"{Value}";

		if (Type != PriceTypes.Absolute)
			str += IsPercent ? PercentChar : LimitChar;

		return str;
	}

	public const char PercentChar = '%';
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