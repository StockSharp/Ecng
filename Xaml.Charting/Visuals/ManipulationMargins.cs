// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ManipulationMargins.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    /// <summary>
    /// Contains properties related to mouse and touch manipulation precision.
    /// </summary>
    public static class ManipulationMargins
    {
        private static double _annotationResizingThumbSize = 20;

        /// <summary>
        /// Defines size of thumbs on the corners of annotations, which serve for annotation resizing.
        /// </summary>
        public static double AnnotationResizingThumbSize
        {
            get { return _annotationResizingThumbSize; }
            set { _annotationResizingThumbSize = value; }
        }

        private static double _annotationLineWidth = 11;

        /// <summary>
        /// Defines width of ghost line around annotation line that can be dragged to move the annotation line.
        /// </summary>
        public static double AnnotationLineWidth
        {
            get { return _annotationLineWidth; }
            set { _annotationLineWidth = value; }
        }
    }
}