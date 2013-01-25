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
    using System.Diagnostics;
    
    /// <summary>
    /// This is the implementation of an extensible framework ShaderEffect which loads
    /// a shader model 2 pixel shader. Dependecy properties declared in this class are mapped
    /// to registers as defined in the *.ps file being loaded below.
    /// </summary>
    public class ToonShaderEffect : ShaderEffect
    {
        #region Dependency Properties

        /// <summary>
        /// Dependency property for the shader sampler.
        /// </summary>
        public static readonly DependencyProperty InputProperty =
            ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(ToonShaderEffect), 0);

        #endregion

        #region Member Data

        /// <summary>
        /// The pixel shader instance.
        /// </summary>
        private static PixelShader pixelShader;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a PixelShader by loading the bytecode.
        /// </summary>
        static ToonShaderEffect()
        {
            pixelShader = new PixelShader();
            pixelShader.UriSource = Global.MakePackUri("ShaderSource/ToonShader.ps");

            // Just saying hardware only for now since our drop of sw doesn't have sin/cos in
            // it, and thus we can't validate against that.
#if !SILVERLIGHT 
            // TODO: 
            pixelShader.ShaderRenderMode = ShaderRenderMode.HardwareOnly;
#endif 
        }

        /// <summary>
        /// Creates the and updates the registered values defined within the pixel shader using the default values.
        /// </summary>
        public ToonShaderEffect()
        {
            this.PixelShader = pixelShader;
            UpdateShaderValue(InputProperty);
        }

        #endregion

        /// <summary>
        /// Gets or sets the Input variable within the shader.
        /// </summary>
		[System.ComponentModel.BrowsableAttribute(false)]
        public Brush Input
        {
            get { return (Brush)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }
    }
}
