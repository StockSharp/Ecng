namespace Ecng.Tests.Common;

[TestClass]
public class MathHelperTest : BaseTestClass
{
	const double _deltad = 0.00001d;
	const float  _deltaf = 0.00001f;

	[TestMethod]
	public void Floor()
	{
		0.5.Floor().AssertEqual(0);
		(-0.5).Floor().AssertEqual(-1);

		( 10.1).Floor(0.2).AssertEqual(10);
		(-10.1).Floor(0.2).AssertEqual(-10.2d, _deltad);

		( 10.1f).Floor(0.2f).AssertEqual(10f, _deltaf);
		(-10.1f).Floor(0.2f).AssertEqual(-10.2f, _deltaf);

		( 11).Floor(2).AssertEqual(10);
		(-11).Floor(2).AssertEqual(-12);

		( 11L).Floor(2).AssertEqual(10L);
		(-11L).Floor(2).AssertEqual(-12L);
	}

	[TestMethod]
	public void RoundMidpointDouble()
	{
		( 10.3d).Round(MidpointRounding.ToEven).AssertEqual(10d);
		(-10.3d).Round(MidpointRounding.ToEven).AssertEqual(-10d);
		( 10.5d).Round(MidpointRounding.ToEven).AssertEqual(10d);
		(-10.5d).Round(MidpointRounding.ToEven).AssertEqual(-10d);
		( 10.8d).Round(MidpointRounding.ToEven).AssertEqual(11d);
		(-10.8d).Round(MidpointRounding.ToEven).AssertEqual(-11d);
		( 11.3d).Round(MidpointRounding.ToEven).AssertEqual(11d);
		(-11.3d).Round(MidpointRounding.ToEven).AssertEqual(-11d);
		( 11.5d).Round(MidpointRounding.ToEven).AssertEqual(12d);
		(-11.5d).Round(MidpointRounding.ToEven).AssertEqual(-12d);
		( 11.8d).Round(MidpointRounding.ToEven).AssertEqual(12d);
		(-11.8d).Round(MidpointRounding.ToEven).AssertEqual(-12d);

		( 10.3d).Round(MidpointRounding.AwayFromZero).AssertEqual(10d);
		(-10.3d).Round(MidpointRounding.AwayFromZero).AssertEqual(-10d);
		( 10.5d).Round(MidpointRounding.AwayFromZero).AssertEqual(11d);
		(-10.5d).Round(MidpointRounding.AwayFromZero).AssertEqual(-11d);
		( 10.8d).Round(MidpointRounding.AwayFromZero).AssertEqual(11d);
		(-10.8d).Round(MidpointRounding.AwayFromZero).AssertEqual(-11d);
		( 11.3d).Round(MidpointRounding.AwayFromZero).AssertEqual(11d);
		(-11.3d).Round(MidpointRounding.AwayFromZero).AssertEqual(-11d);
		( 11.5d).Round(MidpointRounding.AwayFromZero).AssertEqual(12d);
		(-11.5d).Round(MidpointRounding.AwayFromZero).AssertEqual(-12d);
		( 11.8d).Round(MidpointRounding.AwayFromZero).AssertEqual(12d);
		(-11.8d).Round(MidpointRounding.AwayFromZero).AssertEqual(-12d);

		( 10.3d).Round(MidpointRounding.ToZero).AssertEqual(10d);
		(-10.3d).Round(MidpointRounding.ToZero).AssertEqual(-10d);
		( 10.5d).Round(MidpointRounding.ToZero).AssertEqual(10d);
		(-10.5d).Round(MidpointRounding.ToZero).AssertEqual(-10d);
		( 10.8d).Round(MidpointRounding.ToZero).AssertEqual(10d);
		(-10.8d).Round(MidpointRounding.ToZero).AssertEqual(-10d);
		( 11.3d).Round(MidpointRounding.ToZero).AssertEqual(11d);
		(-11.3d).Round(MidpointRounding.ToZero).AssertEqual(-11d);
		( 11.5d).Round(MidpointRounding.ToZero).AssertEqual(11d);
		(-11.5d).Round(MidpointRounding.ToZero).AssertEqual(-11d);
		( 11.8d).Round(MidpointRounding.ToZero).AssertEqual(11d);
		(-11.8d).Round(MidpointRounding.ToZero).AssertEqual(-11d);

		( 10.3d).Round(MidpointRounding.ToNegativeInfinity).AssertEqual(10d);
		(-10.3d).Round(MidpointRounding.ToNegativeInfinity).AssertEqual(-11d);
		( 10.5d).Round(MidpointRounding.ToNegativeInfinity).AssertEqual(10d);
		(-10.5d).Round(MidpointRounding.ToNegativeInfinity).AssertEqual(-11d);
		( 10.8d).Round(MidpointRounding.ToNegativeInfinity).AssertEqual(10d);
		(-10.8d).Round(MidpointRounding.ToNegativeInfinity).AssertEqual(-11d);
		( 11.3d).Round(MidpointRounding.ToNegativeInfinity).AssertEqual(11d);
		(-11.3d).Round(MidpointRounding.ToNegativeInfinity).AssertEqual(-12d);
		( 11.5d).Round(MidpointRounding.ToNegativeInfinity).AssertEqual(11d);
		(-11.5d).Round(MidpointRounding.ToNegativeInfinity).AssertEqual(-12d);
		( 11.8d).Round(MidpointRounding.ToNegativeInfinity).AssertEqual(11d);
		(-11.8d).Round(MidpointRounding.ToNegativeInfinity).AssertEqual(-12d);

		( 10.3d).Round(MidpointRounding.ToPositiveInfinity).AssertEqual(11d);
		(-10.3d).Round(MidpointRounding.ToPositiveInfinity).AssertEqual(-10d);
		( 10.5d).Round(MidpointRounding.ToPositiveInfinity).AssertEqual(11d);
		(-10.5d).Round(MidpointRounding.ToPositiveInfinity).AssertEqual(-10d);
		( 10.8d).Round(MidpointRounding.ToPositiveInfinity).AssertEqual(11d);
		(-10.8d).Round(MidpointRounding.ToPositiveInfinity).AssertEqual(-10d);
		( 11.3d).Round(MidpointRounding.ToPositiveInfinity).AssertEqual(12d);
		(-11.3d).Round(MidpointRounding.ToPositiveInfinity).AssertEqual(-11d);
		( 11.5d).Round(MidpointRounding.ToPositiveInfinity).AssertEqual(12d);
		(-11.5d).Round(MidpointRounding.ToPositiveInfinity).AssertEqual(-11d);
		( 11.8d).Round(MidpointRounding.ToPositiveInfinity).AssertEqual(12d);
		(-11.8d).Round(MidpointRounding.ToPositiveInfinity).AssertEqual(-11d);
	}

