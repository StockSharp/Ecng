// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IDeltaCalculator.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Numerics
{
    /// <summary>
    /// Provides an interface for Tick calculators
    /// </summary>
    public interface IDeltaCalculator
    {
        /// <summary>
        /// Given an absolute Axis Min and Max, returns a TickRange instance containing sensible MinorDelta and MajorDelta values
        /// </summary>
        IAxisDelta GetDeltaFromRange(IComparable min, IComparable max, int minorsPerMajor, uint maxTicks);
    }

    /// <summary>
    /// Defines the interface for DateTime or TimeSpan Tick calculators
    /// </summary>
    public interface IDateDeltaCalculator : IDeltaCalculator
    {
        /// <summary>
        /// Given an absolute Axis Min and Max, returns a TickRange instance containing sensible MinorDelta and MajorDelta values
        /// </summary>
        new TimeSpanDelta GetDeltaFromRange(IComparable min, IComparable max, int minorsPerMajor, uint maxTicks);
    }
}
