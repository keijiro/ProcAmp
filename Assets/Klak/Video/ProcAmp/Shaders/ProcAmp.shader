Shader "Hidden/Klak/Video/ProcAmp"
{
    Properties
    {
        _MainTex("", 2D) = "white"{}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragAdjust
            #pragma multi_compile __ UNITY_COLORSPACE_GAMMA
            #include "Adjust.cginc"
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragAdjust
            #pragma multi_compile __ UNITY_COLORSPACE_GAMMA
            #define _APPLY_COLOR_BALANCE
            #include "Adjust.cginc"
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragKeying
            #pragma multi_compile __ UNITY_COLORSPACE_GAMMA
            #include "Keying.cginc"
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragDilate
            #include "Keying.cginc"
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragBlur
            #include "Keying.cginc"
            ENDCG
        }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex VertDebug
            #pragma fragment FragDebug
            #include "Debug.cginc"
            ENDCG
        }
    }
}