	[TestMethod]
	public void RoundMidpointDecimal()
	{
		( 10.3m).Round(MidpointRounding.ToEven).AssertEqual(10m);
		(-10.3m).Round(MidpointRounding.ToEven).AssertEqual(-10m);
		( 10.5m).Round(MidpointRounding.ToEven).AssertEqual(10m);
		(-10.5m).Round(MidpointRounding.ToEven).AssertEqual(-10m);
		( 10.8m).Round(MidpointRounding.ToEven).AssertEqual(11m);
		(-10.8m).Round(MidpointRounding.ToEven).AssertEqual(-11m);
		( 11.3m).Round(MidpointRounding.ToEven).AssertEqual(11m);
		(-11.3m).Round(MidpointRounding.ToEven).AssertEqual(-11m);
		( 11.5m).Round(MidpointRounding.ToEven).AssertEqual(12m);
		(-11.5m).Round(MidpointRounding.ToEven).AssertEqual(-12m);
		( 11.8m).Round(MidpointRounding.ToEven).AssertEqual(12m);
		(-11.8m).Round(MidpointRounding.ToEven).AssertEqual(-12m);

		( 10.3m).Round(MidpointRounding.AwayFromZero).AssertEqual(10m);
		(-10.3m).Round(MidpointRounding.AwayFromZero).AssertEqual(-10m);
		( 10.5m).Round(MidpointRounding.AwayFromZero).AssertEqual(11m);
		(-10.5m).Round(MidpointRounding.AwayFromZero).AssertEqual(-11m);
		( 10.8m).Round(MidpointRounding.AwayFromZero).AssertEqual(11m);
		(-10.8m).Round(MidpointRounding.AwayFromZero).AssertEqual(-11m);
		( 11.3m).Round(MidpointRounding.AwayFromZero).AssertEqual(11m);
		(-11.3m).Round(MidpointRounding.AwayFromZero).AssertEqual(-11m);
		( 11.5m).Round(MidpointRounding.AwayFromZero).AssertEqual(12m);
		(-11.5m).Round(MidpointRounding.AwayFromZero).AssertEqual(-12m);
		( 11.8m).Round(MidpointRounding.AwayFromZero).AssertEqual(12m);
		(-11.8m).Round(MidpointRounding.AwayFromZero).AssertEqual(-12m);

		( 10.3m).Round(MidpointRounding.ToZero).AssertEqual(10m);
		(-10.3m).Round(MidpointRounding.ToZero).AssertEqual(-10m);
		( 10.5m).Round(MidpointRounding.ToZero).AssertEqual(10m);
		(-10.5m).Round(MidpointRounding.ToZero).AssertEqual(-10m);
		( 10.8m).Round(MidpointRounding.ToZero).AssertEqual(10m);
		(-10.8m).Round(MidpointRounding.ToZero).AssertEqual(-10m);
		( 11.3m).Round(MidpointRounding.ToZero).AssertEqual(11m);
		(-11.3m).Round(MidpointRounding.ToZero).AssertEqual(-11m);
		( 11.5m).Round(MidpointRounding.ToZero).AssertEqual(11m);
		(-11.5m).Round(MidpointRounding.ToZero).AssertEqual(-11m);
		( 11.8m).Round(MidpointRounding.ToZero).AssertEqual(11m);
		(-11.8m).Round(MidpointRounding.ToZero).AssertEqual(-11m);

		( 10.3m).Round(MidpointRounding.ToNegativeInfinity).AssertEqual(10m);
		(-10.3m).Round(MidpointRounding.ToNegativeInfinity).AssertEqual(-11m);
		( 10.5m).Round(MidpointRounding.ToNegativeInfinity).AssertEqual(10m);
		(-10.5m).Round(MidpointRounding.ToNegativeInfinity).AssertEqual(-11m);
		( 10.8m).Round(MidpointRounding.ToNegativeInfinity).AssertEqual(10m);
		(-10.8m).Round(MidpointRounding.ToNegativeInfinity).AssertEqual(-11m);
		( 11.3m).Round(MidpointRounding.ToNegativeInfinity).AssertEqual(11m);
		(-11.3m).Round(MidpointRounding.ToNegativeInfinity).AssertEqual(-12m);
		( 11.5m).Round(MidpointRounding.ToNegativeInfinity).AssertEqual(11m);
		(-11.5m).Round(MidpointRounding.ToNegativeInfinity).AssertEqual(-12m);
		( 11.8m).Round(MidpointRounding.ToNegativeInfinity).AssertEqual(11m);
		(-11.8m).Round(MidpointRounding.ToNegativeInfinity).AssertEqual(-12m);

		( 10.3m).Round(MidpointRounding.ToPositiveInfinity).AssertEqual(11m);
		(-10.3m).Round(MidpointRounding.ToPositiveInfinity).AssertEqual(-10m);
		( 10.5m).Round(MidpointRounding.ToPositiveInfinity).AssertEqual(11m);
		(-10.5m).Round(MidpointRounding.ToPositiveInfinity).AssertEqual(-10m);
		( 10.8m).Round(MidpointRounding.ToPositiveInfinity).AssertEqual(11m);
		(-10.8m).Round(MidpointRounding.ToPositiveInfinity).AssertEqual(-10m);
		( 11.3m).Round(MidpointRounding.ToPositiveInfinity).AssertEqual(12m);
		(-11.3m).Round(MidpointRounding.ToPositiveInfinity).AssertEqual(-11m);
		( 11.5m).Round(MidpointRounding.ToPositiveInfinity).AssertEqual(12m);
		(-11.5m).Round(MidpointRounding.ToPositiveInfinity).AssertEqual(-11m);
		( 11.8m).Round(MidpointRounding.ToPositiveInfinity).AssertEqual(12m);
		(-11.8m).Round(MidpointRounding.ToPositiveInfinity).AssertEqual(-11m);
	}

	//[TestMethod]
	//public void Round_MathRoundingRules_double()
	//{
	//	( 10.3d).Round(MathRoundingRules.ToEven).AssertEqual(10d);
	//	(-10.3d).Round(MathRoundingRules.ToEven).AssertEqual(-10d);
	//	( 10.5d).Round(MathRoundingRules.ToEven).AssertEqual(10d);
	//	(-10.5d).Round(MathRoundingRules.ToEven).AssertEqual(-10d);
	//	( 10.8d).Round(MathRoundingRules.ToEven).AssertEqual(11d);
	//	(-10.8d).Round(MathRoundingRules.ToEven).AssertEqual(-11d);
	//	( 11.3d).Round(MathRoundingRules.ToEven).AssertEqual(11d);
	//	(-11.3d).Round(MathRoundingRules.ToEven).AssertEqual(-11d);
	//	( 11.5d).Round(MathRoundingRules.ToEven).AssertEqual(12d);
	//	(-11.5d).Round(MathRoundingRules.ToEven).AssertEqual(-12d);
	//	( 11.8d).Round(MathRoundingRules.ToEven).AssertEqual(12d);
	//	(-11.8d).Round(MathRoundingRules.ToEven).AssertEqual(-12d);

	//	( 10.3d).Round(MathRoundingRules.AwayFromZero).AssertEqual(10d);
	//	(-10.3d).Round(MathRoundingRules.AwayFromZero).AssertEqual(-10d);
	//	( 10.5d).Round(MathRoundingRules.AwayFromZero).AssertEqual(11d);
	//	(-10.5d).Round(MathRoundingRules.AwayFromZero).AssertEqual(-11d);
	//	( 10.8d).Round(MathRoundingRules.AwayFromZero).AssertEqual(11d);
	//	(-10.8d).Round(MathRoundingRules.AwayFromZero).AssertEqual(-11d);
	//	( 11.3d).Round(MathRoundingRules.AwayFromZero).AssertEqual(11d);
	//	(-11.3d).Round(MathRoundingRules.AwayFromZero).AssertEqual(-11d);
	//	( 11.5d).Round(MathRoundingRules.AwayFromZero).AssertEqual(12d);
	//	(-11.5d).Round(MathRoundingRules.AwayFromZero).AssertEqual(-12d);
	//	( 11.8d).Round(MathRoundingRules.AwayFromZero).AssertEqual(12d);
	//	(-11.8d).Round(MathRoundingRules.AwayFromZero).AssertEqual(-12d);

