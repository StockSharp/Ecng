// *************************************************************************************
// ULTRACHART © Copyright ulc software Services Ltd. 2011-2012. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: info@ulcsoftware.co.uk
//  
// SmaaAntiAliasEffect.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Media;
using System.Windows.Media.Effects;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Effects
{
    public class SmaaAntiAliasEffect : ShaderEffect
    {
        static SmaaAntiAliasEffect()
        {
            _pixelShader.UriSource = UriUtil.MakePackUri("SmaaAntiAliasEffect.ps");
        }

        public SmaaAntiAliasEffect()
        {
            this.PixelShader = _pixelShader;

            // Update each DependencyProperty that's registered with a shader register.  This
            // is needed to ensure the shader gets sent the proper default value.
            UpdateShaderValue(InputProperty);
            UpdateShaderValue(ColorFilterProperty);
        }

        public Brush Input
        {
            get { return (Brush)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }

        // Brush-valued properties turn into sampler-property in the shader.
        // This helper sets "ImplicitInput" as the default, meaning the default
        // sampler is whatever the rendering of the element it's being applied to is.
        public static readonly DependencyProperty InputProperty =
            ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(SmaaAntiAliasEffect), 0);



        public Color ColorFilter
        {
            get { return (Color)GetValue(ColorFilterProperty); }
            set { SetValue(ColorFilterProperty, value); }
        }

        // Scalar-valued properties turn into shader constants with the register
        // number sent into PixelShaderConstantCallback().
        public static readonly DependencyProperty ColorFilterProperty =
            DependencyProperty.Register("ColorFilter", typeof(Color), typeof(SmaaAntiAliasEffect),
                    new UIPropertyMetadata(Colors.Yellow, PixelShaderConstantCallback(0)));

        private static PixelShader _pixelShader = new PixelShader();
    }
}
