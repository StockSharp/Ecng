//--------------------------------------------------------------------------------------
// 
// WPF ShaderEffect HLSL -- SmaaAntiAliasEffect
//
//--------------------------------------------------------------------------------------

//-----------------------------------------------------------------------------------------
// Shader constant register mappings (scalars - float, double, Point, Color, Point3D, etc.)
//-----------------------------------------------------------------------------------------

float4 colorFilter : register(C0);

//--------------------------------------------------------------------------------------
// Sampler Inputs (Brushes, including ImplicitInput)
//--------------------------------------------------------------------------------------

sampler2D implicitInputSampler : register(S0);


//--------------------------------------------------------------------------------------
// Pixel Shader
//--------------------------------------------------------------------------------------

float4 main(float2 uv : TEXCOORD) : COLOR
{
   // This particular shader just multiplies the color at 
   // a point by the colorFilter shader constant.
   float4 color = tex2D(implicitInputSampler, uv);
   return color * colorFilter;
}


