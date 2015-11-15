// *************************************************************************************
// Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ulcsoftware.co.uk
// Support: info@ulcsoftware.co.uk
// 
// XElementExtensions.cs is part of Ecng.Xaml.Licensing.Core, a client-side licensing implementation 
// used by ulc software controls such as Ultrachart
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
using System.Xml.Linq;

namespace Ecng.Xaml.Licensing.Core
{
    internal static class XElementExtensions
    {
        internal static string GetOptionalAttributeValue(this XElement element, string attrName)
        {
            var attribute = element.Attribute(attrName);
            return attribute != null ? attribute.Value : null;
        }

        internal static string GetRequiredAttributeValue(this XElement element, string attributeName)
        {
            var attribute = element.Attribute(attributeName);
            if (attribute == null)
                throw new InvalidOperationException(string.Format("Expected attribute <{0}/>", attributeName));

            return attribute.Value;
        }

        internal static string GetRequiredElementValue(this XElement element, string elementName)
        {
            var childElement = element.Element(elementName);
            if (childElement == null)
                throw new InvalidOperationException(string.Format("Expected element <{0}/>", elementName));

            return childElement.Value;
        }

        internal static string GetOptionalElementValue(this XElement element, string elementName)
        {
            var childElement = element.Element(elementName);
            if (childElement == null) return null;
            return childElement.Value;
        }
    }
}