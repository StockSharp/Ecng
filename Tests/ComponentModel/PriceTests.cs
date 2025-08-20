namespace Ecng.Tests.ComponentModel;

using Ecng.ComponentModel;

[TestClass]
public class PriceTests
{
	[TestMethod]
	public void Parse()
	{
		for (var i = 0; i < 1000; i++)
		{
			var v = RandomGen.GetInt(-1000, 1000);

			var u = v.To<string>().ToPriceType();
			u.AssertEqual(new Price(v, PriceTypes.Absolute));
			u.ToString().AssertEqual(v.To<string>());

			//u = v.ToString();
			//u.AssertEqual(new Price(v, PriceTypes.Absolute));
			//u.ToString().AssertEqual(v.ToString());

			u = (v + "%").ToPriceType();
			u.AssertEqual(new Price(v, PriceTypes.Percent));
			u.ToString().AssertEqual(v + "%");

			u = (v + "l").ToPriceType();
			u.AssertEqual(new Price(v, PriceTypes.Limit));
			u.ToString().AssertEqual(v + "l");
			(v + "l").ToPriceType().AssertEqual((v + "L").ToPriceType());

			//u = v + "%";
			//u.AssertEqual(new Price(v, PriceTypes.Percent));
			//u.ToString().AssertEqual(v + "%");
		}
	}

	[TestMethod]
	public void InvalidCast()
	{
		Assert.ThrowsExactly<InvalidOperationException>(() => ((double)3.Percents()).AssertEqual(0));
	}

	//[TestMethod]
	//[ExpectedException(typeof(ArgumentException), "Единица измерения не может быть 'Step' так как не передана информация об инструменте.")]
	//public void InvalidParse()
	//{
	//    "10ш".ToPriceType(true);
	//}

	[TestMethod]
	public void InvalidParse2()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => "10н".ToPriceType());
	}

	[TestMethod]
	public void Percent()
	{
		var u = 10.Percents();
		(u == 0).AssertFalse();
		(u + 0 == 0).AssertTrue();
		(u - 0 == 0).AssertTrue();
	}

	[TestMethod]
	public void InvalidCompare()
	{
		Assert.ThrowsExactly<ArgumentException>(() => (10.Percents() > 10).AssertTrue());
	}

	[TestMethod]
	public void Compare()
	{
		((Price)10 > 10).AssertFalse();
		(new Price(10, PriceTypes.Absolute) == 10).AssertTrue();
		(10 == new Price(10, PriceTypes.Absolute)).AssertTrue();

		(new Price(10, PriceTypes.Limit) == new Price(10, PriceTypes.Limit)).AssertTrue();
		new Price(10, PriceTypes.Limit).AssertEqual(new Price(10, PriceTypes.Limit));
	}

	[TestMethod]
	public void Arithmetic()
	{
		for (var i = 0; i < 100000; i++)
		{
			var u1 = RandomUnit();
			var u2 = RandomUnit();

			ProcessArithmetic(u1, u2, u1 + u2, (v1, v2) => v1 + v2, true);
			ProcessArithmetic(u1, u2, u1 - u2, (v1, v2) => v1 - v2, true);
			ProcessArithmetic(u1, u2, u1 * u2, (v1, v2) => v1 * v2, false);

			if (u2.Value == 0 || (u1.Value == 0 && u2.Type == PriceTypes.Percent))
				continue;

			ProcessArithmetic(u1, u2, u1 / u2, (v1, v2) => v1 / v2, false);
		}
	}

	private static void ProcessArithmetic(Price u1, Price u2, Price result, Func<decimal, decimal, decimal> opr, bool transAbs)
	{
		//result.Security.AssertSame(security);

		if (u1.Type == u2.Type)
		{
			var resultValue = opr(u1.Value, u2.Value);

			result.Value.AssertEqual(resultValue);
			result.Type.AssertEqual(u1.Type);
		}
		else
		{
			if (u1.Type != PriceTypes.Percent && u2.Type != PriceTypes.Percent)
			{
				result.Type.AssertEqual(u1.Type);

				var resultValue = /*transAbs ? u2.Convert(u1.Type).Value : */(decimal)u2;

				resultValue = opr(u1.Value, resultValue);

				result.Value.Round(5).AssertEqual(resultValue.Round(5));
			}
			else
			{
				result.Type.AssertEqual(u1.Type != PriceTypes.Percent ? u1.Type : u2.Type);

				var abs = u1.Type != PriceTypes.Percent ? u1.Value : u2.Value;
				var per = u1.Type != PriceTypes.Percent ? u2.Value : u1.Value;

				per = (abs.Abs() * per) / 100;

				var resultValue = u1.Type != PriceTypes.Percent ? opr(abs, per) : opr(per, abs);

				result.Value.AssertEqual(resultValue);
			}
		}
	}

	private static Price RandomUnit()
	{
		return new(RandomGen.GetInt(-100, 100), RandomGen.GetEnum(
		[
			PriceTypes.Absolute,
			PriceTypes.Percent
		]));
	}
	[TestMethod]
	public void Empty1()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => "".ToPriceType().AssertNull());
	}

	[TestMethod]
	public void Empty2()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => ((string)null).ToPriceType().AssertNull());
	}

	[TestMethod]
	public void NotEquals()
	{
		var u1 = "1".ToPriceType();
		var u2 = "1L".ToPriceType();
		(u1 == u2).AssertFalse();
		(u1 != u2).AssertTrue();
		u1.AssertNotEqual(u2);
	}

	[TestMethod]
	public void NotEquals2()
	{
		var u1 = "1".ToPriceType();
		var u2 = "2".ToPriceType();
		(u1 == u2).AssertFalse();
		(u1 != u2).AssertTrue();
	}
}
