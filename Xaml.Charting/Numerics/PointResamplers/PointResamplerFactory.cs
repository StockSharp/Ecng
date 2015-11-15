// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// PointResamplerFactory.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Numerics.PointResamplers
{
    /// <summary>
    /// A Factory class to get PointResamplers, which are used to reduce datasets to minimal sets for efficient on-screen rendering
    /// </summary>
    public interface IPointResamplerFactory
    {        
        /// <summary>
        /// Gets the <see cref="IPointResampler"/> instance to handle this combination of Tx and Ty generic type parameters
        /// </summary>
        /// <typeparam name="TX"></typeparam>
        /// <typeparam name="TY"></typeparam>
        /// <returns></returns>
        IPointResampler GetPointResampler<TX, TY>()
            where TX : IComparable
            where TY : IComparable;
    }
}