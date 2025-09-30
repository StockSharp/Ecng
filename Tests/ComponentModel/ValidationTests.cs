namespace Ecng.Tests.ComponentModel;

using System.ComponentModel.DataAnnotations;

using Ecng.ComponentModel;

[TestClass]
public class ValidationTests
{
	#region GreaterThanZero
	[TestMethod]
	public void GreaterThanZero_Int()
	{
		var attr = new IntGreaterThanZeroAttribute();
		attr.IsValid(1).AssertTrue();
		attr.IsValid(int.MaxValue).AssertTrue();
		attr.IsValid(0).AssertFalse();
		attr.IsValid(-1).AssertFalse();
		attr.IsValid("5").AssertTrue(); // convertible string
		attr.IsValid("0").AssertFalse();
		attr.IsValid("-2").AssertFalse();

		attr.IsValid(null).AssertFalse();
		attr.DisableNullCheck = true;
		attr.IsValid(null).AssertTrue();
	}

	[TestMethod]
	public void GreaterThanZero_Long()
	{
		var attr = new LongGreaterThanZeroAttribute();
		attr.IsValid(1L).AssertTrue();
		attr.IsValid(long.MaxValue).AssertTrue();
		attr.IsValid(0L).AssertFalse();
		attr.IsValid(-1L).AssertFalse();
		attr.IsValid(null).AssertFalse();
		attr.DisableNullCheck = true;
		attr.IsValid(null).AssertTrue();
	}

	[TestMethod]
	public void GreaterThanZero_Decimal()
	{
		var attr = new DecimalGreaterThanZeroAttribute();
		attr.IsValid(0.01m).AssertTrue();
		attr.IsValid(decimal.MaxValue).AssertTrue();
		attr.IsValid(0m).AssertFalse();
		attr.IsValid(-0.01m).AssertFalse();
		attr.IsValid(null).AssertFalse();
		attr.DisableNullCheck = true;
		attr.IsValid(null).AssertTrue();
	}

	[TestMethod]
	public void GreaterThanZero_Double()
	{
		var attr = new DoubleGreaterThanZeroAttribute();
		attr.IsValid(0.0001d).AssertTrue();
		attr.IsValid(double.MaxValue).AssertTrue();
		attr.IsValid(0d).AssertFalse();
		attr.IsValid(-0.0001d).AssertFalse();
		attr.IsValid(-0d).AssertFalse(); // -0 treated as 0 => not > 0
		attr.IsValid(null).AssertFalse();
		attr.DisableNullCheck = true;
		attr.IsValid(null).AssertTrue();
	}

	[TestMethod]
	public void GreaterThanZero_Float()
	{
		var attr = new FloatGreaterThanZeroAttribute();
		attr.IsValid(0.1f).AssertTrue();
		attr.IsValid(float.MaxValue).AssertTrue();
		attr.IsValid(0f).AssertFalse();
		attr.IsValid(-0.1f).AssertFalse();
		attr.IsValid(-0f).AssertFalse();
		attr.IsValid(null).AssertFalse();
		attr.DisableNullCheck = true;
		attr.IsValid(null).AssertTrue();
	}
	#endregion

	#region NotNegative
	[TestMethod]
	public void NotNegative_Int()
	{
		var attr = new IntNotNegativeAttribute();
		attr.IsValid(0).AssertTrue();
		attr.IsValid(1).AssertTrue();
		attr.IsValid(-1).AssertFalse();
		attr.IsValid("0").AssertTrue();
		attr.IsValid("-5").AssertFalse();
		attr.IsValid(null).AssertFalse();
		attr.DisableNullCheck = true;
		attr.IsValid(null).AssertTrue();
	}

	[TestMethod]
	public void NotNegative_Long()
	{
		var attr = new LongNotNegativeAttribute();
		attr.IsValid(0L).AssertTrue();
		attr.IsValid(1L).AssertTrue();
		attr.IsValid(-1L).AssertFalse();
		attr.IsValid(null).AssertFalse();
		attr.DisableNullCheck = true;
		attr.IsValid(null).AssertTrue();
	}

	[TestMethod]
	public void NotNegative_Decimal()
	{
		var attr = new DecimalNotNegativeAttribute();
		attr.IsValid(0m).AssertTrue();
		attr.IsValid(1.23m).AssertTrue();
		attr.IsValid(-0.0001m).AssertFalse();
		attr.IsValid(null).AssertFalse();
		attr.DisableNullCheck = true;
		attr.IsValid(null).AssertTrue();
	}

