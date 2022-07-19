namespace Ecng.Tests.Common
{
	using System;

	using Ecng.Common;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

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
		public void Round_MidpointRounding_double()
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
		public void Round_MidpointRounding_decimal()
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
	}
}