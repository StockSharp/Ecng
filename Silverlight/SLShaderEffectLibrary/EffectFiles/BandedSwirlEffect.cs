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

#if SILVERLIGHT
    using UIPropertyMetadata = System.Windows.PropertyMetadata;    
#endif 
    /// <summary>
    /// This is the implementation of an extensible framework ShaderEffect which loads
    /// a shader model 2 pixel shader. Dependecy properties declared in this class are mapped
    /// to registers as defined in the *.ps file being loaded below.
    /// </summary>
    public class BandedSwirlEffect : ShaderEffect
    {
        #region Dependency Properties
        /// <summary>
        /// Dependency property which modifies the Center variable within the pixel shader.
        /// </summary>
        public static readonly DependencyProperty CenterProperty = DependencyProperty.Register("Center", typeof(Point), typeof(BandedSwirlEffect), new UIPropertyMetadata(new Point(0.5, 0.5), PixelShaderConstantCallback(0)));

        /// <summary>
        /// Dependency property which modifies the SwirlStrength variable within the pixel shader.
        /// </summary>
        public static readonly DependencyProperty SwirlStrengthProperty = DependencyProperty.Register("SwirlStrength", typeof(double), typeof(BandedSwirlEffect), new UIPropertyMetadata(0.5, PixelShaderConstantCallback(1)));

        /// <summary>
        /// Dependency property which modifies the DistanceThreshold variable within the pixel shader.
        /// </summary>
        public static readonly DependencyProperty DistanceThresholdProperty = DependencyProperty.Register("DistanceThreshold", typeof(double), typeof(BandedSwirlEffect), new UIPropertyMetadata(0.2, PixelShaderConstantCallback(2)));

        /// <summary>
        /// Dependency property for the shader sampler.
        /// </summary>
        public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(BandedSwirlEffect), 0);

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
        static BandedSwirlEffect()
        {
            pixelShader = new PixelShader();
            pixelShader.UriSource = Global.MakePackUri("ShaderSource/BandedSwirl.ps");
        }

        /// <summary>
        /// Creates the and updates the registered values defined within the pixel shader using the default values.
        /// </summary>
        public BandedSwirlEffect()
        {
            this.PixelShader = pixelShader;

            UpdateShaderValue(CenterProperty);
            UpdateShaderValue(SwirlStrengthProperty);
            UpdateShaderValue(DistanceThresholdProperty);
            UpdateShaderValue(InputProperty);
        }

        #endregion

        /// <summary>
        /// Gets or sets the Center variable within the shader.
        /// </summary>
        public Point Center
        {
            get { return (Point)GetValue(CenterProperty); }
            set { SetValue(CenterProperty, value); }
        }

        /// <summary>
        /// Gets or sets SwirlStength variable within the shader.
        /// </summary>
        public double SwirlStrength
        {
            get { return (double)GetValue(SwirlStrengthProperty); }
            set { SetValue(SwirlStrengthProperty, value); }
        }

        /// <summary>
        /// Gets or sets the DistanceThreshold variable within the shader.
        /// </summary>
        public double DistanceThreshold
        {
            get { return (double)GetValue(DistanceThresholdProperty); }
            set { SetValue(DistanceThresholdProperty, value); }
        }

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