	[TestMethod]
	public void NotNegative_Double()
	{
		var attr = new DoubleNotNegativeAttribute();
		attr.IsValid(0d).AssertTrue();
		attr.IsValid(-0d).AssertTrue(); // -0 compares equal to 0 => not negative
		attr.IsValid(10d).AssertTrue();
		attr.IsValid(-0.1d).AssertFalse();
		attr.IsValid(null).AssertFalse();
		attr.DisableNullCheck = true;
		attr.IsValid(null).AssertTrue();
	}

	[TestMethod]
	public void NotNegative_Float()
	{
		var attr = new FloatNotNegativeAttribute();
		attr.IsValid(0f).AssertTrue();
		attr.IsValid(-0f).AssertTrue();
		attr.IsValid(10f).AssertTrue();
		attr.IsValid(-0.1f).AssertFalse();
		attr.IsValid(null).AssertFalse();
		attr.DisableNullCheck = true;
		attr.IsValid(null).AssertTrue();
	}
	#endregion

	#region NullOrMoreZero ( > 0 or null )
	[TestMethod]
	public void NullOrMoreZero_Int()
	{
		var attr = new IntNullOrMoreZeroAttribute();
		attr.IsValid(null).AssertTrue();
		attr.IsValid(1).AssertTrue();
		attr.IsValid(0).AssertFalse();
		attr.IsValid(-1).AssertFalse();
		attr.DisableNullCheck = true; // no effect
		attr.IsValid(null).AssertTrue();
	}

	[TestMethod]
	public void NullOrMoreZero_Long()
	{
		var attr = new LongNullOrMoreZeroAttribute();
		attr.IsValid(null).AssertTrue();
		attr.IsValid(100L).AssertTrue();
		attr.IsValid(0L).AssertFalse();
		attr.IsValid(-1L).AssertFalse();
		attr.DisableNullCheck = true;
		attr.IsValid(null).AssertTrue();
	}

	[TestMethod]
	public void NullOrMoreZero_Decimal()
	{
		var attr = new DecimalNullOrMoreZeroAttribute();
		attr.IsValid(null).AssertTrue();
		attr.IsValid(0.01m).AssertTrue();
		attr.IsValid(0m).AssertFalse();
		attr.IsValid(-0.01m).AssertFalse();
	}

	[TestMethod]
	public void NullOrMoreZero_Double()
	{
		var attr = new DoubleNullOrMoreZeroAttribute();
		attr.IsValid(null).AssertTrue();
		attr.IsValid(0.1d).AssertTrue();
		attr.IsValid(0d).AssertFalse();
		attr.IsValid(-0.1d).AssertFalse();
	}

	[TestMethod]
	public void NullOrMoreZero_Float()
	{
		var attr = new FloatNullOrMoreZeroAttribute();
		attr.IsValid(null).AssertTrue();
		attr.IsValid(1f).AssertTrue();
		attr.IsValid(0f).AssertFalse();
		attr.IsValid(-1f).AssertFalse();
	}
	#endregion

	#region NullOrNotNegative ( >=0 or null )
	[TestMethod]
	public void NullOrNotNegative_Int()
	{
		var attr = new IntNullOrNotNegativeAttribute();
		attr.IsValid(null).AssertTrue();
		attr.IsValid(0).AssertTrue();
		attr.IsValid(1).AssertTrue();
		attr.IsValid(-1).AssertFalse();
	}

	[TestMethod]
	public void NullOrNotNegative_Long()
	{
		var attr = new LongNullOrNotNegativeAttribute();
		attr.IsValid(null).AssertTrue();
		attr.IsValid(0L).AssertTrue();
		attr.IsValid(1L).AssertTrue();
		attr.IsValid(-1L).AssertFalse();
	}

	[TestMethod]
	public void NullOrNotNegative_Decimal()
	{
		var attr = new DecimalNullOrNotNegativeAttribute();
		attr.IsValid(null).AssertTrue();
		attr.IsValid(0m).AssertTrue();
		attr.IsValid(10m).AssertTrue();
		attr.IsValid(-0.00001m).AssertFalse();
	}