	//	( 10.3d).Round(MathRoundingRules.ToZero).AssertEqual(10d);
	//	(-10.3d).Round(MathRoundingRules.ToZero).AssertEqual(-10d);
	//	( 10.5d).Round(MathRoundingRules.ToZero).AssertEqual(10d);
	//	(-10.5d).Round(MathRoundingRules.ToZero).AssertEqual(-10d);
	//	( 10.8d).Round(MathRoundingRules.ToZero).AssertEqual(10d);
	//	(-10.8d).Round(MathRoundingRules.ToZero).AssertEqual(-10d);
	//	( 11.3d).Round(MathRoundingRules.ToZero).AssertEqual(11d);
	//	(-11.3d).Round(MathRoundingRules.ToZero).AssertEqual(-11d);
	//	( 11.5d).Round(MathRoundingRules.ToZero).AssertEqual(11d);
	//	(-11.5d).Round(MathRoundingRules.ToZero).AssertEqual(-11d);
	//	( 11.8d).Round(MathRoundingRules.ToZero).AssertEqual(11d);
	//	(-11.8d).Round(MathRoundingRules.ToZero).AssertEqual(-11d);

	//	( 10.3d).Round(MathRoundingRules.ToNegativeInfinity).AssertEqual(10d);
	//	(-10.3d).Round(MathRoundingRules.ToNegativeInfinity).AssertEqual(-11d);
	//	( 10.5d).Round(MathRoundingRules.ToNegativeInfinity).AssertEqual(10d);
	//	(-10.5d).Round(MathRoundingRules.ToNegativeInfinity).AssertEqual(-11d);
	//	( 10.8d).Round(MathRoundingRules.ToNegativeInfinity).AssertEqual(10d);
	//	(-10.8d).Round(MathRoundingRules.ToNegativeInfinity).AssertEqual(-11d);
	//	( 11.3d).Round(MathRoundingRules.ToNegativeInfinity).AssertEqual(11d);
	//	(-11.3d).Round(MathRoundingRules.ToNegativeInfinity).AssertEqual(-12d);
	//	( 11.5d).Round(MathRoundingRules.ToNegativeInfinity).AssertEqual(11d);
	//	(-11.5d).Round(MathRoundingRules.ToNegativeInfinity).AssertEqual(-12d);
	//	( 11.8d).Round(MathRoundingRules.ToNegativeInfinity).AssertEqual(11d);
	//	(-11.8d).Round(MathRoundingRules.ToNegativeInfinity).AssertEqual(-12d);

	//	( 10.3d).Round(MathRoundingRules.ToPositiveInfinity).AssertEqual(11d);
	//	(-10.3d).Round(MathRoundingRules.ToPositiveInfinity).AssertEqual(-10d);
	//	( 10.5d).Round(MathRoundingRules.ToPositiveInfinity).AssertEqual(11d);
	//	(-10.5d).Round(MathRoundingRules.ToPositiveInfinity).AssertEqual(-10d);
	//	( 10.8d).Round(MathRoundingRules.ToPositiveInfinity).AssertEqual(11d);
	//	(-10.8d).Round(MathRoundingRules.ToPositiveInfinity).AssertEqual(-10d);
	//	( 11.3d).Round(MathRoundingRules.ToPositiveInfinity).AssertEqual(12d);
	//	(-11.3d).Round(MathRoundingRules.ToPositiveInfinity).AssertEqual(-11d);
	//	( 11.5d).Round(MathRoundingRules.ToPositiveInfinity).AssertEqual(12d);
	//	(-11.5d).Round(MathRoundingRules.ToPositiveInfinity).AssertEqual(-11d);
	//	( 11.8d).Round(MathRoundingRules.ToPositiveInfinity).AssertEqual(12d);
	//	(-11.8d).Round(MathRoundingRules.ToPositiveInfinity).AssertEqual(-11d);
	//}

	//[TestMethod]
	//public void Round_MathRoundingRules_decimal()
	//{
	//	( 10.3m).Round(MathRoundingRules.ToEven).AssertEqual(10m);
	//	(-10.3m).Round(MathRoundingRules.ToEven).AssertEqual(-10m);
	//	( 10.5m).Round(MathRoundingRules.ToEven).AssertEqual(10m);
	//	(-10.5m).Round(MathRoundingRules.ToEven).AssertEqual(-10m);
	//	( 10.8m).Round(MathRoundingRules.ToEven).AssertEqual(11m);
	//	(-10.8m).Round(MathRoundingRules.ToEven).AssertEqual(-11m);
	//	( 11.3m).Round(MathRoundingRules.ToEven).AssertEqual(11m);
	//	(-11.3m).Round(MathRoundingRules.ToEven).AssertEqual(-11m);
	//	( 11.5m).Round(MathRoundingRules.ToEven).AssertEqual(12m);
	//	(-11.5m).Round(MathRoundingRules.ToEven).AssertEqual(-12m);
	//	( 11.8m).Round(MathRoundingRules.ToEven).AssertEqual(12m);
	//	(-11.8m).Round(MathRoundingRules.ToEven).AssertEqual(-12m);

	//	( 10.3m).Round(MathRoundingRules.AwayFromZero).AssertEqual(10m);
	//	(-10.3m).Round(MathRoundingRules.AwayFromZero).AssertEqual(-10m);
	//	( 10.5m).Round(MathRoundingRules.AwayFromZero).AssertEqual(11m);
	//	(-10.5m).Round(MathRoundingRules.AwayFromZero).AssertEqual(-11m);
	//	( 10.8m).Round(MathRoundingRules.AwayFromZero).AssertEqual(11m);
	//	(-10.8m).Round(MathRoundingRules.AwayFromZero).AssertEqual(-11m);
	//	( 11.3m).Round(MathRoundingRules.AwayFromZero).AssertEqual(11m);
	//	(-11.3m).Round(MathRoundingRules.AwayFromZero).AssertEqual(-11m);
	//	( 11.5m).Round(MathRoundingRules.AwayFromZero).AssertEqual(12m);
	//	(-11.5m).Round(MathRoundingRules.AwayFromZero).AssertEqual(-12m);
	//	( 11.8m).Round(MathRoundingRules.AwayFromZero).AssertEqual(12m);
	//	(-11.8m).Round(MathRoundingRules.AwayFromZero).AssertEqual(-12m);

	//	( 10.3m).Round(MathRoundingRules.ToZero).AssertEqual(10m);
	//	(-10.3m).Round(MathRoundingRules.ToZero).AssertEqual(-10m);
	//	( 10.5m).Round(MathRoundingRules.ToZero).AssertEqual(10m);
	//	(-10.5m).Round(MathRoundingRules.ToZero).AssertEqual(-10m);
	//	( 10.8m).Round(MathRoundingRules.ToZero).AssertEqual(10m);
	//	(-10.8m).Round(MathRoundingRules.ToZero).AssertEqual(-10m);
	//	( 11.3m).Round(MathRoundingRules.ToZero).AssertEqual(11m);
	//	(-11.3m).Round(MathRoundingRules.ToZero).AssertEqual(-11m);
	//	( 11.5m).Round(MathRoundingRules.ToZero).AssertEqual(11m);
	//	(-11.5m).Round(MathRoundingRules.ToZero).AssertEqual(-11m);
	//	( 11.8m).Round(MathRoundingRules.ToZero).AssertEqual(11m);
	//	(-11.8m).Round(MathRoundingRules.ToZero).AssertEqual(-11m);

