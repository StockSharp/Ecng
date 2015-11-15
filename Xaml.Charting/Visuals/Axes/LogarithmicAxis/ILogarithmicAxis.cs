// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ILogarithmicAxis.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// Defines the interface to a logarithmic axis, the value axis which uses a logarithmic scale. 
    /// The <see cref="LogarithmicBase"/> property determines which base is used for the logarithm.
    /// </summary>
    public interface ILogarithmicAxis : IAxis
    {
        /// <summary>
        /// Gets or sets the value which determines the base used for the logarithm.
        /// </summary>
        double LogarithmicBase { get; set; }

        /// <summary>
        /// Gets or sets used number format.
        /// </summary>
        ScientificNotation ScientificNotation { get; set; }
    }
}
