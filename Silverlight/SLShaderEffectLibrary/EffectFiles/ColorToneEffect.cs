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
    public class ColorToneEffect : ShaderEffect
    {
        #region Dependency Properties

        // Brush-valued properties turn into sampler-property in the shader.
        // This helper sets "ImplicitInput" as the default, meaning the default
        // sampler is whatever the rendering of the element it's being applied to is.
        
        /// <summary>
        /// The explict input for this pixel shader.
        /// </summary>
        public static readonly DependencyProperty InputProperty =
            ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(ColorToneEffect), 0);

        /// <summary>
        /// This property is mapped to the Desaturation variable within the pixel shader. 
        /// </summary>
        public static readonly DependencyProperty DesaturationProperty =
            DependencyProperty.Register("Desaturation", typeof(double), typeof(ColorToneEffect), new UIPropertyMetadata(0.5, PixelShaderConstantCallback(0)));

        /// <summary>
        /// This property is mapped to the Toned variable within the pixel shader. 
        /// </summary>
        public static readonly DependencyProperty TonedProperty =
            DependencyProperty.Register("Toned", typeof(double), typeof(ColorToneEffect), new UIPropertyMetadata(1.0, PixelShaderConstantCallback(1)));

        /// <summary>
        /// This property is mapped to the LightColor variable within the pixel shader. 
        /// </summary>
        public static readonly DependencyProperty LightColorProperty =
            DependencyProperty.Register("LightColor", typeof(Color), typeof(ColorToneEffect), new UIPropertyMetadata(Color.FromArgb(255, 255, 229, 128), PixelShaderConstantCallback(2)));

        /// <summary>
        /// This property is mapped to the DarkColor variable within the pixel shader. 
        /// </summary>
        public static readonly DependencyProperty DarkColorProperty =
            DependencyProperty.Register("DarkColor", typeof(Color), typeof(ColorToneEffect), new UIPropertyMetadata(Color.FromArgb(255, 51, 128, 0), PixelShaderConstantCallback(3)));

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
        static ColorToneEffect()
        {
            pixelShader.UriSource = Global.MakePackUri("ShaderSource/ColorTone.ps");
        }

        /// <summary>
        /// Creates an instance and updates the shader's variables to the default values.
        /// </summary>
        public ColorToneEffect()
        {
            this.PixelShader = pixelShader;

            // Update each DependencyProperty that's registered with a shader register.  This
            // is needed to ensure the shader gets sent the proper default value.
            UpdateShaderValue(InputProperty);
            UpdateShaderValue(DesaturationProperty);
            UpdateShaderValue(TonedProperty);
            UpdateShaderValue(LightColorProperty);
            UpdateShaderValue(DarkColorProperty);
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
        /// Gets or sets the Desaturation variable within the shader.
        /// </summary>
        public double Desaturation
        {
            get { return (double)GetValue(DesaturationProperty); }
            set { SetValue(DesaturationProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Toned variable within the shader.
        /// </summary>
        public double Toned
        {
            get { return (double)GetValue(TonedProperty); }
            set { SetValue(TonedProperty, value); }
        }

        /// <summary>
        /// Gets or sets the LightColor variable within the shader.
        /// </summary>
        public Color LightColor
        {
            get { return (Color)GetValue(LightColorProperty); }
            set { SetValue(LightColorProperty, value); }
        }

        /// <summary>
        /// Gets or sets the DarkColor variable within the shader.
        /// </summary>
        public Color DarkColor
        {
            get { return (Color)GetValue(DarkColorProperty); }
            set { SetValue(DarkColorProperty, value); }
        }
    }
}