	//	( 10.3m).Round(MathRoundingRules.ToNegativeInfinity).AssertEqual(10m);
	//	(-10.3m).Round(MathRoundingRules.ToNegativeInfinity).AssertEqual(-11m);
	//	( 10.5m).Round(MathRoundingRules.ToNegativeInfinity).AssertEqual(10m);
	//	(-10.5m).Round(MathRoundingRules.ToNegativeInfinity).AssertEqual(-11m);
	//	( 10.8m).Round(MathRoundingRules.ToNegativeInfinity).AssertEqual(10m);
	//	(-10.8m).Round(MathRoundingRules.ToNegativeInfinity).AssertEqual(-11m);
	//	( 11.3m).Round(MathRoundingRules.ToNegativeInfinity).AssertEqual(11m);
	//	(-11.3m).Round(MathRoundingRules.ToNegativeInfinity).AssertEqual(-12m);
	//	( 11.5m).Round(MathRoundingRules.ToNegativeInfinity).AssertEqual(11m);
	//	(-11.5m).Round(MathRoundingRules.ToNegativeInfinity).AssertEqual(-12m);
	//	( 11.8m).Round(MathRoundingRules.ToNegativeInfinity).AssertEqual(11m);
	//	(-11.8m).Round(MathRoundingRules.ToNegativeInfinity).AssertEqual(-12m);

	//	( 10.3m).Round(MathRoundingRules.ToPositiveInfinity).AssertEqual(11m);
	//	(-10.3m).Round(MathRoundingRules.ToPositiveInfinity).AssertEqual(-10m);
	//	( 10.5m).Round(MathRoundingRules.ToPositiveInfinity).AssertEqual(11m);
	//	(-10.5m).Round(MathRoundingRules.ToPositiveInfinity).AssertEqual(-10m);
	//	( 10.8m).Round(MathRoundingRules.ToPositiveInfinity).AssertEqual(11m);
	//	(-10.8m).Round(MathRoundingRules.ToPositiveInfinity).AssertEqual(-10m);
	//	( 11.3m).Round(MathRoundingRules.ToPositiveInfinity).AssertEqual(12m);
	//	(-11.3m).Round(MathRoundingRules.ToPositiveInfinity).AssertEqual(-11m);
	//	( 11.5m).Round(MathRoundingRules.ToPositiveInfinity).AssertEqual(12m);
	//	(-11.5m).Round(MathRoundingRules.ToPositiveInfinity).AssertEqual(-11m);
	//	( 11.8m).Round(MathRoundingRules.ToPositiveInfinity).AssertEqual(12m);
	//	(-11.8m).Round(MathRoundingRules.ToPositiveInfinity).AssertEqual(-11m);
	//}

	[TestMethod]
	public void RoundToNearest()
	{
		const double delta = 1e-8;

		0.9999.RoundToNearest().AssertEqual(1, delta);
		0.09999.RoundToNearest().AssertEqual(0.1, delta);
		0.009999.RoundToNearest().AssertEqual(0.01, delta);
		0.0009999.RoundToNearest().AssertEqual(0.001, delta);
		0.00009999.RoundToNearest().AssertEqual(0.0001, delta);
		0.000009999.RoundToNearest().AssertEqual(0.00001, delta);
	}

	[TestMethod]
	public void RoundToNearestBig()
	{
		const double delta = 1e-8;

		9999.9999.RoundToNearest().AssertEqual(10000, delta);
		99999.09999.RoundToNearest().AssertEqual(100000, delta);
		999999.009999.RoundToNearest().AssertEqual(1000000, delta);
		9999999.0009999.RoundToNearest().AssertEqual(10000000, delta);
		99999999.00009999.RoundToNearest().AssertEqual(100000000, delta);
		999999999.000009999.RoundToNearest().AssertEqual(1000000000, delta);
		9999999999.0000009999.RoundToNearest().AssertEqual(10000000000, delta);
	}

	[TestMethod]
	public void RoundToNearestNegative()
	{
		const double delta = 1e-8;

		(-0.9999).RoundToNearest().AssertEqual(-1, delta);
		(-0.09999).RoundToNearest().AssertEqual(-0.1, delta);
		(-0.009999).RoundToNearest().AssertEqual(-0.01, delta);
		(-0.0009999).RoundToNearest().AssertEqual(-0.001, delta);
		(-0.00009999).RoundToNearest().AssertEqual(-0.0001, delta);
		(-0.000009999).RoundToNearest().AssertEqual(-0.00001, delta);

		(-9999.9999).RoundToNearest().AssertEqual(-10000, delta);
		(-99999.09999).RoundToNearest().AssertEqual(-100000, delta);
		(-999999.009999).RoundToNearest().AssertEqual(-1000000, delta);
		(-9999999.0009999).RoundToNearest().AssertEqual(-10000000, delta);
		(-99999999.00009999).RoundToNearest().AssertEqual(-100000000, delta);
		(-999999999.000009999).RoundToNearest().AssertEqual(-1000000000, delta);
		(-9999999999.0000009999).RoundToNearest().AssertEqual(-10000000000, delta);
	}

	[TestMethod]
	public void RoundToNearestSpecial()
	{
		double.NaN.RoundToNearest().IsNaN().AssertTrue();
		double.PositiveInfinity.RoundToNearest().IsPositiveInfinity().AssertTrue();
		double.NegativeInfinity.RoundToNearest().IsNegativeInfinity().AssertTrue();
	}

	[TestMethod]
	public void Decimals()
	{
		Do.Invariant(() =>
		{
			"0.0".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(0);
			"00.0".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(0);
			"0.00".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(0);
			"0.01".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(2);
			"0.0100".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(2);
			"10.0".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(0);
			"10.000".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(0);
			"03.0000".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(0);
			"303.033".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(3);
			"00.00".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(0);
			"0.456".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(3);
			"0.56676".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(5);
			"3.45443".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(5);
			"3333.45443".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(5);

			0.0m.GetDecimalInfo().EffectiveScale.AssertEqual(0);
			1.0m.GetDecimalInfo().EffectiveScale.AssertEqual(0);
			0.1m.GetDecimalInfo().EffectiveScale.AssertEqual(1);
			0.0011m.GetDecimalInfo().EffectiveScale.AssertEqual(4);
			0.056570006674m.GetDecimalInfo().EffectiveScale.AssertEqual(12);
		});
	}

	[TestMethod]
	public void TrailingZeros()
	{
		static decimal removeTrailingZeros(decimal value)
		{
			var strValue = value.ToString("G29").TrimEnd('0');

			var decimalPointIndex = strValue.IndexOf('.');
			if (decimalPointIndex == -1)
				return value;

			var integerPart = strValue[..decimalPointIndex];
			var fractionalPart = strValue[(decimalPointIndex + 1)..].TrimEnd('0');

			if (fractionalPart.IsEmpty())
				return integerPart.To<long>();

			return $"{integerPart}.{fractionalPart}".To<decimal>();
		}

		for (var i = 0; i < 100000; i++)
		{
			var v = RandomGen.GetDecimal();
			v.RemoveTrailingZeros().AssertEqual(removeTrailingZeros(v));
		}
	}

