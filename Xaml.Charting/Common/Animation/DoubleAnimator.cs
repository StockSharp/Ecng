// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DoubleAnimator.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using System.Windows.Media.Animation;

namespace Ecng.Xaml.Charting.Common.Animation
{
    internal class DoubleAnimator
    {
        private TimeSpan duration;
        private UIElement target;
        private string targetProperty;
        private double @from;
        private double to;
        private EventHandler handler;

        public DoubleAnimator WithTarget(UIElement target)
        {
            this.target = target;
            return this;
        }

        public DoubleAnimator WithFromTo(double from, double to)
        {
            this.from = from;
            this.to = to;
            return this;
        }

        public DoubleAnimator WithTargetProperty(string targetProperty)
        {
            this.targetProperty = targetProperty;
            return this;
        }

        public DoubleAnimator WithDuration(TimeSpan duration)
        {
            this.duration = duration;
            return this;
        }

        public DoubleAnimator WithCompletedHandler(EventHandler handler)
        {
            this.handler = handler;
            return this;
        }

        public void Go()
        {
            var startKeyFrame = new SplineDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = this.from };
            var endKeyFrame = new SplineDoubleKeyFrame
            {
                KeyTime = KeyTime.FromTimeSpan(duration),
                KeySpline = new KeySpline
                {
                    ControlPoint1 = new Point(0.73, 0.14),
                    ControlPoint2 = new Point(0.1, 1)
                },
                Value = this.to,
            };

            var animation = new DoubleAnimationUsingKeyFrames() { KeyFrames = { startKeyFrame, endKeyFrame } };
            Storyboard.SetTarget(animation, this.target);
            Storyboard.SetTargetProperty(animation, new PropertyPath(this.targetProperty));

            if(this.handler!=null)
                animation.Completed += this.handler;

            var storyboard = new Storyboard { Children = { animation } };
            storyboard.Begin();
        }
    }
}
