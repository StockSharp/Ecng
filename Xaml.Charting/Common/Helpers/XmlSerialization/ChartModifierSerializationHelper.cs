// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ChartModifierSerializationHelper.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.ChartModifiers;

namespace Ecng.Xaml.Charting.Common.Helpers.XmlSerialization
{
    internal class ChartModifierSerializationHelper : SerializationHelper<ChartModifierBase>
    {
        private static ChartModifierSerializationHelper _instance;

        internal static ChartModifierSerializationHelper Instance
        {
            get { return _instance ?? (_instance = new ChartModifierSerializationHelper()); }
        }

        private ChartModifierSerializationHelper()
        {
            BaseProperties = new[] { "ExecuteOn", "ReceiveHandledEvents", "IsEnabled" };

            AddittionalPropertiesDictionary = new Dictionary<Type, string[]>()
            {
                {typeof(AnnotationCreationModifier), new[] {"YAxisId"}},
                {typeof(ZoomPanModifier), new[]{"ZoomExtentsY", "XyDirection", "ClipModeX", }},
                {typeof(ZoomExtentsModifier), new[] {"XyDirection", "IsAnimated", }},
                {typeof(AxisDragModifierBase), new[] {"DragMode","AxisId"}},
                {typeof(XAxisDragModifier), new[]{"ClipModeX"}},
                {typeof(TooltipModifierBase),new []{"ShowTooltipOn", "ShowAxisLabels"}},
                {typeof(RubberBandXyZoomModifier), new []{"IsAnimated", "IsXAxisOnly","ZoomExtentsY","RubberBandFill","RubberBandStroke"}}
            };
        }
    } 
}
