Shader "Hidden/Klak/Video/ProcAmp Blit"
{
    Properties
    {
        _MainTex("", 2D) = "gray" {}
        _BaseTex("", 2D) = "gray" {}
        _SrcBlend("", Int) = 1
        _DstBlend("", Int) = 0
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // ProcAmp with simple blit
        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ UNITY_COLORSPACE_GAMMA
            #pragma multi_compile _ _KEYING

            #include "ProcAmp.cginc"

            v2f_img vert(appdata_img v)
            {
                v2f_img o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TransformUV(v.texcoord);
                return o;
            }

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

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ UNITY_COLORSPACE_GAMMA
            #pragma multi_compile _ _KEYING

            #include "ProcAmp.cginc"

            v2f_img vert(appdata_img v)
            {
                v2f_img o;
                o.pos = NearPlaneQuad(v.vertex);
                o.pos.xy *= VertexAspectConversion();
                o.uv = TransformUV(v.texcoord);
                return o;
            }

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

            #pragma vertex vert
            #pragma fragment frag

            #include "ProcAmp.cginc"

            v2f_img vert(appdata_img v)
            {
                v2f_img o;
                o.pos = NearPlaneQuad(v.vertex);
                o.pos.xy *= VertexAspectConversion();
                o.uv = v.texcoord;
                return o;
            }

            half4 frag(v2f_img i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }

            ENDCG
        }

        // ProcAmp as an image effect
        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ UNITY_COLORSPACE_GAMMA
            #pragma multi_compile _ _KEYING

            #include "ProcAmp.cginc"

            sampler2D _BaseTex;

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            v2f vert(appdata_img v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv0 = TransformUV((v.texcoord - 0.5) * UVAspectConversion() + 0.5);
                o.uv1 = v.texcoord;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 cv = ProcAmp(i.uv0);
                half4 cb = tex2D(_BaseTex, i.uv1);
                return half4(lerp(cb.rgb, cv.rgb, cv.a), cv.a);
            }

            ENDCG
        }
    }
}
