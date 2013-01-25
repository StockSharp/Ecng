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
    public class PixelateEffect : ShaderEffect
    {
        #region Dependency Properties

        // Brush-valued properties turn into sampler-property in the shader.
        // This helper sets "ImplicitInput" as the default, meaning the default
        // sampler is whatever the rendering of the element it's being applied to is.
        
        /// <summary>
        /// The explict input for this pixel shader.
        /// </summary>
        public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(PixelateEffect), 0);

        /// <summary>
        /// This property is mapped to the HorizontalPixelCounts variable within the pixel shader. 
        /// </summary>
        public static readonly DependencyProperty HorizontalPixelCountsProperty = DependencyProperty.Register("HorizontalPixelCounts", typeof(double), typeof(PixelateEffect), new UIPropertyMetadata(80.0, PixelShaderConstantCallback(0)));

        /// <summary>
        /// This property is mapped to the VerticalPixelCounts variable within the pixel shader. 
        /// </summary>
        public static readonly DependencyProperty VerticalPixelCountsProperty = DependencyProperty.Register("VerticalPixelCounts", typeof(double), typeof(PixelateEffect), new UIPropertyMetadata(80.0, PixelShaderConstantCallback(1)));

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
        static PixelateEffect()
        {
            pixelShader.UriSource = Global.MakePackUri("ShaderSource/Pixelate.ps");
        }

        /// <summary>
        /// Creates an instance and updates the shader's variables to the default values.
        /// </summary>
        public PixelateEffect()
        {
            this.PixelShader = pixelShader;

            // Update each DependencyProperty that's registered with a shader register.  This
            // is needed to ensure the shader gets sent the proper default value.
            UpdateShaderValue(InputProperty);
            UpdateShaderValue(HorizontalPixelCountsProperty);
            UpdateShaderValue(VerticalPixelCountsProperty);
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
        /// Gets or sets the HorizontalPixelCounts variable within the shader.
        /// </summary>
        public double HorizontalPixelCounts
        {
            get { return (double)GetValue(HorizontalPixelCountsProperty); }
            set { SetValue(HorizontalPixelCountsProperty, value); }
        }

        /// <summary>
        /// Gets or sets the VerticalPixelCounts variable within the shader.
        /// </summary>
        public double VerticalPixelCounts
        {
            get { return (double)GetValue(VerticalPixelCountsProperty); }
            set { SetValue(VerticalPixelCountsProperty, value); }
        }
    }
}
