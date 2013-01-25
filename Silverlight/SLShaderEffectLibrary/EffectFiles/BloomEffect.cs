// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Permissive License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.


namespace ShaderEffectLibrary
{
    using System;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Effects;
#if SILVERLIGHT 
    using UIPropertyMetadata = System.Windows.PropertyMetadata ;      
#endif
    /// <summary>
    /// This is the implementation of an extensible framework ShaderEffect which loads
    /// a shader model 2 pixel shader. Dependecy properties declared in this class are mapped
    /// to registers as defined in the *.ps file being loaded below.
    /// </summary>
    public class BloomEffect : ShaderEffect
    {
        #region Dependency Properties

        /// <summary>
        /// The explict input for this pixel shader.
        /// </summary>
        public static readonly DependencyProperty InputProperty =
            ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(BloomEffect), 0);

        /// <summary>
        /// This property is mapped to the BloomIntensity variable within the pixel shader.
        /// </summary>
        public static readonly DependencyProperty BloomIntensityProperty =
            DependencyProperty.Register("BloomIntensity", typeof(double), typeof(BloomEffect), new UIPropertyMetadata(1.0, PixelShaderConstantCallback(0)));

        /// <summary>
        /// This property is mapped to the BaseIntensity variable within the pixel shader.
        /// </summary>
        public static readonly DependencyProperty BaseIntensityProperty =
            DependencyProperty.Register("BaseIntensity", typeof(double), typeof(BloomEffect), new UIPropertyMetadata(1.0, PixelShaderConstantCallback(1)));

        /// <summary>
        /// This property is mapped to the BloomSaturation variable within the pixel shader.
        /// </summary>
        public static readonly DependencyProperty BloomSaturationProperty =
            DependencyProperty.Register("BloomSaturation", typeof(double), typeof(BloomEffect), new UIPropertyMetadata(1.0, PixelShaderConstantCallback(2)));

        /// <summary>
        /// This property is mapped to the BaseSaturation variable within the pixel shader.
        /// </summary>
        public static readonly DependencyProperty BaseSaturationProperty =
            DependencyProperty.Register("BaseSaturation", typeof(double), typeof(BloomEffect), new UIPropertyMetadata(1.0, PixelShaderConstantCallback(3)));

        #endregion

        #region Member Data

        /// <summary>
        /// A refernce to the pixel shader used.
        /// </summary>
        private static PixelShader pixelShader = new PixelShader();

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an instance of the shader from the included pixel shader.
        /// </summary>
        static BloomEffect()
        {
            pixelShader.UriSource = Global.MakePackUri("ShaderSource/Bloom.ps");
        }

        /// <summary>
        /// Creates an instance and updates the shader's variables to the default values.
        /// </summary>
        public BloomEffect()
        {
            this.PixelShader = pixelShader;

            // Update each DependencyProperty that's registered with a shader register.  This
            // is needed to ensure the shader gets sent the proper default value.
            UpdateShaderValue(InputProperty);
            UpdateShaderValue(BloomIntensityProperty);
            UpdateShaderValue(BaseIntensityProperty);
            UpdateShaderValue(BloomSaturationProperty);
            UpdateShaderValue(BaseSaturationProperty);
        }

        #endregion

        // Brush-valued properties turn into sampler-property in the shader.
        // This helper sets "ImplicitInput" as the default, meaning the default
        // sampler is whatever the rendering of the element it's being applied to is.

        /// <summary>
        /// Gets or sets the Input shader sampler.
        /// </summary>
		[System.ComponentModel.BrowsableAttribute(false)]
        public Brush Input
        {
            get { return (Brush)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }

        /// <summary>
        /// Gets or sets the BloomIntensity variable within the shader.
        /// </summary>
        public double BloomIntensity
        {
            get { return (double)GetValue(BloomIntensityProperty); }
            set { SetValue(BloomIntensityProperty, value); }
        }

        /// <summary>
        /// Gets or sets the BaseIntensity variable within the shader.
        /// </summary>
        public double BaseIntensity
        {
            get { return (double)GetValue(BaseIntensityProperty); }
            set { SetValue(BaseIntensityProperty, value); }
        }

        /// <summary>
        /// Gets or sets the BloomSaturation variable within the shader.
        /// </summary>
        public double BloomSaturation
        {
            get { return (double)GetValue(BloomSaturationProperty); }
            set { SetValue(BloomSaturationProperty, value); }
        }

        /// <summary>
        /// Gets or sets the BaseSaturation variable within the shader.
        /// </summary>
        public double BaseSaturation
        {
            get { return (double)GetValue(BaseSaturationProperty); }
            set { SetValue(BaseSaturationProperty, value); }
        }
    }
}
