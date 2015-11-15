// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IUltrachartSurface.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Visuals.Annotations;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Visuals
{
    /// <summary>
    /// Defines the interface to the <see cref="UltrachartSurface"/>, which contains a single <see cref="Ecng.Xaml.Charting.Rendering.Common.RenderSurfaceBase"/> viewport 
    /// for rendering multiple <see cref="IRenderableSeries"/>, X and Y <see cref="IAxis"/> instances, and where each <see cref="IRenderableSeries"/> may have a <see cref="IDataSeries"/> data source. 
    /// 
    /// The <see cref="UltrachartSurface"/> may have zero to many <see cref="UIElement"/> <see cref="AnnotationBase">annotations</see> and may have a <see cref="ChartModifierBase"/> to enable interaction with the chart.
    /// Where many <see cref="ChartModifierBase">ChartModifiers</see> are used, you may use a <see cref="ModifierGroup"/> to group them.
    /// </summary>
    /// <seealso cref="UltrachartSurface"/>
    /// <seealso cref="DataSeries{TX,TY}"/>
    /// <seealso cref="BaseRenderableSeries"/>
    /// <seealso cref="AxisBase"/>
    /// <seealso cref="AnnotationBase"/>
    /// <seealso cref="ChartModifierBase"/>
    /// <seealso cref="ModifierGroup"/>
    public interface IUltrachartSurface : IUltrachartSurfaceBase, IUltrachartController, IDisposable
    {
        /// <summary>
        /// Event raised when alignment of any axis changed
        /// </summary>
        event EventHandler<AxisAlignmentChangedEventArgs> AxisAlignmentChanged;

        /// <summary>
        /// Event raised when Annotations DependencyProperty is changed
        /// </summary>
        event EventHandler AnnotationsCollectionNewCollectionAssigned;

        /// <summary>
        /// Event raised when YAxes DependnecyProperty is changed
        /// </summary>
        event EventHandler YAxesCollectionNewCollectionAssigned;

        /// <summary>
        /// Event raised when XAxes DependnecyProperty is changed
        /// </summary>
        event EventHandler XAxesCollectionNewCollectionAssigned;

#if SILVERLIGHT
        /// <summary>Gets a valid indicating whether this FrameworkElement has been loaded for presentation </summary>
        bool IsLoaded { get; }
#endif

        /// <summary>
        /// Gets or sets the current ChartModifier, which alters the behaviour of the chart
        /// </summary>
        /// <remarks></remarks>
        IChartModifier ChartModifier { get; set; }

        /// <summary>
        /// Gets the <see cref="AnnotationCollection"/> which provides renderable annotations over the <see cref="UltrachartSurface"/>
        /// </summary>
        AnnotationCollection Annotations { get; }

        /// <summary>
        /// Gets or sets the XAxis control on the UltrachartSurface
        /// </summary>
        IAxis XAxis { get; set; } // todo: rename into "FirstXAxis" 

        /// <summary>
        /// Gets or sets the primary YAxis control on the UltrachartSurface (default side=Right)
        /// </summary>
        IAxis YAxis { get; set; } // todo: rename into "FirstyAxis" 

        /// <summary>
        /// Gets the collection of Y-Axis <see cref="IAxis"/> that this UltrachartSurface measures against
        /// </summary>
        AxisCollection YAxes { get; }

        /// <summary>
        /// Gets the collection of X-Axis <see cref="IAxis"/> that this UltrachartSurface measures against
        /// </summary>
        AxisCollection XAxes { get; }

        /// <summary>
        /// Gets the GridLinesPanel where gridlines are drawn
        /// </summary>
        IGridLinesPanel GridLinesPanel { get; }

        /// <summary>
        /// Gets the collection of RenderableSeries that this UltrachartSurface draws.        
        /// </summary>
        /// <remarks>A <see cref="IRenderableSeries"/> is bound to an <see cref="IDataSeries"/> derived type.
        /// If a RenderableSeries.IsEnabled=false, then this series is skipped when evaluating the series to draw</remarks>
        ObservableCollection<IRenderableSeries> RenderableSeries { get; }

        /// <summary>
        /// Gets the collection of RenderableSeries that are selected.
        /// </summary>
        /// <value>The renderable series.</value>
        /// <remarks></remarks>
        ObservableCollection<IRenderableSeries> SelectedRenderableSeries { get; }

        /// <summary>
        /// Gets the Root Grid that hosts the Ultrachart RenderSurface, GridLinesPanel, X-Axis and Y-Axes (Left and right)
        /// </summary>
        IMainGrid RootGrid { get; }

        /// <summary>
        /// Gets or sets the current ViewportManager, which alters the behaviour of the viewport (X,Y range) when the chart is rendered
        /// </summary>
        IViewportManager ViewportManager { get; set; }

        /// <summary>
        /// Gets the Annotation Canvas over the chart
        /// </summary>
        IAnnotationCanvas AnnotationOverlaySurface { get; }

        /// <summary>
        /// Gets the Annotation Canvas under the chart
        /// </summary>
        IAnnotationCanvas AnnotationUnderlaySurface { get; }

        /// <summary>
        /// Gets the Adorner Layer over the chart
        /// </summary>
        Canvas AdornerLayerCanvas { get; }

        /// <summary>
        /// Gets the number of license days remaining
        /// </summary>
        int LicenseDaysRemaining { get; }

        /// <summary>
        /// The SeriesSource property allows data-binding to a collection of <see cref="IChartSeriesViewModel"/> instances, 
        /// for pairing of <see cref="DataSeries{TX,TY}"/> with <see cref="IRenderableSeries"/>
        /// </summary>
        ObservableCollection<IChartSeriesViewModel> SeriesSource { get; set; }

        /// <summary>
        /// Removes all DataSeries from the Ultrachart
        /// </summary>
        void ClearSeries();

        /// <summary>
        /// Preparations for a render pass, called internally, returns the viewport size
        /// </summary>
        Size OnArrangeUltrachart();

        /// <summary>
        /// Returns true if the Point is within the bounds of the current HitTestable element
        /// </summary>
        /// <param name="point">The point to test</param>
        /// <returns>true if the Point is within the bounds</returns>
        /// <remarks></remarks>
        [Obsolete("Obsolete. Please use UltrachartSurface.RootGrid.IsPointWithinBounds instead", true)]
        bool IsPointWithinBounds(Point point);

        /// <summary>
        /// Gets the bounds of the current HitTestable element relative to another HitTestable element
        /// </summary>
        /// <param name="relativeTo">The relative to.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        [Obsolete("Obsolete. Please use UltrachartSurface.RootGrid.GetBoundsRelativeTo instead", true)]
        Rect GetBoundsRelativeTo(IHitTestable relativeTo);

        /// <summary>
        /// Translates the point relative to the other hittestable element
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="relativeTo">The relative to.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        [Obsolete("Obsolete. Please use UltrachartSurface.RootGrid.TranslatePoint instead", true)]
        Point TranslatePoint(Point point, IHitTestable relativeTo);

        /// <summary>
        /// Equivalent of calling YAxis.GetMaximumRange() however returns the max range only for that axis (by the data-series on it)
        /// </summary>
        /// <param name="yAxis"></param>
        /// <param name="xRange"></param>
        /// <returns></returns>
        [Obsolete("IUltrachartSurface.GetWindowedYRange is obsolete. Use IAxis.GetWindowedYRange instead")]
        IRange GetWindowedYRange(IAxis yAxis, IRange xRange);

        //void ZoomExtentsY(IRange xRange, TimeSpan duration);
        //void ZoomExtentsX(IRange maxXRange, TimeSpan duration);

        /// <summary>
        /// Called internally by Ultrachart when <see cref="IAxis.AxisAlignment"/> changes. Allows the <see cref="UltrachartSurface"/> to reposition the axis, e.g. at the top, left, bottom, right
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="oldValue"></param>
        void OnAxisAlignmentChanged(IAxis axis, AxisAlignment oldValue);

        /// <summary>
        /// Called internally by Ultrachart when <see cref="IAxis.IsCenterAxis"/> changes. Allows the <see cref="UltrachartSurface"/> to place the axis in the center of chart
        /// </summary>
        /// <param name="axis"></param>
        void OnIsCenterAxisChanged(IAxis axis);

        /// <summary>
        /// Detaches listeners for DataSeries.DataSeriesChanged
        /// </summary>
        /// <param name="dataSeries"></param>
        void DetachDataSeries(IDataSeries dataSeries);

        /// <summary>
        /// Attaches listeners for DataSeries.DataSeriesChanged
        /// </summary>
        /// <param name="dataSeries"></param>
        void AttachDataSeries(IDataSeries dataSeries);

        /// <summary>
        /// Export snapshot of current <see cref="UltrachartSurface"/> to <see cref="BitmapSource"/>
        /// </summary>
        /// <returns></returns>
        BitmapSource ExportToBitmapSource();
    }
}