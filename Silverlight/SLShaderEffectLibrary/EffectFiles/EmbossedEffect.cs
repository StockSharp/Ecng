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
    public class EmbossedEffect : ShaderEffect
    {
        #region Dependency Properties

        // Brush-valued properties turn into sampler-property in the shader.
        // This helper sets "ImplicitInput" as the default, meaning the default
        // sampler is whatever the rendering of the element it's being applied to is.
        
        /// <summary>
        /// The explict input for this pixel shader.
        /// </summary>
        public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(EmbossedEffect), 0, SamplingMode.Bilinear);

        /// <summary>
        /// This property is mapped to the Amount variable within the pixel shader. 
        /// </summary>
        public static readonly DependencyProperty AmountProperty = DependencyProperty.Register("Amount", typeof(double), typeof(EmbossedEffect), new UIPropertyMetadata(15.0, PixelShaderConstantCallback(0)));

        /// <summary>
        /// This property is mapped to the Width variable within the pixel shader. 
        /// </summary>
        public static readonly DependencyProperty WidthProperty = DependencyProperty.Register("Width", typeof(double), typeof(EmbossedEffect), new UIPropertyMetadata(0.0001, PixelShaderConstantCallback(1)));

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
        static EmbossedEffect()
        {
            pixelShader.UriSource = Global.MakePackUri("ShaderSource/Embossed.ps");
        }

        /// <summary>
        /// Creates an instance and updates the shader's variables to the default values.
        /// </summary>
        public EmbossedEffect()
        {
            this.PixelShader = pixelShader;

            // Update each DependencyProperty that's registered with a shader register.  This
            // is needed to ensure the shader gets sent the proper default value.
            UpdateShaderValue(InputProperty);
            UpdateShaderValue(AmountProperty);
            UpdateShaderValue(WidthProperty);
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
        /// Gets or sets the Amount variable within the shader.
        /// </summary>
        public double Amount
        {
            get { return (double)GetValue(AmountProperty); }
            set { SetValue(AmountProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Width variable within the shader.
        /// </summary>
        public double Width
        {
            get { return (double)GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }
    }
}