	[TestMethod]
	public void DigitCount()
	{
		for (var i = 0; i < 100000; i++)
		{
			var v = RandomGen.GetInt();
			v.GetDigitCount().AssertEqual(v.ToString().Length);

			var l = RandomGen.GetInt();
			l.GetDigitCount().AssertEqual(l.ToString().Length);
		}
	}

	private static (long mantissa, int exponent) ParseDecimalToMantissaExponent(decimal value)
	{
		var isNegative = value < 0;
		var str = Math.Abs(value).ToString(System.Globalization.CultureInfo.InvariantCulture);
		long mantissa;
		int exponent;

		if (str.Contains('.'))
		{
			var parts = str.Split('.');
			var integerPart = parts[0].TrimStart('0');
			if (integerPart.Length == 0) integerPart = "0";
			var fractionalPart = parts[1];

			var combined = integerPart + fractionalPart;
			if (combined.Length > 19)
				throw new OverflowException("Mantissa exceeds long range.");

			mantissa = combined.To<long>();
			exponent = -fractionalPart.Length; // Экспонента = -scale
		}
		else
		{
			var trimmed = str.TrimStart('0');
			
			if (trimmed.Length == 0) trimmed = "0";
			
			if (trimmed.Length > 19)
				throw new OverflowException("Mantissa exceeds long range.");

			mantissa = trimmed.To<long>();
			exponent = 0;
		}

		if (isNegative)
			mantissa = -mantissa;

		return (mantissa, exponent);
	}

	[TestMethod]
	public void TestDecimalInfo()
	{
		TestValue(123.456m);
		TestValue(-2920000.00m);
		TestValue(0.00m);
		TestValue(123456m);
		TestValue(0.00123m);
		TestValue(123456789.12345m);

		for (var i = 0; i < 100; i++)
		{
			decimal value;

			if (RandomGen.GetInt(0, 2) == 0)
			{
				value = RandomGen.GetInt(int.MinValue, int.MaxValue);
			}
			else
			{
				value = RandomGen.GetDecimal();
			}

			if (RandomGen.GetInt(0, 2) == 0)
				value = -value;

			TestValue(value);
		}
	}

	private static void TestValue(decimal value)
	{
		var info = value.GetDecimalInfo();
		var (parsedMantissa, parsedExponent) = ParseDecimalToMantissaExponent(value);

		// Сравнение с GetDecimalInfo
		info.Mantissa.AssertEqual(parsedMantissa);
		info.Exponent.AssertEqual(parsedExponent);

		// Проверка восстановления значения
		var reconstructed = MathHelper.ToDecimal(info.Mantissa, info.Exponent);
		reconstructed.AssertEqual(value);

		var parsedReconstructed = MathHelper.ToDecimal(parsedMantissa, parsedExponent);
		parsedReconstructed.AssertEqual(value);
	}

	[TestMethod]
	public void TestOverflowMantissa()
	{
		var value = 9999999999999999999999999999m;
		ThrowsExactly<OverflowException>(() => value.GetDecimalInfo());
	}

	#region ExtractMantissaExponent for double

	[TestMethod]
	public void ExtractMantissaExponent_Double_PositiveInteger()
	{
		// 8.0 = 1 * 2^3
		8.0.ExtractMantissaExponent(out var mantissa, out var exponent);
		(mantissa * Math.Pow(2, exponent)).AssertEqual(8.0, _deltad);
		(mantissa > 0).AssertTrue();
	}

	[TestMethod]
	public void ExtractMantissaExponent_Double_NegativeInteger()
	{
		// -8.0 = -1 * 2^3
		(-8.0).ExtractMantissaExponent(out var mantissa, out var exponent);
		(mantissa * Math.Pow(2, exponent)).AssertEqual(-8.0, _deltad);
		(mantissa < 0).AssertTrue();
	}

	[TestMethod]
	public void ExtractMantissaExponent_Double_PositiveFraction()
	{
		0.5.ExtractMantissaExponent(out var mantissa, out var exponent);
		(mantissa * Math.Pow(2, exponent)).AssertEqual(0.5, _deltad);
		(mantissa > 0).AssertTrue();
	}

	[TestMethod]
	public void ExtractMantissaExponent_Double_NegativeFraction()
	{
		(-0.5).ExtractMantissaExponent(out var mantissa, out var exponent);
		(mantissa * Math.Pow(2, exponent)).AssertEqual(-0.5, _deltad);
		(mantissa < 0).AssertTrue();
	}

	[TestMethod]
	public void ExtractMantissaExponent_Double_Zero()
	{
		0.0.ExtractMantissaExponent(out var mantissa, out var exponent);
		mantissa.AssertEqual(0);
		exponent.AssertEqual(0);
	}

	[TestMethod]
	public void ExtractMantissaExponent_Double_One()
	{
		1.0.ExtractMantissaExponent(out var mantissa, out var exponent);
		(mantissa * Math.Pow(2, exponent)).AssertEqual(1.0, _deltad);
	}

	[TestMethod]
	public void ExtractMantissaExponent_Double_LargeValue()
	{
		var value = 123456789.123456;
		value.ExtractMantissaExponent(out var mantissa, out var exponent);
		(mantissa * Math.Pow(2, exponent)).AssertEqual(value, 0.0001);
	}

	[TestMethod]
	public void ExtractMantissaExponent_Double_SmallValue()
	{
		var value = 0.00000123456;
		value.ExtractMantissaExponent(out var mantissa, out var exponent);
		(mantissa * Math.Pow(2, exponent)).AssertEqual(value, 1e-15);
	}

	#endregion

	#region Min/Max Tests

	[TestMethod]
	public void Min_Int()
	{
		5.Min(3).AssertEqual(3);
		3.Min(5).AssertEqual(3);
		(-5).Min(-3).AssertEqual(-5);
		0.Min(0).AssertEqual(0);
	}

	[TestMethod]
	public void Max_Int()
	{
		5.Max(3).AssertEqual(5);
		3.Max(5).AssertEqual(5);
		(-5).Max(-3).AssertEqual(-3);
		0.Max(0).AssertEqual(0);
	}

	[TestMethod]
	public void Min_Long()
	{
		5L.Min(3L).AssertEqual(3L);
		(-5L).Min(-3L).AssertEqual(-5L);
	}

	[TestMethod]
	public void Max_Long()
	{
		5L.Max(3L).AssertEqual(5L);
		(-5L).Max(-3L).AssertEqual(-3L);
	}

	[TestMethod]
	public void Min_Double()
	{
		5.5.Min(3.3).AssertEqual(3.3, _deltad);
		(-5.5).Min(-3.3).AssertEqual(-5.5, _deltad);
	}

	[TestMethod]
	public void Max_Double()
	{
		5.5.Max(3.3).AssertEqual(5.5, _deltad);
		(-5.5).Max(-3.3).AssertEqual(-3.3, _deltad);
	}

