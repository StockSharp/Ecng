// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AxisSerializationHelper.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Common.Helpers
{
    internal sealed class AxisSerializationHelper : SerializationHelper<AxisBase>
    {
        private static AxisSerializationHelper _instance;

        internal static AxisSerializationHelper Instance
        {
            get { return _instance ?? (_instance = new AxisSerializationHelper()); }
        }

        private AxisSerializationHelper()
        {
            BaseProperties = new[]
            {
                "AutoRange",
                "AutoTicks",
                "AxisAlignment",
                "DrawMajorBands",
                "DrawMajorTicks",
                "DrawLabels",
                "DrawMinorTicks",
                "DrawMajorGridLines",
                "DrawMinorGridLines",
                "AxisTitle",
                "VisibleRange",
                "Id",
                "GrowBy",
                "MinorsPerMajor",
                "MaxAutoTicks",
                "FlipCoordinates"
            };
        }

        public override void DeserializeProperties(AxisBase element, XmlReader reader)
        {
            base.DeserializeProperties(element, reader);

            string deltaTypeString = reader["DeltaType"];
            if (deltaTypeString != null)
            {
                var deltaType = Type.GetType(deltaTypeString);
                element.MajorDelta = (IComparable) reader.GetValue("MajorDelta", deltaType);
                element.MinorDelta = (IComparable) reader.GetValue("MinorDelta", deltaType);
            }
        }

        public override void SerializeProperties(AxisBase element, XmlWriter writer)
        {
            base.SerializeProperties(element, writer);
            var axisBase = element as AxisBase;
            if (axisBase != null && axisBase.AutoTicks == false && element.MinorDelta != null &&
                element.MajorDelta != null)
            {
                var deltaType = element.MajorDelta.GetType();
                writer.WriteAttributeString("DeltaType", deltaType.ToTypeString());

                writer.SerializeProperty(element, "MinorDelta");
                writer.SerializeProperty(element, "MajorDelta");
            }
        }
    }
}
