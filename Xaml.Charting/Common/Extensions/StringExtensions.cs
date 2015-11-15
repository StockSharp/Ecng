// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// StringExtensions.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Reflection;

namespace Ecng.Xaml.Charting.Common.Extensions
{
    internal static class StringExtensions
    {
        /// <summary>
        /// Returns the substring of the input string which is sandwiched between the Before and After strings
        /// </summary>
        /// <param name="input"></param>
        /// <param name="before"></param>
        /// <param name="after"></param>
        /// <returns></returns>
        internal static string Substring(this string input, string before, string after)
        {
            int beforeIndex = string.IsNullOrEmpty(before) ? 0 : input.IndexOf(before) + before.Length;
            int afterIndex = string.IsNullOrEmpty(after) ? input.Length : input.IndexOf(after) - beforeIndex;
            return input.Substring(beforeIndex, afterIndex);
        }

        internal static bool IsNullOrWhiteSpace(this string input)
        {
            return string.IsNullOrEmpty(input) || string.Equals(input, new string(' ', input.Length));
        }
    }
}