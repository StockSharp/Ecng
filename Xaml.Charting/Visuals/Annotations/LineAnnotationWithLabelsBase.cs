// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
//
// LineAnnotationWithLabelsBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart.
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Shapes;
using Ecng.Xaml.Charting.Common.Databinding;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Themes;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Visuals.Annotations
{
    /// <summary>
    /// Provides a base class for HorizontalLineAnnotation, VerticalLineAnnotation
    /// </summary>
    public abstract class LineAnnotationWithLabelsBase: LineAnnotation
    {

        /// <summary>
        /// Defines the ShowLabel Property
        /// </summary>
        public static readonly DependencyProperty ShowLabelProperty = DependencyProperty.Register("ShowLabel", typeof(bool), typeof(LineAnnotationWithLabelsBase), new PropertyMetadata(false, OnShowLabelChanged));

        /// <summary>
        /// Defines the DefaultLabelValue Property
        /// </summary>
        protected internal static readonly DependencyProperty DefaultLabelValueProperty = DependencyProperty.Register("DefaultLabelValue", typeof(IComparable), typeof(LineAnnotationWithLabelsBase), new PropertyMetadata(null));

        /// <summary>
        /// Defines the DefaultTextFormatting Property
        /// </summary>
        protected static readonly DependencyProperty DefaultTextFormattingProperty = DependencyProperty.Register("DefaultTextFormatting", typeof(string), typeof(LineAnnotationWithLabelsBase), new PropertyMetadata(null));

        /// <summary>
        /// Defines the LabelPlacement Property
        /// </summary>
        public static readonly DependencyProperty LabelPlacementProperty = DependencyProperty.Register("LabelPlacement", typeof(LabelPlacement), typeof(LineAnnotationWithLabelsBase), new PropertyMetadata(LabelPlacement.Auto));

        /// <summary>
        /// Defines the LabelValue Property
        /// </summary>
        public static readonly DependencyProperty LabelValueProperty = DependencyProperty.Register("LabelValue", typeof(IComparable), typeof(LineAnnotationWithLabelsBase), new PropertyMetadata(default(IComparable)));

        /// <summary>
        /// Defines the LabelTextFormatting Property
        /// </summary>
        public static readonly DependencyProperty LabelTextFormattingProperty = DependencyProperty.Register("LabelTextFormatting", typeof(string), typeof(LineAnnotationWithLabelsBase), new PropertyMetadata(String.Empty, OnLabelTextFormattingChanged));

        /// <summary>
        /// Defines the FormattedLabel Property
        /// </summary>
        public static readonly DependencyProperty FormattedLabelProperty = DependencyProperty.Register("FormattedLabel", typeof(string), typeof(LineAnnotationWithLabelsBase), new PropertyMetadata(String.Empty));

        /// <summary>
        /// Defines the AnnotationLabels Property
        /// </summary>
        public static readonly DependencyProperty AnnotationLabelsProperty = DependencyProperty.Register("AnnotationLabels", typeof(ObservableCollection<AnnotationLabel>), typeof(LineAnnotationWithLabelsBase), new PropertyMetadata(OnAnnotationLabelsChanged));

        private CategoryIndexToDataValueConverter _xyValueConverter;

        /// <summary>
        /// Initializes a new instance of the <see cref="LineAnnotationWithLabelsBase" /> class.
        /// </summary>
        protected LineAnnotationWithLabelsBase()
        {
            AnnotationLabels = new ObservableCollection<AnnotationLabel>();


            var formattedValueBinding = new Binding("LabelValue")
            {
                Source = this,
                Mode = BindingMode.OneWay,
                Converter = new GetAxisFormattedValueConverter(this)
            };
            SetBinding(FormattedLabelProperty, formattedValueBinding);
        }

        /// <summary>
        /// Gets or sets value which labels will be bound to
        /// </summary>
        protected IComparable DefaultLabelValue
        {
            get { return (IComparable)GetValue(DefaultLabelValueProperty); }
        }

        /// <summary>
        /// Gets the default text formatting value
        /// </summary>
        protected string DefaultTextFormatting
        {
            get { return (string)GetValue(DefaultTextFormattingProperty); }
        }

        /// <summary>
        /// Gets the formatted label value
        /// </summary>
        protected string FormattedLabel
        {
            get { return (string)GetValue(FormattedLabelProperty); }
        }

        /// <summary>
        /// Gets or sets a collection of annotation labels
        /// </summary>
        public ObservableCollection<AnnotationLabel> AnnotationLabels
        {
            get { return (ObservableCollection<AnnotationLabel>)GetValue(AnnotationLabelsProperty); }
            set { SetValue(AnnotationLabelsProperty, value); }
        }

        /// <summary>
        /// Gets or sets value, indicating whether show the default label or not
        /// </summary>
        public bool ShowLabel
        {
            get { return (bool)GetValue(ShowLabelProperty); }
            set { SetValue(ShowLabelProperty, value); }
        }

        /// <summary>
        /// Gets or sets placement for the default label
        /// </summary>
        public LabelPlacement LabelPlacement
        {
            get { return (LabelPlacement)GetValue(LabelPlacementProperty); }
            set { SetValue(LabelPlacementProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value for the default label
        /// </summary>
        [TypeConverter(typeof(StringToLabelValueConverter))]
        public IComparable LabelValue
        {
            get { return (IComparable)GetValue(LabelValueProperty); }
            set { SetValue(LabelValueProperty, value); }
        }

        /// <summary>
        /// Gets or sets formatting string which is applied to all annotation labels
        /// </summary>
        public string LabelTextFormatting
        {
            get { return (string)GetValue(LabelTextFormattingProperty); }
            set { SetValue(LabelTextFormattingProperty, value); }
        }

        /// <summary>
        /// Adds a collection of <see cref="AnnotationLabel"/> instances to the <see cref="LineAnnotationWithLabelsBase"/>
        /// </summary>
        /// <param name="labels">The collection of labels to add</param>
        protected void AttachLabels(IEnumerable<AnnotationLabel> labels)
        {
            var hasAxisLabels = false;
            foreach (var label in labels)
            {
                Attach(label);

                hasAxisLabels = label.IsAxisLabel;
            }

            if (hasAxisLabels)
            {
                Refresh();
            }
        }

        /// <summary>
        /// Removes a collection of <see cref="AnnotationLabel"/> instances to the <see cref="LineAnnotationWithLabelsBase"/>
        /// </summary>
        /// <param name="labels">The collection of labels to remove</param>
        protected void DetachLabels(IEnumerable<AnnotationLabel> labels)
        {
            foreach (var label in labels)
            {
                Detach(label);
            }
        }

        /// <summary>
        /// Called internally to attach an <see cref="AnnotationLabel"/> to the current instance
        /// </summary>
        /// <param name="label">The AnnotationLabel to attach</param>
        protected virtual void Attach(AnnotationLabel label)
        {
            if (!IsHidden)
            {
                var placement = GetLabelPlacement(label);

                ApplyPlacement(label, placement);

                label.DataContext = this;
                label.ParentAnnotation = this;

                var axis = GetUsedAxis();

                if (label.IsAxisLabel)
                {
                    if (axis != null)
                    {
                        axis.ModifierAxisCanvas.SafeAddChild(label);
                    }
                }
                else
                {
                    (AnnotationRoot as Grid).SafeAddChild(label);
                }
            }
        }

        /// <summary>
        /// Returns axis, which current annotation shows data value for
        /// </summary>
        /// <returns></returns>
        public abstract IAxis GetUsedAxis();

        /// <summary>
        /// Virtual method to override if you wish to be notified that the parent <see cref="UltrachartSurface.XAxes" /> has changed
        /// </summary>
        protected override void OnXAxesCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            InvalidateAnnotation();
        }

        private void InvalidateAnnotation()
        {
            InvalidateAxisLabels();

            BindDefaultLabelValue();
        }

        private void InvalidateAxisLabels()
        {
            using (SuspendUpdates())
            {
                AnnotationLabels.Where(label => label.IsAxisLabel).ForEachDo(label =>
                {
                    Detach(label);
                    InvalidateLabel(label);
                });
            }
        }

        /// <summary>
        /// Invalidates annotation label
        /// </summary>
        /// <param name="annotationLabel">Label to invalidate</param>
        public void InvalidateLabel(AnnotationLabel annotationLabel)
        {
            if(annotationLabel != null)
                Attach(annotationLabel);

            MeasureRefresh();
        }

        private void BindDefaultLabelValue()
        {
            var axis = GetUsedAxis();

            _xyValueConverter = _xyValueConverter ?? (_xyValueConverter = new CategoryIndexToDataValueConverter(this));

            var valueBinding = new Binding(axis != null && axis.IsXAxis ? "X1" : "Y1") { Source = this, Converter = _xyValueConverter };
            SetBinding(DefaultLabelValueProperty, valueBinding);

            if (axis != null)
            {
                var formatBinding = new Binding("CursorTextFormatting") { Source = axis };
                SetBinding(DefaultTextFormattingProperty, formatBinding);
            }
        }

        /// <summary>
        /// Virtual method to override if you wish to be notified that the parent <see cref="UltrachartSurface.YAxes" /> has changed
        /// </summary>
        protected override void OnYAxesCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            InvalidateAnnotation();
        }

        /// <summary>
        /// Virtual method to override if you wish to be notified that the <see cref="AnnotationBase.YAxisId" /> has changed
        /// </summary>
        protected override void OnYAxisIdChanged()
        {
            InvalidateAnnotation();
        }

        /// <summary>
        /// Virtual method to override if you wish to be notified that the <see cref="AnnotationBase.XAxisId"/> has changed
        /// </summary>
        protected override void OnXAxisIdChanged()
        {
            InvalidateAnnotation();
        }

        /// <summary>
        /// Virtual method to override if you wish to be notified that the <see cref="IAxis.AxisAlignment" /> has changed
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="oldAlignment"></param>
        protected override void OnAxisAlignmentChanged(IAxis axis, AxisAlignment oldAlignment)
        {
            base.OnAxisAlignmentChanged(axis, oldAlignment);

            InvalidateAnnotation();
        }

        /// <summary>
        /// Hides current instance of <see cref="LineAnnotationWithLabelsBase"/>
        /// </summary>
        protected override void MakeInvisible()
        {
            base.MakeInvisible();

            DetachLabels(AnnotationLabels.Where(label => label.IsAxisLabel));
        }

        /// <summary>
        /// Hides current instance of <see cref="LineAnnotationWithLabelsBase"/>
        /// </summary>
        protected override void MakeVisible(AnnotationCoordinates coordinates)
        {
            base.MakeVisible(coordinates);

            var axis = GetUsedAxis();
            if (axis != null && axis.ModifierAxisCanvas != null)
            {
                //attach axis labels which are detached and all labels from AnnoationRoot which weren't attached before
                AttachLabels(AnnotationLabels.Where(label => label.Parent == null));
            }
        }

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate" />.
        /// </summary>
        public override void OnApplyTemplate()
        {
            DetachLabels(AnnotationLabels);

            AnnotationRoot = GetAndAssertTemplateChild<Grid>("PART_LineAnnotationRoot");
            var ghostLine = GetAndAssertTemplateChild<Line>("PART_GhostLine");

            AttachLabels(AnnotationLabels);

            Refresh();
        }

        /// <summary>
        /// Called when the Annotation is attached to parent surface
        /// </summary>
        public override void OnAttached()
        {
            base.OnAttached();

            BindDefaultLabelValue();
        }

        protected override void OnAnnotationLoaded(object sender, RoutedEventArgs e)
        {
            base.OnAnnotationLoaded(sender, e);

            var binding = GetBindingExpression(DefaultLabelValueProperty);
            if (binding != null)
            {
#if !SILVERLIGHT
                binding.UpdateTarget();
#else
                // workaround for Sl, causes all targets to be updated
                SetBinding(DefaultLabelValueProperty, binding.ParentBinding);
#endif
            }
        }

        /// <summary>
        /// Adds new label to <see cref="AnnotationLabels"/>
        /// </summary>
        /// <returns>Label which has been created</returns>
        public AnnotationLabel AddLabel()
        {
            var label = new AnnotationLabel();

            var labelPlacementBinding = new Binding("LabelPlacement"){Source = this, Mode=BindingMode.OneWay};
            label.SetBinding(AnnotationLabel.LabelPlacementProperty, labelPlacementBinding);

            var binding = new Binding("ContextMenu") { Source = this, Mode=BindingMode.OneWay };
            label.SetBinding(ContextMenuProperty, binding);

            AnnotationLabels.Add(label);

            return label;
        }

        /// <summary>
        /// Try to place all annotation labels on ModifierAxisCanvas of appropriate axis at <paramref name="offset"/> position.
        /// </summary>
        protected void TryPlaceAxisLabels(Point offset)
        {
            var axis = GetUsedAxis();
            if (axis != null && axis.ModifierAxisCanvas != null)
            {
                AnnotationLabels.Where(label => label.IsAxisLabel && label.ParentAnnotation != null)
                    .ForEachDo(label => PlaceAxisLabel(axis, label, offset));
            }
        }

        /// <summary>
        /// Place <paramref name="axisLabel"/> on ModifierAxisCanvas of appropriate axis at <paramref name="offset"/> position.
        /// </summary>
        protected virtual void PlaceAxisLabel(IAxis axis, AnnotationLabel axisLabel, Point offset)
        {
            if (axisLabel.Parent == null)
            {
                Attach(axisLabel);
                Refresh();
            }

            axis.SetHorizontalOffset(axisLabel, offset);
            axis.SetVerticalOffset(axisLabel, offset);
        }

        /// <summary>
        /// Detaches the <see cref="AnnotationLabel"/> from the current <see cref="LineAnnotationWithLabelsBase"/>
        /// </summary>
        /// <param name="label">The label to detach</param>
        protected virtual void Detach(AnnotationLabel label)
        {
            label.ParentAnnotation = null;

            var rootGrid = (AnnotationRoot as Grid);

            var axis = GetUsedAxis();
            var axisCanvas = axis != null ? axis.ModifierAxisCanvas : null;

            // Handles case when label is attached to old axis,
            // for example, AxesCollection was changed
            axisCanvas = axisCanvas ?? label.Parent as ModifierAxisCanvas;

            rootGrid.SafeRemoveChild(label);
            axisCanvas.SafeRemoveChild(label);
        }

        /// <summary>
        /// Positions the <see cref="AnnotationLabel"/> using the value of the <see cref="LabelPlacement"/> enum
        /// </summary>
        /// <param name="label">The label to place</param>
        /// <param name="placement">Placement arguments</param>
        protected virtual void ApplyPlacement(AnnotationLabel label, LabelPlacement placement)
        {
            var isTop = placement.IsTop();
            var isBottom = placement.IsBottom();
            var isLeft = placement.IsLeft();
            var isRight = placement.IsRight();

            if (isTop || isBottom)
            {
                label.SetValue(Grid.ColumnProperty, 1);
                label.SetValue(Grid.RowProperty, isTop ? 0 : 2);

                label.HorizontalAlignment = HorizontalAlignment.Center;
                label.VerticalAlignment = isTop ? VerticalAlignment.Bottom : VerticalAlignment.Top;

                if (isRight)
                {
                    label.HorizontalAlignment = HorizontalAlignment.Right;
                }

                if (isLeft)
                {
                    label.HorizontalAlignment = HorizontalAlignment.Left;
                }
            }
            else
            {
                label.SetValue(Grid.ColumnProperty, isRight ? 2 : isLeft ? 0 : 1);

                label.SetValue(Grid.RowProperty, 1);

                label.HorizontalAlignment = HorizontalAlignment.Center;
                label.VerticalAlignment = VerticalAlignment.Center;
            }
        }

        internal virtual LabelPlacement ResolveAutoPlacement()
        {
            return LabelPlacement.Top;
        }

        internal LabelPlacement GetLabelPlacement(AnnotationLabel label)
        {
            return label.LabelPlacement != LabelPlacement.Auto ? label.LabelPlacement : ResolveAutoPlacement();
        }

        /// <summary>
        /// Gets the <see cref="Cursor"/> to use for the annotation when selected
        /// </summary>
        /// <returns></returns>
        protected override Cursor GetSelectedCursor()
        {
            return Cursors.SizeNS;
        }

        /// <summary>
        /// Returns true if the Point is within the bounds of the current <see cref="IHitTestable" /> element
        /// </summary>
        /// <param name="point">The point to test</param>
        /// <returns>
        /// true if the Point is within the bounds
        /// </returns>
        public override bool IsPointWithinBounds(Point point)
        {
            var rootGrid = AnnotationRoot as Grid;

            point = ParentSurface.ModifierSurface.TranslatePoint(point, this);

            //TODO: Hit test line and all labels here
            //var result = AnnotationLabels.Any(label =>label.IsPointWithinBounds(TranslatePoint(point,(IHitTestable) label)));

            bool result = rootGrid.IsPointWithinBounds(point);

            return result;
        }

        private static void OnAnnotationLabelsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var newCollection = e.NewValue as ObservableCollection<AnnotationLabel>;
            var oldCollection = e.OldValue as ObservableCollection<AnnotationLabel>;

            var annotation = (LineAnnotationWithLabelsBase)d;

            if (newCollection != null)
            {
                newCollection.CollectionChanged += annotation.OnAnnotationLabelsCollectionChanged;
            }

            if (oldCollection != null)
            {
                oldCollection.CollectionChanged -= annotation.OnAnnotationLabelsCollectionChanged;
            }

            annotation.OnAnnotationLabelsChanged(newCollection, oldCollection);

            annotation.Refresh();
        }

        private void OnAnnotationLabelsChanged(IList newItems, IList oldItems)
        {
            if (newItems != null) AttachLabels(newItems.OfType<AnnotationLabel>());

            if (oldItems != null) DetachLabels(oldItems.OfType<AnnotationLabel>());
        }

        private void OnAnnotationLabelsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnAnnotationLabelsChanged(e.NewItems, e.OldItems);
        }

        private static void OnLabelTextFormattingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var annotation = d as LineAnnotationWithLabelsBase;

            if (annotation != null)
            {
                var binding = annotation.GetBindingExpression(FormattedLabelProperty);
                if (binding != null)
                {
#if !SILVERLIGHT
                    binding.UpdateTarget();
#else
                    //workaround for Sl, causes all targets to be updated
                    binding = annotation.GetBindingExpression(DefaultLabelValueProperty);

                    annotation.SetValue(DefaultLabelValueProperty, null);
                    if (binding != null)
                    {
                        annotation.SetBinding(DefaultLabelValueProperty, binding.ParentBinding);
                    }
#endif
                }
            }
        }

        private static void OnShowLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var annotation = d as LineAnnotationWithLabelsBase;

            if (annotation != null)
            {
                AnnotationLabel label;
                if (annotation.ShowLabel && annotation.AnnotationLabels.Count == 0)
                {
                    label = annotation.AddLabel();
                    annotation.InvalidateLabel(label);
                }
                else if (!annotation.ShowLabel && annotation.AnnotationLabels.Count == 1)
                {
                    label = annotation.AnnotationLabels[0];
                    annotation.AnnotationLabels.Remove(label);
                    annotation.InvalidateLabel(null);
                }
            }
        }
    }

}
