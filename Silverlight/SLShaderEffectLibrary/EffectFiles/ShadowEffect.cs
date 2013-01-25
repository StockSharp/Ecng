namespace ShaderEffectLibrary
{
	using System.Windows;
	using System.Windows.Media;
	using System.Windows.Media.Effects;

	public class ShadowEffect : ShaderEffect
	{
		/// <summary>
		/// Gets or sets the Input of the shader.
		/// </summary>
		public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(ShadowEffect), 0);

		#region Member Data

        /// <summary>
        /// The shader instance.
        /// </summary>
        private static PixelShader pixelShader;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an instance of the shader from the included pixel shader.
        /// </summary>
        static ShadowEffect()
        {
            pixelShader = new PixelShader();
            pixelShader.UriSource = Global.MakePackUri("ShaderSource/Shadow.ps");
        }

        /// <summary>
        /// Creates an instance and updates the shader's variables to the default values.
        /// </summary>
		public ShadowEffect()
        {
            this.PixelShader = pixelShader;

            UpdateShaderValue(InputProperty);
        }

        #endregion

		/// <summary>
		/// Gets or sets the input used in the shader.
		/// </summary>
		[System.ComponentModel.BrowsableAttribute(false)]
		public Brush Input
		{
			get { return (Brush)GetValue(InputProperty); }
			set { SetValue(InputProperty, value); }
		}
	}
}