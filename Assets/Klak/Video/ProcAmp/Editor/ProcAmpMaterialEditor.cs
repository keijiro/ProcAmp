using UnityEngine;
using UnityEditor;

namespace Klak.Video
{
    class ProcAmpMaterialEditor : ShaderGUI
    {
        MaterialProperty _mainTex;
        MaterialProperty _brightness;
        MaterialProperty _contrast;
        MaterialProperty _saturation;
        MaterialProperty _temperature;
        MaterialProperty _tint;
        MaterialProperty _keying;
        MaterialProperty _keyColor;
        MaterialProperty _threshold;
        MaterialProperty _tolerance;
        MaterialProperty _spillRemoval;
        MaterialProperty _fadeToColor;
        MaterialProperty _opacity;

        public override void OnGUI(MaterialEditor editor, MaterialProperty[] props)
        {
            // These references should be updated every time :(
            _mainTex = FindProperty("_MainTex", props);
            _brightness = FindProperty("_Brightness", props);
            _contrast = FindProperty("_Contrast", props);
            _saturation = FindProperty("_Saturation", props);
            _temperature = FindProperty("_Temperature", props);
            _tint = FindProperty("_Tint", props);
            _keying = FindProperty("_Keying", props);
            _keyColor = FindProperty("_KeyColor", props);
            _threshold = FindProperty("_Threshold", props);
            _tolerance = FindProperty("_Tolerance", props);
            _spillRemoval = FindProperty("_SpillRemoval", props);
            _fadeToColor = FindProperty("_FadeToColor", props);
            _opacity = FindProperty("_Opacity", props);

            ShaderPropertiesGUI(editor.target as Material, editor);
        }

        public void ShaderPropertiesGUI(Material material, MaterialEditor editor)
        {
            editor.TexturePropertySingleLine(new GUIContent("Source"), _mainTex);

            EditorGUILayout.Space();

            editor.ShaderProperty(_brightness, "Brightness");
            editor.ShaderProperty(_contrast, "Contrast");
            editor.ShaderProperty(_saturation, "Saturation");

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            editor.ShaderProperty(_temperature, "Temperature");
            editor.ShaderProperty(_tint, "Tint (cyan-purple)");
            if (EditorGUI.EndChangeCheck())
                material.SetVector("_ColorBalance", CalculateColorBalance(
                    _temperature.floatValue, _tint.floatValue
                ));

            EditorGUILayout.Space();

            editor.ShaderProperty(_keying, "Keying");

            EditorGUI.BeginChangeCheck();
            editor.ShaderProperty(_keyColor, "Key Color");
            if (EditorGUI.EndChangeCheck())
            {
                var ycgco = RGB2YCgCo(_keyColor.colorValue);
                material.SetVector("_KeyCgCo", new Vector2(ycgco.y, ycgco.z));
            }

            if (_keying.floatValue > 0)
            {
                editor.ShaderProperty(_threshold, "Threshold");
                editor.ShaderProperty(_tolerance, "Tolerance");
                editor.ShaderProperty(_spillRemoval, "Spill Removal");
            }

            EditorGUILayout.Space();

            editor.ShaderProperty(_fadeToColor, "Fade To Color");

            EditorGUILayout.Space();

            editor.ShaderProperty(_opacity, "Opacity");

            if (_opacity.floatValue == 1 && _keying.floatValue == 0)
            {
                // Opaque
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.SetOverrideTag("RenderType", "");
                material.renderQueue = -1;
            }
            else
            {
                // Transparent
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
        }

        // An analytical model of chromaticity of the standard illuminant, by Judd et al.
        // http://en.wikipedia.org/wiki/Standard_illuminant#Illuminant_series_D
        // Slightly modifed to adjust it with the D65 white point (x=0.31271, y=0.32902).
        static float StandardIlluminantY(float x)
        {
            return 2.87f * x - 3.0f * x * x - 0.27509507f;
        }

        // CIE xy chromaticity to CAT02 LMS.
        // http://en.wikipedia.org/wiki/LMS_color_space#CAT02
        static Vector3 CIExyToLMS(float x, float y)
        {
            var Y = 1.0f;
            var X = Y * x / y;
            var Z = Y * (1.0f - x - y) / y;

            var L =  0.7328f * X + 0.4296f * Y - 0.1624f * Z;
            var M = -0.7036f * X + 1.6975f * Y + 0.0061f * Z;
            var S =  0.0030f * X + 0.0136f * Y + 0.9834f * Z;

            return new Vector3(L, M, S);
        }

        // Calculate the color balance coefficients.
        static Vector3 CalculateColorBalance(float temp, float tint)
        {
            // Get the CIE xy chromaticity of the reference white point.
            // Note: 0.31271 = x value on the D65 white point
            var x = 0.31271f - temp * (temp < 0.0f ? 0.1f : 0.05f);
            var y = StandardIlluminantY(x) + tint * 0.05f;

            // Calculate the coefficients in the LMS space.
            var w1 = new Vector3(0.949237f, 1.03542f, 1.08728f); // D65 white point
            var w2 = CIExyToLMS(x, y);
            return new Vector3(w1.x / w2.x, w1.y / w2.y, w1.z / w2.z);
        }

        // YCgCo color space conversion
        static Vector3 RGB2YCgCo(Color rgb)
        {
            var y  =  0.25f * rgb.r + 0.5f * rgb.g + 0.25f * rgb.b;
            var cg = -0.25f * rgb.r + 0.5f * rgb.g - 0.25f * rgb.b;
            var co =  0.50f * rgb.r                - 0.50f * rgb.b;
            return new Vector3(y, cg, co);
        }
    }
}
