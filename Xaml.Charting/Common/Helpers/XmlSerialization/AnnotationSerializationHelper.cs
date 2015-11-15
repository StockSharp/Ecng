// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AnnotationSerializationHelper.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Common.Helpers.XmlSerialization;
using Ecng.Xaml.Charting.Visuals.Annotations;

namespace Ecng.Xaml.Charting.Common.Helpers
{
    internal sealed class AnnotationSerializationHelper : SerializationHelper<IAnnotation>
    {
        private static AnnotationSerializationHelper _instance;

        internal static AnnotationSerializationHelper Instance
        {
            get { return _instance ?? (_instance = new AnnotationSerializationHelper()); }
        }

        private AnnotationSerializationHelper()
        {
            BaseProperties = new[]
            {
                "IsEditable",
                "IsHidden",
                "IsSelected",
                "XAxisId",
                "YAxisId",
                "CoordinateMode",
                "Background",                
                "BorderBrush",
                "BorderThickness",
                "Foreground",
                "FontSize",
                "FontWeight",
                "FontFamily",
                "FontStyle"
            };

            AddittionalPropertiesDictionary = new Dictionary<Type, string[]>
            {
                {typeof (TextAnnotation), new[] {"Text", "TextAlignment"}},
                {typeof (LineAnnotationBase), new[] {"Stroke", "StrokeThickness"}},
                {typeof (LineArrowAnnotation), new[] {"HeadWidth", "HeadLength"}},
                {typeof (LineAnnotationWithLabelsBase), new[] {"ShowLabel", "LabelPlacement"}},
                {typeof (HorizontalLineAnnotation), new[] {"HorizontalAlignment"}},
                {typeof (VerticalLineAnnotation), new[] {"LabelsOrientation", "VerticalAlignment"}},
            };
        }

        public override void DeserializeProperties(IAnnotation element, XmlReader reader)
        {            
            base.DeserializeProperties(element, reader);            
            string xTypeString = reader["XType"];
            if (xTypeString != null)
            {
                var xType = Type.GetType(xTypeString);
                element.X1 = (IComparable) reader.GetValue("X1", xType);
                element.X2 = (IComparable) reader.GetValue("X2", xType);
            }

            string yTypeString = reader["YType"];
            if (yTypeString != null)
            {
                var yType = Type.GetType(yTypeString);
                element.Y1 = (IComparable) reader.GetValue("Y1", yType);
                element.Y2 = (IComparable) reader.GetValue("Y2", yType);
            }
        }

        public override void SerializeProperties(IAnnotation element, XmlWriter writer)
        {
            base.SerializeProperties(element, writer);

            if (element.X1 != null)
            {
                var xType = element.X1.GetType();
                writer.WriteAttributeString("XType", xType.ToTypeString());

                writer.SerializeProperty(element, "X1");
                writer.SerializeProperty(element, "X2");
            }

            if (element.Y1 != null)
            {
                var yType = element.Y1.GetType();
                writer.WriteAttributeString("YType", yType.ToTypeString());

                writer.SerializeProperty(element, "Y1");
                writer.SerializeProperty(element, "Y2");
            }
        }
    }
}
