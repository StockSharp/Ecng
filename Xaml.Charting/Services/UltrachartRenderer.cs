// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// UltrachartRenderer.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Numerics.PointResamplers;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Services;
using Ecng.Xaml.Charting.StrategyManager;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Defines the interface to the <see cref="UltrachartRenderer"/>, which handles the render pass on the <see cref="UltrachartSurface"/>
    /// </summary>
    public interface IUltrachartRenderer
    {
        /// <summary>
        /// Executes a single render pass
        /// </summary>
        RendererErrorCode RenderLoop(IRenderContext2D renderContext);
    }

    /// <summary>
    /// Handles the render pass on the <see cref="UltrachartSurface"/>
    /// </summary>
    /// <remarks></remarks>
    internal class UltrachartRenderer : IUltrachartRenderer
    {
        private readonly UltrachartSurface _ultraChartSurface;

        /// <summary>
        /// Initializes a new instance of the <see cref="UltrachartRenderer"/> class.
        /// </summary>
        /// <param name="ultraChartSurface">The parent <see cref="UltrachartSurface"/></param>
        /// <remarks></remarks>
        internal UltrachartRenderer(UltrachartSurface ultraChartSurface)
        {
            _ultraChartSurface = ultraChartSurface;
        }

        /// <summary>
        /// Executes a single render pass
        /// </summary>
        /// <remarks></remarks>
        public RendererErrorCode RenderLoop(IRenderContext2D renderContext)
        {
            RendererErrorCode errorResult;

            if (!IsSurfaceValid(_ultraChartSurface, out errorResult))            
            {
                _ultraChartSurface.XAxes.ForEachDo(x => x.Clear());
                _ultraChartSurface.YAxes.ForEachDo(x => x.Clear());

                renderContext.Clear();
                return errorResult;
            }

            using (var suspender = _ultraChartSurface.SuspendUpdates())
            {
                suspender.ResumeTargetOnDispose = false;

                UltrachartDebugLogger.Instance.WriteLine("Beginning Render Loop ... ");

                // Step 1: Perform measure/arrange pass manually as we're not drawing WPF primitives
                Size viewportSize = OnLayoutUltrachart(_ultraChartSurface);

                if (IsSurfaceValid(_ultraChartSurface, viewportSize, out errorResult))
                {
                    // Step 2: Prepare data for this render pass
                    RenderPassInfo rpi = PrepareRenderData(_ultraChartSurface, viewportSize);

                    renderContext.Clear();

                    // Step 3: Draw X, Y axis and gridlines
                    OnDrawAxes(_ultraChartSurface, rpi, renderContext);

                    // Step 4: Draw series
                    OnDrawSeries(_ultraChartSurface, rpi, renderContext);

                    // Step 5: Draw annotations
                    OnDrawAnnotations(_ultraChartSurface, rpi);

                    _ultraChartSurface.OnUltrachartRendered();

                    if (rpi.Warnings.Any())
                    {
                        errorResult = new RendererErrorCode(RendererErrorCodes.BecauseOneOrMoreWarningsOccurred
                                                            + "\r\n - "
                                                            + string.Join("\r\n - ", rpi.Warnings)
                                                            + "\r\n"
                                                            + RendererErrorCodes.ToDisableThisMessage);
                    }
                }
            }

            return errorResult;
        }

        private bool IsSurfaceValid(IUltrachartSurface surface, out RendererErrorCode errorResult)
        {
            var isValid = true;
            string errorText = string.Empty;

            if (surface.RenderSurface == null)
            {
                errorText = RendererErrorCodes.BecauseRenderSurfaceIsNull.Value + RendererErrorCodes.ToDisableThisMessage;
                isValid = false;
            }
            else if (surface.XAxes == null || surface.YAxes == null)
            {
                errorText = RendererErrorCodes.BecauseXAxesOrYAxesIsNull.Value + RendererErrorCodes.ToDisableThisMessage;
                isValid = false;
            }
            else if (surface.XAxes.IsEmpty())
            {
                errorText = RendererErrorCodes.BecauseThereAreNoXAxes.Value + RendererErrorCodes.ToDisableThisMessage;
                isValid = false;
            }
            else if (surface.YAxes.IsEmpty())
            {
                errorText = RendererErrorCodes.BecauseThereAreNoYAxes.Value + RendererErrorCodes.ToDisableThisMessage;
                isValid = false;
            }
            else if (surface.XAxis is CategoryDateTimeAxis && surface.RenderableSeries.IsNullOrEmpty())
            {
                errorText = RendererErrorCodes.BecauseUsingCategoryDateTimeAxisAndNoRenderableSeries.Value + RendererErrorCodes.ToDisableThisMessage;
                isValid = false;
            }

            errorResult = String.IsNullOrWhiteSpace(errorText) ? RendererErrorCodes.Success : new RendererErrorCode(errorText);

            return isValid;
        }

        private bool IsSurfaceValid(IUltrachartSurface surface, Size viewportSize, out RendererErrorCode errorResult)
        {
            const double minValidViewportSize = 2d;

            var isValidSize = viewportSize.Width >= minValidViewportSize && viewportSize.Height >= minValidViewportSize;
            var hasValidRanges = !(surface.XAxes.Any(x => !x.HasValidVisibleRange && x.AutoRange == AutoRange.Never) ||
                                   surface.YAxes.Any(y => !y.HasValidVisibleRange && y.AutoRange == AutoRange.Never));
            var hasTickProviders = !(surface.XAxes.Any(x => x.TickProvider == null) ||
                                    surface.YAxes.Any(y => y.TickProvider == null));
            var hasRenderableSeries = !_ultraChartSurface.RenderableSeries.IsNullOrEmpty();
            var hasDataSeries = hasRenderableSeries && _ultraChartSurface.RenderableSeries.All(x => x.DataSeries != null);
            var hasRenderSurface = surface.RenderSurface != null;

            const string endOfLine = "\r\n\r\n";
            string errorText = string.Empty;

            if (!isValidSize)
            {
                errorText += RendererErrorCodes.BecauseViewportSizeIsNotValid + endOfLine;
            }

            if (!hasValidRanges)
            {
                errorText += RendererErrorCodes.BecauseVisibleRangeIsNullOrZeroOnOneOrMoreXOrYAxes + endOfLine;
            }

            if (!hasTickProviders)
            {
                errorText += RendererErrorCodes.BecauseTickProviderIsNull + endOfLine;
            }

            if (!hasRenderableSeries)
            {
                errorText += RendererErrorCodes.BecauseThereAreNoRenderableSeries + endOfLine;
            }

            if (!hasDataSeries)
            {
                errorText += RendererErrorCodes.BecauseThereAreNoDataSeries + endOfLine;
            }

            if (!hasRenderSurface)
            {
                errorText += RendererErrorCodes.BecauseRenderSurfaceIsNull + endOfLine;
            }

            if (!String.IsNullOrWhiteSpace(errorText))
            {
                errorText += RendererErrorCodes.ToDisableThisMessage;
            }

            errorResult = String.IsNullOrWhiteSpace(errorText) ? RendererErrorCodes.Success : new RendererErrorCode(errorText);

            return isValidSize && hasValidRanges && hasTickProviders;
        }

        /// <summary>
        /// Step 1, perform layout, resize components
        /// </summary>
        /// <param name="scs"></param>
        /// <returns></returns>
        internal Size OnLayoutUltrachart(IUltrachartSurface scs)
        {
            // Prevent nested calls InvalidateElement on UltrachartSurface as visible ranges are set
            using (var s1 = scs.SuspendUpdates())
            {
                s1.ResumeTargetOnDispose = false;

                if (scs.ViewportManager == null)
                {
                    throw new InvalidOperationException("UltrachartSurface.ViewportManager is null. Try setting a new DefaultViewportManager()");
                }

                // Handles initial range conditions
                scs.XAxes.ForEachDo(x => TryPerformAutorangeOn(x, scs));
                scs.YAxes.ForEachDo(x => TryPerformAutorangeOn(x, scs));
            }

            var viewportSize = scs.OnArrangeUltrachart();

            return viewportSize;
        }

        /// <summary>
        /// // Set a default range prior to layout if one doesn't exist
        /// </summary>
        internal void TryPerformAutorangeOn(IAxis axis, IUltrachartSurface parentSurface)
        {
            using (var s3 = axis.SuspendUpdates())
            {
                s3.ResumeTargetOnDispose = false;

                var shouldAutoRange = (!axis.HasValidVisibleRange || axis.HasDefaultVisibleRange) &&
                                      (axis.AutoRange == AutoRange.Once || axis.AutoRange == AutoRange.Always);

                if (shouldAutoRange)
                {
                    var newRange = parentSurface.ViewportManager.CalculateAutoRange(axis);

                    // Note: leave this .Equals check in place. Although Dep Property won't update if value is the same, there
                    // is a bizzarre race condition when visiblerange is updated in a binding from another thread
                    if (!newRange.Equals(axis.VisibleRange) && axis.IsValidRange(newRange))
                    {
                        axis.VisibleRange = newRange;
                    }
                }
            }
        }

        /// <summary>
        /// Step 2, prepare render data
        /// </summary>
        internal static RenderPassInfo PrepareRenderData(IUltrachartSurface scs, Size viewportSize)
        {
            // Renderableseries needs
            // 1. DataSeries
            // 2. DataSeries.Count
            // 3. Associated Axis Params on X and Y
            // 4. Point range in view
            // 5. Width/Height of rendersurface            

            var allRenderableSeries = scs.RenderableSeries;
            int renderableSeriesCount = allRenderableSeries != null ? allRenderableSeries.Count : 0;
            var renderPassInfo = new RenderPassInfo {ViewportSize = viewportSize, Warnings = new List<string>()};

            if (Math.Abs(renderPassInfo.ViewportSize.Width) < double.Epsilon ||
                Math.Abs(renderPassInfo.ViewportSize.Height) < double.Epsilon)
                return default(RenderPassInfo);

            UltrachartDebugLogger.Instance.WriteLine("Drawing {0}: Width={1}, Height={2}",
                scs.GetType().Name, renderPassInfo.ViewportSize.Width, renderPassInfo.ViewportSize.Height);

            renderPassInfo.RenderableSeries = new IRenderableSeries[renderableSeriesCount];
            renderPassInfo.PointSeries = new IPointSeries[renderableSeriesCount];
            renderPassInfo.DataSeries = new IDataSeries[renderableSeriesCount];
            renderPassInfo.IndicesRanges = new IndexRange[renderableSeriesCount];

            var resamplerFactory = scs.Services.GetService<IPointResamplerFactory>();
            Guard.NotNull(resamplerFactory, "resamplerFactory");

            // Handles AutoRange=Always for X axes,
            // need to be calculated before series will be resampled
            PrepareXAxes(scs);

            // Non-FrameworkElement Proxy required to access VisibleRange, IsCategoryAxis properties in background thread
            //var axisProxyCollection = new AxisCollection(scs.XAxes.Select(x => new AxisProxy(x)));
            //var renderSeriesProxyCollection = allRenderableSeries.Select(x => (IRenderableSeries)new RenderSeriesProxy(x)).ToArray();

            for (int i = 0; i < renderableSeriesCount; i++)
            //MultiThreaded.For(0, renderableSeriesCount, (i) =>
            {
                var renderableSeries = allRenderableSeries[i];

                IndexRange pointRange;
                IPointSeries resampledSeries;
                IDataSeries dataSeries;

                ResampleSeries(scs.XAxes, renderableSeries, renderPassInfo, resamplerFactory, out dataSeries, out pointRange, out resampledSeries);

                renderPassInfo.RenderableSeries[i] = allRenderableSeries[i];
                renderPassInfo.DataSeries[i] = dataSeries;
                renderPassInfo.IndicesRanges[i] = pointRange;
                renderPassInfo.PointSeries[i] = resampledSeries;
            }
            //);

            PrepareXAxes(scs, renderPassInfo);
            PrepareYAxes(scs, renderPassInfo);

            renderPassInfo.XCoordinateCalculators = new Dictionary<string, ICoordinateCalculator<double>>();
            renderPassInfo.YCoordinateCalculators = new Dictionary<string, ICoordinateCalculator<double>>();

            scs.YAxes.ForEachDo(y => renderPassInfo.YCoordinateCalculators.Add(y.Id, y.GetCurrentCoordinateCalculator()));
            scs.XAxes.ForEachDo(x => renderPassInfo.XCoordinateCalculators.Add(x.Id, x.GetCurrentCoordinateCalculator()));

            var strategyManager = scs.Services.GetService<IStrategyManager>();
            renderPassInfo.TransformationStrategy = strategyManager.GetTransformationStrategy();
            
            return renderPassInfo;
        }

        private static void ResampleSeries(
            AxisCollection xAxisCollection, 
            IRenderableSeries renderableSeries, 
            RenderPassInfo renderPassInfo, 
            IPointResamplerFactory resamplerFactory, 
            out IDataSeries dataSeries, 
            out IndexRange pointRange, 
            out IPointSeries resampledSeries)
        {
            pointRange = null;
            resampledSeries = null;
            dataSeries = renderableSeries.DataSeries;

            if (dataSeries != null && renderableSeries.IsVisible)
            {
                var xAxis = xAxisCollection.GetAxisById(renderableSeries.XAxisId, true);

                // Throws if Axis type is not correct for DataSeries.XType
                xAxis.AssertDataType(dataSeries.XType);

                var xAxisRange = xAxis.VisibleRange;
                pointRange = dataSeries.GetIndicesRange(xAxisRange);

                // PointRange is indefinite if the series doesn't have points inside the range
                if (pointRange.IsDefined)
                {
                    var displayDataAs2D = renderableSeries.DisplaysDataAsXy;
                    var isCategoryAxis = xAxis.IsCategoryAxis;

                    // Expand PointRange because of offsets in coordinate calculators,
                    // to provide correct rendering for continuous series on the edges
                    if (isCategoryAxis)
                    {
                        pointRange.Min = Math.Max(pointRange.Min - 1, 0);
                        pointRange.Max = Math.Min(pointRange.Max + 1, dataSeries.Count - 1);
                    }

                    pointRange = renderableSeries.GetExtendedXRange(pointRange);
                    pointRange = new IndexRange(Math.Max(0, pointRange.Min), Math.Min(dataSeries.Count - 1, pointRange.Max));

                    resampledSeries = dataSeries.ToPointSeries(renderableSeries.ResamplingMode, pointRange,
                        (int) renderPassInfo.ViewportSize.Width, isCategoryAxis,
                        displayDataAs2D,
                        xAxisRange,
                        resamplerFactory);
                }
            }
        }

        internal static void PrepareXAxes(IUltrachartSurface scs)
        {
            foreach (var xAxis in scs.XAxes)
            {
                using (var s0 = xAxis.SuspendUpdates())
                {
                    s0.ResumeTargetOnDispose = false;
                    var newRange = scs.ViewportManager.CalculateNewXAxisRange(xAxis);

                    if (!newRange.Equals(xAxis.VisibleRange) && xAxis.IsValidRange(newRange))
                    {
                        xAxis.VisibleRange = newRange;
                    }
                }
            }
        }

        internal static void PrepareXAxes(IUltrachartSurface scs, RenderPassInfo rpi)
        {
            foreach (var xAxis in scs.XAxes.Where(x => x != null)) {
                var firstPointSeries = GetFirstPointSeries(scs, rpi, scs.RenderableSeries.FirstOrDefault(x => x.XAxisId == xAxis.Id && x.IsVisible));

                xAxis.OnBeginRenderPass(rpi, firstPointSeries);
            }
        }

        private static void PrepareYAxes(IUltrachartSurface scs, RenderPassInfo renderPassInfo)
        {
            foreach (var yAxis in scs.YAxes)
            {
                using (var s0 = yAxis.SuspendUpdates())
                {
                    s0.ResumeTargetOnDispose = false;

                    var newYRange = scs.ViewportManager.CalculateNewYAxisRange(yAxis, renderPassInfo);
                    if (!newYRange.Equals(yAxis.VisibleRange) && yAxis.IsValidRange(newYRange))
                    {
                        yAxis.VisibleRange = newYRange;
                    }

                    var firstPointSeries = GetFirstPointSeries(scs, renderPassInfo, scs.RenderableSeries.FirstOrDefault(x => x.YAxisId == yAxis.Id && x.IsVisible));

                    yAxis.OnBeginRenderPass(renderPassInfo, firstPointSeries);
                }
            }
        }

        static IPointSeries GetFirstPointSeries(IUltrachartSurface scs, RenderPassInfo rpi, IRenderableSeries renderableSeries) {
            if(rpi.PointSeries.IsNullOrEmpty() || renderableSeries == null)
                return null;

            var dataSeries = rpi.DataSeries;

            for(var i = 0; i < dataSeries.Length; i++) {
                // Try to get the first visible RenderableSeries where its DataSeries is the first in DataSet
                // This is the "base" series used to calculate categories in CategoryDateTimeAxis
                if(rpi.RenderableSeries[i].IsVisible && dataSeries[i] == renderableSeries.DataSeries)
                    return rpi.PointSeries[i];
            }

            return null;
        }

        /// <summary>
        /// Step 3, draw axes
        /// </summary>
        internal static void OnDrawAxes(IUltrachartSurface scs, RenderPassInfo rpi, IRenderContext2D renderContext)
        {
            foreach (var xAxis in scs.XAxes)
            {
                xAxis.ValidateAxis();
                xAxis.OnDraw(renderContext, null);
            }

            foreach (var yAxis in scs.YAxes)
            {
                yAxis.ValidateAxis();
                yAxis.OnDraw(renderContext, null);
            }

            // Flush layers which have been drawn out of order, but require Z-order to be preserved
            renderContext.Layers.Flush();
        }

        /// <summary>
        /// Step 4, draw series
        /// </summary>
        internal static void OnDrawSeries(IUltrachartSurface scs, RenderPassInfo rpi, IRenderContext2D renderContext)
        {
            if (rpi.RenderableSeries == null)
            {
                return;
            }

            var selectedSeriesIndexes = new List<int>();
            for (int i = 0; i < rpi.RenderableSeries.Length; i++)
            {
                var rSeries = rpi.RenderableSeries[i];

                if (rSeries != null)
                {
                    rSeries.XAxis = scs.XAxes.GetAxisById(rSeries.XAxisId, true);
                    rSeries.YAxis = scs.YAxes.GetAxisById(rSeries.YAxisId, true);

                    if (rpi.RenderableSeries[i].IsSelected)
                    {
                        selectedSeriesIndexes.Add(i);
                    }
                    else
                    {
                        //Firstly draw unselected series
                        DrawSeries(scs, rpi, renderContext, i);
                    }
                }
            }

            //Draw selected series after another
            foreach (var index in selectedSeriesIndexes)
            {
                DrawSeries(scs, rpi, renderContext, index);
            }

            // Publish render event after the above
            scs.Services.GetService<IEventAggregator>().Publish(new UltrachartRenderedMessage(scs, renderContext));
        }

        private static void DrawSeries(IUltrachartSurface scs, RenderPassInfo rpi, IRenderContext2D renderContext, int seriesIndex)
        {
            var renderableSeries = rpi.RenderableSeries[seriesIndex];

            ICoordinateCalculator<double> xCoordinateCalculator, yCoordinateCalculator;
            if (rpi.YCoordinateCalculators.TryGetValue(renderableSeries.YAxisId, out yCoordinateCalculator) &&
                rpi.XCoordinateCalculators.TryGetValue(renderableSeries.XAxisId, out xCoordinateCalculator))
            {
                var renderPassData = new RenderPassData(
                    rpi.IndicesRanges[seriesIndex],
                    xCoordinateCalculator,
                    yCoordinateCalculator,
                    rpi.PointSeries[seriesIndex], 
                    rpi.TransformationStrategy);

                renderableSeries.OnDraw(renderContext, renderPassData);
            }
        }

        /// <summary>
        /// Step 5, draw annotations
        /// </summary>
        internal static void OnDrawAnnotations(UltrachartSurface scs, RenderPassInfo rpi)
        {
            if (scs.Annotations != null)
            {
                scs.Annotations.RefreshPositions(rpi);
            }
        }
    }
}