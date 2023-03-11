namespace Ecng.MathLight;

using System;

public static class Normal
{
	// MathNet.Numerics

	private static readonly double[] ErfImpAn = new double[8] { 0.0033791670955125737, -0.00073695653048167951, -0.37473233739291961, 0.081744244873358726, -0.042108931993654862, 0.0070165709512095753, -0.004950912559824351, 0.00087164659903792247 };
	private static readonly double[] ErfImpAd = new double[8] { 1.0, -0.21808821808792464, 0.4125429727254421, -0.08418911478731067, 0.065533885640024159, -0.012001960445494177, 0.00408165558926174, -0.00061590072155776965 };
	private static readonly double[] ErfImpBn = new double[6] { -0.036179039071826249, 0.29225188344488268, 0.28144704179760449, 0.12561020886276694, 0.027413502826893053, 0.0025083967216806575 };
	private static readonly double[] ErfImpBd = new double[6] { 1.0, 1.8545005897903486, 1.4357580303783142, 0.58282765875303655, 0.12481047693294975, 0.011372417654635328 };
	private static readonly double[] ErfImpCn = new double[7] { -0.039787689261113687, 0.15316521246787829, 0.19126029560093624, 0.10276327061989304, 0.029637090615738836, 0.0046093486780275491, 0.00030760782034868021 };
	private static readonly double[] ErfImpCd = new double[7] { 1.0, 1.955200729876277, 1.6476231719938486, 0.76823860702212621, 0.20979318593650978, 0.031956931689991336, 0.0021336316089578537 };
	private static readonly double[] ErfImpDn = new double[7] { -0.030083856055794972, 0.053857882984445452, 0.072621154165191423, 0.036762846988804936, 0.0096462901557252748, 0.0013345348007529107, 7.7808759978250427E-05 };
	private static readonly double[] ErfImpDd = new double[8] { 1.0, 1.7596709814716753, 1.3288357143796112, 0.55252859650875763, 0.13379305694133287, 0.017950964517628076, 0.0010471244001993736, -1.0664038182035734E-08 };
	private static readonly double[] ErfImpEn = new double[7] { -0.011790757013722784, 0.014262132090538809, 0.020223443590296084, 0.0093066829999043209, 0.00213357802422066, 0.00025022987386460105, 1.2053491221958819E-05 };
	private static readonly double[] ErfImpEd = new double[7] { 1.0, 1.5037622520362048, 0.96539778620446293, 0.33926523047679669, 0.068974064954156977, 0.0077106026249176831, 0.00037142110153106928 };
	private static readonly double[] ErfImpFn = new double[7] { -0.0054695479553872927, 0.0040419027873170709, 0.0054963369553161171, 0.0021261647260394541, 0.00039498401449508392, 3.6556547706444238E-05, 1.3548589710993232E-06 };
	private static readonly double[] ErfImpFd = new double[8] { 1.0, 1.2101969777363077, 0.62091466822114394, 0.17303843066114277, 0.027655081377343203, 0.0024062597442430973, 8.9181181725133651E-05, -4.6552883628338267E-12 };
	private static readonly double[] ErfImpGn = new double[6] { -0.0027072253590577837, 0.00131875634250294, 0.0011992593326100233, 0.00027849619811344664, 2.6782298821833186E-05, 9.2304367231502819E-07 };
	private static readonly double[] ErfImpGd = new double[7] { 1.0, 0.81463280854314157, 0.26890166585629954, 0.044987721610304114, 0.0038175966332024847, 0.00013157189788859692, 4.0481535967576414E-12 };
	private static readonly double[] ErfImpHn = new double[6] { -0.001099467206917422, 0.00040642544275042267, 0.00027449948941690071, 4.6529377064665937E-05, 3.2095542539576746E-06, 7.7828601814502088E-08 };
	private static readonly double[] ErfImpHd = new double[6] { 1.0, 0.58817371061184609, 0.13936333128940975, 0.016632934041708368, 0.0010002392131023491, 2.4254837521587224E-05 };
	private static readonly double[] ErfImpIn = new double[5] { -0.00056907993601094963, 0.00016949854037376225, 5.1847235458110088E-05, 3.8281931223192885E-06, 8.2498993128189441E-08 };
	private static readonly double[] ErfImpId = new double[6] { 1.0, 0.33963725005113937, 0.04347264787031066, 0.002485493352246371, 5.3563330533715289E-05, -1.1749094440545958E-13 };
	private static readonly double[] ErfImpJn = new double[5] { -0.00024131359948399134, 5.742249752025015E-05, 1.1599896292738377E-05, 5.8176213440259376E-07, 8.539715550856736E-09 };
	private static readonly double[] ErfImpJd = new double[5] { 1.0, 0.23304413829968784, 0.02041869405464403, 0.00079718564756439832, 1.1701928167017232E-05 };
	private static readonly double[] ErfImpKn = new double[5] { -0.00014667469927776036, 1.6266655211228053E-05, 2.6911624850916523E-06, 9.79584479468092E-08, 1.0199464762572346E-09 };
	private static readonly double[] ErfImpKd = new double[5] { 1.0, 0.16590781294484722, 0.010336171619150588, 0.00028659302637386839, 2.9840157084090034E-06 };
	private static readonly double[] ErfImpLn = new double[5] { -5.8390579762977178E-05, 4.125103251054962E-06, 4.3179092242025094E-07, 9.9336515559001325E-09, 6.5348051002010468E-11 };
	private static readonly double[] ErfImpLd = new double[5] { 1.0, 0.10507708607203992, 0.0041427842867547563, 7.2633875464452377E-05, 4.7781847104739878E-07 };
	private static readonly double[] ErfImpMn = new double[4] { -1.9645779760922958E-05, 1.572438876668007E-06, 5.4390251119270091E-08, 3.1747249236911772E-10 };
	private static readonly double[] ErfImpMd = new double[5] { 1.0, 0.052803989240957631, 0.00092687606915175331, 5.4101172322663028E-06, 5.3509384580364237E-16 };
	private static readonly double[] ErfImpNn = new double[4] { -7.892247039787227E-06, 6.22088451660987E-07, 1.457284456768824E-08, 6.0371550554271534E-11 };
	private static readonly double[] ErfImpNd = new double[4] { 1.0, 0.037532884635629371, 0.00046791953597462532, 1.9384703927584565E-06 };

