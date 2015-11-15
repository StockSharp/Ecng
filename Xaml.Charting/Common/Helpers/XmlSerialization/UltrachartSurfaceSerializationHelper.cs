// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// UltrachartSurfaceSerializationHelper.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Common.Helpers.XmlSerialization;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Annotations;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Common.Helpers
{
    internal class UltrachartSurfaceSerializationHelper:SerializationHelper<UltrachartSurface>
    {
        private static UltrachartSurfaceSerializationHelper _instance;

        internal static UltrachartSurfaceSerializationHelper Instance
        {
            get { return _instance ?? (_instance = new UltrachartSurfaceSerializationHelper()); }
        }

        private UltrachartSurfaceSerializationHelper()
        {
            BaseProperties = new[]
            {
                "MaxFrameRate", 
                "ClipOverlayAnnotations", 
                "ClipUnderlayAnnotations", 
                "ClipModifierSurface", 
                "ChartTitle"
            };
        }

        public override void SerializeProperties(UltrachartSurface element, XmlWriter writer)
        {
            base.SerializeProperties(element, writer);
            
            // serialization values of theme and render surface attached properties
            var theme = ThemeManager.GetTheme(element);
            if(theme!=null)
                writer.WriteAttributeString("Theme",theme);

            var renderSurfaceType = RenderSurfaceBase.GetRenderSurfaceType(element);
            if(renderSurfaceType!=null)
                writer.WriteAttributeString("RenderSurfaceType",renderSurfaceType);

            SerilalizeRenderableSeries(element.RenderableSeries, writer);

            SerializeElement(element.XAxes, "XAxes", writer);
            SerializeElement(element.YAxes, "YAxes", writer);

            SerializeElement(element.Annotations, "Annotations", writer);

            SerializeElement((ChartModifierBase)element.ChartModifier, "ChartModifier", writer);
        }

        public override void DeserializeProperties(UltrachartSurface element, XmlReader reader)
        {
            base.DeserializeProperties(element, reader);

            var theme = reader["Theme"];
            if (theme != null)
            {
                ThemeManager.SetTheme(element, theme);
            }

            var renderSurfaceType = reader["RenderSurfaceType"];
            if (renderSurfaceType != null)
            {
                RenderSurfaceBase.SetRenderSurfaceType(element, renderSurfaceType);
            }

            // Deserializing of renderable series, axes, annotations and modifiers
            if (reader.Read())
            {
                while (reader.MoveToContent() == XmlNodeType.Element)
                {
                    var name = reader.LocalName;

                    if (name == "RenderableSeries")
                    {
                        var instance = (ObservableCollection<IRenderableSeries>)Activator.CreateInstance(typeof(ObservableCollection<IRenderableSeries>));

                        var renderableSeries = RenderableSeriesSerializationHelper.Instance.DeserializeCollection(reader);
                        instance.AddRange(renderableSeries);

                        element.RenderableSeries = instance;
                    }
                    else
                    {
                        var type = Type.GetType(reader["Type"]);
                        var instance = (IXmlSerializable)Activator.CreateInstance(type);

                        instance.ReadXml(reader);

                        var propertyInfo = element.GetType().GetProperty(name);

                        propertyInfo.SetValue(element, instance, null);
                    }

                    reader.Read();
                }
            }
        }

        private static void SerializeElement<T>(T element, string elementName, XmlWriter writer) where T : IXmlSerializable
        {
            if (element == null)
                return;

            writer.WriteStartElement(elementName);
            writer.WriteAttributeString("Type", element.GetType().ToTypeString());

            element.WriteXml(writer);

            writer.WriteEndElement();
        }

        private static void SerilalizeRenderableSeries(IEnumerable<IRenderableSeries> renderableSeries, XmlWriter writer)
        {
            if(renderableSeries == null)
                return;

            writer.WriteStartElement("RenderableSeries");

            RenderableSeriesSerializationHelper.Instance.SerializeCollection(renderableSeries, writer);

            writer.WriteEndElement();
        }
    }
}
