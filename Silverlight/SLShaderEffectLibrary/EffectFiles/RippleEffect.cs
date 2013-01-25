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
    using UIPropertyMetadata = System.Windows.PropertyMetadata ;      
#endif
    /// <summary>
    /// This is the implementation of an extensible framework ShaderEffect which loads
    /// a shader model 2 pixel shader. Dependecy properties declared in this class are mapped
    /// to registers as defined in the *.ps file being loaded below.
    /// </summary>
    public class RippleEffect : ShaderEffect
    {
        #region Dependency Properties

        /// <summary>
        /// Gets or sets the Center variable within the shader.
        /// </summary>
        public static readonly DependencyProperty CenterProperty = DependencyProperty.Register("Center", typeof(Point), typeof(RippleEffect), new UIPropertyMetadata(new Point(0.5, 0.5), PixelShaderConstantCallback(0)));

        /// <summary>
        /// Gets or sets the Amplitude variable within the shader.
        /// </summary>
        public static readonly DependencyProperty AmplitudeProperty = DependencyProperty.Register("Amplitude", typeof(double), typeof(RippleEffect), new UIPropertyMetadata(0.1, PixelShaderConstantCallback(1)));
        
        /// <summary>
        /// Gets or sets the Frequency variable within the shader.
        /// </summary>
        public static readonly DependencyProperty FrequencyProperty = DependencyProperty.Register("Frequency", typeof(double), typeof(RippleEffect), new UIPropertyMetadata(50.0, PixelShaderConstantCallback(2)));

        /// <summary>
        /// Gets or sets the Phase variable within the shader.
        /// </summary>
        public static readonly DependencyProperty PhaseProperty = DependencyProperty.Register("Phase", typeof(double), typeof(RippleEffect), new UIPropertyMetadata(0.0, PixelShaderConstantCallback(3)));

        /// <summary>
        /// Gets or sets the input brush used in the shader.
        /// </summary>
        public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(RippleEffect), 0);

        #endregion

        #region Member Data

        /// <summary>
        /// The pixel shader instance.
        /// </summary>
        private static PixelShader pixelShader;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an instance of the shader from the included pixel shader.
        /// </summary>
        static RippleEffect()
        {
            pixelShader = new PixelShader();
            pixelShader.UriSource = Global.MakePackUri("ShaderSource/Ripple.ps");
        }

        /// <summary>
        /// Creates an instance and updates the shader's variables to the default values.
        /// </summary>
        public RippleEffect()
        {
            this.PixelShader = pixelShader;

            UpdateShaderValue(CenterProperty);
            UpdateShaderValue(AmplitudeProperty);
            UpdateShaderValue(PhaseProperty);
            UpdateShaderValue(FrequencyProperty);
            UpdateShaderValue(InputProperty);
        }

        #endregion

        /// <summary>
        /// Gets or sets the center variable within the shader.
        /// </summary>
        public Point Center
        {
            get { return (Point)GetValue(CenterProperty); }
            set { SetValue(CenterProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Amplitude variable within the shader.
        /// </summary>
        public double Amplitude
        {
            get { return (double)GetValue(AmplitudeProperty); }
            set { SetValue(AmplitudeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the frequency variable within the shader.
        /// </summary>
        public double Frequency
        {
            get { return (double)GetValue(FrequencyProperty); }
            set { SetValue(FrequencyProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Phase variable within the shader.
        /// </summary>
        public double Phase
        {
            get { return (double)GetValue(PhaseProperty); }
            set { SetValue(PhaseProperty, value); }
        }

        /// <summary>
        /// Gets or sets the input used within the shader.
        /// </summary>
		[System.ComponentModel.BrowsableAttribute(false)]
        public Brush Input
        {
            get { return (Brush)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }
    }
}
