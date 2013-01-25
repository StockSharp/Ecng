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
    public class ContrastAdjustEffect : ShaderEffect
    {
        #region Dependency Properties

        // Brush-valued properties turn into sampler-property in the shader.
        // This helper sets "ImplicitInput" as the default, meaning the default
        // sampler is whatever the rendering of the element it's being applied to is.

        /// <summary>
        /// The explict input for this pixel shader.
        /// </summary>
        public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(ContrastAdjustEffect), 0);

        /// <summary>
        /// This property is mapped to the Brightness variable within the pixel shader. 
        /// </summary>
        public static readonly DependencyProperty BrightnessProperty = DependencyProperty.Register("Brightness", typeof(double), typeof(ContrastAdjustEffect), new UIPropertyMetadata(0.0, PixelShaderConstantCallback(0)));

        /// <summary>
        /// This property is mapped to the Contrast variable within the pixel shader. 
        /// </summary>
        public static readonly DependencyProperty ContrastProperty = DependencyProperty.Register("Contrast", typeof(double), typeof(ContrastAdjustEffect), new UIPropertyMetadata(1.0, PixelShaderConstantCallback(1)));

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
        static ContrastAdjustEffect()
        {
            pixelShader.UriSource = Global.MakePackUri("ShaderSource/ContrastAdjust.ps");
        }

        /// <summary>
        /// Creates an instance and updates the shader's variables to the default values.
        /// </summary>
        public ContrastAdjustEffect()
        {
            this.PixelShader = pixelShader;

            // Update each DependencyProperty that's registered with a shader register.  This
            // is needed to ensure the shader gets sent the proper default value.
            UpdateShaderValue(InputProperty);
            UpdateShaderValue(BrightnessProperty);
            UpdateShaderValue(ContrastProperty);
        }

        #endregion

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
        /// Gets or sets the Brightness variable within the shader.
        /// </summary>
        public double Brightness
        {
            get { return (double)GetValue(BrightnessProperty); }
            set { SetValue(BrightnessProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Contrast variable within the shader.
        /// </summary>
        public double Contrast
        {
            get { return (double)GetValue(ContrastProperty); }
            set { SetValue(ContrastProperty, value); }
        }
    }
}
