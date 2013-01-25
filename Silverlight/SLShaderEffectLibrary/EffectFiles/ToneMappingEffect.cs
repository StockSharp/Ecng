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
    public class ToneMappingEffect : ShaderEffect
    {
        #region Dependency Properties

        // Brush-valued properties turn into sampler-property in the shader.
        // This helper sets "ImplicitInput" as the default, meaning the default
        // sampler is whatever the rendering of the element it's being applied to is.
        
        /// <summary>
        /// The explict input for this pixel shader.
        /// </summary>
        public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(ToneMappingEffect), 0);

        /// <summary>
        /// This property is mapped to the Exposure variable within the pixel shader. 
        /// </summary>
        public static readonly DependencyProperty ExposureProperty = DependencyProperty.Register("Exposure", typeof(double), typeof(ToneMappingEffect), new UIPropertyMetadata(0.0, PixelShaderConstantCallback(0)));

        /// <summary>
        /// This property is mapped to the Defog variable within the pixel shader. 
        /// </summary>
        public static readonly DependencyProperty DefogProperty = DependencyProperty.Register("Defog", typeof(double), typeof(ToneMappingEffect), new UIPropertyMetadata(0.0, PixelShaderConstantCallback(1)));

        /// <summary>
        /// This property is mapped to the Gamma variable within the pixel shader. 
        /// </summary>
        public static readonly DependencyProperty GammaProperty = DependencyProperty.Register("Gamma", typeof(double), typeof(ToneMappingEffect), new UIPropertyMetadata(0.454545, PixelShaderConstantCallback(2)));

        /// <summary>
        /// This property is mapped to the FogColor variable within the pixel shader. 
        /// </summary>
        public static readonly DependencyProperty FogColorProperty = DependencyProperty.Register("FogColor", typeof(Color), typeof(ToneMappingEffect), new UIPropertyMetadata(Colors.White, PixelShaderConstantCallback(3)));

        /// <summary>
        /// This property is mapped to the VignetteRadius variable within the pixel shader. 
        /// </summary>
        public static readonly DependencyProperty VignetteRadiusProperty = DependencyProperty.Register("VignetteRadius", typeof(double), typeof(ToneMappingEffect), new UIPropertyMetadata(0.35, PixelShaderConstantCallback(4)));

        /// <summary>
        /// This property is mapped to the VignetteCenterX variable within the pixel shader. 
        /// </summary>
        public static readonly DependencyProperty VignetteCenterProperty = DependencyProperty.Register("VignetteCenter", typeof(Point), typeof(ToneMappingEffect), new UIPropertyMetadata( new Point(0.5, 0.5), PixelShaderConstantCallback(5)));

        
        /// <summary>
        /// This property is mapped to the BlueShift variable within the pixel shader. 
        /// </summary>
        public static readonly DependencyProperty BlueShiftProperty = DependencyProperty.Register("BlueShift", typeof(double), typeof(ToneMappingEffect), new UIPropertyMetadata(1.0, PixelShaderConstantCallback(6)));

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
        static ToneMappingEffect()
        {
            pixelShader.UriSource = Global.MakePackUri("ShaderSource/ToneMapping.ps");
        }

        /// <summary>
        /// Creates an instance and updates the shader's variables to the default values.
        /// </summary>
        public ToneMappingEffect()
        {
            this.PixelShader = pixelShader;

            // Update each DependencyProperty that's registered with a shader register.  This
            // is needed to ensure the shader gets sent the proper default value.
            UpdateShaderValue(InputProperty);
            UpdateShaderValue(ExposureProperty);
            UpdateShaderValue(DefogProperty);
            UpdateShaderValue(GammaProperty);
            UpdateShaderValue(FogColorProperty);
            UpdateShaderValue(VignetteRadiusProperty);
            UpdateShaderValue(VignetteCenterProperty);            
            UpdateShaderValue(BlueShiftProperty);
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
        /// Gets or sets the Exposure variable within the shader.
        /// </summary>
        public double Exposure
        {
            get { return (double)GetValue(ExposureProperty); }
            set { SetValue(ExposureProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Defog variable within the shader.
        /// </summary>
        public double Defog
        {
            get { return (double)GetValue(DefogProperty); }
            set { SetValue(DefogProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Gamma variable within the shader.
        /// </summary>
        public double Gamma
        {
            get { return (double)GetValue(GammaProperty); }
            set { SetValue(GammaProperty, value); }
        }

        /// <summary>
        /// Gets or sets the FogColor variable within the shader.
        /// </summary>
        public Color FogColor
        {
            get { return (Color)GetValue(FogColorProperty); }
            set { SetValue(FogColorProperty, value); }
        }

        /// <summary>
        /// Gets or sets the VignetteRadius variable within the shader.
        /// </summary>
        public double VignetteRadius
        {
            get { return (double)GetValue(VignetteRadiusProperty); }
            set { SetValue(VignetteRadiusProperty, value); }
        }

        /// <summary>
        /// Gets or sets the VignetteCenterX variable within the shader.
        /// </summary>
        public Point VignetteCenter
        {
            get { return (Point)GetValue(VignetteCenterProperty); }
            set { SetValue(VignetteCenterProperty, value); }
        }

        

        /// <summary>
        /// Gets or sets the BlueShift variable within the shader.
        /// </summary>
        public double BlueShift
        {
            get { return (double)GetValue(BlueShiftProperty); }
            set { SetValue(BlueShiftProperty, value); }
        }
    }
}
