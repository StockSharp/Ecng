﻿namespace Ecng.Test.Common
{
	using Ecng.Common;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class MathHelperTest
	{
		[TestMethod]
		public void RoundToNearest()
		{
			0.9999.RoundToNearest().AssertEqual(1);
			0.09999.RoundToNearest().AssertEqual(0.1);
			0.009999.RoundToNearest().AssertEqual(0.01);
			0.0009999.RoundToNearest().AssertEqual(0.001);
			0.00009999.RoundToNearest().AssertEqual(0.0001);
			0.000009999.RoundToNearest().AssertEqual(0.00001);
		}

		[TestMethod]
		public void Decimals()
		{
			"0,0".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(0);
			"00,0".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(0);
			"0,00".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(0);
			"0,01".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(2);
			"0,0100".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(2);
			"10,0".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(0);
			"10,000".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(0);
			"03,0000".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(0);
			"303,033".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(3);
			"00,00".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(0);
			"0,456".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(3);
			"0,56676".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(5);
			"3,45443".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(5);
			"3333,45443".To<decimal>().GetDecimalInfo().EffectiveScale.AssertEqual(5);

			//0.0.GetDecimals().AssertEqual(0);
			//1.0.GetDecimals().AssertEqual(0);
			//0.1.GetDecimals().AssertEqual(1);
			//0.0011.GetDecimals().AssertEqual(4);
			//0.056570006674.GetDecimals().AssertEqual(12);
		}
	}
}