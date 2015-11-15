// *************************************************************************************
// ULTRACHART © Copyright ulc software Services Ltd. 2011-2012. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: info@ulcsoftware.co.uk
//  
// RenderPassData.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
namespace Ecng.Xaml.Charting.Visuals
{
    public struct RenderPassData
    {
        private readonly DoubleRange _dataXRange;
        private readonly IRange _xVisibleRangeAligned;
        private readonly IRange _yVisibleRange;
        private readonly IntegerRange _dataPointRange;

        public RenderPassData(DoubleRange dataXRange, IRange xVisibleRangeAligned, IRange yVisibleRange, IntegerRange dataPointRange)
        {
            _dataXRange = dataXRange;
            _xVisibleRangeAligned = xVisibleRangeAligned;
            _yVisibleRange = yVisibleRange;
            _dataPointRange = dataPointRange;
        }

        public IntegerRange DataPointRange
        {
            get { return _dataPointRange; }
        }

        public IRange YVisibleRange
        {
            get { return _yVisibleRange; }
        }

        public IRange XVisibleRangeAligned
        {
            get { return _xVisibleRangeAligned; }
        }

        public DoubleRange DataXRange
        {
            get { return _dataXRange; }
        }
    }
}
