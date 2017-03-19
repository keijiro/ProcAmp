#include "UnityCG.cginc"

sampler2D _MainTex;
float4 _MainTex_TexelSize;

fixed2 _CgCo;  // Key color as YCgCo without Y
fixed2 _Matte; // (threshold, threshold + tolerance)
fixed _Spill;
half2 _BlurDir;

// Keying shader

fixed3 RGB2YCgCo(fixed3 rgb)
{
    return fixed3(
        dot(rgb, half3( 0.25, 0.5,  0.25)),
        dot(rgb, half3(-0.25, 0.5, -0.25)),
        dot(rgb, half3( 0.50, 0.0, -0.50)));
}

fixed3 YCgCo2RGB(fixed3 ycgco)
{
    return fixed3(
        dot(ycgco, half3(1, -1,  1)),
        dot(ycgco, half3(1,  1,  0)),
        dot(ycgco, half3(1, -1, -1)));
}

fixed FetchAlpha(float2 uv)
{
    fixed3 src = tex2D(_MainTex, uv);
    #if !defined(UNITY_COLORSPACE_GAMMA)
    src = LinearToGammaSpace(src);
    #endif
    fixed3 src_ycgco = RGB2YCgCo(src);

    // chroma-difference based alpha
    half dist = distance(src_ycgco.yz, _CgCo);
    return smoothstep(_Matte.x, _Matte.y, dist);
}

fixed4 FragKeying(v2f_img i) : SV_Target
{
    fixed3 src = tex2D(_MainTex, i.uv);
    #if !defined(UNITY_COLORSPACE_GAMMA)
    src = LinearToGammaSpace(src);
    #endif
    fixed3 src_ycgco = RGB2YCgCo(src);

    float4 duv = _MainTex_TexelSize.xyxy * float4(-0.5, -0.5, 0.5, 0.5);
    fixed alpha =      FetchAlpha(i.uv + duv.xy);
    alpha = min(alpha, FetchAlpha(i.uv + duv.zy));
    alpha = min(alpha, FetchAlpha(i.uv + duv.xw));
    alpha = min(alpha, FetchAlpha(i.uv + duv.zw));

    // Spill removal
    half2 cgco = src_ycgco.yz;
    cgco -= _CgCo * (dot(_CgCo, cgco) / dot(_CgCo, _CgCo) + 0.5) * _Spill;
    half3 rgb = YCgCo2RGB(half3(src_ycgco.x, cgco));

    #if !defined(UNITY_COLORSPACE_GAMMA)
    rgb = GammaToLinearSpace(rgb);
    #endif
    return fixed4(rgb, alpha);
}

// Alpha dilate shader

fixed4 FragDilate(v2f_img i) : SV_Target
{
    fixed4 c0 = tex2D(_MainTex, i.uv);

    float3 d = float3(_MainTex_TexelSize.xy, 0);

    fixed a1 = tex2D(_MainTex, i.uv - d.xz).a;
    fixed a2 = tex2D(_MainTex, i.uv - d.zy).a;
    fixed a3 = tex2D(_MainTex, i.uv + d.xz).a;
    fixed a4 = tex2D(_MainTex, i.uv + d.zy).a;

    fixed a = min(min(min(min(c0.a, a1), a2), a3), a4);

    return fixed4(c0.rgb, a);
}

// Alpha blur shader

fixed4 FragBlur(v2f_img i) : SV_Target
{
    fixed4 c0 = tex2D(_MainTex, i.uv);

    float2 d = _MainTex_TexelSize.xy * _BlurDir;

    fixed a1 = tex2D(_MainTex, i.uv - d * 2).a;
    fixed a2 = tex2D(_MainTex, i.uv - d    ).a;
    fixed a3 = tex2D(_MainTex, i.uv + d    ).a;
    fixed a4 = tex2D(_MainTex, i.uv + d * 2).a;

    fixed a =
        0.38774 * c0.a +
        0.24477 * (a2 + a3) +
        0.06136 * (a1 + a4);

    return fixed4(c0.rgb, a);
}
