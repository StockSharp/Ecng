// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// TickLabelAxisCanvas.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Numerics.GenericMath;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Themes
{
    /// <summary>
    /// A canvas which overlays an axis and is used to place Tick Labels
    /// </summary>
    public class TickLabelAxisCanvas : AxisCanvas
    {
        private readonly DoubleMath _doubleMath = new DoubleMath();
        readonly List<DefaultTickLabel> _placedLabels = new List<DefaultTickLabel>();

        /// <summary>Defines the IsLabelCullingEnabled DependendencyProperty</summary>
        public static readonly DependencyProperty IsLabelCullingEnabledProperty = DependencyProperty.Register("IsLabelCullingEnabled", typeof (bool), typeof (TickLabelAxisCanvas), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether Label Culling is enabled on this Axis Canvas
        /// </summary>
        public bool IsLabelCullingEnabled
        {
            get { return (bool) GetValue(IsLabelCullingEnabledProperty); }
            set { SetValue(IsLabelCullingEnabledProperty, value); }
        }

        /// <summary>
        /// Measures all the children and returns their size.
        /// </summary>
        /// <param name="constraint">The available size.</param>
        /// <returns>The desired size.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            return SizeWidthToContent ? MeasureWidth() : MeasureHeight();
        }

        private Size MeasureWidth()
        {
            var availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

            double maxWidth = 0;
            foreach (UIElement child in Children)
            {
                child.Measure(availableSize);

                var width = child.DesiredSize.Width;
                width = _doubleMath.Max(GetLeft(child) + width, GetCenterLeft(child) + width/2);

                if (_doubleMath.IsNaN(width))
                {
                    width = GetRight(child) + child.DesiredSize.Width;
                }

                maxWidth = _doubleMath.Max(width, maxWidth);
            }

            return new Size(maxWidth, 0);
        }

        private Size MeasureHeight()
        {
            var availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

            double maxHeight = 0;
            foreach (UIElement child in Children)
            {
                child.Measure(availableSize);

                var height = child.DesiredSize.Height;
                height = _doubleMath.Max(GetTop(child) + height, GetCenterTop(child) + height / 2);

                if (_doubleMath.IsNaN(height))
                {
                    height = GetBottom(child) + child.DesiredSize.Height;
                }

                maxHeight = _doubleMath.Max(height, maxHeight);
            }

            return new Size(0, maxHeight);
        }

        /// <summary>
        /// Arranges all children in the correct position.
        /// </summary>
        /// <param name="arrangeSize">The size to arrange element's within.
        /// </param>
        /// <returns>The size that element's were arranged in.</returns>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            _placedLabels.Clear();

            var intersects = false;
            var isCullingEnabled = IsLabelCullingEnabled;

            // Performs tick labels culling
            foreach (
                var labelsWithSamePriority in
                    Children.OfType<DefaultTickLabel>().GroupBy(x => x.CullingPriority).OrderByDescending(x => x.Key))
            {
                if (!intersects)
                {
                    // Check if there is an intersection between labels.
                    // If there is, need to make all labels with current priority and lower invisible
                    foreach (var label in labelsWithSamePriority)
                    {
                        var arrangedRect = GetArrangedRect(arrangeSize, label);
                        label.ArrangedRect = arrangedRect;

                        if (isCullingEnabled)
                        {                                                                                
                            intersects = _placedLabels.Any(lbl => lbl.ArrangedRect.IntersectsWith(arrangedRect));

                            if (intersects) break;
                        }

                        ShowLabel(label);

                        _placedLabels.Add(label);
                        label.Arrange(arrangedRect);
                    }
                }

                // Once intersection occured, hide all the labels from current group, also from groups with lower priorities
                if (intersects)
                {
                    // Hide labels from current group, 
                    // but leave one if it is the only label shown
                    var skip = _placedLabels.Count == 1 &&
                               _placedLabels[0].CullingPriority == labelsWithSamePriority.Key;

                    labelsWithSamePriority.Skip(skip ? 1 : 0).ForEachDo(lbl =>
                    {
                        HideLabel(lbl);
                        _placedLabels.Remove(lbl);
                    });
                }
            }

            return arrangeSize;
        }

        private static void ShowLabel(DefaultTickLabel label)
        {
            label.Opacity = 1d;
        }

        private static void HideLabel(DefaultTickLabel label)
        {
            label.Opacity = 0d;
        }
    }
}