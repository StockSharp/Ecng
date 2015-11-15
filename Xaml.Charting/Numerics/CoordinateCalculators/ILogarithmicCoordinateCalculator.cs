// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ILogarithmicCoordinateCalculator.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Numerics.CoordinateCalculators
{
    /// <summary>
    /// Defines the interface to a <see cref="LogarithmicNumericAxis"/> specific ICoordinateCalculator, to obtain LogarithmicBase
    /// </summary>
    public interface ILogarithmicCoordinateCalculator : ICoordinateCalculator<double>
    {
        /// <summary>
        /// Gets or sets the value which determines the base used for the logarithm.
        /// </summary>
        double LogarithmicBase { get; }
    }
}
