#include "UnityCG.cginc"

sampler2D _MainTex;

half3 _AmpParams; // brightness, contrast, saturation
half4 _ColorBalance;
fixed4 _FadeToColor; // given in gamma

// Color space conversion between linear RGB and LMS
// based on the CIECAM02 model (CAT02).
// http://en.wikipedia.org/wiki/LMS_color_space#CAT02
float3 LRGB2LMS(float3 c)
{
    float3x3 m = {
        3.90405e-1f, 5.49941e-1f, 8.92632e-3f,
        7.08416e-2f, 9.63172e-1f, 1.35775e-3f,
        2.31082e-2f, 1.28021e-1f, 9.36245e-1f
    };
    return mul(m, c);
}

float3 LMS2LRGB(float3 c)
{
    float3x3 m = {
         2.85847e+0f, -1.62879e+0f, -2.48910e-2f,
        -2.10182e-1f,  1.15820e+0f,  3.24281e-4f,
        -4.18120e-2f, -1.18169e-1f,  1.06867e+0f
    };
    return mul(m, c);
}

// Adjustment shader
fixed4 FragAdjust(v2f_img i) : SV_Target
{
    fixed4 src = tex2D(_MainTex, i.uv);

    fixed3 rgb = src.rgb;

#if defined(_APPLY_COLOR_BALANCE)
    #if defined(UNITY_COLORSPACE_GAMMA)
    rgb = GammaToLinearSpace(rgb);
    #endif

    // Color balance
    rgb = saturate(LMS2LRGB(LRGB2LMS(rgb) * _ColorBalance));

    rgb = LinearToGammaSpace(rgb);
#else
    #if !defined(UNITY_COLORSPACE_GAMMA)
    rgb = LinearToGammaSpace(rgb);
    #endif
#endif

    // Brightness
    rgb = saturate(rgb + _AmpParams.x);

    // Contrast
    rgb = saturate((rgb - 0.5) * _AmpParams.y + 0.5);

    // Saturation
    fixed l = dot(rgb, fixed3(0.2126, 0.7152, 0.0722));
    rgb = saturate(lerp(l, rgb, _AmpParams.z));


    // Fade to color
    rgb = lerp(rgb, _FadeToColor.rgb, _FadeToColor.a);

    #if !defined(UNITY_COLORSPACE_GAMMA)
    rgb = GammaToLinearSpace(rgb);
    #endif

    return fixed4(rgb, src.a);
}
