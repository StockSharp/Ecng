// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DashSplitter.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using Ecng.Xaml.Charting.Rendering.Common;

namespace Ecng.Xaml.Charting.Common.Extensions
{
    internal sealed class DashSplitter : IEnumerator<LineD>
    {
        private bool _hasDashes;

        private bool _isInViewport;

        private readonly LengthSplitter lengthSplitter = new LengthSplitter();

        private double _dx;

        private double _dy;

        private double _length;

        private int _index;

        private Point _pt1;

        private Point _pt2;

        private LineD _current;
        private double _oneOverLength;

        internal DashSplitter(Point pt1, Point pt2, Size viewportSize, IDashSplittingContext dashSplittingContext)
        {
            this.Reset(pt1, pt2, viewportSize, dashSplittingContext);
        }

        internal DashSplitter()
        {
            this._index = -1;
        }

        public LineD Current
        {
            get { return this._current; }
        }

        public void Dispose()
        {
        }

        object IEnumerator.Current
        {
            get { return this._current; }
        }

        public bool MoveNext()
        {
            if (this._hasDashes)
            {
                if (this.lengthSplitter.MoveNext())
                {
                    var segment = this.lengthSplitter.Current;
                    var x1 = this._pt1.X + segment.Start * this._oneOverLength * this._dx;
                    var y1 = this._pt1.Y + segment.Start * this._oneOverLength * this._dy;
                    var x2 = this._pt1.X + segment.End * this._oneOverLength * this._dx;
                    var y2 = this._pt1.Y + segment.End * this._oneOverLength * this._dy;
                    this._current = new LineD(x1, y1, x2, y2);

                    return true;
                }

                return false;
            }
            else
            {
                if (_index < 0)
                {
                    _index++;
                    this._current = new LineD(_pt1.X, _pt1.Y, _pt2.X, _pt2.Y);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        internal void Reset(Point pt1, Point pt2, Size viewportSize, IDashSplittingContext dashSplittingContext)
        {
            this._hasDashes = dashSplittingContext.HasDashes;
            this._pt1 = pt1;
            this._pt2 = pt2;
            var viewportRect = new Rect(0, 0, viewportSize.Width, viewportSize.Height);

            if (_hasDashes)
            {
                // Clip line to viewport rect to compute dashes for visible part only
                var pt1X = pt1.X; var pt1Y = pt1.Y; var pt2X = pt2.X; var pt2Y = pt2.Y;
                this._isInViewport = WriteableBitmapExtensions.CohenSutherlandLineClip(viewportRect, ref pt1X, ref pt1Y,
                                                                                     ref pt2X, ref pt2Y);

                this._dx = pt2X - pt1X;
                this._dy = pt2Y - pt1Y;
                this._length = (float)Math.Sqrt(_dx * _dx + _dy * _dy);
                this._oneOverLength = 1.0/_length;

                this.lengthSplitter.Reset(_length, dashSplittingContext);

            }
            else
            {
                this._isInViewport = true;
            }

            this._index = -1;

        }

        /// <summary>
        /// This is the decompiled enumerator for SplitLengthIntoDashes, with an added Reset method.
        /// TODO: Needs cleanup.
        /// </summary>
        private sealed class LengthSplitter : IEnumerator<DashLength>, IEnumerator, IDisposable
        {
            public double length;
            public IDashSplittingContext dashSplittingContext;

            private DashLength _current;
            private int _state;

            private double passedLength;
            private double remainingLengthToPass;
            private double itemLength;
            private double itemPassedLength;
            private double remainingItemLength;
            private bool itemFlag;
            private double passedLength0;
            private double passedLength0_2;

            public DashLength Current
            {
                get
                {
                    return this._current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return this._current;
                }
            }

            public bool MoveNext()
            {
                switch (this._state)
                {
                    case 0:
                        this._state = -1;
                        this.passedLength = 0f;
                        this.remainingLengthToPass = this.length;
                        goto IL_1EE;
                    case 1:
                        this._state = -1;
                        break;
                    case 2:
                        this._state = -1;
                        goto IL_1EB;
                    default:
                        goto IL_1F6;
                }
            IL_129:
                this.dashSplittingContext.StrokeDashArrayIndex++;
                this.dashSplittingContext.StrokeDashArrayItemPassedLength = 0f;
                if (this.dashSplittingContext.StrokeDashArrayIndex >= this.dashSplittingContext.StrokeDashArray.Length)
                {
                    this.dashSplittingContext.StrokeDashArrayIndex = 0;
                }
                goto IL_1EE;
            IL_1EB:
                goto IL_1F6;
            IL_1EE:
                this.itemLength = this.dashSplittingContext.StrokeDashArray[this.dashSplittingContext.StrokeDashArrayIndex];
                this.itemPassedLength = this.dashSplittingContext.StrokeDashArrayItemPassedLength;
                this.remainingItemLength = this.itemLength - this.itemPassedLength;
                this.itemFlag = (this.dashSplittingContext.StrokeDashArrayIndex % 2 == 0);
                bool result;
                if (this.remainingLengthToPass >= this.remainingItemLength)
                {
                    this.passedLength0 = this.passedLength;
                    this.passedLength += this.remainingItemLength;
                    this.remainingLengthToPass -= this.remainingItemLength;
                    if (this.itemFlag)
                    {
                        this._current = new DashLength(this.passedLength0, this.passedLength);
                        this._state = 1;
                        result = true;
                        return result;
                    }
                    goto IL_129;
                }
                else
                {
                    this.passedLength0_2 = this.passedLength;
                    this.passedLength += this.remainingLengthToPass;
                    this.dashSplittingContext.StrokeDashArrayItemPassedLength += this.remainingLengthToPass;
                    if (this.itemFlag)
                    {
                        this._current = new DashLength(this.passedLength0_2, this.passedLength);
                        this._state = 2;
                        result = true;
                        return result;
                    }
                }
            IL_1F6:
                result = false;
                return result;
            }

            public void Reset(double length, IDashSplittingContext dashSplittingContext)
            {
                this.dashSplittingContext = dashSplittingContext;
                this.dashSplittingContext.StrokeDashArrayIndex = 0;
                this.dashSplittingContext.StrokeDashArrayItemPassedLength = 0f;
                this.length = length;
                this._state = 0;

            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }

            public LengthSplitter()
            {
                this._state = 0;
            }
        }

        internal struct DashLength
        {
            internal readonly double Start;
            internal readonly double End;

            internal DashLength(double start, double end)
            {
                this.Start = start;
                this.End = end;
            }
        }
    }

    internal struct LineD
    {
        internal readonly double X1;
        internal readonly double Y1;
        internal readonly double X2;
        internal readonly double Y2;

        internal LineD(double x1, double y1, double x2, double y2)
        {
            this.X1 = x1;
            this.Y1 = y1;
            this.X2 = x2;
            this.Y2 = y2;
        }
    }
}
