// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AnnotationCollection.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Common.Helpers;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Licensing.Core;

namespace Ecng.Xaml.Charting.Visuals.Annotations
{
    /// <summary>
    /// Contains a collection of <see cref="IAnnotation"/> derived types, which allow custom drawing elements 
    /// over or under the parent <see cref="UltrachartSurface"/>
    /// </summary>
    [UltrachartLicenseProvider(typeof(AnnotationCollectionLicenseProvider))]
    public sealed class AnnotationCollection : ObservableCollection<IAnnotation>, IXmlSerializable
    {
        private IServiceContainer _serviceContainer;
        private IUltrachartSurface _parentSurface;
        private Delegate _parentSurfaceMouseDownDelegate;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationCollection"/> class.
        /// </summary>
        public AnnotationCollection()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationCollection"/> class.
        /// </summary>
        public AnnotationCollection(IEnumerable<IAnnotation> collection)
            : base(collection)
        {
            Initialize();
        }

        /// <summary>
        /// Gets or sets the parent <see cref="UltrachartSurface"/> to perform operations on 
        /// </summary>
        public IUltrachartSurface ParentSurface
        {
            get { return _parentSurface; }
            set
            {
                var oldSurface = _parentSurface;
                if (oldSurface != null)
                {
                    _serviceContainer = null;
                    UnsubscribeSurfaceEvents(oldSurface);
                    this.ForEachDo(DetachAnnotation);
                }

                _parentSurface = value;

                if (_parentSurface != null)
                {
                    SubscribeSurfaceEvents(_parentSurface);
                    _serviceContainer = _parentSurface.Services;
                    this.ForEachDo(AttachAnnotation);
                }
            }
        }

        /// <summary>
        /// Subscribes the AnnotationCollection to events on the parent <see cref="UltrachartSurface"/>. 
        /// Should be called internally by the Annotations API when attaching to a surface. 
        /// </summary>
        /// <param name="parentSurface">The parent <see cref="UltrachartSurface"/></param>
        public void SubscribeSurfaceEvents(IUltrachartSurface parentSurface)
        {
            UnsubscribeSurfaceEvents(parentSurface);

            var scs = parentSurface as UltrachartSurface;
            if (scs != null && ParentSurfaceMouseDownDelegate == null)
            {
                ParentSurfaceMouseDownDelegate = new MouseButtonEventHandler(RootGridMouseDown);
                scs.AddHandler(UIElement.MouseLeftButtonDownEvent, ParentSurfaceMouseDownDelegate, true);
            }
        }

        /// <summary>
        /// Unsubscribes the AnnotationCollection to events on the parent <see cref="UltrachartSurface"/>. 
        /// Should be called internally by the Annotations API when detaching from a surface. 
        /// </summary>
        /// <param name="parentSurface">The parent <see cref="UltrachartSurface"/></param>
        public void UnsubscribeSurfaceEvents(IUltrachartSurface parentSurface)
        {
            var scs = parentSurface as UltrachartSurface;
            if (scs != null && ParentSurfaceMouseDownDelegate != null)
            {
                scs.RemoveHandler(UIElement.MouseLeftButtonDownEvent, ParentSurfaceMouseDownDelegate);
                ParentSurfaceMouseDownDelegate = null;
            }
        }

        private void RootGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            //Annotation can contain Image, so need to check whether event is handled
            var oSrc = e.OriginalSource as Rectangle;
            if (oSrc != null && RenderSurfaceBase.RectIdentifier.Equals(oSrc.Tag))
            {
                // Deselect all            
                DeselectAll();
                e.Handled = true;
            }

            OnRootGridMouseDownHandled();
        }

        /// <summary>
        /// Deselects all annotations in the AnnotationCollection
        /// </summary>
        public void DeselectAll()
        {
            this.ForEachDo(annotation =>
            {
                annotation.IsSelected = false;
            });
        }

        /// <summary>
        /// Clears all Annotations from the parent <see cref="UltrachartSurface"/>
        /// </summary>
        protected override void ClearItems()
        {
            foreach (var item in this)
            {
                DetachAnnotation(item);
            }
            base.ClearItems();
        }

