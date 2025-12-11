namespace Ecng.Tests.ComponentModel;

using System.ComponentModel.DataAnnotations;

using Ecng.ComponentModel;

[TestClass]
public class ValidationTests : BaseTestClass
{
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

	[TestMethod]
	public void GreaterThanZero_TimeSpan()
	{
		var attr = new TimeSpanGreaterThanZeroAttribute();
		attr.IsValid(TimeSpan.FromMilliseconds(1)).AssertTrue();
		attr.IsValid(TimeSpan.Zero).AssertFalse();
		attr.IsValid(TimeSpan.FromSeconds(-1)).AssertFalse();
		attr.IsValid("00:00:01").AssertTrue();
		attr.IsValid(null).AssertFalse();
		attr.DisableNullCheck = true;
		attr.IsValid(null).AssertTrue();
	}

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

	[TestMethod]
	public void NotNegative_TimeSpan()
	{
		var attr = new TimeSpanNotNegativeAttribute();
		attr.IsValid(TimeSpan.Zero).AssertTrue();
		attr.IsValid(TimeSpan.FromMilliseconds(1)).AssertTrue();
		attr.IsValid(TimeSpan.FromSeconds(-1)).AssertFalse();
		attr.IsValid("00:00:00").AssertTrue();
		attr.IsValid(null).AssertFalse();
		attr.DisableNullCheck = true;
		attr.IsValid(null).AssertTrue();
	}

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

	[TestMethod]
	public void NullOrMoreZero_TimeSpan()
	{
		var attr = new TimeSpanNullOrMoreZeroAttribute();
		attr.IsValid(null).AssertTrue();
		attr.IsValid(TimeSpan.FromMilliseconds(1)).AssertTrue();
		attr.IsValid(TimeSpan.Zero).AssertFalse();
		attr.IsValid(TimeSpan.FromSeconds(-1)).AssertFalse();
		attr.IsValid("00:00:01").AssertTrue();
	}

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

	[TestMethod]
	public void NullOrNotNegative_TimeSpan()
	{
		var attr = new TimeSpanNullOrNotNegativeAttribute();
		attr.IsValid(null).AssertTrue();
		attr.IsValid(TimeSpan.Zero).AssertTrue();
		attr.IsValid(TimeSpan.FromMilliseconds(1)).AssertTrue();
		attr.IsValid(TimeSpan.FromSeconds(-1)).AssertFalse();
		attr.IsValid("00:00:00").AssertTrue();
	}

	[TestMethod]
	public void DisableNullCheck_Ignored_NullOrVariants()
	{
		var a1 = new IntNullOrNotNegativeAttribute();
		var a2 = new IntNullOrMoreZeroAttribute();
		var a3 = new DecimalNullOrMoreZeroAttribute();
		var a4 = new DoubleNullOrNotNegativeAttribute();
		var a5 = new TimeSpanNullOrNotNegativeAttribute();
		var a6 = new TimeSpanNullOrMoreZeroAttribute();

		foreach (var a in new ValidationAttribute[] { a1, a2, a3, a4, a5, a6 })
		{
			a.IsValid(null).AssertTrue();
			(a as IValidator).DisableNullCheck = true; // should not change semantics
			a.IsValid(null).AssertTrue();
		}
	}

