// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// SeriesInfoTemplateSelector.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting
{
    /// <summary>
    ///     Provides class for choosing proper DataTemplate according to a <see cref="Type" /> of <see cref="SeriesInfo" />
    /// </summary>
    public class SeriesInfoTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// Defines the HeatmapSeriesTemplate DependencyProperty
        /// </summary>
        public static readonly DependencyProperty HeatmapSeriesTemplateProperty =DependencyProperty.Register("HeatmapSeriesTemplate", typeof (DataTemplate), typeof (SeriesInfoTemplateSelector), new PropertyMetadata(OnDefautlTemplateDependencyPropertyChanged));
        /// <summary>
        /// Defines the BandSeries1Template DependencyProperty
        /// </summary>
        public static readonly DependencyProperty BandSeries1TemplateProperty = DependencyProperty.Register("BandSeries1Template", typeof(DataTemplate), typeof(SeriesInfoTemplateSelector), new PropertyMetadata(OnDefautlTemplateDependencyPropertyChanged));
        /// <summary>
        /// Defines the BandSeries2Template DependencyProperty
        /// </summary>
        public static readonly DependencyProperty BandSeries2TemplateProperty = DependencyProperty.Register("BandSeries2Template", typeof(DataTemplate), typeof(SeriesInfoTemplateSelector), new PropertyMetadata(OnDefautlTemplateDependencyPropertyChanged));
        /// <summary>
        /// Defines the BoxPlotSeriesTemplate DependencyProperty
        /// </summary>
        public static readonly DependencyProperty BoxPlotSeriesTemplateProperty = DependencyProperty.Register("BoxPlotSeriesTemplate", typeof(DataTemplate), typeof(SeriesInfoTemplateSelector), new PropertyMetadata(OnDefautlTemplateDependencyPropertyChanged));
        /// <summary>
        /// Defines the OhlcSeriesTemplate DependencyProperty
        /// </summary>
        public static readonly DependencyProperty OhlcSeriesTemplateProperty = DependencyProperty.Register("OhlcSeriesTemplate", typeof(DataTemplate), typeof(SeriesInfoTemplateSelector), new PropertyMetadata(OnDefautlTemplateDependencyPropertyChanged));
        /// <summary>
        /// Defines the HlcSeriesTemplate DependencyProperty
        /// </summary>
        public static readonly DependencyProperty HlcSeriesTemplateProperty = DependencyProperty.Register("HlcSeriesTemplate", typeof(DataTemplate), typeof(SeriesInfoTemplateSelector), new PropertyMetadata(OnDefautlTemplateDependencyPropertyChanged));
        /// <summary>
        /// Defines the OneHundredPercentStackedSeriesTemplate DependencyProperty
        /// </summary>
        public static readonly DependencyProperty OneHundredPercentStackedSeriesTemplateProperty = DependencyProperty.Register("OneHundredPercentStackedSeriesTemplate", typeof(DataTemplate), typeof(SeriesInfoTemplateSelector), new PropertyMetadata(OnDefautlTemplateDependencyPropertyChanged));
        /// <summary>
        /// Defines the TimeframeSegmentSeriesTemplate DependencyProperty
        /// </summary>
        public static readonly DependencyProperty TimeframeSegmentSeriesTemplateProperty = DependencyProperty.Register("TimeframeSegmentSeriesTemplate", typeof(DataTemplate), typeof(SeriesInfoTemplateSelector), new PropertyMetadata(OnDefautlTemplateDependencyPropertyChanged));

        /// <summary>
        /// Initializes a new instance of the <seealso cref="SeriesInfoTemplateSelector"/> class.
        /// </summary>
        public SeriesInfoTemplateSelector()
        {
            DefaultStyleKey = typeof (SeriesInfoTemplateSelector);
        }

        /// <summary>
        /// Gets or sets the DataTemplate for <see cref="HeatmapSeriesInfo" />
        /// </summary>
        public DataTemplate HeatmapSeriesTemplate
        {
            get { return (DataTemplate) GetValue(HeatmapSeriesTemplateProperty); }
            set { SetValue(HeatmapSeriesTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets the DataTemplate for the first series of the <see cref="BandSeriesInfo" />
        /// </summary>
        public DataTemplate BandSeries1Template
        {
            get { return (DataTemplate) GetValue(BandSeries1TemplateProperty); }
            set { SetValue(BandSeries1TemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets the DataTemplate for the second series of the <see cref="BandSeriesInfo" />
        /// </summary>
        public DataTemplate BandSeries2Template
        {
            get { return (DataTemplate) GetValue(BandSeries2TemplateProperty); }
            set { SetValue(BandSeries2TemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets the DataTemplate for <see cref="OhlcSeriesInfo" />
        /// </summary>
        public DataTemplate BoxPlotSeriesTemplate
        {
            get { return (DataTemplate) GetValue(BoxPlotSeriesTemplateProperty); }
            set { SetValue(BoxPlotSeriesTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets the DataTemplate for <see cref="OhlcSeriesInfo" />
        /// </summary>
        public DataTemplate OhlcSeriesTemplate
        {
            get { return (DataTemplate) GetValue(OhlcSeriesTemplateProperty); }
            set { SetValue(OhlcSeriesTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets the DataTemplate for <see cref="OhlcSeriesInfo" />
        /// </summary>
        public DataTemplate HlcSeriesTemplate
        {
            get { return (DataTemplate)GetValue(HlcSeriesTemplateProperty); }
            set { SetValue(HlcSeriesTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets the DataTemplate for <see cref="OneHundredPercentStackedSeriesInfo" />
        /// </summary>
        public DataTemplate OneHundredPercentStackedSeriesTemplate
        {
            get { return (DataTemplate)GetValue(OneHundredPercentStackedSeriesTemplateProperty); }
            set { SetValue(OneHundredPercentStackedSeriesTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets the DataTemplate for <see cref="TimeframeSegmentSeriesInfo" />
        /// </summary>
        public DataTemplate TimeframeSegmentSeriesTemplate
        {
            get { return (DataTemplate)GetValue(TimeframeSegmentSeriesTemplateProperty); }
            set { SetValue(TimeframeSegmentSeriesTemplateProperty, value); }
        }

        /// <summary>
        /// When overidden in derived classes, contains the logic for choosing a proper DataTemplate
        /// </summary>
        /// <param name="item"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is BoxPlotSeriesInfo)
            {
                return BoxPlotSeriesTemplate;
            }

            if (item is OhlcSeriesInfo)
            {
                return OhlcSeriesTemplate;
            }

            if (item is HlcSeriesInfo)
            {
                return HlcSeriesTemplate;
            }

            var bandInfo = item as BandSeriesInfo;
            if (bandInfo != null)
            {
                return bandInfo.IsFirstSeries ? BandSeries1Template : BandSeries2Template;
            }

            if (item is HeatmapSeriesInfo)
            {
                return HeatmapSeriesTemplate;
            }
            if (item is OneHundredPercentStackedSeriesInfo)
            {
                return OneHundredPercentStackedSeriesTemplate;
            }

            if(item is TimeframeSegmentSeriesInfo)
                return TimeframeSegmentSeriesTemplate;

            return base.SelectTemplate(item, container);
        }
    }
}