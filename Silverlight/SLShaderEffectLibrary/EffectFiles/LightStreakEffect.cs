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
    public class LightStreakEffect : ShaderEffect
    {
        #region Dependency Properties

        // Brush-valued properties turn into sampler-property in the shader.
        // This helper sets "ImplicitInput" as the default, meaning the default
        // sampler is whatever the rendering of the element it's being applied to is.
        
        /// <summary>
        /// The explict input for this pixel shader.
        /// </summary>
        public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(LightStreakEffect), 0);

        /// <summary>
        /// This property is mapped to the BrightThreshold variable within the pixel shader. 
        /// </summary>
        public static readonly DependencyProperty BrightThresholdProperty = DependencyProperty.Register("BrightThreshold", typeof(double), typeof(LightStreakEffect), new UIPropertyMetadata(0.25, PixelShaderConstantCallback(0)));

        /// <summary>
        /// This property is mapped to the Scale variable within the pixel shader. 
        /// </summary>
        public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register("Scale", typeof(double), typeof(LightStreakEffect), new UIPropertyMetadata(0.75, PixelShaderConstantCallback(1)));

        /// <summary>
        /// This property is mapped to the Attenuation variable within the pixel shader. 
        /// </summary>
        public static readonly DependencyProperty AttenuationProperty = DependencyProperty.Register("Attenuation", typeof(double), typeof(LightStreakEffect), new UIPropertyMetadata(1.0, PixelShaderConstantCallback(2)));

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
        static LightStreakEffect()
        {
            pixelShader.UriSource = Global.MakePackUri("ShaderSource/LightStreak.ps");
        }

        /// <summary>
        /// Creates an instance and updates the shader's variables to the default values.
        /// </summary>
        public LightStreakEffect()
        {
            this.PixelShader = pixelShader;

            // Update each DependencyProperty that's registered with a shader register.  This
            // is needed to ensure the shader gets sent the proper default value.
            UpdateShaderValue(InputProperty);
            UpdateShaderValue(BrightThresholdProperty);
            UpdateShaderValue(ScaleProperty);
            UpdateShaderValue(AttenuationProperty);
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
        /// Gets or sets the BrightThreshold variable within the shader.
        /// </summary>
        public double BrightThreshold
        {
            get { return (double)GetValue(BrightThresholdProperty); }
            set { SetValue(BrightThresholdProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Scale variable within the shader.
        /// </summary>
        public double Scale
        {
            get { return (double)GetValue(ScaleProperty); }
            set { SetValue(ScaleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Attenuation variable within the shader.
        /// </summary>
        public double Attenuation
        {
            get { return (double)GetValue(AttenuationProperty); }
            set { SetValue(AttenuationProperty, value); }
        }
    }
}
