// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IRenderableSeriesExtension.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Common.Extensions
{
    internal static class RenderableSeriesExtension
    {
        internal static DependencyProperty SeriesStyleProperty = DependencyProperty.RegisterAttached("SeriesStyle",
                                                                                                     typeof (Style),
                                                                                                     typeof (
                                                                                                         RenderableSeriesExtension
                                                                                                         ),
                                                                                                     new PropertyMetadata
                                                                                                         (null));

        internal static Style GetSeriesStyle(DependencyObject o)
        {
            return (Style)o.GetValue(SeriesStyleProperty);
        }

        internal static void SetSeriesStyle(DependencyObject o, Style value)
        {
            o.SetValue(SeriesStyleProperty, value);
        }

        internal static void SetStyle(this BaseRenderableSeries series, Style style)
        {
            if (style != null)
            {
                var oldStyle = new Style { TargetType = typeof(BaseRenderableSeries) };

                foreach (var setter in style.Setters.OfType<Setter>())
                {
                    var prop = setter.Property;
                    var currValue = series.GetValue(prop);

#if SILVERLIGHT
                    if (setter.Value is String)
                    {
                        if (currValue is Brush || currValue is Color)
                        {
                            var brush = BrushExtensions.GetBrushFromColorName((string)setter.Value);

                            oldStyle.Setters.Add(new Setter(prop, currValue.ToString()));

                            if (currValue is Brush)
                            {
                                series.SetCurrentValue(prop, brush);
                            }
                            else
                            {
                                series.SetCurrentValue(prop, brush.Color);
                            }
                        }
                        else
                        {
                            var parseMethod = currValue.GetType().GetMethod("Parse", new[] { typeof(String) });

                            oldStyle.Setters.Add(new Setter(prop, currValue.ToString()));
                            series.SetCurrentValue(prop, parseMethod.Invoke(currValue, new[] { setter.Value }));
                        }
                    }
                    else
                    {
#endif
                    oldStyle.Setters.Add(new Setter(prop, currValue));
                    series.SetCurrentValue(prop, setter.Value);
#if SILVERLIGHT
                    }
#endif
                }

                SetSeriesStyle(series, oldStyle);
            }
        }

        internal static bool HasDigitalLine(this IRenderableSeries rSeries)
        {
            return (rSeries is FastLineRenderableSeries &&
                    ((FastLineRenderableSeries) rSeries).IsDigitalLine) ||
                   (rSeries is FastMountainRenderableSeries &&
                    ((FastMountainRenderableSeries) rSeries).IsDigitalLine) ||
                   (rSeries is FastBandRenderableSeries &&
                    ((FastBandRenderableSeries) rSeries).IsDigitalLine);
        }

        internal static SeriesInfo GetSeriesInfo(this IRenderableSeries rSeries, HitTestInfo hitTestInfo)
        {
            SeriesInfo seriesInfo;

            switch(hitTestInfo.DataSeriesType)
            {
                case DataSeriesType.Hlc:
                    seriesInfo = new HlcSeriesInfo(rSeries, hitTestInfo);
                    break;
                case DataSeriesType.Ohlc:
                    seriesInfo = new OhlcSeriesInfo(rSeries, hitTestInfo);
                    break;
                case DataSeriesType.Xyy:
                    seriesInfo = new BandSeriesInfo(rSeries, hitTestInfo);
                    break;
                case DataSeriesType.Xyz:
                    seriesInfo = new XyzSeriesInfo(rSeries, hitTestInfo);
                    break;
                case DataSeriesType.StackedXy:
                    seriesInfo = new XyStackedSeriesInfo(rSeries, hitTestInfo);
                    break;
                case DataSeriesType.Heatmap:
                    seriesInfo = new HeatmapSeriesInfo(rSeries, hitTestInfo);
                    break;              
                case DataSeriesType.OneHundredPercentStackedXy:
                    seriesInfo = new OneHundredPercentStackedSeriesInfo(rSeries, hitTestInfo);
                    break;
                default:
                    seriesInfo = new XySeriesInfo(rSeries, hitTestInfo);
                    break;
            }

            return seriesInfo;
        }

        /// <summary>
        /// Returns the color of a particular data point in the passed <see cref="IRenderableSeries"/> instance.
        /// </summary>
        internal static Color GetSeriesColorAtPoint(this IRenderableSeries rSeries, HitTestInfo hitTestInfo)
        {
            var paletteProvider = rSeries.PaletteProvider;
            var color = rSeries.SeriesColor;

            var candlesticks = rSeries as FastCandlestickRenderableSeries;
            var ohlc = rSeries as FastOhlcRenderableSeries;
            if (candlesticks != null || ohlc != null)
            {
                var upWickColor = candlesticks != null ? candlesticks.UpWickColor : ohlc.UpWickColor;
                var downWickColor = candlesticks != null ? candlesticks.DownWickColor : ohlc.DownWickColor;

                color = hitTestInfo.CloseValue.CompareTo(hitTestInfo.OpenValue) >= 0 ? upWickColor : downWickColor;
            }

            if (rSeries.PaletteProvider != null)
            {
                Color? overrideColor = null;

                if (hitTestInfo.DataSeriesType == DataSeriesType.Ohlc)
                {
                    overrideColor = paletteProvider.OverrideColor(rSeries,
                        hitTestInfo.XValue.ToDouble(),
                        hitTestInfo.OpenValue.ToDouble(),
                        hitTestInfo.HighValue.ToDouble(),
                        hitTestInfo.LowValue.ToDouble(),
                        hitTestInfo.CloseValue.ToDouble());
                }
                else if (hitTestInfo.DataSeriesType == DataSeriesType.Xyz)
                {
                    overrideColor = paletteProvider.OverrideColor(rSeries,
                        hitTestInfo.XValue.ToDouble(),
                        hitTestInfo.YValue.ToDouble(),
                        hitTestInfo.ZValue.ToDouble());
                }
                else
                {
                    overrideColor = paletteProvider.GetColor(rSeries, hitTestInfo.XValue.ToDouble(),
                        hitTestInfo.YValue.ToDouble());
                }

                color = overrideColor.HasValue ? overrideColor.Value : color;
            }

            return color;
        }
    }
}
