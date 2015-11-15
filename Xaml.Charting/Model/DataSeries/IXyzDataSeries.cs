// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IXyzDataSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections;
using System.Collections.Generic;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// Provides the interface to a <seealso cref="IDataSeries"/> to hold X,Y,Z values. Used as a data-source for the <seealso cref="FastBubbleRenderableSeries"/>, 
    /// if this DataSeries is assigned to any other X-Y type, then the X-Y values will be rendered (Z ignored). 
    /// </summary>
    /// <seealso cref="DataSeries{TX,TY}"/>
    /// <seealso cref="IDataSeries"/>
    /// <seealso cref="IDataSeries{TX,TY}"/>
    /// <seealso cref="IXyDataSeries{TX,TY}"/>
    /// <seealso cref="IXyDataSeries"/>
    /// <seealso cref="XyDataSeries{TX,TY}"/>
    /// <seealso cref="IXyyDataSeries{TX,TY}"/>
    /// <seealso cref="IXyyDataSeries"/>
    /// <seealso cref="XyyDataSeries{TX,TY}"/>
    /// <seealso cref="IOhlcDataSeries{TX,TY}"/>
    /// <seealso cref="IOhlcDataSeries"/>
    /// <seealso cref="OhlcDataSeries{TX,TY}"/>
    /// <seealso cref="IHlcDataSeries{TX,TY}"/>
    /// <seealso cref="IHlcDataSeries"/>
    /// <seealso cref="HlcDataSeries{TX,TY}"/>
    /// <seealso cref="IXyzDataSeries{TX,TY,TZ}"/>
    /// <seealso cref="IXyzDataSeries"/>
    /// <seealso cref="XyzDataSeries{TX,TY,TZ}"/>
    /// <seealso cref="BoxPlotDataSeries{TX,TY}"/>
    /// <seealso cref="BaseRenderableSeries"/>
    /// <seealso cref="FastBubbleRenderableSeries"/>
    /// <remarks>DataSeries are assigned to the RenderableSeries via the <see cref="IRenderableSeries.DataSeries"/> property. Any time a DataSeries is appended to, the
    /// parent <see cref="UltrachartSurface"/> will be redrawn</remarks>
    public interface IXyzDataSeries : IDataSeries
    {
        /// <summary>
        /// Gets the Z Values as a list of <see cref="IComparable"/>
        /// </summary>
        IList ZValues { get; }
    }

    /// <summary>
    /// Provides a generic interface to a <seealso cref="IDataSeries"/> to hold X,Y,Z values. Used as a data-source for the <seealso cref="FastBubbleRenderableSeries"/>, 
    /// if this DataSeries is assigned to any other X-Y type, then the X-Y values will be rendered (Z ignored). 
    /// </summary>
    /// <typeparam name="TX">The type of the X-data</typeparam>
    /// <typeparam name="TY">The type of the Y-data</typeparam>
    /// <typeparam name="TZ">The type of the Z-data</typeparam>
    /// <seealso cref="DataSeries{TX,TY}"/>
    /// <seealso cref="IDataSeries"/>
    /// <seealso cref="IDataSeries{TX,TY}"/>
    /// <seealso cref="IXyDataSeries{TX,TY}"/>
    /// <seealso cref="IXyDataSeries"/>
    /// <seealso cref="XyDataSeries{TX,TY}"/>
    /// <seealso cref="IXyyDataSeries{TX,TY}"/>
    /// <seealso cref="IXyyDataSeries"/>
    /// <seealso cref="XyyDataSeries{TX,TY}"/>
    /// <seealso cref="IOhlcDataSeries{TX,TY}"/>
    /// <seealso cref="IOhlcDataSeries"/>
    /// <seealso cref="OhlcDataSeries{TX,TY}"/>
    /// <seealso cref="IHlcDataSeries{TX,TY}"/>
    /// <seealso cref="IHlcDataSeries"/>
    /// <seealso cref="HlcDataSeries{TX,TY}"/>
    /// <seealso cref="IXyzDataSeries{TX,TY,TZ}"/>
    /// <seealso cref="IXyzDataSeries"/>
    /// <seealso cref="XyzDataSeries{TX,TY,TZ}"/>
    /// <seealso cref="BoxPlotDataSeries{TX,TY}"/>
    /// <seealso cref="BaseRenderableSeries"/>
    /// <seealso cref="FastBubbleRenderableSeries"/>
    /// <remarks>DataSeries are assigned to the RenderableSeries via the <see cref="IRenderableSeries.DataSeries"/> property. Any time a DataSeries is appended to, the
    /// parent <see cref="UltrachartSurface"/> will be redrawn</remarks>
    public interface IXyzDataSeries<TX, TY, TZ> : IDataSeries<TX, TY>, IXyzDataSeries
        where TX : IComparable
        where TY : IComparable
        where TZ : IComparable
    {
        /// <summary>
        /// Gets the Z values
        /// </summary>
        new IList<TZ> ZValues { get; }

        /// <summary>
        /// Appends a single X, Y, Z point to the series, automatically triggering a redraw
        /// </summary>
        /// <param name="x">The X-value</param>
        /// <param name="y">The Y-value</param>
        /// <param name="z">The Z-value</param>
        void Append(TX x, TY y, TZ z);

        /// <summary>
        /// Appends a collection of X, Y and Z points to the series, automatically triggering a redraw
        /// </summary>
        /// <param name="x">The X-values</param>
        /// <param name="y">The Y-values</param>
        /// <param name="z">The Z-values</param>
        void Append(IEnumerable<TX> x, IEnumerable<TY> y, IEnumerable<TZ> z);

        /// <summary>
        /// Updates (overwrites) the Y, Z values at the specified X-value. Automatically triggers a redraw
        /// </summary>
        /// <param name="x">The X-value</param>
        /// <param name="y">The Y-value</param>
        /// <param name="z">The Z-value</param>
        void Update(TX x, TY y, TZ z);

        /// <summary>
        /// Inserts an X, Y, Z point at the specified index. Automatically triggers a redraw
        /// </summary>
        /// <param name="index">The index to insert at</param>
        /// <param name="x">The X-value</param>
        /// <param name="y">The y-value</param>
        /// <param name="z">The z-value</param>
        void Insert(int index, TX x, TY y, TZ z);

        /// <summary>
        /// Inserts a collection of X, Y and Z points at the specified index, automatically triggering a redraw
        /// </summary>
        /// <param name="startIndex">The index to insert at</param>
        /// <param name="x">The X-values</param>
        /// <param name="y">The Y-values</param>
        /// <param name="z">The Z-values</param>
        void InsertRange(int startIndex, IEnumerable<TX> x, IEnumerable<TY> y, IEnumerable<TZ> z);
    }
}