        internal void AnnotationCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ParentSurface == null) return;

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    DetachAnnotation((IAnnotation)item);
                }
            }

            foreach (var item in this)
            {
                if (!item.IsAttached)
                {
                    AttachAnnotation(item);
                }

                ParentSurface.InvalidateElement();
            }
        }

        private static void DetachAnnotation(IAnnotation item)
        {
            item.OnDetached();

            item.Services = null;
            item.ParentSurface = null;
            item.IsAttached = false;
        }

        private void AttachAnnotation(IAnnotation item)
        {
            item.Services = _serviceContainer;
            item.ParentSurface = _parentSurface;
            item.IsAttached = true;

            item.OnAttached();
        }

        /// <summary>
        /// Returns an XmlSchema that describes the XML representation of the object that is produced by the WriteXml method and consumed by the ReadXml method
        /// </summary>
        /// <remarks>
        /// This method is reserved by <see cref="System.Xml.Serialization.IXmlSerializable"/> and should not be used
        /// </remarks>
        /// <returns></returns>
        public XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary>
        /// Generates <see cref="AnnotationCollection"/> from its XML representation.
        /// </summary>
        /// <param name="reader"></param>
        public void ReadXml(XmlReader reader)
        {
            IUpdateSuspender updateSuspener = null;

            if (ParentSurface != null)
                updateSuspener = ParentSurface.SuspendUpdates();

            var annotations = AnnotationSerializationHelper.Instance.DeserializeCollection(reader);
            this.AddRange(annotations);

            if (updateSuspener != null)
                updateSuspener.Dispose();
        }

        /// <summary>
        /// Converts <see cref="AnnotationCollection"/> into its XML representation.
        /// </summary>
        /// <param name="writer"></param>
        public void WriteXml(XmlWriter writer)
        {
            AnnotationSerializationHelper.Instance.SerializeCollection(this, writer);
        }

        /// <summary>
        /// Refreshes positions of all annotations within the collection
        /// </summary>
        /// <param name="rpi"></param>
        public void RefreshPositions(RenderPassInfo rpi)
        {
            foreach (var annotation in this)
            {
                var xCoordinateCalculator = GetCoordinateCalculator(rpi.XCoordinateCalculators, annotation, annotation.XAxisId, true);
                var yCoordinateCalculator = GetCoordinateCalculator(rpi.YCoordinateCalculators, annotation, annotation.YAxisId, false);

                // SC-2533 allow update with null coordinate calculators then throw after, so that annotation is added to the parent surface 
                // allowing bindings to propagate to the annotation
                annotation.Update(xCoordinateCalculator, yCoordinateCalculator);

                if (xCoordinateCalculator == null)
                {
                    rpi.Warnings.Add(String.Format("Could not draw an annotation of type {0}. XAxis with Id == {1} doesn't exist. Please ensure that the XAxisId property is set to a valid value.",
                            annotation.GetType(),
                            annotation.XAxisId ?? "NULL"));                    
                }

                if (yCoordinateCalculator == null)
                {
                    rpi.Warnings.Add(String.Format("Could not draw an annotation of type {0}. YAxis with Id == {1} doesn't exist. Please ensure that the YAxisId property is set to a valid value.",
                            annotation.GetType(),
                            annotation.YAxisId ?? "NULL"));
                }
            }
        }

        private ICoordinateCalculator<double> GetCoordinateCalculator(
            IDictionary<string, ICoordinateCalculator<double>> coordinateCalculators,
            IAnnotation annotation,
            string axisId,
            bool isXAxis)
        {
            if (axisId == null) return null;

            ICoordinateCalculator<double> xCalc;
            if (coordinateCalculators.TryGetValue(axisId, out xCalc))
            {
                return xCalc;
            }

            // SC-2533 allow update with null coordinate calculators then throw after, so that annotation is added to the parent surface 
            // allowing bindings to propagate to the annotation
            return null;
        }

        [Obfuscation(Feature = "encryptmethod", Exclude = false)]
        private void Initialize()
        {
            new LicenseManager().Validate(this, new UltrachartLicenseProviderFactory());
        }

        /// <summary>
        /// Gets attempt to select annotation, and returns whether attempt was successful or not
        /// </summary>
        /// <param name="annotationBase">Annotation <see cref="IAnnotation"/> for selection</param>
        /// <returns></returns>
        public bool TrySelectAnnotation(IAnnotation annotationBase)
        {
            if (annotationBase.IsEditable && !annotationBase.IsSelected && annotationBase.IsAttached)
            {
                // Deselect all
                this.ForEachDo(annotation =>
                {
                    annotation.IsSelected = false;
                });

                SelectAnnotation(annotationBase);
                return true;
            }

            return false;
        }

        private void SelectAnnotation(IAnnotation annotationBase)
        {
            annotationBase.IsSelected = true;
        }


        // Used internally for unit tests
        internal Delegate ParentSurfaceMouseDownDelegate
        {
            get { return _parentSurfaceMouseDownDelegate; }
            set { _parentSurfaceMouseDownDelegate = value; }
        }

        // Used internally for unit tests
        internal Action OnRootGridMouseDownHandled = () => { };

        /// <summary>
        /// Called with the <see cref="UltrachartSurface.XAxes" /> <see cref="AxisCollection" /> changes
        /// </summary>
        public void OnXAxesCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            foreach (var annotation in Items)
            {
                annotation.OnXAxesCollectionChanged(sender, args);
            }
        }

        /// <summary>
        /// Called with the <see cref="UltrachartSurface.YAxes" /> <see cref="AxisCollection" /> changes
        /// </summary>
        public void OnYAxesCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            foreach (var annotation in Items)
            {
                annotation.OnYAxesCollectionChanged(sender, args);
            }
        }
    }
}