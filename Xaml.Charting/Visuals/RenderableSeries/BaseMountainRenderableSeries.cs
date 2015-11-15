// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// BaseMountainRenderableSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using System.Windows.Media;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Numerics;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// An abstract base class which factors out many properties from the <seealso cref="FastMountainRenderableSeries"/>
    /// and <see cref="StackedMountainRenderableSeries"/> types. 
    /// </summary>
    /// <seealso cref="FastMountainRenderableSeries"/>
    /// <seealso cref="StackedMountainRenderableSeries"/>
    public abstract class BaseMountainRenderableSeries : BaseRenderableSeries
    {
        /// <summary>
        /// Defines the IsDigitalLine DependencyProperty
        /// </summary>
        public static readonly DependencyProperty IsDigitalLineProperty = DependencyProperty.Register("IsDigitalLine", typeof(bool), typeof(BaseMountainRenderableSeries), new PropertyMetadata(false, OnInvalidateParentSurface));

        /// <summary>
        /// Defines the AreaBrush DependencyProperty
        /// </summary>
        public static readonly DependencyProperty AreaBrushProperty = DependencyProperty.Register("AreaBrush", typeof(Brush), typeof(BaseMountainRenderableSeries), new PropertyMetadata(default(Brush), OnInvalidateParentSurface));

        /// <summary>
        /// Initializes a new instance of the <seealso cref="FastMountainRenderableSeries"/> class.
        /// </summary>
        /// <remarks></remarks>
        protected BaseMountainRenderableSeries()
        {
            this.SetCurrentValue(ResamplingModeProperty, ResamplingMode.Max);
        }

        /// <summary>
        /// Gets or sets the Area Brush for the <seealso cref="FastMountainRenderableSeries"/>. The mountain chart outline is specified by <see cref="BaseRenderableSeries.SeriesColor"/>
        /// </summary>
        public Brush AreaBrush
        {
            get { return (Brush) GetValue(AreaBrushProperty); }
            set { SetValue(AreaBrushProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Area Color for the <seealso cref="FastMountainRenderableSeries"/>. The mountain chart outline is specified by <see cref="BaseRenderableSeries.SeriesColor"/>
        /// </summary>
        [Obsolete("AreaColor is obsolete. Please use the AreaBrush property instead", true)]
        public Color AreaColor { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this mountain series has a digital (step) line
        /// </summary>
        public bool IsDigitalLine
        {
            get { return (bool)GetValue(IsDigitalLineProperty); }
            set { SetValue(IsDigitalLineProperty, value); }
        }
    }
}
