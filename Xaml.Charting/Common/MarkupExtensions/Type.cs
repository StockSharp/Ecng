// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// Type.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using System.Windows.Markup;

namespace Ecng.Xaml.Charting.Common.MarkupExtensions
{
    /// <summary>
    /// A MarkupExtension which introduces x:Type like syntax to both WPF and Silverlight (Cross-platform). This is used internally
    /// for the themes, but is also useful e.g. when creating custom Control Templates for Ultrachart
    /// </summary>
    /// <remarks>
    /// Licensed under the CodeProject Open License
    /// http://www.codeproject.com/Articles/305932/Static-and-Type-markup-extensions-for-Silverlight
    /// </remarks>
    /// 
    internal class TypeExtension : MarkupExtension
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TypeExtension" /> class.
        /// </summary>
        public TypeExtension()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeExtension" /> class.
        /// </summary>
        /// <param name="type">The type to wrap</param>
        public TypeExtension(Type type)
        {
            Type = type;
        }

        /// <summary>
        /// Gets or sets the type information for this extension.
        /// </summary>
        public System.Type Type { get; set; }

        /// <summary>
        /// Gets or sets the type name represented by this markup extension.
        /// </summary>
        public String TypeName { get; set; }

        public override Object ProvideValue(IServiceProvider serviceProvider)
        {
            if (Type == null)
            {
                if (String.IsNullOrWhiteSpace(TypeName)) throw new InvalidOperationException("No TypeName or Type specified.");
                if (serviceProvider == null) return DependencyProperty.UnsetValue;

                IXamlTypeResolver resolver = serviceProvider.GetService(typeof(IXamlTypeResolver)) as IXamlTypeResolver;
                if (resolver == null) return DependencyProperty.UnsetValue;

                Type = resolver.Resolve(TypeName);
            }
            return Type;
        }
    }
}
