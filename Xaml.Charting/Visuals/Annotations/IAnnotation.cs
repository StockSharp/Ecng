// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IAnnotation.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Specialized;
using System.Windows;
using System.Xml.Serialization;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Utility.Mouse;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Visuals.Annotations
{
    /// <summary>
    /// Defines the interface to an annotation, a custom drawable element over or under the UltrachartSurface
    /// </summary>
    public interface IAnnotation : IHitTestable, IPublishMouseEvents, IXmlSerializable
    {
        /// <summary>
        /// Occurs when a Drag or move operation starts
        /// </summary>
        event EventHandler<EventArgs> DragStarted;

        /// <summary>
        /// Occurs when a Drag or move operation ends
        /// </summary>
        event EventHandler<EventArgs> DragEnded;

        /// <summary>
        /// Occurs when current <see cref="AnnotationBase"/> is dragged or moved
        /// </summary>
        event EventHandler<AnnotationDragDeltaEventArgs> DragDelta;

        /// <summary>
        /// Occurs when <see cref="IAnnotation"/> becomes selected. 
        /// </summary>
        event EventHandler Selected;

        /// <summary>
        /// Occurs when <see cref="IAnnotation"/> becomes unselected. 
        /// </summary>
        event EventHandler Unselected;

        /// <summary>
        /// Gets or sets the XAxis Id that this annotation is measured against. See <see cref="AxisBase.Id"/>
        /// </summary>
        string XAxisId { get; set; }

        /// <summary>
        /// Gets or sets the YAxis Id that this annotation is measured against. See <see cref="AxisBase.Id"/>
        /// </summary>
        string YAxisId { get; set; }

        /// <summary>
        /// Gets or sets whether the current annotation is attached
        /// </summary>
        bool IsAttached { get; set; }

        /// <summary>
        /// Gets or sets whether the current annotation is selected
        /// </summary>
        bool IsSelected { get; set; }

        /// <summary>
        /// Gets or sets whether you can interact current annotation
        /// </summary>
        bool IsEditable { get; set; }

        /// <summary>
        /// Gets or sets value, indicates whether current annotation was hidden by <see cref="Hide"/> call
        /// </summary>
        bool IsHidden { get; set; }

        /// <summary>
        /// Gets the primary YAxis, this is the first axis in the YAxes collection
        /// </summary>
        IAxis YAxis { get; }

        /// <summary>
        /// Returns the YAxes on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        IEnumerable<IAxis> YAxes { get; }

        /// <summary>
        /// Gets the XAxis <see cref="IAxis"/> instance on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        IAxis XAxis { get; }

        /// <summary>
        /// Returns the XAxes on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        IEnumerable<IAxis> XAxes { get; }

        /// <summary>
        /// Gets or sets a <see cref="IServiceContainer"/> container 
        /// </summary>
        IServiceContainer Services { get; set; }

        /// <summary>
        /// Gets or sets the X1 Coordinate of the Annotation. 
        /// 
        /// For <see cref="AnnotationCoordinateMode.Absolute"/>, this must be a data-value on the X-Axis such as a DateTime for <see cref="DateTimeAxis"/>, double for <see cref="NumericAxis"/> or integer index for <see cref="CategoryDateTimeAxis"/>.
        /// 
        /// For <see cref="AnnotationCoordinateMode.Relative"/>, this must be a double value between 0.0 and 1.0, where 0.0 is the far left of the XAxis and 1.0 is the far right.
        /// </summary>
        IComparable X1 { get; set; }

        /// <summary>
        /// Gets or sets the Y1 Coordinate of the Annotation. 
        /// 
        /// For <see cref="AnnotationCoordinateMode.Absolute"/>, this must be a data-value on the Y-Axis such as a double for <see cref="NumericAxis"/> 
        /// 
        /// For <see cref="AnnotationCoordinateMode.Relative"/>, this must be a double value between 0.0 and 1.0, where 0.0 is the bottom of the YAxis and 1.0 is the top
        /// </summary>
        IComparable Y1 { get; set; }

        /// <summary>
        /// Gets or sets the X2 Coordinate of the Annotation. 
        /// 
        /// For <see cref="AnnotationCoordinateMode.Absolute"/>, this must be a data-value on the X-Axis such as a DateTime for <see cref="DateTimeAxis"/>, double for <see cref="NumericAxis"/> or integer index for <see cref="CategoryDateTimeAxis"/>.
        /// 
        /// For <see cref="AnnotationCoordinateMode.Relative"/>, this must be a double value between 0.0 and 1.0, where 0.0 is the far left of the XAxis and 1.0 is the far right.
        /// </summary>
        IComparable X2 { get; set; }

        /// <summary>
        /// Gets or sets the Y2 Coordinate of the Annotation. 
        /// 
        /// For <see cref="AnnotationCoordinateMode.Absolute"/>, this must be a data-value on the Y-Axis such as a double for <see cref="NumericAxis"/> 
        /// 
        /// For <see cref="AnnotationCoordinateMode.Relative"/>, this must be a double value between 0.0 and 1.0, where 0.0 is the bottom of the YAxis and 1.0 is the top
        /// </summary>
        IComparable Y2 { get; set; }

        /// <summary>
        /// Gets or sets the parent <see cref="IUltrachartSurface"/> that this Annotation belongs to
        /// </summary>
        IUltrachartSurface ParentSurface { get; set; }

        /// <summary>
        /// Limits the Drag direction when dragging the annotation using the mouse, e.g in the X-Direction, Y-Direction or XyDirection. See the <see cref="XyDirection"/> enumeration for options
        /// </summary>
        XyDirection DragDirections { get; set; }

        /// <summary>
        /// Limits the Resize direction when resiaing the annotation using the mouse, e.g in the X-Direction, Y-Direction or XyDirection. See the <see cref="XyDirection"/> enumeration for options
        /// </summary>
        XyDirection ResizeDirections { get; set; }

        /// <summary>
        /// Gets value, indicates whether current instance is resizable
        /// </summary>
        bool IsResizable { get; }

        /// <summary>
        /// Gets or sets the data context
        /// </summary>
        object DataContext { get; set; }

        /// <summary>
        /// Captures the mouse
        /// </summary>
        bool CaptureMouse();

        /// <summary>
        /// Releases mouse capture
        /// </summary>
        void ReleaseMouseCapture();

        /// <summary>
        /// Updates the coordinate calculators and refreshes the annotation position on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="xCoordinateCalculator">The XAxis <see cref="ICoordinateCalculator{T}"/></param>
        /// <param name="yCoordinateCalculator">The YAxis <see cref="ICoordinateCalculator{T}"/></param>
        void Update(ICoordinateCalculator<double> xCoordinateCalculator, ICoordinateCalculator<double> yCoordinateCalculator);
        
        /// <summary>
        /// Called when the Annotation is detached from its parent surface
        /// </summary>
        void OnDetached();

        /// <summary>
        /// Called when the Annotation is attached to parent surface
        /// </summary>
        void OnAttached();

        /// <summary>
        /// Hides the Annotation by removing adorner markers from the <see cref="ParentSurface"/> AdornerLayerCanvas
        /// and setting Visibility to Collapsed
        /// </summary>
        void Hide();

        /// <summary>
        /// Shows annotation which being hidden by <see cref="Hide"/> call
        /// </summary>
        void Show();

        /// <summary>
        /// This method is used internally by the <see cref="AnnotationDragAdorner"/>. Programmatically moves the annotation by an X,Y offset. 
        /// </summary>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        void MoveAnnotation(double offsetX, double offsetY);

        /// <summary>
        /// This method is used in internally by the <see cref="AnnotationResizeAdorner"/>. Programmatically sets an adorner point position
        /// </summary>
        /// <param name="newPoint"></param>
        /// <param name="index"></param>
        void SetBasePoint(Point newPoint, int index);

        /// <summary>
        /// This method is used in internally by the <see cref="AnnotationResizeAdorner"/>. Gets the adorner point positions
        /// </summary>
        Point[] GetBasePoints();

        /// <summary>
        /// Refreshes the annnotation position on the parent <see cref="UltrachartSurface"/>, without causing a full redraw of the chart
        /// </summary>
        bool Refresh();

        /// <summary>
        /// Raises the <see cref="AnnotationBase.DragStarted"/> event, called when a drag operation starts
        /// </summary>
        void OnDragStarted();

        /// <summary>
        /// Raises the <see cref="AnnotationBase.DragEnded"/> event, called when a drag operation ends
        /// </summary>
        void OnDragEnded();

        /// <summary>
        /// Raises the <see cref="AnnotationBase.DragDelta"/> event, called when a drag operation is in progress and each time the X1 Y1 X2 Y2 points update in the annotation
        /// </summary>
        void OnDragDelta();

        /// <summary>
        /// Raises notification when parent <see cref="UltrachartSurface.XAxes"/> changes.
        /// </summary>
        void OnXAxesCollectionChanged(object sender, NotifyCollectionChangedEventArgs args);

        /// <summary>
        /// Raises notification when parent <see cref="UltrachartSurface.YAxes"/> changes.
        /// </summary>
        void OnYAxesCollectionChanged(object sender, NotifyCollectionChangedEventArgs args);
    }
}