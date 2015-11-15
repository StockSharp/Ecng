// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// StringlyTyped.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Common
{
    /// <summary>
    /// An abstract base-class that wraps a string, to avoid Stringly Typed circumstances where methods return or pass too many strings
    /// </summary>
    public abstract class StringlyTyped
    {
        protected StringlyTyped(string value)
        {
            this.Value = value;
        }

        protected bool Equals(StringlyTyped other)
        {
            var otherValue = other != null ? other.Value : null;

            return string.Equals(Value, otherValue);
        }

        public override int GetHashCode()
        {
            return (Value != null ? Value.GetHashCode() : 0);
        }

        /// <summary>
        /// Gets the string value
        /// </summary>
        public string Value { get; private set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as StringlyTyped);
        }

        public override string ToString()
        {
            return string.Format("{0}: [{1}]", GetType().Name, Value ?? "Null");
        }
    }
}
