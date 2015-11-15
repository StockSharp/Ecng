// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// BindingExtensions.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Data;

namespace Ecng.Xaml.Charting.Common.Extensions
{
    internal static class BindingExtensions
    {
        internal static Binding CloneBinding(this Binding original)
        {
            var copy = new Binding
            {
                Path = original.Path,
                Mode = original.Mode,
                Converter = original.Converter,
                ConverterCulture = original.ConverterCulture,
                ConverterParameter = original.ConverterParameter,
                FallbackValue = original.FallbackValue,
                TargetNullValue = original.TargetNullValue,
                NotifyOnValidationError = original.NotifyOnValidationError,
                UpdateSourceTrigger = original.UpdateSourceTrigger,
                ValidatesOnDataErrors = original.ValidatesOnDataErrors,
                ValidatesOnExceptions = original.ValidatesOnExceptions,
                BindsDirectlyToSource = original.BindsDirectlyToSource,
                StringFormat = original.StringFormat,
#if !SILVERLIGHT
                AsyncState = original.AsyncState,
                IsAsync = original.IsAsync,
                NotifyOnSourceUpdated = original.NotifyOnSourceUpdated,
                NotifyOnTargetUpdated = original.NotifyOnTargetUpdated,
                BindingGroupName = original.BindingGroupName,
                UpdateSourceExceptionFilter = original.UpdateSourceExceptionFilter,
                XPath = original.XPath,
#endif
            };

            if (original.Source != null)
                copy.Source = original.Source;
            if (original.RelativeSource != null)
                copy.RelativeSource = original.RelativeSource;
            if (original.ElementName != null)
                copy.ElementName = original.ElementName;

#if !SILVERLIGHT
            foreach (var rule in original.ValidationRules)
            {
                copy.ValidationRules.Add(rule);
            }
#endif

            return copy;
        }
    }
}
