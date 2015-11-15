// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// CanvasExtensions.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Controls;
using System.Windows.Media;

namespace Ecng.Xaml.Charting.Common.Extensions
{
    /// <summary>
    /// Defines attached properties for Canvases
    /// </summary>
    public static class CanvasExtensions
    {

#if !SILVERLIGHT
        internal static void RemoveWhere(this UIElementCollection uiElementCollection, Func<UIElement, bool> predicate)
        {
            for(int i = uiElementCollection.Count-1; i>=0; --i)
            {
                if (predicate(uiElementCollection[i]))
                {
                    uiElementCollection.RemoveAt(i);
                }
            }
        }
#endif

        internal static UIElement FirstOrDefault(this UIElementCollection uiElementCollection, Func<UIElement, bool> predicate)
        {
            for (int i = 0; i < uiElementCollection.Count; i++)
            {
                if (predicate(uiElementCollection[i]))
                {
                    return uiElementCollection[i];
                }
            }

            return null;
        }
    }
}