	[TestMethod]
	public void Max_Float()
	{
		5.5f.Max(3.3f).AssertEqual(5.5f, _deltaf);
		3.3f.Max(5.5f).AssertEqual(5.5f, _deltaf);
		(-5.5f).Max(-3.3f).AssertEqual(-3.3f, _deltaf);
	}

	[TestMethod]
	public void Min_Decimal()
	{
		5.5m.Min(3.3m).AssertEqual(3.3m);
		(-5.5m).Min(-3.3m).AssertEqual(-5.5m);
	}

	[TestMethod]
	public void Max_Decimal()
	{
		5.5m.Max(3.3m).AssertEqual(5.5m);
		(-5.5m).Max(-3.3m).AssertEqual(-3.3m);
	}

	[TestMethod]
	public void Min_TimeSpan()
	{
		var ts1 = TimeSpan.FromSeconds(5);
		var ts2 = TimeSpan.FromSeconds(3);
		ts1.Min(ts2).AssertEqual(ts2);
		ts2.Min(ts1).AssertEqual(ts2);
	}

	[TestMethod]
	public void Max_TimeSpan()
	{
		var ts1 = TimeSpan.FromSeconds(5);
		var ts2 = TimeSpan.FromSeconds(3);
		ts1.Max(ts2).AssertEqual(ts1);
		ts2.Max(ts1).AssertEqual(ts1);
	}

	[TestMethod]
	public void Min_DateTime()
	{
		var dt1 = new DateTime(2024, 1, 1);
		var dt2 = new DateTime(2023, 1, 1);
		dt1.Min(dt2).AssertEqual(dt2);
	}

	[TestMethod]
	public void Max_DateTime()
	{
		var dt1 = new DateTime(2024, 1, 1);
		var dt2 = new DateTime(2023, 1, 1);
		dt1.Max(dt2).AssertEqual(dt1);
	}

	[TestMethod]
	public void Min_ThreeArgs()
	{
		MathHelper.Min(5m, 3m, 7m).AssertEqual(3m);
		MathHelper.Min(3m, 5m, 7m).AssertEqual(3m);
		MathHelper.Min(7m, 5m, 3m).AssertEqual(3m);
		MathHelper.Min(-1m, -5m, -3m).AssertEqual(-5m);
	}

	[TestMethod]
	public void Min_FourArgs()
	{
		MathHelper.Min(5m, 3m, 7m, 1m).AssertEqual(1m);
		MathHelper.Min(1m, 2m, 3m, 4m).AssertEqual(1m);
		MathHelper.Min(4m, 3m, 2m, 1m).AssertEqual(1m);
		MathHelper.Min(-1m, -5m, -3m, -2m).AssertEqual(-5m);
	}

	[TestMethod]
	public void Min_FiveArgs()
	{
		MathHelper.Min(5m, 3m, 7m, 1m, 9m).AssertEqual(1m);
		MathHelper.Min(9m, 8m, 7m, 6m, 5m).AssertEqual(5m);
		MathHelper.Min(1m, 2m, 3m, 4m, 5m).AssertEqual(1m);
		MathHelper.Min(-1m, -5m, -3m, -2m, -4m).AssertEqual(-5m);
	}

	[TestMethod]
	public void Max_ThreeArgs()
	{
		MathHelper.Max(5m, 3m, 7m).AssertEqual(7m);
		MathHelper.Max(7m, 5m, 3m).AssertEqual(7m);
		MathHelper.Max(3m, 7m, 5m).AssertEqual(7m);
		MathHelper.Max(-1m, -5m, -3m).AssertEqual(-1m);
	}

	[TestMethod]
	public void Max_FourArgs()
	{
		MathHelper.Max(5m, 3m, 7m, 1m).AssertEqual(7m);
		MathHelper.Max(1m, 2m, 3m, 4m).AssertEqual(4m);
		MathHelper.Max(4m, 3m, 2m, 1m).AssertEqual(4m);
		MathHelper.Max(-1m, -5m, -3m, -2m).AssertEqual(-1m);
	}

	[TestMethod]
	public void Max_FiveArgs()
	{
		MathHelper.Max(5m, 3m, 7m, 1m, 9m).AssertEqual(9m);
		MathHelper.Max(9m, 8m, 7m, 6m, 5m).AssertEqual(9m);
		MathHelper.Max(1m, 2m, 3m, 4m, 5m).AssertEqual(5m);
		MathHelper.Max(-1m, -5m, -3m, -2m, -4m).AssertEqual(-1m);
	}

	#endregion

	#region Abs Tests

	[TestMethod]
	public void Abs_Int()
	{
		5.Abs().AssertEqual(5);
		(-5).Abs().AssertEqual(5);
		0.Abs().AssertEqual(0);
	}

	[TestMethod]
	public void Abs_Long()
	{
		5L.Abs().AssertEqual(5L);
		(-5L).Abs().AssertEqual(5L);
	}

	[TestMethod]
	public void Abs_Double()
	{
		5.5.Abs().AssertEqual(5.5, _deltad);
		(-5.5).Abs().AssertEqual(5.5, _deltad);
	}

	[TestMethod]
	public void Abs_Decimal()
	{
		5.5m.Abs().AssertEqual(5.5m);
		(-5.5m).Abs().AssertEqual(5.5m);
	}

	[TestMethod]
	public void Abs_TimeSpan()
	{
		TimeSpan.FromSeconds(5).Abs().AssertEqual(TimeSpan.FromSeconds(5));
		TimeSpan.FromSeconds(-5).Abs().AssertEqual(TimeSpan.FromSeconds(5));
	}

	#endregion

	#region Sign Tests

	[TestMethod]
	public void Sign_Int()
	{
		5.Sign().AssertEqual(1);
		(-5).Sign().AssertEqual(-1);
		0.Sign().AssertEqual(0);
	}

	[TestMethod]
	public void Sign_Double()
	{
		5.5.Sign().AssertEqual(1);
		(-5.5).Sign().AssertEqual(-1);
		0.0.Sign().AssertEqual(0);
	}

	[TestMethod]
	public void Sign_Decimal()
	{
		5.5m.Sign().AssertEqual(1);
		(-5.5m).Sign().AssertEqual(-1);
		0m.Sign().AssertEqual(0);
	}

	[TestMethod]
	public void Sign_TimeSpan()
	{
		TimeSpan.FromSeconds(5).Sign().AssertEqual(1);
		TimeSpan.FromSeconds(-5).Sign().AssertEqual(-1);
		TimeSpan.Zero.Sign().AssertEqual(0);
	}

	#endregion

	#region Bit Operations Tests

	[TestMethod]
	public void GetBit_Int()
	{
		// 5 = 0101 in binary
		5.GetBit(0).AssertTrue();  // bit 0 = 1
		5.GetBit(1).AssertFalse(); // bit 1 = 0
		5.GetBit(2).AssertTrue();  // bit 2 = 1
		5.GetBit(3).AssertFalse(); // bit 3 = 0
	}

	[TestMethod]
	public void SetBit_Int()
	{
		0.SetBit(0, true).AssertEqual(1);
		0.SetBit(1, true).AssertEqual(2);
		0.SetBit(2, true).AssertEqual(4);
		7.SetBit(0, false).AssertEqual(6);
		7.SetBit(1, false).AssertEqual(5);
	}

	[TestMethod]
	public void GetBit_Long()
	{
		5L.GetBit(0).AssertTrue();
		5L.GetBit(1).AssertFalse();
		5L.GetBit(2).AssertTrue();
	}

