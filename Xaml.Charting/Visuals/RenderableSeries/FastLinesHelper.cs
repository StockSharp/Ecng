using System;
using System.Collections.Generic;
using System.Windows;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Rendering.Common;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    internal static class FastPointsHelper
    {
        internal static void IteratePoints(IPathContextFactory lineContextFactory, Func<double, double, IPathColor> createPenFunc, IPointSeries pointSeries, ICoordinateCalculator<double> xCalc, ICoordinateCalculator<double> yCalc)
        {
            IteratePoints(lineContextFactory, createPenFunc, pointSeries.XValues.ItemsArray, pointSeries.YValues.ItemsArray, pointSeries.Count, xCalc, yCalc);
        }

        public static void IteratePoints(IPathContextFactory pathFactory, IPointSeries pointSeries, ICoordinateCalculator<double> xCalc, ICoordinateCalculator<double> yCalc)
        {
            IteratePoints(pathFactory, (x, y) => null, pointSeries.XValues.ItemsArray, pointSeries.YValues.ItemsArray, pointSeries.Count, xCalc, yCalc);
        }

        private static void IteratePoints(IPathContextFactory pathContextFactory, Func<double, double, IPathColor> createPenFunc, double[] xValues, double[] yValues, int count, ICoordinateCalculator<double> xCalc, ICoordinateCalculator<double> yCalc)
        {
            bool isVerticalChart = !xCalc.IsHorizontalAxisCalculator;

            // Setup variables for first point
            double lastX = double.NaN, x, lastY = double.NaN, y, xCoord, yCoord, xTemp;
            IPathDrawingContext pathContext = null;
            x = xValues[0];
            y = yValues[0];
            var lastPen = createPenFunc(x, y);

            xCoord = xCalc.GetCoordinate(x);
            yCoord = yCalc.GetCoordinate(y);

            // Vertical chart case, flip the X,Y coordinates
            xTemp = isVerticalChart ? yCoord : xCoord;
            yCoord = isVerticalChart ? xCoord : yCoord;
            xCoord = xTemp;

            // Case where first point is not NaN, begin the line 
            if (!double.IsNaN(y))
            {
                pathContext = pathContextFactory.Begin(lastPen, xCoord, yCoord);
            }

            // Iterate points 1 to N
            for (int i = 1; i < count; i++)
            {
                x = xValues[i];
                y = yValues[i];

                // If the Y value is NaN, implement closed lines or gaps
                if (double.IsNaN(y))
                {
                    continue;
                }

                // Stop and start line segment when pens change
                var currentPen = createPenFunc(x, y);
                if (currentPen != lastPen)
                {
                    lastPen.Dispose();
                    if (pathContext != null) pathContext.End();
                    pathContext = null;
                    lastPen = currentPen;
                }                

                xCoord = xCalc.GetCoordinate(x);
                yCoord = yCalc.GetCoordinate(y);

                xTemp = isVerticalChart ? yCoord : xCoord;
                yCoord = isVerticalChart ? xCoord : yCoord;
                xCoord = xTemp;

                // Restart drawing if current pathContext ended
                if (pathContext == null)
                {
                    // Begin the next line segment after a NaN gap (or run of NaN points at the start of the line) has finished
                    pathContext = pathContextFactory.Begin(lastPen, xCoord, yCoord);
                    continue;
                }

                // Draws next point in line segment
                pathContext.MoveTo(xCoord, yCoord);
            }

            if (pathContext != null)
                pathContext.End();
        }
    }

    /// <summary>
    /// Very fast implementation of DrawLines(IEnumerable)
    ///  - handles NaN gaps/closed, vertical/horizontal chart, Digital or standard line
    ///  - NOTE: Dashed line is handled inside the RenderContext. Dashes are performed per line-segment
    /// </summary>   
    internal static class FastLinesHelper
    {                  
        internal static void IterateLines(IPathContextFactory lineContextFactory, IPointSeries pointSeries,  ICoordinateCalculator<double> xCalc, ICoordinateCalculator<double> yCalc, bool isDigitalLine)
        {
            IterateLines(lineContextFactory, (x,y) => null, pointSeries.XValues.ItemsArray, pointSeries.YValues.ItemsArray, pointSeries.Count, xCalc, yCalc, isDigitalLine, true);
        }      
        
        internal static void IterateLines(IPathContextFactory lineContextFactory, IPen2D pen, IPointSeries pointSeries,  ICoordinateCalculator<double> xCalc, ICoordinateCalculator<double> yCalc, bool isDigitalLine, bool closeGaps)
        {
            if (pen.StrokeThickness > 0 && pen.Color.A != 0)
            {
                IterateLines(lineContextFactory, (x,y) => pen, pointSeries.XValues.ItemsArray, pointSeries.YValues.ItemsArray, pointSeries.Count, xCalc, yCalc, isDigitalLine, closeGaps);
            }
        }

        internal static void IterateLines(IPathContextFactory lineContextFactory, IBrush2D brush, IPointSeries pointSeries, ICoordinateCalculator<double> xCalc, ICoordinateCalculator<double> yCalc, bool isDigitalLine, bool closeGaps)
        {
            IterateLines(lineContextFactory, (x, y) => brush, pointSeries.XValues.ItemsArray, pointSeries.YValues.ItemsArray, pointSeries.Count, xCalc, yCalc, isDigitalLine, closeGaps);
        }

        internal static void IterateLines(IPathContextFactory lineContextFactory, Func<double, double, IPathColor> createPenFunc, IPointSeries pointSeries, ICoordinateCalculator<double> xCalc, ICoordinateCalculator<double> yCalc, bool isDigitalLine, bool closeGaps)
        {
            IterateLines(lineContextFactory, createPenFunc, pointSeries.XValues.ItemsArray, pointSeries.YValues.ItemsArray, pointSeries.Count, xCalc, yCalc, isDigitalLine, closeGaps);
        }

        private static void IterateLines(IPathContextFactory pathContextFactory, Func<double, double, IPathColor> createPenFunc, double[] xValues, double[] yValues, int count, ICoordinateCalculator<double> xCalc, ICoordinateCalculator<double> yCalc, bool isDigitalLine, bool closeGaps)
        {
            bool isVerticalChart = !xCalc.IsHorizontalAxisCalculator;               

            // Setup variables for first point
            double lastX = double.NaN, x, lastY = double.NaN, y, xCoord, yCoord, lastYCoord, lastXCoord, xTemp;
            IPathDrawingContext pathContext = null;
            x = xValues[0];
            y = yValues[0];
            var lastPen = createPenFunc(x, y);

            xCoord = xCalc.GetCoordinate(x);
            yCoord = yCalc.GetCoordinate(y);
            
            // Vertical chart case, flip the X,Y coordinates
            xTemp = isVerticalChart ? yCoord : xCoord;
            yCoord = isVerticalChart ? xCoord : yCoord;
            xCoord = xTemp;

            lastX = x;
            lastY = y;
            lastYCoord = yCoord;
            lastXCoord = xCoord;

            // Case where first point is not NaN, begin the line 
            if (!(y != y))
            {
                pathContext = pathContextFactory.Begin(lastPen, xCoord, yCoord);                
            }

            // Iterate points 1 to N
            for (int i = 1; i < count; i++)
            {
                x = xValues[i];
                y = yValues[i];

                // If the Y value is NaN, implement closed lines or gaps
                if (y != y)
                {
                    if (pathContext != null)
                    {
                        // Complete the line when NaN encountered 
                        pathContext.End();
                        pathContext = null;
                    }

                    // When close gaps, we store the lastX,Y before the NaN gap until the next non-NaN point is found
                    // Else, we store double.NaN, which is used to skip the first non-NaN point once non-NaN values have restarted
                    lastX = closeGaps ? lastX : double.NaN;
                    lastY = closeGaps ? lastY : double.NaN;
                    
                    continue;
                }

                // Stop and start line segment when pens change
                var currentPen = createPenFunc(x, y);
                if (currentPen != lastPen)
                {
                    lastPen.Dispose();
                    if (pathContext != null) pathContext.End();
                    pathContext = null;
                    lastPen = currentPen;
                }

                // Restart Line segment if current ended
                if (pathContext == null)
                {
                    xCoord = xCalc.GetCoordinate(lastX);
                    yCoord = yCalc.GetCoordinate(lastY);

                    // Vertical chart case, flip the X,Y coordinates
                    xTemp = isVerticalChart ? yCoord : xCoord;
                    yCoord = isVerticalChart ? xCoord : yCoord;
                    xCoord = xTemp;
                    
                    lastYCoord = yCoord;
                    lastXCoord = xCoord;

                    if (lastY != lastY)
                    {
                        // Skip one point in case where lastY is still NaN, occurs when first point is NaN or CloseGaps is false
                        lastX = x;
                        lastY = y;

                        continue;
                    }

                    // Begin the next line segment after a NaN gap (or run of NaN points at the start of the line) has finished
                    pathContext = pathContextFactory.Begin(lastPen, xCoord, yCoord);
                }
                
                xCoord = xCalc.GetCoordinate(x);
                yCoord = yCalc.GetCoordinate(y);

                xTemp = isVerticalChart ? yCoord : xCoord;
                yCoord = isVerticalChart ? xCoord : yCoord;
                xCoord = xTemp;

                // Draws next point in line segment
                if (isDigitalLine)
                {
                    if (isVerticalChart)
                    {
                        pathContext.MoveTo(lastXCoord, yCoord);
                        pathContext.MoveTo(xCoord, yCoord);
                    }
                    else
                    {
                        pathContext.MoveTo(xCoord, lastYCoord);
                        pathContext.MoveTo(xCoord, yCoord);
                    }
                }
                else
                {
                    pathContext.MoveTo(xCoord, yCoord);
                }

                lastX = x;
                lastY = y;
                lastYCoord = yCoord;
                lastXCoord = xCoord;
            }

            if (pathContext != null)
                pathContext.End();
        }
    }
}