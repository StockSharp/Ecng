// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// InterfaceHelpers.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Reflection;

namespace Ecng.Xaml.Charting.Common.Helpers
{
    internal static class InterfaceHelpers
    {
        /// <summary>
        /// Copies the interface properties from one object to another
        /// </summary>
        /// <typeparam name="T">The interface type that both objects implement to copy the properties on</typeparam>
        /// <param name="from">The object that implements interface type T to copy from</param>
        /// <param name="to">The object that implements interface type T to copy to</param>
        /// <exception cref="System.Exception"/>
        public static void CopyInterfaceProperties<T>(T from, T to)
        {
            CheckInterfaceProperties<T>();

            // Get the properties from interface type T
            PropertyInfo[] props = typeof(T).GetProperties();

            // Copy the properties from one object to the other
            foreach (PropertyInfo prop in props)
            {
                if (prop.CanRead && prop.CanWrite)
                {
                    prop.SetValue(to, prop.GetValue(from, null), null);
                }
            }
        }

        /// <summary>
        /// Performs the following checks on generic type T, and objects From, To. If T is not an interface, 
        /// an exception is thrown. If either object From or To do not implement T, an exception is thrown
        /// </summary>
        /// <typeparam name="T">The generic type to check if it is an interface</typeparam>
        /// <exception cref="System.Exception"/>
        private static void CheckInterfaceProperties<T>()
        {
            // Check if T is an interface
            if (!typeof(T).IsInterface)
            {
                throw new Exception(string.Format("Unable to copy interface properties as typeparam {0} is not an interface", typeof(T)));
            }
        }
    }
}
