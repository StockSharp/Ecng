// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AxisPanel.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Ecng.Xaml.Charting.Common;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Common.Helpers;
using Ecng.Xaml.Charting.Numerics.TickCoordinateProviders;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Rendering.HighSpeedRasterizer;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Themes
{
    /// <summary>
    /// Provides drawing capabilities for labels and ticks on an Axis within Ultrachart
    /// </summary>
    public interface IAxisPanel: INotifyPropertyChanged
    {
        /// <summary>
        /// Removes all the labels from an axis.
        /// </summary>
        void ClearLabels();

        /// <summary>
        /// Forces the layout pass on this panel.
        /// </summary>
        void Invalidate();

        /// <summary>
        /// Draws ticks on an axis at the given coordinates.
        /// </summary>
        /// <param name="tickCoords"></param>
        /// <param name="offset"></param>
        void DrawTicks(TickCoordinates tickCoords, float offset);

        /// <summary>
        /// Draws labels on an axis using 
        /// </summary>
        /// <param name="addOnCanvas"></param>
        void AddTickLabels(Action<AxisCanvas> addOnCanvas);
    }

    /// <summary>
    /// A panel providing a Bitmap and Labels Canvas for use on Axis within Ultrachart
    /// </summary>
    public class AxisPanel : Panel, IAxisPanel
    {
        /// <summary>
        /// The draw labels property
        /// </summary>
        public static readonly DependencyProperty DrawLabelsProperty =
            DependencyProperty.Register("DrawLabels", typeof (bool), typeof (AxisPanel), new PropertyMetadata(true));

        /// <summary>
        /// The draw minor ticks property
        /// </summary>
        public static readonly DependencyProperty DrawMinorTicksProperty =
            DependencyProperty.Register("DrawMinorTicks", typeof (bool), typeof (AxisPanel),
                new PropertyMetadata(true));

        /// <summary>
        /// The draw major ticks property
        /// </summary>
        public static readonly DependencyProperty DrawMajorTicksProperty =
            DependencyProperty.Register("DrawMajorTicks", typeof (bool), typeof (AxisPanel),
                new PropertyMetadata(true));

        /// <summary>
        /// The major tick line style property
        /// </summary>
        public static readonly DependencyProperty MajorTickLineStyleProperty =
            DependencyProperty.Register("MajorTickLineStyle", typeof (Style), typeof (AxisPanel),
                new PropertyMetadata(null, OnMajorTickLineStyleDependencyPropertyChanged));

        /// <summary>
        /// The minor tick line style property
        /// </summary>
        public static readonly DependencyProperty MinorTickLineStyleProperty =
            DependencyProperty.Register("MinorTickLineStyle", typeof (Style), typeof (AxisPanel),
                new PropertyMetadata(null, OnMinorTickLineStyleDependencyPropertyChanged));

        /// <summary>
        /// The axis alignment property
        /// </summary>
        public static readonly DependencyProperty AxisAlignmentProperty =
            DependencyProperty.Register("AxisAlignment", typeof (AxisAlignment), typeof (AxisPanel),
                new PropertyMetadata(AxisAlignment.Default, OnAxisAlignmentChanged));

        /// <summary>
        /// The AxisLabelToTickIndent property
        /// </summary>
        public static readonly DependencyProperty AxisLabelToTickIndentProperty =
            DependencyProperty.Register("AxisLabelToTickIndent", typeof (double), typeof (AxisPanel),
                new PropertyMetadata(2d, OnAxisLabelToTickIndentChanged));

        /// <summary>
        /// The IsLabelCullingEnabled DependencyProperty
        /// </summary>
        public static readonly DependencyProperty IsLabelCullingEnabledProperty =
            DependencyProperty.Register("IsLabelCullingEnabled", typeof (bool), typeof (AxisPanel),
                new PropertyMetadata(true));

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        protected Line LineToStyle = new Line();

        protected Grid _labelsContainer;
        protected Image _axisImage;
        protected AxisTitle _axisTitle;

        private WriteableBitmap _renderWriteableBitmap;

        private double _minorTickSize;
        private double _majorTickSize;

        private bool _isInitialized = false;

        /// <summary>
        /// Gets or sets a value indicating whether Label Culling is enabled (when labels overlap) on this AxisPanel instance
        /// </summary>
        public bool IsLabelCullingEnabled
        {
            get { return (bool) GetValue(IsLabelCullingEnabledProperty); }
            set { SetValue(IsLabelCullingEnabledProperty, value); }
        }

        /// <summary>
        /// Gets or sets the axis alignment.
        /// </summary>
        public AxisAlignment AxisAlignment
        {
            get { return (AxisAlignment) GetValue(AxisAlignmentProperty); }
            set { SetValue(AxisAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the major tick line style.
        /// </summary>
        public Style MajorTickLineStyle
        {
            get { return (Style) GetValue(MajorTickLineStyleProperty); }
            set { SetValue(MajorTickLineStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the minor tick line style.
        /// </summary>
        public Style MinorTickLineStyle
        {
            get { return (Style) GetValue(MinorTickLineStyleProperty); }
            set { SetValue(MinorTickLineStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this panel draws Major ticks
        /// </summary>
        public bool DrawMajorTicks
        {
            get { return (bool) GetValue(DrawMajorTicksProperty); }
            set { SetValue(DrawMajorTicksProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this panel draws Minor ticks
        /// </summary>        
        public bool DrawMinorTicks
        {
            get { return (bool) GetValue(DrawMinorTicksProperty); }
            set { SetValue(DrawMinorTicksProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this panel draws labels
        /// </summary>        
        public bool DrawLabels
        {
            get { return (bool) GetValue(DrawLabelsProperty); }
            set { SetValue(DrawLabelsProperty, value); }
        }

        public double AxisLabelToTickIndent
        {
            get { return (double) GetValue(AxisLabelToTickIndentProperty); }
            set { SetValue(AxisLabelToTickIndentProperty, value); }
        }

        /// <summary>
        /// Gets a value indicating whether this pabel is horizontal axis.
        /// </summary>
        public bool IsHorizontalAxis
        {
            get
            {
                var axisAlignment = AxisAlignment;
                return axisAlignment == AxisAlignment.Bottom || axisAlignment == AxisAlignment.Top;
            }
        }

        /// <summary>
        /// Gets the label to tick indent.
        /// </summary>
        public Thickness LabelToTickIndent
        {
            get
            {
                return new Thickness(
                    AxisAlignment == AxisAlignment.Right ? AxisLabelToTickIndent : 0d,
                    AxisAlignment == AxisAlignment.Bottom ? AxisLabelToTickIndent : 0d,
                    AxisAlignment == AxisAlignment.Left ? AxisLabelToTickIndent : 0d,
                    AxisAlignment == AxisAlignment.Top ? AxisLabelToTickIndent : 0d);
            }
        }

        /// <summary>
        /// Gets the size of a major tick.
        /// </summary>
        public double MajorTickSize
        {
            get { return DrawMajorTicks ? _majorTickSize : 0d; }
            private set
            {
                if (value.Equals(_majorTickSize)) return;

                _majorTickSize = value;
                OnPropertyChanged("MajorTickSize");
            }
        }

        /// <summary>
        /// Gets the size of a minor tick.
        /// </summary>
        public double MinorTickSize
        {
            get { return DrawMinorTicks ? _minorTickSize : 0d; }
            private set
            {
                if (value.Equals(_minorTickSize)) return;

                _minorTickSize = value;
                OnPropertyChanged("MinorTickSize");
            }
        }

        /// <summary>
        /// Used internally. Specifies a method which is used to fill the panel with tick labels.
        /// </summary>
        public Action<AxisCanvas> AddLabels { get; set; }

        /// <summary>
        /// Performs the measure pass on AxisPanel.
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (!_isInitialized)
            {
                foreach (UIElement child in Children)
                {
                    Initialize(child);
                }
            }

            AddTickLabels(AddLabels);
            _labelsContainer.Measure(availableSize);

            var tickSize = Math.Max(MinorTickSize, MajorTickSize);

            Size result;
            switch (AxisAlignment)
            {
                case AxisAlignment.Right:
                case AxisAlignment.Left:
                    {
                        _axisImage.Measure(new Size(tickSize, availableSize.Height));

                        var width = tickSize + _labelsContainer.DesiredSize.Width;
                        _axisTitle.Measure(new Size(availableSize.Width - width, availableSize.Height));

                        result = new Size(width + _axisTitle.DesiredSize.Width, _labelsContainer.DesiredSize.Height);
                        break;
                    }
                case AxisAlignment.Top:
                case AxisAlignment.Bottom:
                    {
                        _axisImage.Measure(new Size(availableSize.Width, tickSize));

                        var height = tickSize + _labelsContainer.DesiredSize.Height;
                        _axisTitle.Measure(new Size(availableSize.Width, availableSize.Height - height));

                        result = new Size(_labelsContainer.DesiredSize.Width, height + _axisTitle.DesiredSize.Height);
                        break;
                    }
                default:
                    result = new Size(_labelsContainer.DesiredSize.Width, _labelsContainer.DesiredSize.Height);
                    break;
            }

            return result;
        }

        private void Initialize(UIElement child)
        {
            if (child is Image)
            {
                _axisImage = (Image)child;
            }

            if (child is AxisTitle)
            {
                _axisTitle = (AxisTitle)child;
            }

            if (child is Grid)
            {
                _labelsContainer = (Grid)child;
            }

            _isInitialized = _axisImage != null && _axisTitle != null && _labelsContainer != null;
        }

        /// <summary>
        /// Performs the arrange pass on AxisPanel.
        /// </summary>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var tickSize = Math.Max(MinorTickSize, MajorTickSize);

            switch (AxisAlignment)
            {
                case AxisAlignment.Top:
                    {
                        var y = finalSize.Height - tickSize;
                        _axisImage.Arrange(new Rect(0, y, finalSize.Width, tickSize));

                        y -= _labelsContainer.DesiredSize.Height;
                        _labelsContainer.Arrange(new Rect(0, y, finalSize.Width, _labelsContainer.DesiredSize.Height));

                        y -= _axisTitle.DesiredSize.Height;
                        _axisTitle.Arrange(new Rect(0, y, finalSize.Width, _axisTitle.DesiredSize.Height));

                        break;
                    }
                case AxisAlignment.Left:
                    {
                        var x = finalSize.Width - tickSize;
                        _axisImage.Arrange(new Rect(x, 0, tickSize, finalSize.Height));

                        x -= _labelsContainer.DesiredSize.Width;
                        _labelsContainer.Arrange(new Rect(x, 0, _labelsContainer.DesiredSize.Width, finalSize.Height));

                        x -= _axisTitle.DesiredSize.Width;
                        _axisTitle.Arrange(new Rect(x, 0, _axisTitle.DesiredSize.Width, finalSize.Height));

                        break;
                    }
                case AxisAlignment.Right:
                    {
                        _axisImage.Arrange(new Rect(0, 0, tickSize, finalSize.Height));

                        _labelsContainer.Arrange(new Rect(tickSize, 0, _labelsContainer.DesiredSize.Width, finalSize.Height));

                        var x = tickSize + _labelsContainer.DesiredSize.Width;
                        _axisTitle.Arrange(new Rect(x, 0, _axisTitle.DesiredSize.Width, finalSize.Height));

                        break;
                    }
                case AxisAlignment.Bottom:
                    {
                        _axisImage.Arrange(new Rect(0, 0, finalSize.Width, tickSize));

                        _labelsContainer.Arrange(new Rect(0, tickSize, finalSize.Width, _labelsContainer.DesiredSize.Height));

                        var y = tickSize + _labelsContainer.DesiredSize.Height;
                        _axisTitle.Arrange(new Rect(0, y, finalSize.Width, _axisTitle.DesiredSize.Height));

                        break;
                    }
            }

            return finalSize;
        }

        /// <summary>
        /// Draws the tick labels.
        /// </summary>
        public void AddTickLabels(Action<AxisCanvas> addOnCanvas)
        {
            if (_labelsContainer != null && DrawLabels)
            {
                var current =
                    (AxisCanvas)
                        (_labelsContainer.Children[0].IsVisible()
                            ? _labelsContainer.Children[1]
                            : _labelsContainer.Children[0]);
                var stale =
                    (AxisCanvas)
                        (_labelsContainer.Children[0].IsVisible()
                            ? _labelsContainer.Children[0]
                            : _labelsContainer.Children[1]);

                current.Visibility = Visibility.Collapsed;

                current.SizeHeightToContent = IsHorizontalAxis;
                current.SizeWidthToContent = !IsHorizontalAxis;

                addOnCanvas(current);

                current.Visibility = Visibility.Visible;
                stale.Visibility = Visibility.Collapsed;
            }
        }

        public void Invalidate()
        {
            InvalidateMeasure();
            InvalidateArrange();
        }

        /// <summary>
        /// Draws ticks on axis
        /// </summary>
        /// <param name="tickCoords"></param>
        /// <param name="offset"></param>
        public virtual void DrawTicks(TickCoordinates tickCoords, float offset)
        {
            var size = GetRenderContextSize();
            var width = (int)size.Width;
            var height = (int)size.Height;

            if (_renderWriteableBitmap == null ||
                _renderWriteableBitmap.PixelWidth != width ||
                _renderWriteableBitmap.PixelHeight != height)
            {
                _renderWriteableBitmap = BitmapFactory.New(width, height);
            }

            if (_renderWriteableBitmap == null || _axisImage == null) return;

            using (var renderContext = _renderWriteableBitmap.GetRenderContext(_axisImage, null))
            {
                renderContext.Clear();

                if (DrawMinorTicks && tickCoords.MinorTickCoordinates != null)
                    DrawTicks(renderContext, MinorTickLineStyle, MinorTickSize, tickCoords.MinorTickCoordinates, offset);

                if (DrawMajorTicks && tickCoords.MajorTickCoordinates != null)
                    DrawTicks(renderContext, MajorTickLineStyle, MajorTickSize, tickCoords.MajorTickCoordinates, offset);
            }
        }

        /// <summary>
        /// Gets size of image for ticks drawing
        /// </summary>
        /// <returns></returns>
        protected virtual Size GetRenderContextSize()
        {
            var w1 = (int) (IsHorizontalAxis ? _labelsContainer.ActualWidth : MajorTickSize);
            var h1 = (int)(IsHorizontalAxis ? MajorTickSize : _labelsContainer.ActualHeight);

            return new Size(w1, h1);
        }

        /// <summary>
        /// Draws ticks on axis bitmap
        /// </summary>
        /// <param name="renderContext"></param>
        /// <param name="tickStyle"></param>
        /// <param name="tickSize"></param>
        /// <param name="tickCoords"></param>
        /// <param name="offset"></param>
        protected virtual void DrawTicks(IRenderContext2D renderContext, Style tickStyle, double tickSize,
            float[] tickCoords, float offset)
        {
            LineToStyle.Style = tickStyle;
            ThemeManager.SetTheme(LineToStyle, ThemeManager.GetTheme(this));
            
            using (var linePen = renderContext.GetStyledPen(LineToStyle))
            {
                foreach (var coord in tickCoords)
                {
                    DrawTick(renderContext, linePen, coord, offset, tickSize);
                }
            }
        }

        /// <summary>
        /// Draws a single tick on the axis, using the specified pen (TargetType <see cref="IPen2D" />), <see cref="XyDirection" /> and integer coordinate.
        /// </summary>
        /// <param name="renderContext">The canvas to draw on.</param>
        /// <param name="tickPen">The pen (TargetType <see cref="IPen2D" />) to apply to the tick line</param>
        /// <param name="coord">The integer coordinate to draw at. If direction is <see cref="XyDirection.XDirection" />, the coodinate is an X-coordinate, else it is a Y-coordinate</param>
        /// <param name="offset"></param>
        /// <param name="tickSize">The size of the tick</param>
        /// <remarks>
        /// If direction is <see cref="XyDirection.XDirection" />, the coodinate is an X-coordinate, else it is a Y-coordinate
        /// </remarks>
        private void DrawTick(IRenderContext2D renderContext, IPen2D tickPen, float coord, float offset, double tickSize)
        {
            Point pt1, pt2;

            var viewportSize = renderContext.ViewportSize;
            var dimention = (float) (IsHorizontalAxis ? viewportSize.Height : viewportSize.Width);

            var coordToDraw = coord - offset;
            // On horizontal-oriented axis, ticks are drawn like this
            // y1 = 0, y2 = {Set by style}
            // x1 = yCoord, x2 = yCoord
            float x1 = coordToDraw, x2 = coordToDraw, y1 = 0, y2 = (float) tickSize;

            // Flip all coords
            if (AxisAlignment == AxisAlignment.Top || AxisAlignment == AxisAlignment.Left)
            {
                y2 = dimention - y2;
                y1 = dimention;
            }

            if (IsHorizontalAxis)
            {
                pt1 = new Point(x1, y1);
                pt2 = new Point(x2, y2);
            }
            else
            {
                pt1 = new Point(y1, x1);
                pt2 = new Point(y2, x2);
            }

            renderContext.DrawLine(tickPen, pt1, pt2);
        }

        /// <summary>
        /// Clears the <see cref="AxisPanel"/>
        /// </summary>
        public void ClearLabels()
        {
            _labelsContainer.Children.OfType<TickLabelAxisCanvas>().ForEachDo(panel => panel.Children.Clear());
        }

        /// <summary>
        /// <see cref="INotifyPropertyChanged"/> implementation
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private static void OnAxisAlignmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axisPanel = d as AxisPanel;
            if (axisPanel != null)
            {
                axisPanel.OnPropertyChanged("LabelToTickIndent");
                axisPanel.OnPropertyChanged("IsHorizontalAxis");

                axisPanel.MajorTickSize = axisPanel.MeasureTickSize(axisPanel.MajorTickLineStyle);
                axisPanel.MinorTickSize = axisPanel.MeasureTickSize(axisPanel.MinorTickLineStyle);

                axisPanel.Invalidate();
            }
        }

        private static void OnMajorTickLineStyleDependencyPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var axisPanel = d as AxisPanel;
            if (axisPanel != null)
            {
                var style = (Style) e.NewValue;
                axisPanel.MajorTickSize = axisPanel.MeasureTickSize(style);
            }
        }

        private static void OnMinorTickLineStyleDependencyPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var axisPanel = d as AxisPanel;
            if (axisPanel != null)
            {
                var style = (Style) e.NewValue;
                axisPanel.MinorTickSize = axisPanel.MeasureTickSize(style);
            }
        }

        /// <summary>
        /// Measures the size required to draw tick marks on the axis
        /// </summary>
        /// <returns></returns>
        private double MeasureTickSize(Style lineStyle)
        {
            var line = new Line {Style = lineStyle};

            return IsHorizontalAxis ? line.Y2 + 1 : line.X2 + 1;
        }

        private static void OnAxisLabelToTickIndentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axisPanel = d as AxisPanel;
            if (axisPanel != null)
            {
                axisPanel.OnPropertyChanged("LabelToTickIndent");
            }
        }

        internal Image AxisImage
        {
            get { return _axisImage; }
        }

        internal Grid LabelContainer
        {
            get { return _labelsContainer; }
        }
    }
}