	[TestMethod]
	public void NullOrNotNegative_Double()
	{
		var attr = new DoubleNullOrNotNegativeAttribute();
		attr.IsValid(null).AssertTrue();
		attr.IsValid(0d).AssertTrue();
		attr.IsValid(-0d).AssertTrue();
		attr.IsValid(10d).AssertTrue();
		attr.IsValid(-0.1d).AssertFalse();
	}

	[TestMethod]
	public void NullOrNotNegative_Float()
	{
		var attr = new FloatNullOrNotNegativeAttribute();
		attr.IsValid(null).AssertTrue();
		attr.IsValid(0f).AssertTrue();
		attr.IsValid(-0f).AssertTrue();
		attr.IsValid(10f).AssertTrue();
		attr.IsValid(-0.1f).AssertFalse();
	}
	#endregion

	#region DisableNullCheck_Ignored_For_NullOrAttributes
	[TestMethod]
	public void DisableNullCheck_Ignored_NullOrVariants()
	{
		var a1 = new IntNullOrNotNegativeAttribute();
		var a2 = new IntNullOrMoreZeroAttribute();
		var a3 = new DecimalNullOrMoreZeroAttribute();
		var a4 = new DoubleNullOrNotNegativeAttribute();

		foreach (var a in new ValidationAttribute[] { a1, a2, a3, a4 })
		{
			a.IsValid(null).AssertTrue();
			(a as IValidator).DisableNullCheck = true; // should not change semantics
			a.IsValid(null).AssertTrue();
		}
	}
	#endregion

	#region ConversionFailure
	[TestMethod]
	public void ConversionFailure_ReturnsFalse()
	{
		var attr = new IntGreaterThanZeroAttribute();
		attr.IsValid("abc").AssertFalse(); // non-convertible string
		attr.IsValid(new object()).AssertFalse(); // arbitrary object
	}
	#endregion

	[TestMethod]
	public void Step_Basic_PositiveInt()
	{
		var attr = new StepAttribute(5m); // base=0, step=5
		attr.IsValid(0).AssertTrue();
		attr.IsValid(5).AssertTrue();
		attr.IsValid(10).AssertTrue();
		attr.IsValid(7).AssertFalse();
		attr.IsValid(-5).AssertTrue();
		attr.IsValid(-7).AssertFalse();
	}

	[TestMethod]
	public void Step_WithBase()
	{
		var attr = new StepAttribute(2m, 1m); // valid = 1 + 2n => 1,3,5,...
		attr.IsValid(1).AssertTrue();
		attr.IsValid(3).AssertTrue();
		attr.IsValid(5).AssertTrue();
		attr.IsValid(0).AssertFalse();
		attr.IsValid(2).AssertFalse();
	}

	[TestMethod]
	public void Step_DecimalValues()
	{
		var attr = new StepAttribute(0.25m); // step=0.25
		attr.IsValid(0.00m).AssertTrue();
		attr.IsValid(0.25m).AssertTrue();
		attr.IsValid(0.5m).AssertTrue();
		attr.IsValid(0.75m).AssertTrue();
		attr.IsValid(1.0m).AssertTrue();
		attr.IsValid(1.10m).AssertFalse();
	}

	[TestMethod]
	public void Step_ConversionFromString()
	{
		var attr = new StepAttribute(3m);
		attr.IsValid("0").AssertTrue();
		attr.IsValid("6").AssertTrue();
		attr.IsValid("7").AssertFalse();
		attr.IsValid("abc").AssertFalse();
	}

	[TestMethod]
	public void Step_NullHandling()
	{
		var attr = new StepAttribute(10m);
		attr.IsValid(null).AssertFalse();
		attr.DisableNullCheck = true;
		attr.IsValid(null).AssertTrue();
	}

	[TestMethod]
	public void Step_NegativeStepRejected()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new StepAttribute(0m));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new StepAttribute(-1m));
	}

	[TestMethod]
	public void Step_LargeNumbers()
	{
		var attr = new StepAttribute(1000m, 500m);
		attr.IsValid(500).AssertTrue();
		attr.IsValid(1500).AssertTrue();
		attr.IsValid(2500).AssertTrue();
		attr.IsValid(2000).AssertFalse();
	}

	[TestMethod]
	public void Step_NegativeBase()
	{
		var attr = new StepAttribute(4m, -2m); // -2,2,6,...
		attr.IsValid(-2).AssertTrue();
		attr.IsValid(2).AssertTrue();
		attr.IsValid(6).AssertTrue();
		attr.IsValid(0).AssertFalse();
	}
}