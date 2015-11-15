// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// UnsortedXyDataSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Globalization;
using System.Windows;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.GenericMath;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// A DataSeries to store Xy data-points, unsorted containing X and Y values. 
    /// If your data is sorted in the X-direction, for extra performance use the <see cref="XyDataSeries{TX,TY}"/>
    /// May be used as a DataSource for <seealso cref="FastLineRenderableSeries"/> as well as standard XY renderable series types
    /// </summary>
    /// <seealso cref="DataSeries{TX,TY}"/>
    /// <seealso cref="UnsortedXyDataSeries{TX,TY}"/>
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
    /// <remarks>DataSeries are assigned to the RenderableSeries via the <see cref="IRenderableSeries.DataSeries"/> property. Any time a DataSeries is appended to, the
    /// parent <see cref="UltrachartSurface"/> will be redrawn</remarks>
	[Obsolete("UnsortedXyDataSeries is obsolete. Please use the XyDataseries which now correctly detects if your data is sorted or unsorted", true)]
    public class UnsortedXyDataSeries<TX, TY> : XyDataSeries<TX, TY>
        where TX : IComparable
        where TY : IComparable
    {

		// no implementation, left for legacy reasons


		// old code:

		//protected override HitTestInfo NearestHitResult(Point rawPoint, double hitTestRadius, SearchMode searchMode)
		//{
		//	var hitDataPoint = GetHitDataPoint(rawPoint);

		//	// Get index of the dataPoint which is the nearest to the rawPoint
		//	int nearestIndex = 0;
		//	if (CurrentRenderPassData.XCoordinateCalculator.IsCategoryAxisCalculator)
		//	{
		//		var xHitDouble = CurrentRenderPassData.XCoordinateCalculator.TransformDataToIndex(hitDataPoint.Item1);

		//		nearestIndex = NumberUtil.Constrain(xHitDouble, 0, DataSeries.XValues.Count - 1);
		//	}
		//	else
		//	{
		//		nearestIndex = GetNearestDataPointIndex(hitDataPoint.Item1, hitDataPoint.Item2, hitTestRadius, searchMode);
		//	}

		//	var dataPointRadius = GetDataPointRadius(nearestIndex, hitTestRadius);

		//	// Report results
		//	var hitTestInfo = GetHitTestInfo(nearestIndex, rawPoint, dataPointRadius, hitDataPoint.Item1);

		//	return hitTestInfo;
		//}
    }
}