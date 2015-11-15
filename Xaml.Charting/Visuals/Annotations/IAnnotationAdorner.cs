// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IAnnotationAdorner.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Controls.Primitives;

namespace Ecng.Xaml.Charting.Visuals.Annotations
{
    /// <summary>
    /// Defines the interface to an annotation adorner, which may be placed to drag, or resize an annotation
    /// </summary>
    public interface IAnnotationAdorner
    {
        /// <summary>
        /// Initializes this adorner.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Clears child markers from the adorner layer
        /// </summary>
        void Clear();

        /// <summary>
        /// Causes a refresh to update the positions of the adorner
        /// </summary>
        void UpdatePositions();

        /// <summary>
        /// Gets the associated annotation that this instance adorns
        /// </summary>
        IAnnotation AdornedAnnotation { get; }
    }
}
