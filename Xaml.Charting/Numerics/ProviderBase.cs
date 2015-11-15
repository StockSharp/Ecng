// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ProviderBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Numerics
{
    /// <summary>
    /// A base class for TickCoordinate Providers and LabelProviders. 
    /// </summary>
    public abstract class ProviderBase
    {
        private IAxis _parentAxis;

        /// <summary>
        /// Gets the axis current provider instance was initialized with
        /// </summary>
        public IAxis ParentAxis
        {
            get { return _parentAxis; }
            protected set { _parentAxis = value; }
        }

        /// <summary>
        /// Called when the provider instance is initialized as it is attached to the parent axis, with the parent axis instance
        /// </summary>
        /// <param name="parentAxis">The parent <see cref="IAxis"/> instance</param>
        public virtual void Init(IAxis parentAxis)
        {
            ParentAxis = parentAxis;
        }
    }
}
