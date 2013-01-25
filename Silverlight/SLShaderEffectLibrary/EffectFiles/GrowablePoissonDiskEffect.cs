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
    public class GrowablePoissonDiskEffect : ShaderEffect
    {
        #region Dependency Properties

        // Brush-valued properties turn into sampler-property in the shader.
        // This helper sets "ImplicitInput" as the default, meaning the default
        // sampler is whatever the rendering of the element it's being applied to is.
        
        /// <summary>
        /// The explict input for this pixel shader.
        /// </summary>
        public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(GrowablePoissonDiskEffect), 0);

        /// <summary>
        /// This property is mapped to the Radius variable within the pixel shader. 
        /// </summary>
        public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register("Radius", typeof(double), typeof(GrowablePoissonDiskEffect), new UIPropertyMetadata(0.5, PixelShaderConstantCallback(0)));

        /// <summary>
        /// This property is mapped to the Width variable within the pixel shader. 
        /// </summary>
        public static readonly DependencyProperty WidthProperty = DependencyProperty.Register("Width", typeof(double), typeof(GrowablePoissonDiskEffect), new UIPropertyMetadata(0.0, PixelShaderConstantCallback(1)));

        /// <summary>
        /// This property is mapped to the Height variable within the pixel shader. 
        /// </summary>
        public static readonly DependencyProperty HeightProperty = DependencyProperty.Register("Height", typeof(double), typeof(GrowablePoissonDiskEffect), new UIPropertyMetadata(0.0, PixelShaderConstantCallback(2)));

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
        static GrowablePoissonDiskEffect()
        {
            pixelShader.UriSource = Global.MakePackUri("ShaderSource/GrowablePoissonDisk.ps");
        }

        /// <summary>
        /// Creates an instance and updates the shader's variables to the default values.
        /// </summary>
        public GrowablePoissonDiskEffect()
        {
            this.PixelShader = pixelShader;

            // Update each DependencyProperty that's registered with a shader register.  This
            // is needed to ensure the shader gets sent the proper default value.
            UpdateShaderValue(InputProperty);
            UpdateShaderValue(RadiusProperty);
            UpdateShaderValue(WidthProperty);
            UpdateShaderValue(HeightProperty);
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
        /// Gets or sets the Radius variable within the shader.
        /// </summary>
        public double Radius
        {
            get { return (double)GetValue(RadiusProperty); }
            set { SetValue(RadiusProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Width variable within the shader.
        /// </summary>
        public double Width
        {
            get { return (double)GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Height variable within the shader.
        /// </summary>
        public double Height
        {
            get { return (double)GetValue(HeightProperty); }
            set { SetValue(HeightProperty, value); }
        }
    }
}