	[TestMethod]
	public void SetBit_Long()
	{
		0L.SetBit(0, true).AssertEqual(1L);
		0L.SetBit(1, true).AssertEqual(2L);
		7L.SetBit(0, false).AssertEqual(6L);
	}

	[TestMethod]
	public void HasBits_Int()
	{
		// 7 = 0111
		7.HasBits(1).AssertTrue();
		7.HasBits(3).AssertTrue();
		7.HasBits(7).AssertTrue();
		7.HasBits(8).AssertFalse();
	}

	[TestMethod]
	public void HasBits_Long()
	{
		7L.HasBits(1L).AssertTrue();
		7L.HasBits(3L).AssertTrue();
		7L.HasBits(8L).AssertFalse();
	}

	#endregion

	#region Pow Tests

	[TestMethod]
	public void Pow_Int()
	{
		2.Pow(3).AssertEqual(8);
		3.Pow(2).AssertEqual(9);
		5.Pow(0).AssertEqual(1);
		2.Pow(10).AssertEqual(1024);
	}

	[TestMethod]
	public void Pow_Double()
	{
		2.0.Pow(3.0).AssertEqual(8.0, _deltad);
		2.0.Pow(0.5).AssertEqual(Math.Sqrt(2), _deltad);
	}

	[TestMethod]
	public void Pow_Decimal()
	{
		2m.Pow(3m).AssertEqual(8m);
		4m.Pow(0.5m).AssertEqual(2m);
	}

	#endregion

	#region Sqrt Tests

	[TestMethod]
	public void Sqrt_Double()
	{
		4.0.Sqrt().AssertEqual(2.0, _deltad);
		9.0.Sqrt().AssertEqual(3.0, _deltad);
		2.0.Sqrt().AssertEqual(Math.Sqrt(2), _deltad);
	}

	#endregion

	#region Truncate Tests

	[TestMethod]
	public void Truncate_Decimal()
	{
		5.7m.Truncate().AssertEqual(5m);
		(-5.7m).Truncate().AssertEqual(-5m);
		5.0m.Truncate().AssertEqual(5m);
	}

	[TestMethod]
	public void Truncate_Double()
	{
		5.7.Truncate().AssertEqual(5.0, _deltad);
		(-5.7).Truncate().AssertEqual(-5.0, _deltad);
	}

	#endregion

	#region Ceiling Tests

	[TestMethod]
	public void Ceiling_Decimal()
	{
		5.1m.Ceiling().AssertEqual(6m);
		(-5.1m).Ceiling().AssertEqual(-5m);
		5.0m.Ceiling().AssertEqual(5m);
	}

	[TestMethod]
	public void Ceiling_Double()
	{
		5.1.Ceiling().AssertEqual(6);
		(-5.1).Ceiling().AssertEqual(-5);
	}

	[TestMethod]
	public void Ceiling_DecimalWithStep()
	{
		5.1m.Ceiling(0.5m).AssertEqual(5.5m);
		5.6m.Ceiling(0.5m).AssertEqual(6.0m);
	}

	#endregion

	#region Log Tests

	[TestMethod]
	public void Log_Double()
	{
		Math.E.Log().AssertEqual(1.0, _deltad);
		1.0.Log().AssertEqual(0.0, _deltad);
	}

	[TestMethod]
	public void Log_DoubleWithBase()
	{
		8.0.Log(2.0).AssertEqual(3.0, _deltad);
		100.0.Log(10.0).AssertEqual(2.0, _deltad);
	}

	[TestMethod]
	public void Log10_Double()
	{
		100.0.Log10().AssertEqual(2.0, _deltad);
		1000.0.Log10().AssertEqual(3.0, _deltad);
	}

	#endregion

	#region Trigonometric Tests

	[TestMethod]
	public void Sin_Double()
	{
		0.0.Sin().AssertEqual(0.0, _deltad);
		(Math.PI / 2).Sin().AssertEqual(1.0, _deltad);
		Math.PI.Sin().AssertEqual(0.0, _deltad);
	}

	[TestMethod]
	public void Cos_Double()
	{
		0.0.Cos().AssertEqual(1.0, _deltad);
		(Math.PI / 2).Cos().AssertEqual(0.0, _deltad);
		Math.PI.Cos().AssertEqual(-1.0, _deltad);
	}

	[TestMethod]
	public void Tan_Double()
	{
		0.0.Tan().AssertEqual(0.0, _deltad);
		(Math.PI / 4).Tan().AssertEqual(1.0, _deltad);
	}

	[TestMethod]
	public void Asin_Double()
	{
		0.0.Asin().AssertEqual(0.0, _deltad);
		1.0.Asin().AssertEqual(Math.PI / 2, _deltad);
	}

	[TestMethod]
	public void Acos_Double()
	{
		1.0.Acos().AssertEqual(0.0, _deltad);
		0.0.Acos().AssertEqual(Math.PI / 2, _deltad);
	}

	[TestMethod]
	public void Atan_Double()
	{
		0.0.Atan().AssertEqual(0.0, _deltad);
		1.0.Atan().AssertEqual(Math.PI / 4, _deltad);
	}

	#endregion

	#region Hyperbolic Tests

	[TestMethod]
	public void Sinh_Double()
	{
		0.0.Sinh().AssertEqual(0.0, _deltad);
	}

	[TestMethod]
	public void Cosh_Double()
	{
		0.0.Cosh().AssertEqual(1.0, _deltad);
	}

	[TestMethod]
	public void Tanh_Double()
	{
		0.0.Tanh().AssertEqual(0.0, _deltad);
	}

	#endregion

	#region Exp Tests

	[TestMethod]
	public void Exp_Double()
	{
		0.0.Exp().AssertEqual(1.0, _deltad);
		1.0.Exp().AssertEqual(Math.E, _deltad);
	}

	#endregion

	#region GetMiddle Tests

	[TestMethod]
	public void GetMiddle_Int()
	{
		0.GetMiddle(10).AssertEqual(5m);
		(-10).GetMiddle(10).AssertEqual(0m);
		5.GetMiddle(5).AssertEqual(5m);
	}

	[TestMethod]
	public void GetMiddle_Decimal()
	{
		0m.GetMiddle(10m).AssertEqual(5m);
		1.5m.GetMiddle(2.5m).AssertEqual(2m);
	}

	#endregion

	#region GetParts Tests

	[TestMethod]
	public void GetParts_Long()
	{
		var parts = 0x0000000100000002L.GetParts();
		parts[0].AssertEqual(2); // low
		parts[1].AssertEqual(1); // high
	}

	[TestMethod]
	public void GetParts_Double()
	{
		var parts = 5.75.GetParts();
		parts[0].AssertEqual(5); // integer part
		(parts[1]).AssertEqual(0.75, _deltad); // fractional part
	}

	#endregion

	#region DivRem Tests

	[TestMethod]
	public void DivRem_Int()
	{
		var quotient = 17.DivRem(5, out var remainder);
		quotient.AssertEqual(3);
		remainder.AssertEqual(2);
	}

	[TestMethod]
	public void DivRem_Long()
	{
		var quotient = 17L.DivRem(5L, out var remainder);
		quotient.AssertEqual(3L);
		remainder.AssertEqual(2L);
	}

	#endregion

	#region BigMul Tests

