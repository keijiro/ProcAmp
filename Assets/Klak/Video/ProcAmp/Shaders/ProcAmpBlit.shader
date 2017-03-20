Shader "Hidden/Klak/Video/ProcAmp Blit"
{
    Properties
    {
        _MainTex("", 2D) = "gray" {}
        _SrcBlend("", Int) = 1
        _DstBlend("", Int) = 0
    }

    CGINCLUDE

    #include "ProcAmp.cginc"

    // Vertex shader for screen blit
    v2f_img vert_screen(appdata_img v)
    {
        v2f_img o;

        // Move the quad to the near screen plane.
        o.pos = UnityViewToClipPos(float4(v.vertex.xy, -1, 1));
        o.pos.xy /= abs(o.pos.xy);

        // Aspect ratio adjustment (fit to the screen).
        float scr_aspect = _ScreenParams.y * (_ScreenParams.z - 1);
        float tex_aspect = _MainTex_TexelSize.y * _MainTex_TexelSize.z;
        float aspect_fix = tex_aspect * scr_aspect;

        if (aspect_fix > 1)
            o.pos.y /= aspect_fix;
        else
            o.pos.x *= aspect_fix;

        o.uv = v.texcoord;

        return o;
    }

    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // ProcAmp with simple blit
        Pass
        {
            CGPROGRAM

            #pragma vertex vert_img
            #pragma fragment frag

            #pragma multi_compile UNITY_COLORSPACE_GAMMA
            #pragma multi_compile _KEYING

            half4 frag(v2f_img i) : SV_Target
            {
                return ProcAmp(i.uv);
            }

            ENDCG
        }

        // ProcAmp with screen blit
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]

            CGPROGRAM

            #pragma vertex vert_screen
            #pragma fragment frag

            #pragma multi_compile UNITY_COLORSPACE_GAMMA
            #pragma multi_compile _KEYING

            half4 frag(v2f_img i) : SV_Target
            {
                return ProcAmp(i.uv);
            }

            ENDCG
        }

        // Simple screen blit
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]

            CGPROGRAM

            #pragma vertex vert_screen
            #pragma fragment frag

            half4 frag(v2f_img i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }

            ENDCG
        }
    }
}
