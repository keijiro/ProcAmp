Shader "Klak/Video/ProcAmp"
{
    Properties
    {
        _MainTex("", 2D) = "gray" {}

        // Basic adjustment
        _Brightness("", Range(-1, 1)) = 0
        _Contrast("", Range(-1, 2)) = 1
        _Saturation("", Range(0, 2)) = 1

        // Color balance
        [HideInInspector] _Temperature("", Range(-1, 1)) = 0
        [HideInInspector] _Tint("", Range(-1, 1)) = 0
        _ColorBalance("", Vector) = (1, 1, 1, 1)

        // Keying
        [Toggle(_KEYING)] _Keying("", Float) = 0
        [HideInInspector] _KeyColor("", Color) = (0, 1, 0, 0)
        _KeyCgCo("", Vector) = (0, 0, 0, 0)
        _KeyThreshold("", Range(0, 1)) = 0.5
        _KeyTolerance("", Range(0, 1)) = 0.2
        _SpillRemoval("", Range(0, 1)) = 0.5

        // Transform
        _Trim("", Vector) = (0, 0, 0, 0)
        [HideInInspector] _TrimParams("", Vector) = (0, 0, 0, 0)
        _Scale("", Vector) = (1, 1, 1, 1)
        _Offset("", Vector) = (0, 0, 0, 0)

        // Final tweaks
        [Gamma] _FadeToColor("", Color) = (0, 0, 0, 0)
        _Opacity("", Range(0, 1)) = 1

        // Blend mode control
        [HideInInspector] _SrcBlend("", Int) = 1
        [HideInInspector] _DstBlend("", Int) = 0
        [HideInInspector] _ZWrite("", Int) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_Zwrite]

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ UNITY_COLORSPACE_GAMMA
            #pragma shader_feature _KEYING

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
    }
	CustomEditor "Klak.Video.ProcAmpMaterialEditor"
}