	[TestMethod]
	public void ConversionFailure_ReturnsFalse()
	{
		var attr = new IntGreaterThanZeroAttribute();
		attr.IsValid("abc").AssertFalse(); // non-convertible string
		attr.IsValid(new object()).AssertFalse(); // arbitrary object
	}

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
		ThrowsExactly<ArgumentOutOfRangeException>(() => new StepAttribute(0m));
		ThrowsExactly<ArgumentOutOfRangeException>(() => new StepAttribute(-1m));
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
		attr.IsValid(-1).AssertFalse();
		attr.IsValid(2).AssertTrue();
		attr.IsValid(6).AssertTrue();
		attr.IsValid(0).AssertFalse();
		attr.IsValid(1).AssertFalse();
	}

	[TestMethod]
	public void Price_GreaterThanZero()
	{
		var attr = new PriceGreaterThanZeroAttribute();
		attr.IsValid(new Price(1m, PriceTypes.Absolute)).AssertTrue();
		attr.IsValid(new Price(0m, PriceTypes.Absolute)).AssertFalse();
		attr.IsValid(new Price(-1m, PriceTypes.Absolute)).AssertFalse();
		attr.IsValid(null).AssertFalse();
		attr.DisableNullCheck = true;
		attr.IsValid(null).AssertTrue();
	}

	[TestMethod]
	public void Price_NotNegative()
	{
		var attr = new PriceNotNegativeAttribute();
		attr.IsValid(new Price(0m, PriceTypes.Absolute)).AssertTrue();
		attr.IsValid(new Price(10m, PriceTypes.Absolute)).AssertTrue();
		attr.IsValid(new Price(-0.01m, PriceTypes.Absolute)).AssertFalse();
	}

	[TestMethod]
	public void Price_NullOrMoreZero()
	{
		var attr = new PriceNullOrMoreZeroAttribute();
		attr.IsValid(null).AssertTrue();
		attr.IsValid(new Price(1m, PriceTypes.Absolute)).AssertTrue();
		attr.IsValid(new Price(0m, PriceTypes.Absolute)).AssertFalse();
		attr.IsValid(new Price(-1m, PriceTypes.Absolute)).AssertFalse();
	}

	[TestMethod]
	public void Price_NullOrNotNegative()
	{
		var attr = new PriceNullOrNotNegativeAttribute();
		attr.IsValid(null).AssertTrue();
		attr.IsValid(new Price(0m, PriceTypes.Absolute)).AssertTrue();
		attr.IsValid(new Price(-1m, PriceTypes.Absolute)).AssertFalse();
	}

	[TestMethod]
	public void PricePercent_GreaterThanZero()
	{
		var attr = new PriceGreaterThanZeroAttribute();
		attr.IsValid(new Price(10m, PriceTypes.Percent)).AssertTrue();
	}

	[TestMethod]
	public void PricePercent_NotNegative()
	{
		var attr = new PriceNotNegativeAttribute();
		attr.IsValid(new Price(0m, PriceTypes.Percent)).AssertTrue();
		attr.IsValid(new Price(5m, PriceTypes.Percent)).AssertTrue();
	}

	[TestMethod]
	public void PricePercent_NullOrMoreZero()
	{
		var attr = new PriceNullOrMoreZeroAttribute();
		attr.IsValid(null).AssertTrue(); // null path
		attr.IsValid(new Price(5m, PriceTypes.Percent)).AssertTrue();
	}

	[TestMethod]
	public void PricePercent_NullOrNotNegative()
	{
		var attr = new PriceNullOrNotNegativeAttribute();
		attr.IsValid(null).AssertTrue();
		attr.IsValid(new Price(0m, PriceTypes.Percent)).AssertTrue();
	}

	[TestMethod]
	public void TimeSpanStep_Basic()
	{
		var attr = new TimeSpanStepAttribute(500); // step 500 ms
		attr.IsValid(TimeSpan.Zero).AssertTrue();
		attr.IsValid(TimeSpan.FromMilliseconds(500)).AssertTrue();
		attr.IsValid(TimeSpan.FromMilliseconds(1000)).AssertTrue();
		attr.IsValid(TimeSpan.FromMilliseconds(750)).AssertFalse();
	}

	[TestMethod]
	public void TimeSpanStep_Base()
	{
		var attr = new TimeSpanStepAttribute(200, 50); // valid ticks at 50ms + n*200ms => 50,250,450...
		attr.IsValid(TimeSpan.FromMilliseconds(50)).AssertTrue();
		attr.IsValid(TimeSpan.FromMilliseconds(250)).AssertTrue();
		attr.IsValid(TimeSpan.FromMilliseconds(450)).AssertTrue();
		attr.IsValid(TimeSpan.FromMilliseconds(650)).AssertTrue();
		attr.IsValid(TimeSpan.FromMilliseconds(150)).AssertFalse();
	}

	[TestMethod]
	public void TimeSpanStep_NullHandling()
	{
		var attr = new TimeSpanStepAttribute(1000);
		attr.IsValid(null).AssertFalse();
		attr.DisableNullCheck = true;
		attr.IsValid(null).AssertTrue();
	}

	[TestMethod]
	public void PriceStep_Basic()
	{
		var attr = new PriceStepAttribute(0.5m); // 0,0.5,1.0,1.5,...
		attr.IsValid(new Price(0m, PriceTypes.Absolute)).AssertTrue();
		attr.IsValid(new Price(0.5m, PriceTypes.Absolute)).AssertTrue();
		attr.IsValid(new Price(1.5m, PriceTypes.Absolute)).AssertTrue();
		attr.IsValid(new Price(0.25m, PriceTypes.Absolute)).AssertFalse();
	}

	[TestMethod]
	public void PriceStep_Base()
	{
		var attr = new PriceStepAttribute(2m, 1m); // 1,3,5...
		attr.IsValid(new Price(1m, PriceTypes.Absolute)).AssertTrue();
		attr.IsValid(new Price(3m, PriceTypes.Absolute)).AssertTrue();
		attr.IsValid(new Price(5m, PriceTypes.Absolute)).AssertTrue();
		attr.IsValid(new Price(0m, PriceTypes.Absolute)).AssertFalse();
		attr.IsValid(new Price(2m, PriceTypes.Absolute)).AssertFalse();
	}

	[TestMethod]
	public void PriceStep_NullHandling()
	{
		var attr = new PriceStepAttribute(1m);
		attr.IsValid(null).AssertFalse();
		attr.DisableNullCheck = true;
		attr.IsValid(null).AssertTrue();
	}

	[TestMethod]
	public void TimeSpanStep_StringCtor_Basic()
	{
		var attr = new TimeSpanStepAttribute("00:00:00.500"); // step 500 ms
		attr.IsValid(TimeSpan.Zero).AssertTrue();
		attr.IsValid(TimeSpan.FromMilliseconds(500)).AssertTrue();
		attr.IsValid(TimeSpan.FromMilliseconds(1000)).AssertTrue();
		attr.IsValid(TimeSpan.FromMilliseconds(750)).AssertFalse();
	}

	[TestMethod]
	public void TimeSpanStep_StringCtor_Base()
	{
		var attr = new TimeSpanStepAttribute("00:00:00.200", "00:00:00.050"); // 50ms + n*200ms
		attr.IsValid(TimeSpan.FromMilliseconds(50)).AssertTrue();
		attr.IsValid(TimeSpan.FromMilliseconds(250)).AssertTrue();
		attr.IsValid(TimeSpan.FromMilliseconds(450)).AssertTrue();
		attr.IsValid(TimeSpan.FromMilliseconds(150)).AssertFalse();
	}

	[TestMethod]
	public void TimeSpanStep_StringCtor_Invalid()
	{
		ThrowsExactly<ArgumentNullException>(() => new TimeSpanStepAttribute(null));
		ThrowsExactly<ArgumentNullException>(() => new TimeSpanStepAttribute(" "));
		ThrowsExactly<ArgumentOutOfRangeException>(() => new TimeSpanStepAttribute("00:00:00")); // zero step
	}

	[TestMethod]
	public void Range_Int_Basic()
	{
		var attr = new RangeAttribute(0, 100);
		attr.IsValid(0).AssertTrue();
		attr.IsValid(50).AssertTrue();
		attr.IsValid(100).AssertTrue();
		attr.IsValid(-1).AssertFalse();
		attr.IsValid(101).AssertFalse();
	}

	[TestMethod]
	public void Range_Int_Negative()
	{
		var attr = new RangeAttribute(-50, 50);
		attr.IsValid(-50).AssertTrue();
		attr.IsValid(0).AssertTrue();
		attr.IsValid(50).AssertTrue();
		attr.IsValid(-51).AssertFalse();
		attr.IsValid(51).AssertFalse();
	}

	[TestMethod]
	public void Range_Double_Basic()
	{
		var attr = new RangeAttribute(0.0, 1.0);
		attr.IsValid(0.0).AssertTrue();
		attr.IsValid(0.5).AssertTrue();
		attr.IsValid(1.0).AssertTrue();
		attr.IsValid(-0.1).AssertFalse();
		attr.IsValid(1.1).AssertFalse();
	}

	[TestMethod]
	public void Range_Double_Precision()
	{
		var attr = new RangeAttribute(0.001, 0.999);
		attr.IsValid(0.001).AssertTrue();
		attr.IsValid(0.5).AssertTrue();
		attr.IsValid(0.999).AssertTrue();
		attr.IsValid(0.0009).AssertFalse();
		attr.IsValid(1.0).AssertFalse();
	}

	[TestMethod]
	public void Range_String_Numeric()
	{
		var attr = new RangeAttribute(typeof(int), "10", "20");
		attr.IsValid(10).AssertTrue();
		attr.IsValid(15).AssertTrue();
		attr.IsValid(20).AssertTrue();
		attr.IsValid(9).AssertFalse();
		attr.IsValid(21).AssertFalse();
		attr.IsValid("15").AssertTrue(); // string conversion
		attr.IsValid("5").AssertFalse();
	}

	[TestMethod]
	public void Range_String_DateTime()
	{
		var attr = new RangeAttribute(typeof(DateTime), "2020-01-01", "2020-12-31");
		attr.IsValid(new DateTime(2020, 1, 1)).AssertTrue();
		attr.IsValid(new DateTime(2020, 6, 15)).AssertTrue();
		attr.IsValid(new DateTime(2020, 12, 31)).AssertTrue();
		attr.IsValid(new DateTime(2019, 12, 31)).AssertFalse();
		attr.IsValid(new DateTime(2021, 1, 1)).AssertFalse();
		attr.IsValid("2020-06-15").AssertTrue();
	}

	[TestMethod]
	public void Range_Null_Handling()
	{
		var attr = new RangeAttribute(1, 10);
		attr.IsValid(null).AssertTrue(); // RangeAttribute allows null by default
	}

	[TestMethod]
	public void Range_InvalidType()
	{
		var attr = new RangeAttribute(1, 10);
		attr.IsValid("not a number").AssertFalse();
		attr.IsValid(new object()).AssertFalse();
	}

	[TestMethod]
	public void Range_Decimal_Boundary()
	{
		var attr = new RangeAttribute(typeof(decimal), "0.01", "99.99");
		attr.IsValid(0.01m).AssertTrue();
		attr.IsValid(50m).AssertTrue();
		attr.IsValid(99.99m).AssertTrue();
		attr.IsValid(0.009m).AssertFalse();
		attr.IsValid(100m).AssertFalse();
	}

	[TestMethod]
	public void PriceRange_Basic()
	{
		var attr = new PriceRangeAttribute(1m, 100m);
		attr.IsValid(new Price(1m, PriceTypes.Absolute)).AssertTrue();
		attr.IsValid(new Price(50m, PriceTypes.Absolute)).AssertTrue();
		attr.IsValid(new Price(100m, PriceTypes.Absolute)).AssertTrue();
		attr.IsValid(new Price(0.5m, PriceTypes.Absolute)).AssertFalse();
		attr.IsValid(new Price(101m, PriceTypes.Absolute)).AssertFalse();
	}

	[TestMethod]
	public void PriceRange_Percent()
	{
		var attr = new PriceRangeAttribute(0m, 100m);
		attr.IsValid(new Price(0m, PriceTypes.Percent)).AssertTrue();
		attr.IsValid(new Price(50m, PriceTypes.Percent)).AssertTrue();
		attr.IsValid(new Price(100m, PriceTypes.Percent)).AssertTrue();
		attr.IsValid(new Price(-1m, PriceTypes.Percent)).AssertFalse();
		attr.IsValid(new Price(101m, PriceTypes.Percent)).AssertFalse();
	}

	[TestMethod]
	public void PriceRange_Negative()
	{
		var attr = new PriceRangeAttribute(-50m, 50m);
		attr.IsValid(new Price(-50m, PriceTypes.Absolute)).AssertTrue();
		attr.IsValid(new Price(0m, PriceTypes.Absolute)).AssertTrue();
		attr.IsValid(new Price(50m, PriceTypes.Absolute)).AssertTrue();
		attr.IsValid(new Price(-51m, PriceTypes.Absolute)).AssertFalse();
		attr.IsValid(new Price(51m, PriceTypes.Absolute)).AssertFalse();
	}

	[TestMethod]
	public void PriceRange_NullHandling()
	{
		var attr = new PriceRangeAttribute(1m, 10m);
		attr.IsValid(null).AssertFalse();
		attr.DisableNullCheck = true;
		attr.IsValid(null).AssertTrue();
	}

	[TestMethod]
	public void PriceRange_InvalidType()
	{
		var attr = new PriceRangeAttribute(1m, 10m);
		attr.IsValid(5m).AssertFalse();
		attr.IsValid("5").AssertFalse();
		attr.IsValid(new object()).AssertFalse();
	}

	[TestMethod]
	public void PriceRange_InvalidConstructor()
	{
		ThrowsExactly<ArgumentOutOfRangeException>(() => new PriceRangeAttribute(10m, 1m));
	}

	[TestMethod]
	public void TimeSpanRange_Basic()
	{
		var attr = new TimeSpanRangeAttribute(TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1));
		attr.IsValid(TimeSpan.FromSeconds(1)).AssertTrue();
		attr.IsValid(TimeSpan.FromSeconds(30)).AssertTrue();
		attr.IsValid(TimeSpan.FromMinutes(1)).AssertTrue();
		attr.IsValid(TimeSpan.Zero).AssertFalse();
		attr.IsValid(TimeSpan.FromMinutes(2)).AssertFalse();
	}

	[TestMethod]
	public void TimeSpanRange_NullHandling()
	{
		var attr = new TimeSpanRangeAttribute(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
		attr.IsValid(null).AssertFalse();
		attr.DisableNullCheck = true;
		attr.IsValid(null).AssertTrue();
	}

	[TestMethod]
	public void TimeSpanRange_InvalidType()
	{
		var attr = new TimeSpanRangeAttribute(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
		attr.IsValid(5).AssertFalse();
		attr.IsValid("00:00:05").AssertFalse();
		attr.IsValid(new object()).AssertFalse();
	}

	[TestMethod]
	public void TimeSpanRange_InvalidConstructor()
	{
		ThrowsExactly<ArgumentOutOfRangeException>(() => new TimeSpanRangeAttribute(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1)));
	}

	private sealed class AttrEntity : IAttributesEntity
	{
		public IList<Attribute> Attributes { get; } = [];
	}

	[TestMethod]
	public void AttributesEntity_Int_Step_Range()
	{
		var e = new AttrEntity();
		e.Attributes.Add(new StepAttribute(5m));
		e.Attributes.Add(new RangeAttribute(0, 10));

		e.IsValid(0).AssertTrue();
		e.IsValid(5).AssertTrue();
		e.IsValid(10).AssertTrue();

		// inside range but off step
		e.IsValid(7).AssertFalse();

		// on step but outside range
		e.IsValid(-5).AssertFalse();
		e.IsValid(15).AssertFalse();
	}

	[TestMethod]
	public void AttributesEntity_Int_Range_And_GreaterThanZero()
	{
		var e = new AttrEntity();
		e.Attributes.Add(new RangeAttribute(0, 10));
		e.Attributes.Add(new IntGreaterThanZeroAttribute());

		e.IsValid(0).AssertFalse(); // rejected by >0
		e.IsValid(5).AssertTrue();
		e.IsValid(11).AssertFalse(); // rejected by range
		e.IsValid(-1).AssertFalse(); // rejected by both
	}

	[TestMethod]
	public void AttributesEntity_TimeSpan_Step_Range_NotNegative()
	{
		var e = new AttrEntity();
		e.Attributes.Add(new TimeSpanStepAttribute(250));
		e.Attributes.Add(new TimeSpanRangeAttribute(TimeSpan.FromMilliseconds(-250), TimeSpan.FromMilliseconds(1000)));
		e.Attributes.Add(new TimeSpanNotNegativeAttribute());

		// on grid, in range, non-negative
		e.IsValid(TimeSpan.Zero).AssertTrue();
		e.IsValid(TimeSpan.FromMilliseconds(250)).AssertTrue();

		// on grid and in range, but negative -> rejected by NotNegative
		e.IsValid(TimeSpan.FromMilliseconds(-250)).AssertFalse();

		// on grid but above range
		e.IsValid(TimeSpan.FromMilliseconds(1250)).AssertFalse();

		// off grid but in range and non-negative
		e.IsValid(TimeSpan.FromMilliseconds(300)).AssertFalse();
	}

	[TestMethod]
	public void AttributesEntity_Price_Step_Range_NotNegative()
	{
		var e = new AttrEntity();
		e.Attributes.Add(new PriceStepAttribute(0.5m));
		e.Attributes.Add(new PriceRangeAttribute(0m, 2m));
		e.Attributes.Add(new PriceNotNegativeAttribute());

		var good = new Price(1.0m, PriceTypes.Absolute);
		e.IsValid(good).AssertTrue();

		var offStep = new Price(1.1m, PriceTypes.Absolute);
		e.IsValid(offStep).AssertFalse();

		var overRange = new Price(2.5m, PriceTypes.Absolute);
		e.IsValid(overRange).AssertFalse();

		var negative = new Price(-0.5m, PriceTypes.Absolute);
		e.IsValid(negative).AssertFalse();
	}

	[TestMethod]
	public void AttributesEntity_NullHandling_Int()
	{
		var e = new AttrEntity();
		var step = new StepAttribute(2m);
		var range = new RangeAttribute(0, 10); // RangeAttribute allows null
		e.Attributes.Add(step);
		e.Attributes.Add(range);

		// by default, step rejects null -> whole entity rejects
		e.IsValid(null).AssertFalse();

		// enable null for step -> all validators pass
		step.DisableNullCheck = true;
		e.IsValid(null).AssertTrue();
	}

	[TestMethod]
	public void AttributesEntity_NullHandling_Price_Range()
	{
		var e = new AttrEntity();
		var range = new PriceRangeAttribute(0m, 10m) { DisableNullCheck = false };
		e.Attributes.Add(range);

		// default: PriceRange rejects null
		e.IsValid(null).AssertFalse();

		range.DisableNullCheck = true;
		e.IsValid(null).AssertTrue();
	}
}