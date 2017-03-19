Shader "Hidden/Klak/Video/ProcAmp Blit"
{
    Properties
    {
        _MainTex("", 2D) = "gray" {}
    }

    CGINCLUDE

    #include "ProcAmp.cginc"

    v2f_img vert_screen(appdata_img v)
    {
        // a little bit messy way to fit the quad to the screen
        v.vertex.z -= 1;
        v2f_img o;
        o.pos = UnityViewToClipPos(v.vertex);
        o.pos.xy /= abs(o.pos.xy);
        o.uv = v.texcoord;
        return o;
    }

    ENDCG

    SubShader
    {
        // Blit
        Pass
        {
            Cull Off ZWrite Off ZTest Always

            CGPROGRAM

            #pragma vertex vert_img
            #pragma fragment frag

            #pragma multi_compile UNITY_COLORSPACE_GAMMA
            #pragma shader_feature _KEYING

            half4 frag(v2f_img i) : SV_Target
            {
                return ProcAmp(i.uv);
            }

            ENDCG
        }

        // Debug (opaque)
        Pass
        {
            Cull Off ZWrite Off ZTest Always

            CGPROGRAM

            #pragma vertex vert_screen
            #pragma fragment frag

            half4 frag(v2f_img i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }

            ENDCG
        }

        // Debug (transparent)
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

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
