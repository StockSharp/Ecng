﻿// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// TimeSpanDeltaCalculator.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Numerics
{
    internal class TimeSpanDeltaCalculator : TimeSpanDeltaCalculatorBase
    {
        private static TimeSpanDeltaCalculator _instance;

        internal static TimeSpanDeltaCalculator Instance
        {
            get { return _instance ?? (_instance = new TimeSpanDeltaCalculator()); }
        }

        protected TimeSpanDeltaCalculator() { }

        protected override long GetTicks(IComparable value)
        {
            return value.ToTimeSpan().Ticks;
        }
    }
}