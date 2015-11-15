// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ModifierEventArgsBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Utility.Mouse;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// Defines a ModifierEventArgsBase, which provides a set of properties and methods which are common to all derived classes
    /// </summary>
    public abstract class ModifierEventArgsBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModifierEventArgsBase"/> class.
        /// </summary>
        protected ModifierEventArgsBase() {}

        /// <summary>
        /// Initializes a new instance of the <see cref="ModifierEventArgsBase"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="isMaster">if set to <c>true</c> [is master].</param>
        protected ModifierEventArgsBase(IReceiveMouseEvents source, bool isMaster)
        {
            Source = source;
            IsMaster = isMaster;
        }

        /// <summary>
        /// If True, then this mouse event occurred on a master <see cref="ChartModifierBase"/>. 
        /// Used to process which modifier was the source of an event when multiple modifiers are linked
        /// </summary>
        public bool IsMaster { get; set; }

        /// <summary>
        /// Gets or sets whether this event is Handled. If true, no further modifiers will be informed of the mouse event and mouse events will cease bubbling and tunnelling
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// In the case where e.Master is true, this returns the instance of the master chart modifier
        /// </summary>
        public IReceiveMouseEvents Source { get; set; }
    }
}
