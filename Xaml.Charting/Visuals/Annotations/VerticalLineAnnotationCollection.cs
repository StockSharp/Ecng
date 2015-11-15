// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// VerticalLineAnnotationCollection.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Visuals.Annotations
{
    /// <summary>
    /// Contains a collection of <see cref="VerticalLineAnnotation"/> instances, which allow custom vertical lines
    /// over or under the parent <see cref="UltrachartSurface"/>
    /// </summary>
    public class VerticalLineAnnotationCollection : ObservableCollection<VerticalLineAnnotation>
    {
        internal IList<VerticalLineAnnotation> OldItems;

        /// <summary>
        /// Clears the items in the collection
        /// </summary>
        protected override void ClearItems()
        {
            OldItems = new List<VerticalLineAnnotation>(Items);

            base.ClearItems();
        }
    }
}
