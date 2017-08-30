#include "UnityCG.cginc"

sampler2D _MainTex;
float4 _MainTex_TexelSize;

half _Brightness;
half _Contrast;
half _Saturation;

half4 _ColorBalance;

half2 _KeyCgCo;
half _KeyThreshold;
half _KeyTolerance;
half _SpillRemoval;

float4 _TrimParams;
float2 _Scale;
float2 _Offset;

half4 _FadeToColor;   // given in gamma
half _Opacity;

// RGB <-> YCgCo color space conversion
half3 RGB2YCgCo(half3 rgb)
{
    half3x3 m = {
         0.25, 0.5,  0.25,
        -0.25, 0.5, -0.25,
         0.50, 0.0, -0.50
    };
    return mul(m, rgb);
}

half3 YCgCo2RGB(half3 ycgco)
{
    return half3(
        ycgco.x - ycgco.y + ycgco.z,
        ycgco.x + ycgco.y,
        ycgco.x - ycgco.y - ycgco.z
    );
}

// Color space conversion between linear RGB and LMS
// based on the CIECAM02 model (CAT02).
// http://en.wikipedia.org/wiki/LMS_color_space#CAT02
half3 LRGB2LMS(half3 lrgb)
{
    half3x3 m = {
        3.90405e-1f, 5.49941e-1f, 8.92632e-3f,
        7.08416e-2f, 9.63172e-1f, 1.35775e-3f,
        2.31082e-2f, 1.28021e-1f, 9.36245e-1f
    };
    return mul(m, lrgb);
}

half3 LMS2LRGB(half3 lms)
{
    half3x3 m = {
         2.85847e+0f, -1.62879e+0f, -2.48910e-2f,
        -2.10182e-1f,  1.15820e+0f,  3.24281e-4f,
        -4.18120e-2f, -1.18169e-1f,  1.06867e+0f
    };
    return mul(m, lms);
}

// Chroma keying function
half ChromaKeyAt(float2 uv)
{
    half3 rgb = tex2D(_MainTex, uv);

    #if !defined(UNITY_COLORSPACE_GAMMA)
    rgb = LinearToGammaSpace(rgb);
    #endif

    half3 ycgco = RGB2YCgCo(rgb);

    // Chroma distance
    half d = distance(ycgco.yz, _KeyCgCo) * 10;

    return smoothstep(_KeyThreshold, _KeyThreshold + _KeyTolerance, d);
}

// Main ProcAmp function
half4 ProcAmp(float2 uv)
{
    half4 src = tex2D(_MainTex, uv);
    half3 rgb = src.rgb;

#if defined(_KEYING)

    #if !defined(UNITY_COLORSPACE_GAMMA)
    rgb = LinearToGammaSpace(rgb);
    #endif

    // --vv-- gamma color --vv--

    // Calculate keys for surrounding four points and get the minima of them.
    // This works like a blur and dilate filter.
    float4 duv = _MainTex_TexelSize.xyxy * float4(-0.5, -0.5, 0.5, 0.5);
    half alpha = ChromaKeyAt(uv + duv.xy);
    alpha = min(alpha, ChromaKeyAt(uv + duv.zy));
    alpha = min(alpha, ChromaKeyAt(uv + duv.xw));
    alpha = min(alpha, ChromaKeyAt(uv + duv.zw));

    // Spill removal
    // What the following lines do is flattening the CgCo chroma values
    // so that dot(ycgco, _KeyCgCo) == 0.5. This shifts colors toward
    // the anticolor of the key color.
    half3 ycgco = RGB2YCgCo(rgb);
    half sub = dot(_KeyCgCo, ycgco.yz) / dot(_KeyCgCo, _KeyCgCo);
    ycgco.yz -= _KeyCgCo * (sub + 0.5) * _SpillRemoval;
    rgb = YCgCo2RGB(ycgco);

#else
    half alpha = src.a;
#endif

    #if defined(_KEYING) || defined(UNITY_COLORSPACE_GAMMA)
    rgb = GammaToLinearSpace(rgb);
    #endif

    // --vv-- linear color --vv--

    // Color balance
    rgb = saturate(LMS2LRGB(LRGB2LMS(rgb) * _ColorBalance));

    rgb = LinearToGammaSpace(rgb);

    // --vv-- gamma color --vv--

    // Brightness
    rgb = saturate(rgb + _Brightness);

    // Contrast
    rgb = saturate((rgb - 0.5) * _Contrast + 0.5);

    // Saturation
    half l = dot(rgb, half3(0.2126, 0.7152, 0.0722));
    rgb = saturate(lerp((half3)l, rgb, _Saturation));

    // Fade to color
    rgb = lerp(rgb, _FadeToColor.rgb, _FadeToColor.a);

    // Trimming
    half2 ob = abs(floor((uv - _TrimParams.xy) * _TrimParams.zw));
    half mask = saturate(1 - ob.x - ob.y);

    #if !defined(UNITY_COLORSPACE_GAMMA)
    rgb = GammaToLinearSpace(rgb);
    #endif

    return half4(rgb, alpha * _Opacity) * mask;
}

// Utilities for vertex shaders

// UV coordinate transformation
float2 TransformUV(float2 uv)
{
    return (uv - _Offset - 0.5) / _Scale + 0.5;
}

// Aspect ratio conversion coefficient
float AspectConversion()
{
    float scr_aspect = _ScreenParams.y * (_ScreenParams.z - 1);
    float tex_rcp_aspect = _MainTex_TexelSize.y * _MainTex_TexelSize.z;
    return scr_aspect * tex_rcp_aspect;
}

float2 VertexAspectConversion()
{
    float coeff = AspectConversion();
    return lerp(float2(coeff, 1), float2(1, 1 / coeff), coeff > 1);
}

float2 UVAspectConversion()
{
    float coeff = AspectConversion();
    return lerp(float2(1 / coeff, 1), float2(1, coeff), coeff > 1);
}

// Move a quad to the near screen plane.
float4 NearPlaneQuad(float4 vertex)
{
    float4 p = UnityViewToClipPos(float4(vertex.xy, -1, 1));
    p.xy /= abs(p.xy);
    return p;
}
