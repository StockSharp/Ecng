namespace Ecng.Tests.Common;

[TestClass]
public class MathHelperTest
{
	const double deltad = 0.00001d;
	const float  deltaf = 0.00001f;

	[TestMethod]
	public void Floor()
	{
		0.5.Floor().AssertEqual(0);
		(-0.5).Floor().AssertEqual(-1);

		( 10.1).Floor(0.2).AssertEqual(10);
		(-10.1).Floor(0.2).AssertEqual(-10.2d, deltad);

		( 10.1f).Floor(0.2f).AssertEqual(10f, deltaf);
		(-10.1f).Floor(0.2f).AssertEqual(-10.2f, deltaf);

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
		0.9999.RoundToNearest().AssertEqual(1);
		0.09999.RoundToNearest().AssertEqual(0.1);
		0.009999.RoundToNearest().AssertEqual(0.01);

		// TODO
		//0.0009999.RoundToNearest().AssertEqual(0.001);
		//0.00009999.RoundToNearest().AssertEqual(0.0001);
		//0.000009999.RoundToNearest().AssertEqual(0.00001);
	}

	[TestMethod]
	public void RoundToNearestBig()
	{
		9999.9999.RoundToNearest().AssertEqual(10000);
		// TODO
		//99999.09999.RoundToNearest().AssertEqual(100000);
		//999999.009999.RoundToNearest().AssertEqual(1000000);
		//9999999.0009999.RoundToNearest().AssertEqual(10000000);
		//99999999.00009999.RoundToNearest().AssertEqual(10000000);
		//999999999.000009999.RoundToNearest().AssertEqual(100000000);
		//9999999999.0000009999.RoundToNearest().AssertEqual(1000000000);
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

			//0.0.GetDecimals().AssertEqual(0);
			//1.0.GetDecimals().AssertEqual(0);
			//0.1.GetDecimals().AssertEqual(1);
			//0.0011.GetDecimals().AssertEqual(4);
			//0.056570006674.GetDecimals().AssertEqual(12);
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
		Assert.ThrowsExactly<OverflowException>(() => value.GetDecimalInfo());
	}
}