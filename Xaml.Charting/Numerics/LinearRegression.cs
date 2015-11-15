// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// LinearRegression.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
// For full terms and conditions of the license, see http://www.ultrachart.com/ultrachart-eula/
// 
// This source code is protected by international copyright law. Unauthorized
// reproduction, reverse-engineering, or distribution of all or any portion of
// this source code is strictly prohibited.
// 
// This source code contains confidential and proprietary trade secrets of
// ulc software Services Ltd., and should at no time be copied, transferred, sold,
// distributed or made available without express written permission.
// *************************************************************************************
using System;
using System.Collections;
using System.Collections.Generic;
using Ecng.Xaml.Charting.Common.Extensions;

namespace Ecng.Xaml.Charting.Numerics
{
    internal class LinearRegression
    {
        private readonly double _gradient;
        private readonly double _intercept;

        public LinearRegression(IList values)
        {
            //double[] values = { 4.8, 4.8, 4.5, 3.9, 4.4, 3.6, 3.6, 2.9, 3.5, 3.0, 2.5, 2.2, 2.6, 2.1, 2.2 };

            double xAvg = 0;
            double yAvg = 0;

            int valueCount = values.Count;

            for (int x = 0; x < valueCount; x++)
            {
                xAvg += x;
                yAvg += ((IComparable)values[x]).ToDouble();
            }

            xAvg = xAvg / values.Count;
            yAvg = yAvg / values.Count;

            double v1 = 0;
            double v2 = 0;

            for (int x = 0; x < valueCount; x++)
            {
                v1 += (x - xAvg) * (((IComparable)values[x]).ToDouble() - yAvg);
                v2 += Math.Pow(x - xAvg, 2);
            }

            _gradient = v1 / v2;

            _intercept = yAvg - Gradient * xAvg;

//            Console.WriteLine("y = ax + b");
//
//            Console.WriteLine("a = {0}, the slope of the trend line.", Math.Round(a, 2));
//
//            Console.WriteLine("b = {0}, the intercept of the trend line.", Math.Round(b, 2));
        }

        public double GetYValue(double x)
        {
            return _gradient*x + _intercept;
        }

        public double Intercept
        {
            get { return _intercept; }
        }

        public double Gradient
        {
            get { return _gradient; }
        }
    }
}
