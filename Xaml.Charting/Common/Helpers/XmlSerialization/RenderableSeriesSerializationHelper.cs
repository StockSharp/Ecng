// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// RenderableSeriesSerializationHelper.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Xml;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Common.Helpers.XmlSerialization;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Common.Helpers
{
    internal sealed class RenderableSeriesSerializationHelper : SerializationHelper<IRenderableSeries>
    {
        private static RenderableSeriesSerializationHelper _instance;

        internal static RenderableSeriesSerializationHelper Instance
        {
            get { return _instance ?? (_instance = new RenderableSeriesSerializationHelper()); }
        }

        private RenderableSeriesSerializationHelper()
        {
            BaseProperties = new[]
            {
                "XAxisId",
                "YAxisId",
                "IsVisible",
                "StrokeThickness",
                "AntiAliasing",
                "ResamplingMode",
                "SeriesColor",
            };

            AddittionalPropertiesDictionary = new Dictionary<Type, string[]>
            {
                {typeof (FastLineRenderableSeries), new[] {"DrawNaNAs", "IsDigitalLine"}},
                {typeof (FastOhlcRenderableSeries), new[] {"UpWickColor", "DownWickColor", "DataPointWidth"}},
                {typeof (FastBandRenderableSeries), new[] {"IsDigitalLine", "Series1Color", "BandDownColor", "BandUpColor"}},
                {typeof (FastBoxPlotRenderableSeries), new[] {"DataPointWidth", "BodyBrush"}},
                {typeof (FastBubbleRenderableSeries), new[] {"AutoZRange", "ZScaleFactor", "BubbleColor"}},
                {typeof (BaseColumnRenderableSeries), new[] {"FillBrush", "FillBrushMappingMode", "UseUniformWidth", "DataPointWidth"}},
                {typeof (StackedColumnRenderableSeries), new[] {"StackedGroupId"}},
                {typeof (BaseMountainRenderableSeries), new[] {"IsDigitalLine", "AreaBrush"}},
                {typeof (StackedMountainRenderableSeries), new[] {"StackedGroupId"}},
                {typeof (FastCandlestickRenderableSeries), new[] {"UpWickColor", "DownWickColor", "DataPointWidth","UpBodyBrush","DownBodyBrush"}},
                {typeof (FastErrorBarsRenderableSeries), new[] {"DataPointWidth"}},
            };
        }
    }
}
