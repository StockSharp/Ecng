// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IHeatmap2DArrayDataSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// Represents 2D color data for Array2DSegment 
    /// </summary>
    public interface IHeatmap2DArrayDataSeries
    {
        double[,] GetArray2D();

        int ArrayHeight { get; }

        int ArrayWidth { get; }

        HitTestInfo ToHitTestInfo(double xValue, double yValue, bool interpolate=true);
    }

    internal interface IHeatmap2DArrayDataSeriesInternal : IHeatmap2DArrayDataSeries
    {
        int[,] GetArgbColorArray2D(DoubleToColorMappingSettings mappingSettings);
    }
}
