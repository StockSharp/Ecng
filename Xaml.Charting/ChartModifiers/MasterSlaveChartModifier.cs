// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// MasterSlaveChartModifier.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.ObjectModel;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Visuals;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// Provides a base class for Linked Chart Modifiers. Classes that inherit this allow mouse events and interaction to occur across Chart Panes
    /// </summary>
    public abstract class MasterSlaveChartModifier : ChartModifierBase
    {
        private ObservableCollection<MasterSlaveChartModifier> _slaves = new ObservableCollection<MasterSlaveChartModifier>();
        private bool _processingEvent;

        private ObservableCollection<MasterSlaveChartModifier> Slaves
        {
            get { return _slaves; }
        }

        /// <summary>
        /// Called when the Mouse is moved on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse move operation</param>
        public override void OnModifierMouseMove(ModifierMouseArgs e)
        {
            try
            {
                if (_processingEvent) return;
                _processingEvent = true;

                base.OnModifierMouseMove(e);

                Slaves.ForEachDo(x => x.OnModifierMouseMove(e));
            }
            finally
            {
                _processingEvent = false;
            }
        }

        /// <summary>
        /// Called when a Mouse DoubleClick occurs on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        public override void OnModifierDoubleClick(ModifierMouseArgs e)
        {
            try
            {
                if (_processingEvent) return;
                _processingEvent = true;

                base.OnModifierDoubleClick(e);

                Slaves.ForEachDo(x => x.OnModifierDoubleClick(e));
            }
            finally
            {
                _processingEvent = false;
            }
        }

        /// <summary>
        /// Called when a Mouse Button is pressed on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        public override void OnModifierMouseDown(ModifierMouseArgs e)
        {
            try
            {
                if (_processingEvent) return;
                _processingEvent = true;

                base.OnModifierMouseDown(e);

                Slaves.ForEachDo(x => x.OnModifierMouseDown(e));
            }
            finally
            {
                _processingEvent = false;
            }
        }

        /// <summary>
        /// Called when a Mouse Button is released on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        public override void OnModifierMouseUp(ModifierMouseArgs e)
        {
            try
            {
                if (_processingEvent) return;
                _processingEvent = true;

                base.OnModifierMouseUp(e);

                Slaves.ForEachDo(x => x.OnModifierMouseUp(e));
            }
            finally
            {
                _processingEvent = false;
            }
        }

        /// <summary>
        /// Called when the Mouse Wheel is scrolled
        /// </summary>
        /// <param name="e">Arguments detailing the mouse wheel operation</param>
        public override void OnModifierMouseWheel(ModifierMouseArgs e)
        {
            try
            {
                if (_processingEvent) return;
                _processingEvent = true;

                base.OnModifierMouseWheel(e);

                Slaves.ForEachDo(x => x.OnModifierMouseWheel(e));
            }
            finally
            {
                _processingEvent = false;
            }
        }

        /// <summary>
        /// Called when a Multi-Touch Down interaction occurs on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="e">Arguments detailing the manipulation operation</param>
        public override void OnModifierTouchDown(ModifierTouchManipulationArgs e)
        {
            try
            {
                if (_processingEvent) return;
                _processingEvent = true;

                base.OnModifierTouchDown(e);

                Slaves.ForEachDo(x => x.OnModifierTouchDown(e));
            }
            finally
            {
                _processingEvent = false;
            }
        }


        /// <summary>
        /// Called when a Multi-Touch Move interaction occurs on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="e">Arguments detailing the manipulation operation</param>
        public override void OnModifierTouchMove(ModifierTouchManipulationArgs e)
        {
            try
            {
                if (_processingEvent) return;
                _processingEvent = true;

                base.OnModifierTouchDown(e);

                Slaves.ForEachDo(x => x.OnModifierTouchMove(e));
            }
            finally
            {
                _processingEvent = false;
            }
        }


        /// <summary>
        /// Called when a Multi-Touch Up interaction occurs on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="e">Arguments detailing the manipulation operation</param>
        public override void OnModifierTouchUp(ModifierTouchManipulationArgs e)
        {
            try
            {
                if (_processingEvent) return;
                _processingEvent = true;

                base.OnModifierTouchDown(e);

                Slaves.ForEachDo(x => x.OnModifierTouchUp(e));
            }
            finally
            {
                _processingEvent = false;
            }
        }
    }
}