	public static double Evaluate(double z, params double[] coefficients)
	{
		if (coefficients == null)
		{
			throw new ArgumentNullException("coefficients");
		}

		int num = coefficients.Length;
		if (num == 0)
		{
			return 0.0;
		}

		double num2 = coefficients[num - 1];
		for (int num3 = num - 2; num3 >= 0; num3--)
		{
			num2 *= z;
			num2 += coefficients[num3];
		}

		return num2;
	}

	public static double CumulativeDistribution(double x)
	{
		static double ErfImp(double z, bool invert)
		{
			if (z < 0.0)
			{
				if (!invert)
				{
					return 0.0 - ErfImp(0.0 - z, invert: false);
				}

				if (z < -0.5)
				{
					return 2.0 - ErfImp(0.0 - z, invert: true);
				}

				return 1.0 + ErfImp(0.0 - z, invert: false);
			}

			double num;
			if (z < 0.5)
			{
				num = ((!(z < 1E-10)) ? (z * 1.125 + z * Evaluate(z, ErfImpAn) / Evaluate(z, ErfImpAd)) : (z * 1.125 + z * 0.0033791670955125737));
			}
			else if (z < 110.0)
			{
				invert = !invert;
				double num2;
				double num3;
				if (z < 0.75)
				{
					num2 = Evaluate(z - 0.5, ErfImpBn) / Evaluate(z - 0.5, ErfImpBd);
					num3 = 0.34402421116828918;
				}
				else if (z < 1.25)
				{
					num2 = Evaluate(z - 0.75, ErfImpCn) / Evaluate(z - 0.75, ErfImpCd);
					num3 = 0.41999092698097229;
				}
				else if (z < 2.25)
				{
					num2 = Evaluate(z - 1.25, ErfImpDn) / Evaluate(z - 1.25, ErfImpDd);
					num3 = 0.48986250162124634;
				}
				else if (z < 3.5)
				{
					num2 = Evaluate(z - 2.25, ErfImpEn) / Evaluate(z - 2.25, ErfImpEd);
					num3 = 0.53173708915710449;
				}
				else if (z < 5.25)
				{
					num2 = Evaluate(z - 3.5, ErfImpFn) / Evaluate(z - 3.5, ErfImpFd);
					num3 = 0.54899734258651733;
				}
				else if (z < 8.0)
				{
					num2 = Evaluate(z - 5.25, ErfImpGn) / Evaluate(z - 5.25, ErfImpGd);
					num3 = 0.55717408657073975;
				}
				else if (z < 11.5)
				{
					num2 = Evaluate(z - 8.0, ErfImpHn) / Evaluate(z - 8.0, ErfImpHd);
					num3 = 0.56098079681396484;
				}
				else if (z < 17.0)
				{
					num2 = Evaluate(z - 11.5, ErfImpIn) / Evaluate(z - 11.5, ErfImpId);
					num3 = 0.56264936923980713;
				}
				else if (z < 24.0)
				{
					num2 = Evaluate(z - 17.0, ErfImpJn) / Evaluate(z - 17.0, ErfImpJd);
					num3 = 0.56345981359481812;
				}
				else if (z < 38.0)
				{
					num2 = Evaluate(z - 24.0, ErfImpKn) / Evaluate(z - 24.0, ErfImpKd);
					num3 = 0.56384778022766113;
				}
				else if (z < 60.0)
				{
					num2 = Evaluate(z - 38.0, ErfImpLn) / Evaluate(z - 38.0, ErfImpLd);
					num3 = 0.56405282020568848;
				}
				else if (z < 85.0)
				{
					num2 = Evaluate(z - 60.0, ErfImpMn) / Evaluate(z - 60.0, ErfImpMd);
					num3 = 0.56413090229034424;
				}
				else
				{
					num2 = Evaluate(z - 85.0, ErfImpNn) / Evaluate(z - 85.0, ErfImpNd);
					num3 = 0.56415843963623047;
				}

				double num4 = Math.Exp((0.0 - z) * z) / z;
				num = num4 * num3 + num4 * num2;
			}
			else
			{
				num = 0.0;
				invert = !invert;
			}

			if (invert)
			{
				num = 1.0 - num;
			}

			return num;
		}

		static double Erfc(double x)
		{
			if (x == 0.0)
			{
				return 1.0;
			}

			if (double.IsPositiveInfinity(x))
			{
				return 0.0;
			}

			if (double.IsNegativeInfinity(x))
			{
				return 2.0;
			}

			if (double.IsNaN(x))
			{
				return double.NaN;
			}

			return ErfImp(x, invert: true);
		}

		const double _mean = 0;
		const double _stdDev = 1;

		return 0.5 * Erfc((_mean - x) / (_stdDev * 1.4142135623730951));
	}
}