	[TestMethod]
	public void BigMul_Int()
	{
		int.MaxValue.BigMul(2).AssertEqual((long)int.MaxValue * 2);
		1000000.BigMul(1000000).AssertEqual(1000000000000L);
	}

	#endregion

	#region ToRadians/ToAngles Tests

	[TestMethod]
	public void ToRadians()
	{
		0.0.ToRadians().AssertEqual(0.0, _deltad);
		180.0.ToRadians().AssertEqual(Math.PI, _deltad);
		90.0.ToRadians().AssertEqual(Math.PI / 2, _deltad);
	}

	[TestMethod]
	public void ToAngles()
	{
		0.0.ToAngles().AssertEqual(0.0, _deltad);
		Math.PI.ToAngles().AssertEqual(180.0, _deltad);
		(Math.PI / 2).ToAngles().AssertEqual(90.0, _deltad);
	}

	#endregion

	#region IsNaN/IsInfinity Tests

	[TestMethod]
	public void IsNaN_Double()
	{
		double.NaN.IsNaN().AssertTrue();
		0.0.IsNaN().AssertFalse();
		double.PositiveInfinity.IsNaN().AssertFalse();
	}

	[TestMethod]
	public void IsInfinity_Double()
	{
		double.PositiveInfinity.IsInfinity().AssertTrue();
		double.NegativeInfinity.IsInfinity().AssertTrue();
		0.0.IsInfinity().AssertFalse();
		double.NaN.IsInfinity().AssertFalse();
	}

	[TestMethod]
	public void IsPositiveInfinity_Double()
	{
		double.PositiveInfinity.IsPositiveInfinity().AssertTrue();
		double.NegativeInfinity.IsPositiveInfinity().AssertFalse();
	}

	[TestMethod]
	public void IsNegativeInfinity_Double()
	{
		double.NegativeInfinity.IsNegativeInfinity().AssertTrue();
		double.PositiveInfinity.IsNegativeInfinity().AssertFalse();
	}

	#endregion

	#region ToDecimal Tests

	[TestMethod]
	public void ToDecimal_Double()
	{
		123.456.ToDecimal().AssertEqual(123.456m);
		double.NaN.ToDecimal().AssertNull();
		double.PositiveInfinity.ToDecimal().AssertNull();
		double.NegativeInfinity.ToDecimal().AssertNull();
	}

	[TestMethod]
	public void ToDecimal_Float()
	{
		123.456f.ToDecimal().AssertNotNull();
		float.NaN.ToDecimal().AssertNull();
		float.PositiveInfinity.ToDecimal().AssertNull();
	}

	[TestMethod]
	public void ToDecimal_MantissaExponent()
	{
		MathHelper.ToDecimal(123, 0).AssertEqual(123m);
		MathHelper.ToDecimal(123, 2).AssertEqual(12300m);
		MathHelper.ToDecimal(123, -2).AssertEqual(1.23m);
	}

	#endregion

	#region AsRaw Tests

	[TestMethod]
	public void AsRaw_Double()
	{
		var value = 123.456;
		var raw = value.AsRaw();
		raw.AsRaw().AssertEqual(value, _deltad);
	}

	[TestMethod]
	public void AsRaw_Float()
	{
		var value = 123.456f;
		var raw = value.AsRaw();
		raw.AsRaw().AssertEqual(value, _deltaf);
	}

	#endregion

	#region GetRoots Tests

	[TestMethod]
	public void GetRoots_RealRoots()
	{
		// x^2 - 5x + 6 = 0 -> roots are 2 and 3
		var roots = MathHelper.GetRoots(1, -5, 6);
		roots.Length.AssertEqual(2);
		((roots[0] - 3).Abs() < _deltad || (roots[0] - 2).Abs() < _deltad).AssertTrue();
		((roots[1] - 3).Abs() < _deltad || (roots[1] - 2).Abs() < _deltad).AssertTrue();
	}

	[TestMethod]
	public void GetRoots_NoRealRoots()
	{
		// x^2 + 1 = 0 -> no real roots
		var roots = MathHelper.GetRoots(1, 0, 1);
		roots.Length.AssertEqual(0);
	}

	#endregion

	[TestMethod]
	public void ByteBit_Consistency()
	{
		// Test that SetBit and GetBit use consistent indexing
		// Set bit 0 and verify we can get it back
		byte test = 0;
		test = test.SetBit(0, true);
		test.GetBit(0).AssertTrue();
	}

	[TestMethod]
	public void ByteBit_Consistency_Bit2()
	{
		byte test = 0;
		test = test.SetBit(2, true);
		test.GetBit(2).AssertTrue();
	}

	[TestMethod]
	public void SetBit_Byte_Values()
	{
		// SetBit uses 0-based indexing
		byte value = 0;
		value = value.SetBit(0, true);
		value.AssertEqual((byte)1);  // bit 0 set = 1

		value = value.SetBit(1, true);
		value.AssertEqual((byte)3);  // bits 0,1 set = 3

		value = value.SetBit(0, false);
		value.AssertEqual((byte)2);  // bit 1 set = 2
	}

	[TestMethod]
	public void GetBit_Byte_Values()
	{
		// 5 = 0101 in binary
		// If 0-based: bit 0 = 1, bit 1 = 0, bit 2 = 1
		byte value = 5;

		// These expectations assume 0-based indexing
		value.GetBit(0).AssertTrue();   // bit 0 should be 1
		value.GetBit(1).AssertFalse();  // bit 1 should be 0
		value.GetBit(2).AssertTrue();   // bit 2 should be 1
	}

	[TestMethod]
	public void GetBit_Int_Index31_Works()
	{
		var value = int.MinValue; // only bit 31 is set
		value.GetBit(31).AssertTrue();

		ThrowsExactly<ArgumentOutOfRangeException>(() => value.GetBit(32));
	}

	[TestMethod]
	public void SetBit_Int_Index31_Works()
	{
		// Set the highest bit (bit 31)
		0.SetBit(31, true).AssertEqual(int.MinValue);

		// Index 32 should throw
		ThrowsExactly<ArgumentOutOfRangeException>(() => 0.SetBit(32, true));
	}

	[TestMethod]
	public void GetBit_Long_Index63_Works()
	{
		// Test that index 63 (highest bit for long) works correctly
		var value = long.MinValue; // only bit 63 is set
		value.GetBit(63).AssertTrue();

		// Index 64 should throw
		ThrowsExactly<ArgumentOutOfRangeException>(() => value.GetBit(64));
	}

	[TestMethod]
	public void SetBit_Long_Index63_Works()
	{
		// Set the highest bit (bit 63)
		0L.SetBit(63, true).AssertEqual(long.MinValue);

		// Index 64 should throw
		ThrowsExactly<ArgumentOutOfRangeException>(() => 0L.SetBit(64, true));
	}

	[TestMethod]
	public void GetBit_Long_HighBits_Work()
	{
		for (int i = 32; i < 63; i++)
		{
			var value = 1L << i;
			value.GetBit(i).AssertTrue();

			// Other bits should be false
			value.GetBit(0).AssertFalse();
			value.GetBit(31).AssertFalse();
		}
	}

	[TestMethod]
	public void SetBit_Long_HighBits_Work()
	{
		// Test setting bits 32-62
		for (int i = 32; i < 63; i++)
		{
			var result = 0L.SetBit(i, true);
			result.AssertEqual(1L << i);
		}
	}
}