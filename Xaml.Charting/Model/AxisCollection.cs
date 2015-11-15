// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AxisCollection.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Common.Helpers;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Contains a collection of Axes and allows getting of axis by Id
    /// </summary>
    public class AxisCollection : ObservableCollection<IAxis>, IXmlSerializable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AxisCollection"/> class.
        /// </summary>
        public AxisCollection()
        {
            SetUpCollection();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AxisCollection"/> class.
        /// </summary>
        /// <param name="collection">The collection.</param>
        public AxisCollection(IEnumerable<IAxis> collection)
        : base(collection)
        {
            SetUpCollection();
        }

        /// <summary>
        /// Returns true if any of the Axes in the collection have <see cref="AxisBase.IsPrimaryAxis"/> set to true
        /// </summary>
        protected bool HasPrimaryAxis
        {
            get { return this.Any(x => x.IsPrimaryAxis); }
        }

        /// <summary>
        /// Gets the primary axis in the collection. This is the first that has <see cref="AxisBase.IsPrimaryAxis"/> set to true, or null if none exists. 
        /// </summary>
        protected IAxis PrimaryAxis
        {
            get { return this.FirstOrDefault(x => x.IsPrimaryAxis); }
        }

        /// <summary>
        /// Gets the default axis, which is equal to the axis with the <see cref="AxisBase.DefaultAxisId"/>, else null
        /// </summary>
        public IAxis Default
        {
            get { return Count > 0 ? GetAxisById(AxisBase.DefaultAxisId) : null; }
        }

        /// <summary>
        /// Gets the axis specified by Id, else null
        /// </summary>
        /// <param name="axisId">The axis identifier.</param>
        /// <param name="assertAxisExists">if set to <c>true</c> assert and throw if the axis does not exist.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public IAxis GetAxisById(string axisId, bool assertAxisExists = false)
        {
            try
            {
                var axis = this.SingleOrDefault(x => x.Id == axisId);
                if (assertAxisExists && axis == null)
                {
                    throw new InvalidOperationException(string.Format("AxisCollection.GetAxisById('{0}') returned no axis with ID={0}. Please check you have added an axis with this Id to the AxisCollection", axisId ?? "NULL"));
                }
                return axis;
            }
            catch
            {
                throw new InvalidOperationException(string.Format("AxisCollection.GetAxisById('{0}') returned more than one axis with the ID={0}. Please check you have assigned correct axis Ids when you have multiple axes in Ultrachart", axisId ?? "NULL"));
            }            
        }

        private void SetUpCollection()
        {
            CollectionChanged -= AxisCollectionChanged;
            CollectionChanged += AxisCollectionChanged;

            var firstAxis = this.FirstOrDefault();
            if (!HasPrimaryAxis && firstAxis != null)
            {
                firstAxis.IsPrimaryAxis = true;
            }
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
        /// Generates <see cref="AxisCollection"/> from its XML representation.
        /// </summary>
        /// <param name="reader"></param>
        public virtual void ReadXml(XmlReader reader)
        {
            var axes = AxisSerializationHelper.Instance.DeserializeCollection(reader);
            this.AddRange(axes);
        }

        /// <summary>
        /// Converts <see cref="AxisCollection"/> into its XML representation.
        /// </summary>
        /// <param name="writer"></param>
        public virtual void WriteXml(XmlWriter writer)
        {
            AxisSerializationHelper.Instance.SerializeCollection(this.OfType<AxisBase>(), writer);
        }

        private void AxisCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // If user added a new primary axis, preserve this
            var primaryAxis = e.NewItems != null ? e.NewItems.Cast<IAxis>().FirstOrDefault(x => x.IsPrimaryAxis) : null;

            if (primaryAxis == null)
            {
                // Try the existing primary
                primaryAxis = PrimaryAxis;
                if (primaryAxis == null)
                {
                    // If no new primary axis and no existing primary, then set the first axis in the collection to primary
                    primaryAxis = this.FirstOrDefault();
                    if (primaryAxis != null)
                        primaryAxis.IsPrimaryAxis = true;
                }
            }

            // All other axes (non primary) must be reset
            foreach (var axis in this)
            {
                if (axis == primaryAxis) continue;
                axis.IsPrimaryAxis = false;
            }
        }
    }
}