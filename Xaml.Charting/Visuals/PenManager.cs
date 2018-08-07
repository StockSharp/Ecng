// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// PenManager.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Media;
using Ecng.Xaml.Charting.Rendering.Common;

namespace Ecng.Xaml.Charting.Visuals
{
    internal class PenManager : IPenManager
    {
        private readonly IRenderContext2D _renderContext;
        private readonly bool _antiAliasing;
        private readonly float _strokeThickness;
        private readonly double _opacity;
        private readonly Dictionary<Color, IPen2D> _renderPens;
        private readonly Dictionary<Color, IBrush2D> _renderBrushes;
        private readonly Dictionary<Brush, IBrush2D> _textureBrushes;
        private readonly double[] _strokeDashArray;

        public PenManager(IRenderContext2D renderContext, bool antiAliasing, float strokeThickness, double opacity, double[] strokeDashArray = null)
        {
            _renderContext = renderContext;
            _antiAliasing = antiAliasing;
            _strokeThickness = strokeThickness;
            _opacity = opacity;
            _renderPens = new Dictionary<Color, IPen2D>();
            _renderBrushes = new Dictionary<Color, IBrush2D>();
            _textureBrushes = new Dictionary<Brush, IBrush2D>();
            _strokeDashArray = strokeDashArray;
        }

        public IPen2D GetPen(Color color)
        {
            IPen2D pen;
            if (_renderPens.TryGetValue(color, out pen))
                return pen;

            pen = _renderContext.CreatePen(color, _antiAliasing, _strokeThickness, _opacity, _strokeDashArray);
            _renderPens.Add(color, pen);
            return pen;
        }

        public IBrush2D GetBrush(Color color)
        {
            IBrush2D brush;
            if (_renderBrushes.TryGetValue(color, out brush))
                return brush;

            brush = _renderContext.CreateBrush(color, _opacity);
            _renderBrushes.Add(color, brush);
            return brush;
        }

        public IBrush2D GetBrush(Brush fromBrush)
        {
            IBrush2D brush;
            if (_textureBrushes.TryGetValue(fromBrush, out brush))
                return brush;

            brush = _renderContext.CreateBrush(fromBrush, 1, TextureMappingMode.PerPrimitive);
            _textureBrushes.Add(fromBrush, brush);
            return brush;
        }

        public void Dispose()
        {
            foreach (var pen in _renderPens.Values)
            {
                pen.Dispose();
            }
            foreach (var pen in _renderBrushes.Values)
            {
                pen.Dispose();
            }
            foreach(var pen in _textureBrushes.Values)
            {
                pen.Dispose();
            }
            _renderPens.Clear();
            _renderBrushes.Clear();
        }
    }
}
