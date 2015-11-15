// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// SerializationHelper.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Common.Extensions;

namespace Ecng.Xaml.Charting.Common.Helpers.XmlSerialization
{
    internal abstract class SerializationHelper<T> where T:IXmlSerializable
    {
        protected string[] BaseProperties = {};
        protected Dictionary<Type, string[]> AddittionalPropertiesDictionary;
 
        public virtual void SerializeProperties(T element, XmlWriter writer)
        {
            foreach (var property in BaseProperties)
            {
                writer.SerializeProperty(element, property);
            }

            if (AddittionalPropertiesDictionary == null) 
                return;

            var propertyList = AddittionalPropertiesDictionary.Where(x => x.Key.IsInstanceOfType(element)).SelectMany(x => x.Value);

            foreach (var property in propertyList)
            {
                writer.SerializeProperty(element, property);
            }
        }

        public virtual void DeserializeProperties(T element, XmlReader reader)
        {
            foreach (var property in BaseProperties)
            {
                reader.DeserilizeProperty(element, property);
            }

            if (AddittionalPropertiesDictionary == null)
                return;

            var propertyList = AddittionalPropertiesDictionary.Where(x => x.Key.IsInstanceOfType(element)).SelectMany(x => x.Value);

            foreach (var property in propertyList)
            {
                reader.DeserilizeProperty(element, property);
            }
        }

        public void SerializeCollection(IEnumerable<T> collection, XmlWriter writer)
        {
            foreach (var element in collection)
            {
                var type = element.GetType();
                
                writer.WriteStartElement(type.Name);
                writer.WriteAttributeString("Type", type.ToTypeString());

                element.WriteXml(writer);

                writer.WriteEndElement();
            }
        }

        public IEnumerable<T> DeserializeCollection(XmlReader reader)
        {
            if (reader.MoveToContent() == XmlNodeType.Element && !reader.IsEmptyElement)
            {
                if (reader.Read())
                {
                    while (reader.MoveToContent() == XmlNodeType.Element)
                    {
                        var type = Type.GetType(reader["Type"]);
                        var element = (T)Activator.CreateInstance(type);

                        element.ReadXml(reader);
                        yield return element;

                        reader.Read();
                    }
                }
            }
        }
    }
}
