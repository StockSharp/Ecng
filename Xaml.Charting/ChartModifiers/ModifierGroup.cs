// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ModifierGroup.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xml;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Common.Helpers.XmlSerialization;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// Allows a collection of modifiers to be set on the <see cref="UltrachartSurface.ChartModifier"/> property. Child modifiers are stored in the
    /// <see cref="ModifierGroup.ChildModifiers"/> collection, which is backed by a DependencyProperty so may be bound to in Xaml.
    /// </summary>
    [ContentProperty("ChildModifiers")] 
    public class ModifierGroup : MasterSlaveChartModifier
    {        
        /// <summary>
        /// Defines the ChildModifiers DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ChildModifiersProperty = DependencyProperty.Register("ChildModifiers", typeof (ObservableCollection<IChartModifier>), typeof (ModifierGroup), new PropertyMetadata(null, OnChildModifiersChanged));

        private readonly Grid _grid = new Grid();

        /// <summary>
        /// Initializes a new instance of the <see cref="ModifierGroup"/> class.
        /// </summary>
        /// <remarks></remarks>
        public ModifierGroup(): this(new IChartModifier[]{}) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="ModifierGroup"/> class.
        /// </summary>
        /// <param name="childModifiers">The child modifier collection.</param>
        /// <remarks></remarks>
        public ModifierGroup(params IChartModifier[] childModifiers)
        {
            Guard.NotNull(childModifiers, "childModifiers");

            for (int i = 0; i < childModifiers.Length; i++)
            {
                Guard.NotNull(childModifiers[i], string.Format("childModifiers[{0}]", i));
            }

            Content = _grid;
            this.SetCurrentValue(ChildModifiersProperty, new ObservableCollection<IChartModifier>(childModifiers));
        }

        /// <summary>
        /// Gets or sets a collection of child modifiers in this group
        /// </summary>
        /// <value>The child modifiers.</value>
        /// <remarks></remarks>
        public ObservableCollection<IChartModifier> ChildModifiers
        {
            get { return (ObservableCollection<IChartModifier>)GetValue(ChildModifiersProperty); }
            set { SetValue(ChildModifiersProperty, value); }
        }

        /// <summary>
        /// Gets the <see cref="IChartModifier" /> with the specified name.
        /// </summary>
        /// <value>
        /// The <see cref="IChartModifier" />.
        /// </value>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public IChartModifier this[string name]
        {
            get { return FindModifierByName(name); }
        }


        /// <summary>
        /// Gets the <see cref="IChartModifier" /> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="IChartModifier" />.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public IChartModifier this[int index]
        {
            get { return ChildModifiers[index]; }
        }

        /// <summary>
        /// Called when the Chart Modifier is attached to the Chart Surface
        /// </summary>
        /// <remarks></remarks>
        public override void OnAttached()
        {
            base.OnAttached();

            AttachAll(ChildModifiers);
        }

        /// <summary>
        /// Called immediately before the Chart Modifier is detached from the Chart Surface
        /// </summary>
        /// <remarks></remarks>
        public override void OnDetached()
        {
            base.OnDetached();

            DetachAll(ChildModifiers);
        }

        private void AttachAll(IEnumerable<IChartModifier> childModifiers)
        {
            if (IsAttached)
            {
                childModifiers.ForEachDo(AttachChild);
            }
        }

        private void AttachChild(IChartModifier obj)
        {
            AttachAsElement(obj);

            obj.ParentSurface = ParentSurface;
            obj.Services = Services;
            obj.DataContext = DataContext;
            obj.IsAttached = true;
            obj.OnAttached();
        }

        private void AttachAsElement(IChartModifier chartModifier)
        {
            _grid.SafeAddChild(chartModifier);
        }

        private void DetachAll(IEnumerable<IChartModifier> childModifiers)
        {
            childModifiers.ForEachDo(DetachChild);
        }

        private void DetachChild(IChartModifier obj)
        {
            DetachAsElement(obj);

            obj.OnDetached();
            obj.IsAttached = false;
            obj.ParentSurface = null;
            obj.Services = null;
        }

        private void DetachAsElement(IChartModifier chartModifier)
        {
            _grid.SafeRemoveChild(chartModifier);
        }

        /// <summary>
        /// Called with the <see cref="UltrachartSurface.XAxes" /> <see cref="AxisCollection" /> changes
        /// </summary>
        protected override void OnXAxesCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (ChildModifiers != null)
            {
                ChildModifiers.ForEachDo(x => x.OnXAxesCollectionChanged(sender, args));
            }
        }

        /// <summary>
        /// Called with the <see cref="UltrachartSurface.YAxes" /> <see cref="AxisCollection" /> changes
        /// </summary>
        protected override void OnYAxesCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (ChildModifiers != null)
            {
                ChildModifiers.ForEachDo(x => x.OnYAxesCollectionChanged(sender, args));
            }
        }

        /// <summary>
        /// Called when the AnnotationCollection changes
        /// </summary>
        protected override void OnAnnotationCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (ChildModifiers != null)
            {
                ChildModifiers.ForEachDo(x => x.OnAnnotationCollectionChanged(sender, args));
            }
        }

        /// <summary>
        /// Called when the IsEnabled property changes on this <see cref="ChartModifierBase"/> instance
        /// </summary>
        /// <remarks></remarks>
        protected override void OnIsEnabledChanged()
        {
            ChildModifiers.ForEachDo(x => x.IsEnabled = IsEnabled);
        }
        
        /// <summary>
        /// Called when a Mouse DoubleClick occurs on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        /// <remarks></remarks>
        public override void OnModifierDoubleClick(ModifierMouseArgs e)
        {
            HandleEvent((modifier, args) => modifier.OnModifierDoubleClick((ModifierMouseArgs)args), e);
        }

        private void HandleEvent(Action<IChartModifier, ModifierEventArgsBase> handler, ModifierEventArgsBase e)
        {
            if (ChildModifiers == null) return;

            foreach (var modifier in ChildModifiers.Where(x => x.IsEnabled))
            {
                if (modifier.ReceiveHandledEvents || !e.Handled)
                {
                    handler(modifier, e);
                }
            }
        }

        /// <summary>
        /// Called when a Mouse Button is pressed on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        /// <remarks></remarks>
        public override void OnModifierMouseDown(ModifierMouseArgs e)
        {
            HandleEvent((modifier, args) => modifier.OnModifierMouseDown((ModifierMouseArgs)args), e);
        }

        /// <summary>
        /// Called when the Mouse is moved on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse move operation</param>
        /// <remarks></remarks>
        public override void OnModifierMouseMove(ModifierMouseArgs e)
        {
            HandleEvent((modifier, args) => modifier.OnModifierMouseMove((ModifierMouseArgs)args), e);
        }

        /// <summary>
        /// Called when a Mouse Button is released on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        /// <remarks></remarks>
        public override void OnModifierMouseUp(ModifierMouseArgs e)
        {
            HandleEvent((modifier, args) => modifier.OnModifierMouseUp((ModifierMouseArgs)args), e);
        }

        /// <summary>
        /// Called when the Mouse Wheel is scrolled on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse wheel operation</param>
        /// <remarks></remarks>
        public override void OnModifierMouseWheel(ModifierMouseArgs e)
        {
            HandleEvent((modifier, args) => modifier.OnModifierMouseWheel((ModifierMouseArgs)args), e);
        }

        /// <summary>
        /// Called when the mouse leaves the Master of current <see cref="ChartModifierBase.MouseEventGroup" />
        /// </summary>
        /// <param name="e"></param>
        public override void OnMasterMouseLeave(ModifierMouseArgs e)
        {
            HandleEvent((modifier, args) => modifier.OnMasterMouseLeave((ModifierMouseArgs)args), e);
        }

        /// <summary>
        /// Called when a Multi-Touch Down interaction occurs on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="e">Arguments detailing the manipulation operation</param>
        public override void OnModifierTouchDown(ModifierTouchManipulationArgs e)
        {
            HandleEvent((modifier, args) => modifier.OnModifierTouchDown((ModifierTouchManipulationArgs)args), e);
        }

        /// <summary>
        /// Called when a Multi-Touch Move interaction occurs on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="e">Arguments detailing the manipulation operation</param>
        public override void OnModifierTouchMove(ModifierTouchManipulationArgs e)
        {
            HandleEvent((modifier, args) => modifier.OnModifierTouchMove((ModifierTouchManipulationArgs)args), e);
        }

        /// <summary>
        /// Called when a Multi-Touch Up interaction occurs on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="e">Arguments detailing the manipulation operation</param>
        public override void OnModifierTouchUp(ModifierTouchManipulationArgs e)
        {
            HandleEvent((modifier, args) => modifier.OnModifierTouchUp((ModifierTouchManipulationArgs)args), e);
        }

        /// <summary>
        /// Determines whether the current <see cref="ModifierGroup"/> has a child modifier of the desired type
        /// </summary>
        /// <param name="desiredType">The type of child modifier to search for</param>
        /// <returns><c>true</c> if the current <see cref="ModifierGroup"/> has a chlid modifier by this type; otherwise, <c>false</c>.</returns>
        /// <remarks></remarks>
        public bool HasModifier(Type desiredType)
        {
            return ChildModifiers.Any(x => x.GetType() == desiredType);
        }

        /// <summary>
        /// Called when the DataContext of the <see cref="ChartModifierBase"/> changes
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        /// <remarks></remarks>
        protected override void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ChildModifiers.ForEachDo(x => x.DataContext = e.NewValue);
        }

        private IChartModifier FindModifierByName(string name)
        {
            return ChildModifiers.FirstOrDefault(modifier => name == modifier.ModifierName);
        }

        /// <summary>
        /// Generates <see cref="ChartModifierBase"/> from its XML representation.
        /// </summary>
        /// <param name="reader"></param>
        public override void ReadXml(XmlReader reader)
        {
            // Creating collection of ChartModifiersBase because IChartModifier isn't serializable
            var modifierBaseCollection = new ObservableCollection<ChartModifierBase>();

            var modifiers = ChartModifierSerializationHelper.Instance.DeserializeCollection(reader);
            modifierBaseCollection.AddRange(modifiers);

            foreach (var chartModifierBase in modifierBaseCollection)
            {
                ChildModifiers.Add(chartModifierBase);
            }
        }

        /// <summary>
        /// Converts <see cref="ChartModifierBase"/> into its XML representation.
        /// </summary>
        /// <param name="writer"></param>
        public override void WriteXml(XmlWriter writer)
        {
            ChartModifierSerializationHelper.Instance.SerializeCollection(ChildModifiers.Cast<ChartModifierBase>(), writer);
        }

        /// <summary>
        /// Instantly stops any inertia that can be associated with this modifier.
        /// </summary>
        public override void ResetInertia()
        {
            foreach (var modifier in ChildModifiers)
            {
                modifier.ResetInertia();
            }
        }

        private static void OnChildModifiersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var modifierGroup = d as ModifierGroup;
            if (modifierGroup == null)
                return;

            var oldCollection = e.OldValue as ObservableCollection<IChartModifier>;
            var newCollection = e.NewValue as ObservableCollection<IChartModifier>;

            if (oldCollection != null)
            {
                modifierGroup.DetachAll(oldCollection);

                oldCollection.CollectionChanged -= modifierGroup.ModifierCollectionChanged;
            }

            if (newCollection != null)
            {
                modifierGroup.AttachAll(newCollection);

                newCollection.CollectionChanged += modifierGroup.ModifierCollectionChanged;
            }
        }

        private void ModifierCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                AttachAll(e.NewItems.Cast<IChartModifier>());
            }

            if (e.OldItems != null)
            {
                DetachAll(e.OldItems.Cast<IChartModifier>());
            }
        }
    }
}